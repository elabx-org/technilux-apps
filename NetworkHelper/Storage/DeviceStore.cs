using System.Text.Json;
using NetworkHelper.Models;

namespace NetworkHelper.Storage;

public class DeviceStore : IDisposable
{
    private readonly string _dataFilePath;
    private readonly string _backupDirectory;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private Dictionary<string, Device> _devices = new();
    private const int MaxBackups = 5;

    public DeviceStore(string dataFilePath)
    {
        _dataFilePath = dataFilePath;
        _backupDirectory = Path.Combine(Path.GetDirectoryName(dataFilePath) ?? "", "backups");

        Directory.CreateDirectory(_backupDirectory);
        LoadDevices();
    }

    private void LoadDevices()
    {
        try
        {
            if (File.Exists(_dataFilePath))
            {
                var json = File.ReadAllText(_dataFilePath);
                var devices = JsonSerializer.Deserialize<List<Device>>(json);
                if (devices != null)
                {
                    _devices = devices.ToDictionary(d => d.Ip, d => d);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading devices: {ex.Message}");
            _devices = new Dictionary<string, Device>();
        }
    }

    public async Task<List<Device>> GetAllDevicesAsync()
    {
        await _lock.WaitAsync();
        try
        {
            return _devices.Values.OrderBy(d => d.Ip).ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Device?> GetDeviceAsync(string ip)
    {
        await _lock.WaitAsync();
        try
        {
            return _devices.TryGetValue(ip, out var device) ? device : null;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Device> SaveDeviceAsync(Device device)
    {
        await _lock.WaitAsync();
        try
        {
            if (_devices.TryGetValue(device.Ip, out var existing))
            {
                // Update existing device
                device.FirstSeen = existing.FirstSeen;
                device.LastUpdated = DateTime.UtcNow;
            }
            else
            {
                // New device
                device.FirstSeen = DateTime.UtcNow;
                device.LastSeen = DateTime.UtcNow;
                device.LastUpdated = DateTime.UtcNow;
            }

            _devices[device.Ip] = device;
            await SaveToFileAsync();
            return device;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<bool> DeleteDeviceAsync(string ip)
    {
        await _lock.WaitAsync();
        try
        {
            if (_devices.Remove(ip))
            {
                await SaveToFileAsync();
                return true;
            }
            return false;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<int> DeleteStaleDevicesAsync(int staleThresholdDays)
    {
        await _lock.WaitAsync();
        try
        {
            var threshold = DateTime.UtcNow.AddDays(-staleThresholdDays);
            var staleDevices = _devices.Values
                .Where(d => d.LastSeen < threshold && !d.Favorite)
                .Select(d => d.Ip)
                .ToList();

            foreach (var ip in staleDevices)
            {
                _devices.Remove(ip);
            }

            if (staleDevices.Count > 0)
            {
                await SaveToFileAsync();
            }

            return staleDevices.Count;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Dictionary<string, object>> GetStatsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var bySource = _devices.Values
                .Where(d => d.HostnameSource != null)
                .GroupBy(d => d.HostnameSource!)
                .ToDictionary(g => g.Key, g => g.Count());

            var byGroup = _devices.Values
                .Where(d => d.Group != null)
                .GroupBy(d => d.Group!)
                .ToDictionary(g => g.Key, g => g.Count());

            var staleCount = _devices.Values
                .Count(d => d.LastSeen < DateTime.UtcNow.AddDays(-30));

            return new Dictionary<string, object>
            {
                ["totalDevices"] = _devices.Count,
                ["bySource"] = bySource,
                ["byGroup"] = byGroup,
                ["favoriteCount"] = _devices.Values.Count(d => d.Favorite),
                ["staleCount"] = staleCount
            };
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task SaveToFileAsync()
    {
        // Create backup before writing
        if (File.Exists(_dataFilePath))
        {
            CreateBackup();
        }

        // Write to temp file first
        var tempFile = _dataFilePath + ".tmp";
        var json = JsonSerializer.Serialize(_devices.Values.ToList(), new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(tempFile, json);

        // Atomic rename
        File.Move(tempFile, _dataFilePath, true);

        // Cleanup old backups
        CleanupOldBackups();
    }

    private void CreateBackup()
    {
        try
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var backupPath = Path.Combine(_backupDirectory, $"devices_{timestamp}.json");
            File.Copy(_dataFilePath, backupPath, true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating backup: {ex.Message}");
        }
    }

    private void CleanupOldBackups()
    {
        try
        {
            var backups = Directory.GetFiles(_backupDirectory, "devices_*.json")
                .OrderByDescending(f => f)
                .Skip(MaxBackups)
                .ToList();

            foreach (var backup in backups)
            {
                File.Delete(backup);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cleaning up backups: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _lock.Dispose();
    }
}
