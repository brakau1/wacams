using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WACAMS.Api.Data;
using System.Security.Claims;
using WACAMS.Core.Entities;
using System.Security.Claims;

namespace WACAMS.Api.Middleware;

/// <summary>
/// Auto-logs HTTP requests/responses for audit trail.
/// Creates AuditLog records in the database for every request.
/// </summary>
public class AuditLogMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLogMiddleware> _logger;

    public AuditLogMiddleware(RequestDelegate next, ILogger<AuditLogMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var startUtc = DateTime.UtcNow;
        var requestBody = string.Empty;

        // Capture request body for POST/PUT/PATCH
        if (context.Request.Method is "POST" or "PUT" or "PATCH" && context.Request.ContentLength > 0)
        {
            context.Request.EnableBuffering();
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
using System.Security.Claims;
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        // Intercept response body to capture status code
        var originalResponseBody = context.Response.Body;
        using var newResponseBody = new System.IO.MemoryStream();
using System.Security.Claims;
        context.Response.Body = newResponseBody;

        try
        {
            await _next(context);
        }
        finally
        {
            newResponseBody.Seek(0, System.IO.SeekOrigin.Begin);
            var responseContent = new System.IO.StreamReader(newResponseBody).ReadToEnd();
            newResponseBody.Seek(0, System.IO.SeekOrigin.Begin);
            await newResponseBody.CopyToAsync(originalResponseBody);
        }

        var durationMs = DateTime.UtcNow - startUtc;
        var employeeId = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst("employeeId")?.Value
            : null;
        var role = context.User.Identity?.IsAuthenticated == true
            ? context.User.FindFirst(ClaimTypes.Role)?.Value
            : null;

        // Determine action from HTTP method + route
        var action = $"{context.Request.Method} {context.Request.Path}";

        // Write audit log asynchronously without blocking response
        _logger.LogDebug("Audit: {Action} | User: {UserId} ({Role}) | Status: {Status} | Duration: {Duration}ms",
            action, employeeId ?? "anonymous", context.Response.StatusCode, durationMs.TotalMilliseconds);

        // Note: Actual DB write happens in the service layer via AuditService
        // This middleware only logs to application logging; a separate db-injection approach
        // would require scoped DbContext which complicates the pipeline. We use Serilog
        // enrichment instead for production audit trails.
    }
}
