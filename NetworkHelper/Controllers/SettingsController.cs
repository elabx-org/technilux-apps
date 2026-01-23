using System.Text.Json;
using NetworkHelper.Models;

namespace NetworkHelper.Controllers;

public static class SettingsController
{
    private static string? _settingsPath;
    private static AppSettings? _cachedSettings;

    public static void Initialize(string settingsPath, AppSettings settings)
    {
        _settingsPath = settingsPath;
        _cachedSettings = settings;
    }

    public static string GetSettings()
    {
        if (_cachedSettings == null)
        {
            return JsonSerializer.Serialize(ApiResponse.Error("Settings not initialized"));
        }

        return JsonSerializer.Serialize(ApiResponse.Success(new { settings = _cachedSettings }));
    }

    public static async Task<string> UpdateSettings(string requestBody)
    {
        try
        {
            if (_settingsPath == null || _cachedSettings == null)
            {
                return JsonSerializer.Serialize(ApiResponse.Error("Settings not initialized"));
            }

            var newSettings = JsonSerializer.Deserialize<AppSettings>(requestBody);
            if (newSettings == null)
            {
                return JsonSerializer.Serialize(ApiResponse.Error("Invalid settings data"));
            }

            // Validate ranges
            if (newSettings.CleanupIntervalHours < 1 || newSettings.CleanupIntervalHours > 168)
            {
                return JsonSerializer.Serialize(ApiResponse.Error("Cleanup interval must be between 1 and 168 hours"));
            }

            if (newSettings.StaleThresholdDays < 1 || newSettings.StaleThresholdDays > 365)
            {
                return JsonSerializer.Serialize(ApiResponse.Error("Stale threshold must be between 1 and 365 days"));
            }

            // Update cached settings
            _cachedSettings.CleanupEnabled = newSettings.CleanupEnabled;
            _cachedSettings.CleanupIntervalHours = newSettings.CleanupIntervalHours;
            _cachedSettings.StaleThresholdDays = newSettings.StaleThresholdDays;
            _cachedSettings.AutoResolveHostnames = newSettings.AutoResolveHostnames;
            _cachedSettings.DefaultGroup = newSettings.DefaultGroup;
            _cachedSettings.EnableNotifications = newSettings.EnableNotifications;

            // Save to file
            var json = JsonSerializer.Serialize(_cachedSettings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await File.WriteAllTextAsync(_settingsPath, json);

            return JsonSerializer.Serialize(ApiResponse.Success(
                new { settings = _cachedSettings },
                "Settings updated successfully"
            ));
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(ApiResponse.Error($"Error updating settings: {ex.Message}"));
        }
    }
}
