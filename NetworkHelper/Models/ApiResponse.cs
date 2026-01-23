using System.Text.Json.Serialization;

namespace NetworkHelper.Models;

public class ApiResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = "ok";

    [JsonPropertyName("data")]
    public object? Data { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse Success(object? data = null, string? message = null)
    {
        return new ApiResponse
        {
            Status = "ok",
            Data = data,
            Message = message
        };
    }

    public static ApiResponse Error(string message)
    {
        return new ApiResponse
        {
            Status = "error",
            Message = message
        };
    }
}
