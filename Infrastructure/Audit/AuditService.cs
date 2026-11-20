using Microsoft.Extensions.Logging;

namespace WACAMS.Infrastructure.Audit;

/// <summary>
/// Implementation of audit service
/// </summary>
public class AuditService : IAuditService
{
    private readonly List<AuditLog> _logs = new();
    private readonly ILogger<AuditService> _logger;

    public AuditService(ILogger<AuditService> logger)
    {
        _logger = logger;
    }

    public Task LogAsync(string action, string entityType, string entityId, string userId, string? details = null)
    {
        var log = new AuditLog
        {
            Id = Guid.NewGuid(),
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            UserId = userId,
            Details = details,
            Timestamp = DateTime.UtcNow
        };

        _logs.Add(log);
        _logger.LogInformation("Audit log: {Action} on {EntityType} {EntityId} by {UserId}", 
            action, entityType, entityId, userId);

        return Task.CompletedTask;
    }

    public Task<IEnumerable<AuditLog>> GetLogsAsync(string entityType, string entityId)
    {
        var logs = _logs
            .Where(l => l.EntityType == entityType && l.EntityId == entityId)
            .OrderByDescending(l => l.Timestamp)
            .AsEnumerable();

        return Task.FromResult(logs);
    }

    public Task<IEnumerable<AuditLog>> GetLogsByUserAsync(string userId)
    {
        var logs = _logs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.Timestamp)
            .AsEnumerable();

        return Task.FromResult(logs);
    }
}
