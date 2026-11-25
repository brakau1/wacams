using Microsoft.EntityFrameworkCore;
using WACAMS.Api.Data;
using WACAMS.Api.DTOs;
using WACAMS.Core.Entities;

namespace WACAMS.Api.Services;

/// <summary>
/// Service for GIS integration administration.
/// </summary>
public class GISAdminService
{
    private readonly WacamDbContext _context;
    private readonly AuditService _auditService;

    public GISAdminService(WacamDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    /// <summary>
    /// Get GIS connection status.
    /// </summary>
    public async Task<GisConnectionStatusDto> GetConnectionStatusAsync()
    {
        // For now, return mock data. In Phase 4, this will integrate with actual GIS.
        var lastSync = await _context.AuditLogs
            .Where(a => a.Action.StartsWith("GISSync"))
            .OrderByDescending(a => a.Timestamp)
            .FirstOrDefaultAsync();

        return new GisConnectionStatusDto
        {
            IsConnected = false, // Will be updated in Phase 4
            Status = "Not Configured",
            LastSyncAt = lastSync?.Timestamp,
            LastSuccessfulSyncAt = lastSync?.Timestamp,
            FailedRecordsCount = 0,
            RetryQueueSize = 0,
            LastError = null
        };
    }

    /// <summary>
    /// Get GIS sync history.
    /// </summary>
    public async Task<List<GisSyncHistoryDto>> GetSyncHistoryAsync(int limit = 50)
    {
        var syncLogs = await _context.AuditLogs
            .Where(a => a.Action.StartsWith("GISSync"))
            .OrderByDescending(a => a.Timestamp)
            .Take(limit)
            .ToListAsync();

        return syncLogs.Select(log => new GisSyncHistoryDto
        {
            Id = Guid.Parse(log.Id.ToString()),
            SyncType = log.Action.Replace("GISSync", ""),
            Status = log.Details?.Contains("Failed") == true ? "Failed" : "Completed",
            TotalRecords = 0, // Will be populated from Details JSON in Phase 4
            SuccessCount = 0,
            ErrorCount = 0,
            StartedAt = log.Timestamp,
            CompletedAt = log.Timestamp,
            ErrorMessage = log.Details?.Contains("Failed") == true ? log.Details : null
        }).ToList();
    }

    /// <summary>
    /// Trigger manual GIS sync.
    /// </summary>
    public async Task<GisSyncHistoryDto> TriggerManualSyncAsync(
        string syncType,
        DateTime? fromDate,
        DateTime? toDate,
        string initiatedBy)
    {
        // For now, log the attempt. In Phase 4, this will actually sync with GIS.
        var syncResult = new GisSyncHistoryDto
        {
            Id = Guid.NewGuid(),
            SyncType = syncType,
            Status = "Not Implemented",
            TotalRecords = 0,
            SuccessCount = 0,
            ErrorCount = 0,
            StartedAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            ErrorMessage = "GIS integration will be implemented in Phase 4"
        };

        await _auditService.LogAsync(
            $"GISSync{syncType}",
            "SuperAdmin",
            "GIS",
            syncResult.Id.ToString(),
            $"Manual GIS sync triggered: {syncType}",
            null,
            new { syncType, fromDate, toDate },
            initiatedBy);

        return syncResult;
    }

    /// <summary>
    /// Pause GIS sync.
    /// </summary>
    public async Task PauseSyncAsync(string initiatedBy)
    {
        await _auditService.LogAsync(
            "GISSyncPaused",
            "SuperAdmin",
            "GIS",
            "System",
            "GIS sync paused",
            null,
            null,
            initiatedBy);
    }

    /// <summary>
    /// Resume GIS sync.
    /// </summary>
    public async Task ResumeSyncAsync(string initiatedBy)
    {
        await _auditService.LogAsync(
            "GISSyncResumed",
            "SuperAdmin",
            "GIS",
            "System",
            "GIS sync resumed",
            null,
            null,
            initiatedBy);
    }
}
