using System.Text.Json;
using AutoReverseDns.Models;
using AutoReverseDns.Services;
using DnsServerCore.ApplicationCommon;
using TechnitiumLibrary.Net.Dns;

namespace AutoReverseDns;

public class App : IDnsApplication, IDisposable
{
    private IDnsServer? _dnsServer;
    private AppConfig _config = new();
    private SyncService? _syncService;
    private string? _configPath;

    public string Description => "Automatically creates and maintains PTR records for A/AAAA records in configured zones";

    public async Task InitializeAsync(IDnsServer dnsServer, string config)
    {
        _dnsServer = dnsServer;
        _configPath = Path.Combine(dnsServer.ApplicationFolder, "config.json");

        // Load configuration
        _config = await LoadConfigAsync(config);

        // Initialize sync service using direct IDnsServer access (no HTTP API)
        _syncService = new SyncService(dnsServer, () => _config);

        Console.WriteLine($"[AutoReverseDns] Initialized. Enabled: {_config.Enabled}, Interval: {_config.SyncIntervalSeconds}s");
    }

    public Task<object?> ProcessRequestAsync(DnsDatagram request, System.Net.IPEndPoint remoteEP, DnsTransportProtocol protocol, bool isRecursionAllowed)
    {
        // This app doesn't intercept DNS queries
        return Task.FromResult<object?>(null);
    }

    public Task<object?> ProcessRequestAsync(DnsDatagram request, System.Net.IPEndPoint remoteEP, DnsTransportProtocol protocol, bool isRecursionAllowed, string zoneName, string appRecordName, uint appRecordTtl, string appRecordData)
    {
        // This app doesn't use APP records
        return Task.FromResult<object?>(null);
    }

    public Task<DnsDatagram> ProcessResponseAsync(DnsDatagram request, System.Net.IPEndPoint remoteEP, DnsTransportProtocol protocol, DnsDatagram response)
    {
        return Task.FromResult(response);
    }

