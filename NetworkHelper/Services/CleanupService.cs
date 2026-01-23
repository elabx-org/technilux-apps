using NetworkHelper.Models;
using NetworkHelper.Storage;

namespace NetworkHelper.Services;

public class CleanupService : IDisposable
{
    private readonly DeviceStore _deviceStore;
    private readonly AppSettings _settings;
    private readonly Timer _timer;
    private bool _disposed = false;

    public CleanupService(DeviceStore deviceStore, AppSettings settings)
    {
        _deviceStore = deviceStore;
        _settings = settings;

        var intervalMs = TimeSpan.FromHours(_settings.CleanupIntervalHours).TotalMilliseconds;
        _timer = new Timer(OnTimerElapsed, null, (int)intervalMs, (int)intervalMs);
    }

    private async void OnTimerElapsed(object? state)
    {
        try
        {
            Console.WriteLine("[NetworkHelper] Running cleanup task...");
            var deletedCount = await _deviceStore.DeleteStaleDevicesAsync(_settings.StaleThresholdDays);
            Console.WriteLine($"[NetworkHelper] Cleanup completed. Deleted {deletedCount} stale devices.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[NetworkHelper] Cleanup error: {ex.Message}");
        }
    }

    public void Stop()
    {
        _timer?.Change(Timeout.Infinite, Timeout.Infinite);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Stop();
        _timer?.Dispose();
        _disposed = true;
    }
}
