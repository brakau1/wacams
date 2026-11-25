namespace WACAMS.Api.DTOs;

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? Detail { get; set; }
}
