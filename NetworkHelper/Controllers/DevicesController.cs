using System.Text.Json;
using System.Web;
using NetworkHelper.Models;
using NetworkHelper.Storage;
using NetworkHelper.Utils;

namespace NetworkHelper.Controllers;

public static class DevicesController
{
    public static async Task<string> ListDevices(DeviceStore store)
    {
        var devices = await store.GetAllDevicesAsync();
        return JsonSerializer.Serialize(ApiResponse.Success(new { devices }));
    }

    public static async Task<string> GetDevice(DeviceStore store, string queryString)
    {
        var query = HttpUtility.ParseQueryString(queryString);
        var ip = query["ip"];

        if (string.IsNullOrWhiteSpace(ip))
        {
            return JsonSerializer.Serialize(ApiResponse.Error("IP parameter is required"));
        }

        var device = await store.GetDeviceAsync(ip);
        if (device == null)
        {
            return JsonSerializer.Serialize(ApiResponse.Error("Device not found"));
        }

        return JsonSerializer.Serialize(ApiResponse.Success(new { device }));
    }

    public static async Task<string> CreateOrUpdateDevice(DeviceStore store, string requestBody)
    {
        try
        {
            var device = JsonSerializer.Deserialize<Device>(requestBody);
            if (device == null)
            {
                return JsonSerializer.Serialize(ApiResponse.Error("Invalid device data"));
            }

            // Validate
            if (!Validation.IsValidIpAddress(device.Ip))
            {
                return JsonSerializer.Serialize(ApiResponse.Error("Invalid IP address"));
            }

            if (!Validation.IsValidMacAddress(device.Mac))
            {
                return JsonSerializer.Serialize(ApiResponse.Error("Invalid MAC address"));
            }

            if (!Validation.IsValidHostname(device.Hostname))
            {
                return JsonSerializer.Serialize(ApiResponse.Error("Invalid hostname"));
            }

            // Sanitize strings
            device.CustomName = Validation.SanitizeString(device.CustomName, 100);
            device.Notes = Validation.SanitizeString(device.Notes, 500);
            device.Group = Validation.SanitizeString(device.Group, 50);

            // Validate tags
            if (device.Tags.Any(t => !Validation.IsValidTag(t)))
            {
                return JsonSerializer.Serialize(ApiResponse.Error("Invalid tag format"));
            }

            var savedDevice = await store.SaveDeviceAsync(device);
            return JsonSerializer.Serialize(ApiResponse.Success(
                new { device = savedDevice },
                "Device saved successfully"
            ));
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(ApiResponse.Error($"Error saving device: {ex.Message}"));
        }
    }

    public static async Task<string> DeleteDevice(DeviceStore store, string queryString)
    {
        var query = HttpUtility.ParseQueryString(queryString);
        var ip = query["ip"];

        if (string.IsNullOrWhiteSpace(ip))
        {
            return JsonSerializer.Serialize(ApiResponse.Error("IP parameter is required"));
        }

        var deleted = await store.DeleteDeviceAsync(ip);
        if (!deleted)
        {
            return JsonSerializer.Serialize(ApiResponse.Error("Device not found"));
        }

        return JsonSerializer.Serialize(ApiResponse.Success(null, "Device deleted successfully"));
    }

    public static async Task<string> BulkImport(DeviceStore store, string requestBody)
    {
        try
        {
            var devices = JsonSerializer.Deserialize<List<Device>>(requestBody);
            if (devices == null || devices.Count == 0)
            {
                return JsonSerializer.Serialize(ApiResponse.Error("No devices provided"));
            }

            var imported = 0;
            var errors = new List<string>();

            foreach (var device in devices)
            {
                try
                {
                    if (!Validation.IsValidIpAddress(device.Ip))
                    {
                        errors.Add($"Invalid IP: {device.Ip}");
                        continue;
                    }

                    await store.SaveDeviceAsync(device);
                    imported++;
                }
                catch (Exception ex)
                {
                    errors.Add($"{device.Ip}: {ex.Message}");
                }
            }

            return JsonSerializer.Serialize(ApiResponse.Success(
                new { imported, failed = errors.Count, errors },
                $"Imported {imported} devices, {errors.Count} failed"
            ));
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(ApiResponse.Error($"Bulk import error: {ex.Message}"));
        }
    }
}
