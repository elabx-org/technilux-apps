using System.Net;
using System.Text.Json;
using System.Web;

namespace AutoReverseDns.Services;

/// <summary>
/// Internal HTTP client for Technitium DNS API calls
/// </summary>
public class DnsApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private string? _token;

    public DnsApiClient(string baseUrl = "http://localhost:5380")
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };
    }

    public void SetToken(string token)
    {
        _token = token;
    }

    /// <summary>
    /// Get list of all zones
    /// </summary>
    public async Task<List<ZoneInfo>> ListZonesAsync()
    {
        var response = await GetAsync("/api/zones/list");
        var zones = new List<ZoneInfo>();

        if (response.TryGetProperty("response", out var resp) &&
            resp.TryGetProperty("zones", out var zonesArray))
        {
            foreach (var zone in zonesArray.EnumerateArray())
            {
                zones.Add(new ZoneInfo
                {
                    Name = zone.GetProperty("name").GetString() ?? "",
                    Type = zone.GetProperty("type").GetString() ?? "",
                    Disabled = zone.TryGetProperty("disabled", out var d) && d.GetBoolean()
                });
            }
        }

        return zones;
    }

    /// <summary>
    /// Get all records in a zone
    /// </summary>
    public async Task<List<DnsRecord>> GetZoneRecordsAsync(string zoneName)
    {
        var response = await GetAsync($"/api/zones/records/get?zone={Encode(zoneName)}&domain={Encode(zoneName)}&listZone=true");
        var records = new List<DnsRecord>();

        if (response.TryGetProperty("response", out var resp) &&
            resp.TryGetProperty("records", out var recordsArray))
        {
            foreach (var record in recordsArray.EnumerateArray())
            {
                var type = record.GetProperty("type").GetString() ?? "";
                var name = record.GetProperty("name").GetString() ?? "";
                var ttl = record.TryGetProperty("ttl", out var t) ? t.GetUInt32() : 3600u;

                string? rdata = null;
                if (record.TryGetProperty("rData", out var rdataObj))
                {
                    if (type == "A" && rdataObj.TryGetProperty("ipAddress", out var ip))
                        rdata = ip.GetString();
                    else if (type == "AAAA" && rdataObj.TryGetProperty("ipAddress", out var ip6))
                        rdata = ip6.GetString();
                    else if (type == "PTR" && rdataObj.TryGetProperty("ptrName", out var ptr))
                        rdata = ptr.GetString();
                }

                records.Add(new DnsRecord
                {
                    Name = name,
                    Type = type,
                    TTL = ttl,
                    RData = rdata
                });
            }
        }

        return records;
    }

    /// <summary>
    /// Check if a zone exists
    /// </summary>
    public async Task<bool> ZoneExistsAsync(string zoneName)
    {
        var zones = await ListZonesAsync();
        return zones.Any(z => z.Name.Equals(zoneName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Create a primary zone
    /// </summary>
    public async Task<bool> CreateZoneAsync(string zoneName)
    {
        try
        {
            var response = await GetAsync($"/api/zones/create?zone={Encode(zoneName)}&type=Primary");
            return response.TryGetProperty("status", out var status) &&
                   status.GetString() == "ok";
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Add a PTR record
    /// </summary>
    public async Task<bool> AddPtrRecordAsync(string zone, string ptrName, string hostname, uint ttl)
    {
        try
        {
            var url = $"/api/zones/records/add?zone={Encode(zone)}&domain={Encode(ptrName)}&type=PTR&ttl={ttl}&ptrName={Encode(hostname)}";
            var response = await GetAsync(url);
            return response.TryGetProperty("status", out var status) &&
                   status.GetString() == "ok";
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Delete a PTR record
    /// </summary>
    public async Task<bool> DeletePtrRecordAsync(string zone, string ptrName, string hostname)
    {
        try
        {
            var url = $"/api/zones/records/delete?zone={Encode(zone)}&domain={Encode(ptrName)}&type=PTR&ptrName={Encode(hostname)}";
            var response = await GetAsync(url);
            return response.TryGetProperty("status", out var status) &&
                   status.GetString() == "ok";
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get PTR records for a specific name
    /// </summary>
    public async Task<List<string>> GetPtrRecordsAsync(string zone, string ptrName)
    {
        var ptrs = new List<string>();

        try
        {
            var response = await GetAsync($"/api/zones/records/get?zone={Encode(zone)}&domain={Encode(ptrName)}");

            if (response.TryGetProperty("response", out var resp) &&
                resp.TryGetProperty("records", out var recordsArray))
            {
                foreach (var record in recordsArray.EnumerateArray())
                {
                    if (record.TryGetProperty("type", out var type) && type.GetString() == "PTR" &&
                        record.TryGetProperty("rData", out var rdata) &&
                        rdata.TryGetProperty("ptrName", out var ptrValue))
                    {
                        var val = ptrValue.GetString();
                        if (!string.IsNullOrEmpty(val))
                            ptrs.Add(val);
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

    private async Task<JsonElement> GetAsync(string endpoint)
    {
        var url = _baseUrl + endpoint;
        if (!string.IsNullOrEmpty(_token))
        {
            url += (url.Contains('?') ? "&" : "?") + $"token={_token}";
        }

        var response = await _httpClient.GetStringAsync(url);
        return JsonSerializer.Deserialize<JsonElement>(response);
    }

    private static string Encode(string value) => HttpUtility.UrlEncode(value);
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
