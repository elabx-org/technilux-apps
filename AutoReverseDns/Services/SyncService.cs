using System.Diagnostics;
using System.Net;
using AutoReverseDns.Models;
using DnsServerCore.ApplicationCommon;

namespace AutoReverseDns.Services;

/// <summary>
/// Background service that syncs A/AAAA records to PTR records
/// Uses IDnsServer interface directly to avoid HTTP API authentication issues.
/// </summary>
public class SyncService : IDisposable
{
    private readonly IDnsServer _dnsServer;
    private readonly Func<AppConfig> _getConfig;
    private readonly DnsServerAccess _serverAccess;
    private readonly Timer _syncTimer;
    private readonly object _syncLock = new();
    private bool _isSyncing;
    private bool _disposed;

    public AppStats Stats { get; } = new();

    public SyncService(IDnsServer dnsServer, Func<AppConfig> getConfig)
    {
        _dnsServer = dnsServer;
        _getConfig = getConfig;
        _serverAccess = new DnsServerAccess(dnsServer);

        // Start timer - first sync after 10 seconds, then based on config interval
        _syncTimer = new Timer(SyncTimerCallback, null, TimeSpan.FromSeconds(10), Timeout.InfiniteTimeSpan);
    }

    private void SyncTimerCallback(object? state)
    {
        if (_disposed) return;

        try
        {
            var config = _getConfig();

            if (config.Enabled)
            {
                RunSync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AutoReverseDns] Timer error: {ex.Message}");
        }
        finally
        {
            // Schedule next run
            if (!_disposed)
            {
                var config = _getConfig();
                var interval = TimeSpan.FromSeconds(Math.Max(10, config.SyncIntervalSeconds));
                Stats.NextSyncTime = DateTime.UtcNow.Add(interval);
                _syncTimer.Change(interval, Timeout.InfiniteTimeSpan);
            }
        }
    }

    public SyncResult RunSync()
    {
        var result = new SyncResult();
        var stopwatch = Stopwatch.StartNew();

        // Prevent concurrent syncs
        lock (_syncLock)
        {
            if (_isSyncing)
            {
                result.Errors.Add("Sync already in progress");
                return result;
            }
            _isSyncing = true;
            Stats.IsRunning = true;
        }

        try
        {
            var config = _getConfig();

            if (!config.Enabled)
            {
                result.Errors.Add("App is disabled");
                return result;
            }

            Console.WriteLine("[AutoReverseDns] Starting sync...");

            // Get all zones using direct IDnsServer access
            var zones = _serverAccess.ListZones();

            // Cache existing reverse zones for lookup
            var reverseZones = zones
                .Where(z => z.Name.EndsWith(".in-addr.arpa", StringComparison.OrdinalIgnoreCase) ||
                           z.Name.EndsWith(".ip6.arpa", StringComparison.OrdinalIgnoreCase))
                .Select(z => z.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var zone in zones)
            {
                try
                {
                    // Skip reverse zones and excluded patterns
                    if (IsExcluded(zone.Name, config))
                        continue;

                    // Skip disabled zones
                    if (zone.Disabled)
                        continue;

                    // Check zone-specific config
                    var zoneEnabled = IsZoneEnabled(zone.Name, config);
                    if (!zoneEnabled)
                        continue;

                    result.ZonesProcessed++;
                    ProcessZone(zone.Name, config, reverseZones, result);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Error processing zone {zone.Name}: {ex.Message}");
                    Console.WriteLine($"[AutoReverseDns] Error processing zone {zone.Name}: {ex}");
                }
            }

            stopwatch.Stop();
            result.DurationMs = stopwatch.ElapsedMilliseconds;

            // Update stats
            Stats.LastSyncResult = result;
            Stats.TotalSyncs++;
            Stats.TotalPtrsCreated += result.PtrsCreated;
            Stats.TotalPtrsUpdated += result.PtrsUpdated;
            Stats.TotalErrors += result.Errors.Count;

            Console.WriteLine($"[AutoReverseDns] Sync complete: {result.ZonesProcessed} zones, {result.RecordsScanned} records, {result.PtrsCreated} PTRs created, {result.PtrsUpdated} updated, {result.DurationMs}ms");

            return result;
        }
        finally
        {
            lock (_syncLock)
            {
                _isSyncing = false;
                Stats.IsRunning = false;
            }
        }
    }

    /// <summary>
    /// Async wrapper for compatibility with HTTP endpoint
    /// </summary>
    public Task<SyncResult> RunSyncAsync()
    {
        return Task.FromResult(RunSync());
    }

    private void ProcessZone(string zoneName, AppConfig config, HashSet<string> existingReverseZones, SyncResult result)
    {
        var zoneConfig = config.Zones.GetValueOrDefault(zoneName);

        // Get all records in the zone
        var records = _serverAccess.GetZoneRecords(zoneName);

        foreach (var record in records)
        {
            if (record.Type == "A" || record.Type == "AAAA")
            {
                result.RecordsScanned++;

                try
                {
                    ProcessAddressRecord(record, zoneName, config, zoneConfig, existingReverseZones, result);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Error processing {record.Name}: {ex.Message}");
                }
            }
        }
    }

