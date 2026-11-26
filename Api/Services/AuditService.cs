using Microsoft.EntityFrameworkCore;
using Wacam.Api.Data;
using WACAMS.Core.Entities;

namespace Wacam.Api.Services;

/// <summary>
/// Write audit logs, query trails with pagination/filtering.
/// All changes to the system must be audit logged here.
/// </summary>
public class AuditService
{
    private readonly WacamDbContext _context;
    private readonly ILogger<AuditService> _logger;

    public AuditService(WacamDbContext context, ILogger<AuditService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Log an action to the audit trail.
    /// This is called by services after performing state-changing operations.
    /// </summary>
    public async Task LogActionAsync(
        string? performedBy,
        string action,
        string entityType,
        string entityId,
        string? httpEndpoint = null,
        string? changesJson = null)
    {
        var now = DateTime.UtcNow;
        var logEntry = new AuditLog
        {
            PerformedBy = performedBy ?? "system",
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            HttpEndpoint = httpEndpoint,
            IpAddress = null, // Will be set by middleware if needed
            ChangesJson = changesJson,
            PerformedAt = now,
            CreatedAt = now
        };

        _context.AuditLogs.Add(logEntry);
        await _context.SaveChangesAsync();

        _logger.LogDebug("Audit: {Action} | Entity: {EntityType}/{EntityId} | PerformedBy: {PerformedBy}",
            action, entityType, entityId, performedBy ?? "system");
    }

    /// <summary>
    /// Query audit trail with filtering and pagination.
    /// Returns up to 500 results per page (max for performance).
    /// </summary>
    public async Task<(List<AuditLogDto> Logs, int TotalCount)> GetTrailAsync(
        string? performedBy = null,
        string? action = null,
        string? entityType = null,
        string? entityId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int page = 1,
        int pageSize = 50)
    {
        if (pageSize > 500)
            pageSize = 500; // Hard cap for performance

        var query = _context.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(performedBy))
            query = query.Where(l => l.PerformedBy == performedBy);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(l => l.Action == action);

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(l => l.EntityType == entityType);

        if (!string.IsNullOrWhiteSpace(entityId))
            query = query.Where(l => l.EntityId == entityId);

        if (startDate.HasValue)
            query = query.Where(l => l.PerformedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(l => l.PerformedAt <= endDate.Value.AddDays(1));

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(l => l.PerformedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (logs.Select(MapToDto).ToList(), totalCount);
    }

    /// <summary>
    /// Get recent activity — last N actions across all entities.
    /// Used for dashboard/activity feed displays.
    /// </summary>
    public async Task<List<AuditLogDto>> GetRecentActivityAsync(int limit = 20)
    {
        var logs = await _context.AuditLogs
            .OrderByDescending(l => l.CreatedAt)
            .Take(limit)
            .ToListAsync();

        return logs.Select(MapToDto).ToList();
    }

    private static AuditLogDto MapToDto(AuditLog log)
    {
        return new AuditLogDto
        {
            Id = log.Id,
            PerformedBy = log.PerformedBy,
            Action = log.Action,
            EntityType = log.EntityType,
            EntityId = log.EntityId,
            HttpEndpoint = log.HttpEndpoint,
            IpAddress = log.IpAddress,
            ChangesJson = log.ChangesJson,
            PerformedAt = log.PerformedAt
        };
    }

    public async Task LogAsync(string action, string entityType, int entityId, string userId, string ipAddress, string details = null)
    {
        var log = new AuditLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId.ToString(),
            UserId = userId,
            IpAddress = ipAddress,
            Details = details,
            PerformedAt = DateTime.UtcNow
        };
        await _context.AuditLogs.AddAsync(log);
        await _context.SaveChangesAsync();
    }
}

public record AuditLogDto
{
    public int Id { get; init; }
    public string? PerformedBy { get; init; }
    public required string Action { get; init; } = string.Empty;
    public required string EntityType { get; init; } = string.Empty;
    public required string EntityId { get; init; } = string.Empty;
    public string? HttpEndpoint { get; init; }
    public string? IpAddress { get; init; }
    public string? ChangesJson { get; init; }
    public DateTime PerformedAt { get; init; }
}

