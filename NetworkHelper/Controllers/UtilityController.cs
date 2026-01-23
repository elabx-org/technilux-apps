using System.Text.Json;
using System.Web;
using NetworkHelper.Models;
using NetworkHelper.Services;
using NetworkHelper.Storage;

namespace NetworkHelper.Controllers;

public static class UtilityController
{
    public static async Task<string> GetStats(DeviceStore store)
    {
        var stats = await store.GetStatsAsync();
        return JsonSerializer.Serialize(ApiResponse.Success(stats));
    }

    public static async Task<string> ManualCleanup(DeviceStore store, AppSettings settings)
    {
        var deletedCount = await store.DeleteStaleDevicesAsync(settings.StaleThresholdDays);
        return JsonSerializer.Serialize(ApiResponse.Success(
            new { deletedCount },
            $"Deleted {deletedCount} stale devices"
        ));
    }

    public static async Task<string> ExportDevices(DeviceStore store, string queryString)
    {
        var query = HttpUtility.ParseQueryString(queryString);
        var format = query["format"]?.ToLowerInvariant() ?? "json";

        var devices = await store.GetAllDevicesAsync();

        return format switch
        {
            "csv" => ExportService.ExportToCsv(devices),
            "json" => ExportService.ExportToJson(devices),
            _ => JsonSerializer.Serialize(ApiResponse.Error("Invalid format. Use 'json' or 'csv'"))
        };
    }
}
