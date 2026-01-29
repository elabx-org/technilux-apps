using System.Net;
using System.Text.Json;
using DnsServerCore.ApplicationCommon;
using TechnitiumLibrary.Net.Dns;
using TechnitiumLibrary.Net.Dns.ResourceRecords;

namespace AutoReverseDns.Services;

/// <summary>
/// Direct access to Technitium DNS server via IDnsServer interface.
/// This avoids HTTP API calls which require authentication tokens.
/// </summary>
public class DnsServerAccess
{
    private readonly IDnsServer _dnsServer;

    public DnsServerAccess(IDnsServer dnsServer)
    {
        _dnsServer = dnsServer;
    }

    /// <summary>
    /// Get list of all zones from the authoritative zone manager
    /// </summary>
    public List<ZoneInfo> ListZones()
    {
        var zones = new List<ZoneInfo>();

        try
        {
            var zoneManager = _dnsServer.AuthZoneManager;
            if (zoneManager == null)
                return zones;

            // Get all zones - the AuthZoneManager has a method to list zones
            foreach (var zone in zoneManager.GetZones())
            {
                zones.Add(new ZoneInfo
                {
                    Name = zone.Name,
                    Type = zone.Type.ToString(),
                    Disabled = zone.Disabled
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AutoReverseDns] Error listing zones: {ex.Message}");
        }

        return zones;
    }

    /// <summary>
    /// Get all records in a zone
    /// </summary>
    public List<DnsRecord> GetZoneRecords(string zoneName)
    {
        var records = new List<DnsRecord>();

        try
        {
            var zoneManager = _dnsServer.AuthZoneManager;
            if (zoneManager == null)
                return records;

            var zone = zoneManager.GetZone(zoneName);
            if (zone == null)
                return records;

            // Get all records from the zone
            var allRecords = zone.GetRecords(zoneName);
            foreach (var rrset in allRecords)
            {
                foreach (var rr in rrset)
                {
                    string? rdata = null;

                    if (rr is DnsARecordData aRecord)
                        rdata = aRecord.Address.ToString();
                    else if (rr is DnsAAAARecordData aaaaRecord)
                        rdata = aaaaRecord.Address.ToString();
                    else if (rr is DnsPTRRecordData ptrRecord)
                        rdata = ptrRecord.Domain;

                    records.Add(new DnsRecord
                    {
                        Name = rrset.Name,
                        Type = rrset.Type.ToString(),
                        TTL = rrset.TtlValue,
                        RData = rdata
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AutoReverseDns] Error getting zone records for {zoneName}: {ex.Message}");
        }

        return records;
    }

    /// <summary>
    /// Check if a zone exists
    /// </summary>
    public bool ZoneExists(string zoneName)
    {
        try
        {
            var zoneManager = _dnsServer.AuthZoneManager;
            return zoneManager?.GetZone(zoneName) != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Create a primary zone
    /// </summary>
    public bool CreateZone(string zoneName)
    {
        try
        {
            var zoneManager = _dnsServer.AuthZoneManager;
            if (zoneManager == null)
                return false;

            zoneManager.CreatePrimaryZone(zoneName, _dnsServer.ServerDomain, false);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AutoReverseDns] Error creating zone {zoneName}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Add a PTR record
    /// </summary>
    public bool AddPtrRecord(string zone, string ptrName, string hostname, uint ttl)
    {
        try
        {
            var zoneManager = _dnsServer.AuthZoneManager;
            if (zoneManager == null)
                return false;

            zoneManager.AddRecord(zone, ptrName, DnsResourceRecordType.PTR, ttl, new DnsPTRRecordData(hostname));
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AutoReverseDns] Error adding PTR record {ptrName}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Delete a PTR record
    /// </summary>
    public bool DeletePtrRecord(string zone, string ptrName, string hostname)
    {
        try
        {
            var zoneManager = _dnsServer.AuthZoneManager;
            if (zoneManager == null)
                return false;

            zoneManager.DeleteRecord(zone, ptrName, DnsResourceRecordType.PTR, new DnsPTRRecordData(hostname));
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AutoReverseDns] Error deleting PTR record {ptrName}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get PTR records for a specific name
    /// </summary>
    public List<string> GetPtrRecords(string zone, string ptrName)
    {
        var ptrs = new List<string>();

        try
        {
            var zoneManager = _dnsServer.AuthZoneManager;
            if (zoneManager == null)
                return ptrs;

            var zoneObj = zoneManager.GetZone(zone);
            if (zoneObj == null)
                return ptrs;

            var records = zoneObj.GetRecords(ptrName);
            foreach (var rrset in records)
            {
                if (rrset.Type == DnsResourceRecordType.PTR)
                {
                    foreach (var rr in rrset)
                    {
                        if (rr is DnsPTRRecordData ptrData)
                        {
                            ptrs.Add(ptrData.Domain);
                        }
                    }
                }
            }
        }
        catch
        {
            // Zone or record may not exist
        }

        return ptrs;
    }
}

public class ZoneInfo
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public bool Disabled { get; set; }
}

public class DnsRecord
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public uint TTL { get; set; }
    public string? RData { get; set; }
}
