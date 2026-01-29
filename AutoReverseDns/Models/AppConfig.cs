using System.Text.Json.Serialization;

namespace AutoReverseDns.Models;

/// <summary>
/// Main configuration for the Auto Reverse DNS app
/// </summary>
public class AppConfig
{
    /// <summary>
    /// Global enable/disable switch for the app
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Interval in seconds between sync runs
    /// </summary>
    [JsonPropertyName("syncIntervalSeconds")]
    public int SyncIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Whether to create reverse zones automatically if they don't exist
    /// </summary>
    [JsonPropertyName("createReverseZones")]
    public bool CreateReverseZones { get; set; } = true;

    /// <summary>
    /// Whether to overwrite existing PTR records with different values
    /// </summary>
    [JsonPropertyName("overwriteExisting")]
    public bool OverwriteExisting { get; set; } = false;

    /// <summary>
    /// Whether to delete PTR records when corresponding A/AAAA records are removed
    /// </summary>
    [JsonPropertyName("deleteOrphanedPtrs")]
    public bool DeleteOrphanedPtrs { get; set; } = false;

    /// <summary>
    /// TTL for created PTR records (0 = use zone default)
    /// </summary>
    [JsonPropertyName("ptrTtl")]
    public uint PtrTtl { get; set; } = 0;

    /// <summary>
    /// In cluster mode, only run sync on the primary node
    /// </summary>
    [JsonPropertyName("clusterPrimaryOnly")]
    public bool ClusterPrimaryOnly { get; set; } = true;

    /// <summary>
    /// Per-zone configuration. Key is the forward zone name.
    /// If a zone is not listed here, it uses default settings based on defaultZoneEnabled.
    /// </summary>
    [JsonPropertyName("zones")]
    public Dictionary<string, ZoneConfig> Zones { get; set; } = new();

    /// <summary>
    /// Whether zones not explicitly configured are enabled by default
    /// </summary>
    [JsonPropertyName("defaultZoneEnabled")]
    public bool DefaultZoneEnabled { get; set; } = false;

    /// <summary>
    /// List of zone name patterns to exclude (supports wildcards: *.internal)
    /// </summary>
    [JsonPropertyName("excludePatterns")]
    public List<string> ExcludePatterns { get; set; } = new()
    {
        "localhost",
        "*.in-addr.arpa",
        "*.ip6.arpa"
    };
}

/// <summary>
/// Per-zone configuration
/// </summary>
public class ZoneConfig
{
    /// <summary>
    /// Whether auto-reverse is enabled for this zone
    /// </summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Override the reverse zone to use (optional).
    /// If not specified, reverse zone is auto-detected from IP addresses.
    /// </summary>
    [JsonPropertyName("reverseZone")]
    public string? ReverseZone { get; set; }

    /// <summary>
    /// Whether to create the reverse zone if it doesn't exist (overrides global setting)
    /// </summary>
    [JsonPropertyName("createReverseZone")]
    public bool? CreateReverseZone { get; set; }

    /// <summary>
    /// Whether to overwrite existing PTR records (overrides global setting)
    /// </summary>
    [JsonPropertyName("overwriteExisting")]
    public bool? OverwriteExisting { get; set; }
}