    public Task<string> GetConfigAsync()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return Task.FromResult(JsonSerializer.Serialize(_config, options));
    }

    public async Task SetConfigAsync(string config)
    {
        if (string.IsNullOrWhiteSpace(config))
            return;

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };

            var newConfig = JsonSerializer.Deserialize<AppConfig>(config, options);
            if (newConfig != null)
            {
                _config = newConfig;

                // Persist config
                if (_configPath != null)
                {
                    await File.WriteAllTextAsync(_configPath, config);
                }

                Console.WriteLine($"[AutoReverseDns] Configuration updated. Enabled: {_config.Enabled}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AutoReverseDns] Error parsing config: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// HTTP API handler for custom endpoints
    /// </summary>
    public async Task<string> ProcessHttpRequestAsync(string path, string queryString, string method, string requestBody, string token)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        try
        {
            var normalizedPath = path.ToLowerInvariant();

            // Handle paths with or without /api/apps/autoreversedns prefix
            if (normalizedPath.StartsWith("/api/apps/autoreversedns"))
            {
                normalizedPath = normalizedPath.Replace("/api/apps/autoreversedns", "");
            }

            return (normalizedPath, method.ToUpperInvariant()) switch
            {
                // Trigger manual sync
                ("/sync", "POST") or ("sync", "POST") => await HandleManualSync(options),

                // Get sync stats
                ("/stats", "GET") or ("stats", "GET") => HandleGetStats(options),

                // Get zone status
                ("/zones", "GET") or ("zones", "GET") => await HandleGetZones(options),

                // Enable/disable zone
                ("/zones/toggle", "POST") or ("zones/toggle", "POST") => await HandleToggleZone(requestBody, options),

                _ => JsonSerializer.Serialize(new { error = "Endpoint not found", path = normalizedPath }, options)
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AutoReverseDns] API error: {ex}");
            return JsonSerializer.Serialize(new { error = ex.Message }, options);
        }
    }

    // Overload without token for compatibility
    public Task<string> ProcessHttpRequestAsync(string path, string queryString, string method, string requestBody)
    {
        return ProcessHttpRequestAsync(path, queryString, method, requestBody, "");
    }

    private async Task<string> HandleManualSync(JsonSerializerOptions options)
    {
        if (_syncService == null)
        {
            return JsonSerializer.Serialize(new { error = "Service not initialized" }, options);
        }

        var result = await _syncService.RunSyncAsync();
        return JsonSerializer.Serialize(new { success = true, result }, options);
    }

    private string HandleGetStats(JsonSerializerOptions options)
    {
        if (_syncService == null)
        {
            return JsonSerializer.Serialize(new { error = "Service not initialized" }, options);
        }

        return JsonSerializer.Serialize(new { success = true, stats = _syncService.Stats }, options);
    }

    private Task<string> HandleGetZones(JsonSerializerOptions options)
    {
        if (_dnsServer == null)
        {
            return Task.FromResult(JsonSerializer.Serialize(new { error = "Server not initialized" }, options));
        }

        // Use direct IDnsServer access (no HTTP API needed)
        var serverAccess = new DnsServerAccess(_dnsServer);
        var zones = serverAccess.ListZones();
        var zoneList = new List<object>();

        foreach (var zone in zones)
        {
            // Skip reverse zones
            if (zone.Name.EndsWith(".in-addr.arpa", StringComparison.OrdinalIgnoreCase) ||
                zone.Name.EndsWith(".ip6.arpa", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var isEnabled = IsZoneEnabled(zone.Name);
            var hasConfig = _config.Zones.ContainsKey(zone.Name);

            zoneList.Add(new
            {
                name = zone.Name,
                enabled = isEnabled,
                hasExplicitConfig = hasConfig,
                config = hasConfig ? _config.Zones[zone.Name] : null
            });
        }

        return Task.FromResult(JsonSerializer.Serialize(new { success = true, zones = zoneList }, options));
    }

    private async Task<string> HandleToggleZone(string requestBody, JsonSerializerOptions options)
    {
        var request = JsonSerializer.Deserialize<JsonElement>(requestBody);

        if (!request.TryGetProperty("zone", out var zoneProp) ||
            !request.TryGetProperty("enabled", out var enabledProp))
        {
            return JsonSerializer.Serialize(new { error = "Missing zone or enabled parameter" }, options);
        }

        var zoneName = zoneProp.GetString();
        var enabled = enabledProp.GetBoolean();

        if (string.IsNullOrEmpty(zoneName))
        {
            return JsonSerializer.Serialize(new { error = "Invalid zone name" }, options);
        }

        // Update or create zone config
        if (!_config.Zones.ContainsKey(zoneName))
        {
            _config.Zones[zoneName] = new ZoneConfig();
        }

        _config.Zones[zoneName].Enabled = enabled;

        // Persist config
        await SetConfigAsync(JsonSerializer.Serialize(_config, options));

        return JsonSerializer.Serialize(new { success = true, zone = zoneName, enabled }, options);
    }

    private bool IsZoneEnabled(string zoneName)
    {
        if (_config.Zones.TryGetValue(zoneName, out var zoneConfig))
        {
            return zoneConfig.Enabled;
        }

        return _config.DefaultZoneEnabled;
    }

    private async Task<AppConfig> LoadConfigAsync(string configJson)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        // First try to load from passed config
        if (!string.IsNullOrWhiteSpace(configJson) && !configJson.TrimStart().StartsWith("#"))
        {
            try
            {
                var config = JsonSerializer.Deserialize<AppConfig>(configJson, options);
                if (config != null)
                    return config;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AutoReverseDns] Error parsing initial config: {ex.Message}");
            }
        }

        // Try to load from persisted file
        if (_configPath != null && File.Exists(_configPath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(_configPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json, options);
                if (config != null)
                    return config;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AutoReverseDns] Error loading persisted config: {ex.Message}");
            }
        }

        // Return default config
        return new AppConfig();
    }

    public void Dispose()
    {
        _syncService?.Dispose();
    }
}
