using System.Net;
using System.Text.Json;
using WACAMS.Core.Exceptions;

namespace WACAMS.Api.Middleware;

/// <summary>
/// Catch-all exception handling middleware with consistent error response format.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var statusCode = GetStatusCode(exception);
        var response = new ErrorResponse
        {
            StatusCode = statusCode,
            Message = GetErrorMessage(exception),
            Details = context.RequestServices.GetService<IHostEnvironment>()?.IsDevelopment() == true
                ? exception.ToString()
                : null
        };

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        var json = JsonSerializer.Serialize(response, jsonOptions);
        await context.Response.WriteAsync(json);

        // Log the full exception for debugging
        if (statusCode >= 500)
        {
            // Will be logged by Serilog in Program.cs
        }
    }

    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ArgumentException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            ForbiddenException => (int)HttpStatusCode.Forbidden,
            NotFoundException => (int)HttpStatusCode.NotFound,
            ConflictException => (int)HttpStatusCode.Conflict,
            InvalidOperationException => (int)HttpStatusCode.BadRequest,
            _ => (int)HttpStatusCode.InternalServerError
        };
    }

    private static string GetErrorMessage(Exception exception)
    {
        return exception switch
        {
            ArgumentException => exception.Message,
            UnauthorizedAccessException => "Unauthorized access.",
            ForbiddenException => "Access forbidden. Insufficient permissions.",
            NotFoundException => "Resource not found.",
            ConflictException => exception.Message,
            InvalidOperationException => "Invalid operation.",
            _ => "An unexpected error occurred. Please contact the system administrator."
        };
    }
}

// Use exception types from WACAMS.Core.Exceptions namespace
// NotFoundException, ConflictException are defined in Api/Exceptions/

public record ErrorResponse
{
    public int StatusCode { get; init; }
    public required string Message { get; init; }
    public string? Details { get; init; }
}
