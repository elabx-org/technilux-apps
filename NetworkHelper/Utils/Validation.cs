using System.Net;
using System.Text.RegularExpressions;

namespace NetworkHelper.Utils;

public static partial class Validation
{
    [GeneratedRegex(@"^([0-9a-fA-F]{2}[:-]){5}([0-9a-fA-F]{2})$")]
    private static partial Regex MacAddressRegex();

    [GeneratedRegex(@"^[a-zA-Z0-9]([a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$")]
    private static partial Regex HostnameRegex();

    public static bool IsValidIpAddress(string ip)
    {
        return IPAddress.TryParse(ip, out _);
    }

    public static bool IsValidMacAddress(string? mac)
    {
        if (string.IsNullOrWhiteSpace(mac))
            return true; // Optional field

        return MacAddressRegex().IsMatch(mac);
    }

    public static bool IsValidHostname(string? hostname)
    {
        if (string.IsNullOrWhiteSpace(hostname))
            return true; // Optional field

        return hostname.Length <= 253 && HostnameRegex().IsMatch(hostname);
    }

    public static bool IsValidTag(string tag)
    {
        return !string.IsNullOrWhiteSpace(tag) && tag.Length <= 50;
    }

    public static string SanitizeString(string? input, int maxLength = 500)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Remove control characters and limit length
        var sanitized = new string(input
            .Where(c => !char.IsControl(c) || c == '\n' || c == '\r')
            .Take(maxLength)
            .ToArray());

        return sanitized.Trim();
    }
}
