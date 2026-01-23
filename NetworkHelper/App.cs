using System.Text;
using System.Text.Json;
using NetworkHelper.Controllers;
using NetworkHelper.Models;
using NetworkHelper.Services;
using NetworkHelper.Storage;
using TechnitiumLibrary.Net.Dns;
using TechnitiumLibrary.Net.Dns.ResourceRecords;

namespace NetworkHelper;

public class App : IDnsApplication, IDisposable
{
    private DeviceStore? _deviceStore;
    private AppSettings? _settings;
    private CleanupService? _cleanupService;
    private string? _settingsPath;

    public string Description => "Persistent network client information and hostname mappings";

    public async Task InitializeAsync(IDnsServer dnsServer, string config)
    {
        // Determine data directory
        var configDir = dnsServer.ApplicationFolder;
        var dataPath = Path.Combine(configDir, "devices.json");
        _settingsPath = Path.Combine(configDir, "settings.json");

        // Initialize storage
        _deviceStore = new DeviceStore(dataPath);

        // Load settings
        _settings = await LoadSettingsAsync(_settingsPath);

        // Initialize controllers with settings
        SettingsController.Initialize(_settingsPath, _settings);

        // Start background cleanup service
        if (_settings.CleanupEnabled)
        {
            _cleanupService = new CleanupService(_deviceStore, _settings);
        }

        Console.WriteLine($"[NetworkHelper] Initialized. Data: {dataPath}");
    }

    public Task<DnsRequestController> ProcessRequestAsync(DnsDatagram request, System.Net.IPEndPoint remoteEP, DnsTransportProtocol protocol, bool isRecursionAllowed)
    {
        // This app doesn't intercept DNS queries
        return Task.FromResult<DnsRequestController>(null!);
    }

    public async Task<DnsRequestController> ProcessRequestAsync(DnsDatagram request, System.Net.IPEndPoint remoteEP, DnsTransportProtocol protocol, bool isRecursionAllowed, string zoneName, string appRecordName, uint appRecordTtl, string appRecordData)
    {
        // This app doesn't use app record data
        return await ProcessRequestAsync(request, remoteEP, protocol, isRecursionAllowed);
    }

    public async Task<DnsDatagram> ProcessResponseAsync(DnsDatagram request, System.Net.IPEndPoint remoteEP, DnsTransportProtocol protocol, DnsDatagram response)
    {
        // Optionally track queries here for analytics
        return response;
    }

    // HTTP API handler (called by Technitium web server)
    public async Task<string> GetConfigAsync()
    {
        if (_settings == null)
            return "{}";

        return JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task SetConfigAsync(string config)
    {
        if (string.IsNullOrWhiteSpace(config) || _settingsPath == null)
            return;

        var newSettings = JsonSerializer.Deserialize<AppSettings>(config);
        if (newSettings != null)
        {
            _settings = newSettings;
            await File.WriteAllTextAsync(_settingsPath, config);

            // Restart cleanup service if needed
            if (_settings.CleanupEnabled && _cleanupService == null && _deviceStore != null)
            {
                _cleanupService = new CleanupService(_deviceStore, _settings);
            }
            else if (!_settings.CleanupEnabled && _cleanupService != null)
            {
                _cleanupService.Dispose();
                _cleanupService = null;
            }
        }
    }

    // HTTP request handler for API endpoints
    public async Task<string> ProcessHttpRequestAsync(string path, string queryString, string method, string requestBody)
    {
        if (_deviceStore == null || _settings == null)
        {
            return JsonSerializer.Serialize(ApiResponse.Error("App not initialized"));
        }

        try
        {
            return (path.ToLowerInvariant(), method.ToUpperInvariant()) switch
            {
                // Device endpoints
                ("/api/networkhelper/devices", "GET") => await DevicesController.ListDevices(_deviceStore),
                ("/api/networkhelper/devices/get", "GET") => await DevicesController.GetDevice(_deviceStore, queryString),
                ("/api/networkhelper/devices", "POST") => await DevicesController.CreateOrUpdateDevice(_deviceStore, requestBody),
                ("/api/networkhelper/devices", "DELETE") => await DevicesController.DeleteDevice(_deviceStore, queryString),
                ("/api/networkhelper/devices/bulk", "POST") => await DevicesController.BulkImport(_deviceStore, requestBody),

                // Settings endpoints
                ("/api/networkhelper/settings", "GET") => SettingsController.GetSettings(),
                ("/api/networkhelper/settings", "POST") => await SettingsController.UpdateSettings(requestBody),

                // Utility endpoints
                ("/api/networkhelper/stats", "GET") => await UtilityController.GetStats(_deviceStore),
                ("/api/networkhelper/cleanup", "POST") => await UtilityController.ManualCleanup(_deviceStore, _settings),
                ("/api/networkhelper/export", "GET") => await UtilityController.ExportDevices(_deviceStore, queryString),

                _ => JsonSerializer.Serialize(ApiResponse.Error("Endpoint not found"))
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetworkHelper] Error processing request: {ex}");
            return JsonSerializer.Serialize(ApiResponse.Error($"Server error: {ex.Message}"));
        }
    }

    private async Task<AppSettings> LoadSettingsAsync(string path)
    {
        if (File.Exists(path))
        {
            try
            {
                var json = await File.ReadAllTextAsync(path);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                    return settings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[NetworkHelper] Error loading settings: {ex.Message}");
            }
        }

        // Return default settings
        return new AppSettings();
    }

    public void Dispose()
    {
        _cleanupService?.Dispose();
        _deviceStore?.Dispose();
    }
}