    private void ProcessAddressRecord(
        DnsRecord record,
        string zoneName,
        AppConfig config,
        ZoneConfig? zoneConfig,
        HashSet<string> existingReverseZones,
        SyncResult result)
    {
        if (string.IsNullOrEmpty(record.RData))
            return;

        // Parse IP address
        if (!IPAddress.TryParse(record.RData, out var ipAddress))
            return;

        // Calculate reverse zone and PTR name
        var (reverseZone, ptrName) = GetReverseDnsInfo(ipAddress, zoneConfig?.ReverseZone);

        if (string.IsNullOrEmpty(reverseZone) || string.IsNullOrEmpty(ptrName))
            return;

        // Check if reverse zone exists, create if needed
        var reverseZoneExists = existingReverseZones.Contains(reverseZone);

        if (!reverseZoneExists)
        {
            var shouldCreate = zoneConfig?.CreateReverseZone ?? config.CreateReverseZones;

            if (shouldCreate)
            {
                try
                {
                    var created = _serverAccess.CreateZone(reverseZone);
                    if (created)
                    {
                        existingReverseZones.Add(reverseZone);
                        result.ReverseZonesCreated++;
                        Console.WriteLine($"[AutoReverseDns] Created reverse zone: {reverseZone}");
                    }
                    else
                    {
                        result.Errors.Add($"Failed to create reverse zone {reverseZone}");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Failed to create reverse zone {reverseZone}: {ex.Message}");
                    return;
                }
            }
            else
            {
                result.PtrsSkipped++;
                return;
            }
        }

        // Check if PTR record already exists
        var hostname = record.Name;
        var existingPtrs = _serverAccess.GetPtrRecords(reverseZone, ptrName);

        var shouldOverwrite = zoneConfig?.OverwriteExisting ?? config.OverwriteExisting;
        var ttl = config.PtrTtl > 0 ? config.PtrTtl : record.TTL;

        if (existingPtrs.Count > 0)
        {
            // Check if any existing PTR already points to this hostname
            var existingMatch = existingPtrs.FirstOrDefault(p =>
                p.Equals(hostname, StringComparison.OrdinalIgnoreCase));

            if (existingMatch != null)
            {
                // Already exists with correct value
                result.PtrsSkipped++;
                return;
            }

            if (!shouldOverwrite)
            {
                // PTR exists but points elsewhere, and we shouldn't overwrite
                result.PtrsSkipped++;
                return;
            }

            // Delete existing PTR records and create new one
            try
            {
                foreach (var oldPtr in existingPtrs)
                {
                    _serverAccess.DeletePtrRecord(reverseZone, ptrName, oldPtr);
                }

                var success = _serverAccess.AddPtrRecord(reverseZone, ptrName, hostname, ttl);
                if (success)
                {
                    result.PtrsUpdated++;
                    Console.WriteLine($"[AutoReverseDns] Updated PTR: {ptrName} -> {hostname}");
                }
                else
                {
                    result.Errors.Add($"Failed to update PTR {ptrName}");
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Failed to update PTR {ptrName}: {ex.Message}");
            }
        }
        else
        {
            // Create new PTR
            try
            {
                var success = _serverAccess.AddPtrRecord(reverseZone, ptrName, hostname, ttl);
                if (success)
                {
                    result.PtrsCreated++;
                    Console.WriteLine($"[AutoReverseDns] Created PTR: {ptrName} -> {hostname}");
                }
                else
                {
                    result.Errors.Add($"Failed to create PTR {ptrName}");
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Failed to create PTR {ptrName}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Convert an IP address to reverse DNS zone and PTR record name
    /// </summary>
    private (string reverseZone, string ptrName) GetReverseDnsInfo(IPAddress ip, string? overrideZone = null)
    {
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            // IPv4
            var octets = ip.GetAddressBytes();
            var reverseZone = overrideZone ?? $"{octets[2]}.{octets[1]}.{octets[0]}.in-addr.arpa";
            var ptrName = $"{octets[3]}.{octets[2]}.{octets[1]}.{octets[0]}.in-addr.arpa";
            return (reverseZone, ptrName);
        }
        else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            // IPv6
            var bytes = ip.GetAddressBytes();
            var nibbles = new List<char>();

            foreach (var b in bytes)
            {
                nibbles.Add(GetHexChar(b & 0x0F));
                nibbles.Add(GetHexChar((b >> 4) & 0x0F));
            }

            nibbles.Reverse();
            var fullName = string.Join(".", nibbles) + ".ip6.arpa";

            // For reverse zone, use /64 boundary (first 16 nibbles from the end)
            var zoneNibbles = nibbles.Skip(16).ToList();
            var reverseZone = overrideZone ?? string.Join(".", zoneNibbles) + ".ip6.arpa";

            return (reverseZone, fullName);
        }

        return (string.Empty, string.Empty);
    }

    private static char GetHexChar(int value) => "0123456789abcdef"[value];

    private bool IsExcluded(string zoneName, AppConfig config)
    {
        foreach (var pattern in config.ExcludePatterns)
        {
            if (MatchesPattern(zoneName, pattern))
                return true;
        }
        return false;
    }

    private bool MatchesPattern(string zoneName, string pattern)
    {
        // Simple wildcard matching
        if (pattern.StartsWith("*."))
        {
            var suffix = pattern.Substring(1); // ".in-addr.arpa"
            return zoneName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        }

        return zoneName.Equals(pattern, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsZoneEnabled(string zoneName, AppConfig config)
    {
        if (config.Zones.TryGetValue(zoneName, out var zoneConfig))
        {
            return zoneConfig.Enabled;
        }

        return config.DefaultZoneEnabled;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _syncTimer.Dispose();
    }
}
