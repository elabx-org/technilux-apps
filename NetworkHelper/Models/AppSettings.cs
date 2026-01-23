using System.Text.Json.Serialization;

namespace NetworkHelper.Models;

public class AppSettings
{
    [JsonPropertyName("cleanupEnabled")]
    public bool CleanupEnabled { get; set; } = true;

    [JsonPropertyName("cleanupIntervalHours")]
    public int CleanupIntervalHours { get; set; } = 24;

    [JsonPropertyName("staleThresholdDays")]
    public int StaleThresholdDays { get; set; } = 30;

    [JsonPropertyName("autoResolveHostnames")]
    public bool AutoResolveHostnames { get; set; } = false;

    [JsonPropertyName("defaultGroup")]
    public string DefaultGroup { get; set; } = "Ungrouped";

    [JsonPropertyName("enableNotifications")]
    public bool EnableNotifications { get; set; } = false;
}
