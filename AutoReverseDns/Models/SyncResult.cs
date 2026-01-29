using System.Text.Json.Serialization;

namespace AutoReverseDns.Models;

/// <summary>
/// Result of a sync operation
/// </summary>
public class SyncResult
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("zonesProcessed")]
    public int ZonesProcessed { get; set; }

    [JsonPropertyName("recordsScanned")]
    public int RecordsScanned { get; set; }

    [JsonPropertyName("ptrsCreated")]
    public int PtrsCreated { get; set; }

    [JsonPropertyName("ptrsUpdated")]
    public int PtrsUpdated { get; set; }

    [JsonPropertyName("ptrsSkipped")]
    public int PtrsSkipped { get; set; }

    [JsonPropertyName("ptrsDeleted")]
    public int PtrsDeleted { get; set; }

    [JsonPropertyName("reverseZonesCreated")]
    public int ReverseZonesCreated { get; set; }

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();

    [JsonPropertyName("durationMs")]
    public long DurationMs { get; set; }
}

/// <summary>
/// Statistics about the app's operation
/// </summary>
public class AppStats
{
    [JsonPropertyName("lastSyncResult")]
    public SyncResult? LastSyncResult { get; set; }

    [JsonPropertyName("totalSyncs")]
    public long TotalSyncs { get; set; }

    [JsonPropertyName("totalPtrsCreated")]
    public long TotalPtrsCreated { get; set; }

    [JsonPropertyName("totalPtrsUpdated")]
    public long TotalPtrsUpdated { get; set; }

    [JsonPropertyName("totalErrors")]
    public long TotalErrors { get; set; }

    [JsonPropertyName("isRunning")]
    public bool IsRunning { get; set; }

    [JsonPropertyName("nextSyncTime")]
    public DateTime? NextSyncTime { get; set; }
}
