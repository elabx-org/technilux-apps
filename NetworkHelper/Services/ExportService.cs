using System.Text;
using System.Text.Json;
using NetworkHelper.Models;

namespace NetworkHelper.Services;

public static class ExportService
{
    public static string ExportToJson(List<Device> devices)
    {
        return JsonSerializer.Serialize(devices, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    public static string ExportToCsv(List<Device> devices)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("IP,Hostname,CustomName,MAC,Vendor,HostnameSource,Notes,Tags,Group,Icon,FirstSeen,LastSeen,LastUpdated,QueryCount,Favorite");

        // Rows
        foreach (var device in devices)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(device.Ip),
                EscapeCsv(device.Hostname),
                EscapeCsv(device.CustomName),
                EscapeCsv(device.Mac),
                EscapeCsv(device.Vendor),
                EscapeCsv(device.HostnameSource),
                EscapeCsv(device.Notes),
                EscapeCsv(string.Join(";", device.Tags)),
                EscapeCsv(device.Group),
                EscapeCsv(device.Icon),
                device.FirstSeen.ToString("O"),
                device.LastSeen.ToString("O"),
                device.LastUpdated.ToString("O"),
                device.QueryCount,
                device.Favorite
            ));
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
