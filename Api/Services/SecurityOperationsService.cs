using Microsoft.EntityFrameworkCore;
using WACAMS.Api.Data;
using WACAMS.Api.DTOs;
using WACAMS.Core.Entities;

namespace WACAMS.Api.Services;

/// <summary>
/// Service for emergency security operations.
/// Handles force logout, lockdown, session revocation, and other emergency controls.
/// </summary>
public class SecurityOperationsService
{
    private readonly WacamDbContext _context;
    private readonly AuditService _auditService;

    public SecurityOperationsService(WacamDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    /// <summary>
    /// Force logout a specific user by terminating all their active sessions.
    /// </summary>
    public async Task<bool> ForceLogoutAsync(string employeeId, string? reason, string initiatedBy)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee == null) return false;

        var sessions = await _context.UserSessions
            .Where(s => s.EmployeeId == employeeId && s.IsActive)
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.IsActive = false;
            session.EndedAt = DateTime.UtcNow;
            session.EndReason = "ForceLogout";
        }

        await _context.SaveChangesAsync();

        // Log security event
        var securityEvent = new SecurityEvent
        {
            Id = Guid.NewGuid(),
            EventType = "ForceLogout",
            Severity = "High",
            EmployeeId = employeeId,
            DetailsJson = System.Text.Json.JsonSerializer.Serialize(new { Reason = reason, SessionsTerminated = sessions.Count }),
            Timestamp = DateTime.UtcNow
        };
        _context.SecurityEvents.Add(securityEvent);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "ForceLogout",
            "SuperAdmin",
            "UserSession",
            employeeId,
            $"Force logged out user {employeeId}",
            null,
            new { Reason = reason, SessionsTerminated = sessions.Count },
            initiatedBy);

        return true;
    }

    /// <summary>
    /// Lock a user account immediately.
    /// </summary>
    public async Task<bool> LockUserAsync(string employeeId, string? reason, string initiatedBy)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        if (user == null) return false;

        user.IsLocked = true;
        await _context.SaveChangesAsync();

        // Terminate all active sessions
        var sessions = await _context.UserSessions
            .Where(s => s.EmployeeId == employeeId && s.IsActive)
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.IsActive = false;
            session.EndedAt = DateTime.UtcNow;
            session.EndReason = "AccountLocked";
        }

        await _context.SaveChangesAsync();

        var securityEvent = new SecurityEvent
        {
            Id = Guid.NewGuid(),
            EventType = "AccountLocked",
            Severity = "High",
            EmployeeId = employeeId,
            DetailsJson = System.Text.Json.JsonSerializer.Serialize(new { Reason = reason }),
            Timestamp = DateTime.UtcNow
        };
        _context.SecurityEvents.Add(securityEvent);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "AccountLocked",
            "SuperAdmin",
            "User",
            employeeId,
            $"Locked user account: {employeeId}",
            null,
            new { Reason = reason },
            initiatedBy);

        return true;
    }

    /// <summary>
    /// Unlock a user account.
    /// </summary>
    public async Task<bool> UnlockUserAsync(string employeeId, string initiatedBy)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        if (user == null) return false;

        user.IsLocked = false;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "AccountUnlocked",
            "SuperAdmin",
            "User",
            employeeId,
            $"Unlocked user account: {employeeId}",
            null,
            null,
            initiatedBy);

        return true;
    }

    /// <summary>
    /// Disable a user account (soft delete).
    /// </summary>
    public async Task<bool> DisableUserAsync(string employeeId, string? reason, string initiatedBy)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        if (user == null) return false;

        user.IsActive = false;
        user.IsLocked = true;

        // Terminate all sessions
        var sessions = await _context.UserSessions
            .Where(s => s.EmployeeId == employeeId && s.IsActive)
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.IsActive = false;
            session.EndedAt = DateTime.UtcNow;
            session.EndReason = "AccountDisabled";
        }

        await _context.SaveChangesAsync();

        var securityEvent = new SecurityEvent
        {
            Id = Guid.NewGuid(),
            EventType = "AccountDisabled",
            Severity = "Critical",
            EmployeeId = employeeId,
            DetailsJson = System.Text.Json.JsonSerializer.Serialize(new { Reason = reason }),
            Timestamp = DateTime.UtcNow
        };
        _context.SecurityEvents.Add(securityEvent);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "AccountDisabled",
            "SuperAdmin",
            "User",
            employeeId,
            $"Disabled user account: {employeeId}",
            null,
            new { Reason = reason },
            initiatedBy);

        return true;
    }

    /// <summary>
    /// Enable a user account.
    /// </summary>
    public async Task<bool> EnableUserAsync(string employeeId, string initiatedBy)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        if (user == null) return false;

        user.IsActive = true;
        user.IsLocked = false;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "AccountEnabled",
            "SuperAdmin",
            "User",
            employeeId,
            $"Enabled user account: {employeeId}",
            null,
            null,
            initiatedBy);

        return true;
    }

    /// <summary>
    /// Revoke all active sessions for a user.
    /// </summary>
    public async Task<int> RevokeAllSessionsAsync(string employeeId, string? reason, string initiatedBy)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.EmployeeId == employeeId && s.IsActive)
            .ToListAsync();

        foreach (var session in sessions)
        {
            session.IsActive = false;
            session.EndedAt = DateTime.UtcNow;
            session.EndReason = "SessionRevoked";
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "SessionsRevoked",
            "SuperAdmin",
            "UserSession",
            employeeId,
            $"Revoked all sessions for user {employeeId}",
            null,
            new { Reason = reason, SessionsRevoked = sessions.Count },
            initiatedBy);

        return sessions.Count;
    }

    /// <summary>
    /// Force password reset for a user.
    /// </summary>
    public async Task<bool> ForcePasswordResetAsync(string employeeId, string initiatedBy)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        if (user == null) return false;

        user.MustChangePassword = true;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "ForcePasswordReset",
            "SuperAdmin",
            "User",
            employeeId,
            $"Forced password reset for user {employeeId}",
            null,
            null,
            initiatedBy);

        return true;
    }

    /// <summary>
    /// Initiate a security lockdown.
    /// </summary>
    public async Task<SecurityLockdownDto> InitiateLockdownAsync(InitiateLockdownRequest request, string initiatedBy)
    {
        var lockdown = new SecurityLockdown
        {
            Id = Guid.NewGuid(),
            LockdownType = request.LockdownType,
            DepartmentId = request.DepartmentId,
            TargetRole = request.TargetRole,
            Reason = request.Reason,
            IsActive = true,
            StartedAt = DateTime.UtcNow,
            InitiatedBy = initiatedBy
        };

        _context.SecurityLockdowns.Add(lockdown);

        var securityEvent = new SecurityEvent
        {
            Id = Guid.NewGuid(),
            EventType = "SecurityLockdownInitiated",
            Severity = "Critical",
            DetailsJson = System.Text.Json.JsonSerializer.Serialize(new
            {
                request.LockdownType,
                request.DepartmentId,
                request.TargetRole,
                request.Reason
            }),
            Timestamp = DateTime.UtcNow
        };
        _context.SecurityEvents.Add(securityEvent);

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "SecurityLockdownInitiated",
            "SuperAdmin",
            "SecurityLockdown",
            lockdown.Id.ToString(),
            $"Initiated security lockdown: {request.LockdownType}",
            null,
            new { request.LockdownType, request.DepartmentId, request.TargetRole, request.Reason },
            initiatedBy);

        return new SecurityLockdownDto
        {
            Id = lockdown.Id,
            LockdownType = lockdown.LockdownType,
            DepartmentId = lockdown.DepartmentId,
            TargetRole = lockdown.TargetRole,
            Reason = lockdown.Reason,
            IsActive = lockdown.IsActive,
            StartedAt = lockdown.StartedAt,
            InitiatedBy = lockdown.InitiatedBy
        };
    }

    /// <summary>
    /// End a security lockdown.
    /// </summary>
    public async Task<bool> EndLockdownAsync(Guid lockdownId, string endedBy)
    {
        var lockdown = await _context.SecurityLockdowns.FindAsync(lockdownId);
        if (lockdown == null) return false;

        lockdown.IsActive = false;
        lockdown.EndedAt = DateTime.UtcNow;
        lockdown.EndedBy = endedBy;

        var securityEvent = new SecurityEvent
        {
            Id = Guid.NewGuid(),
            EventType = "SecurityLockdownEnded",
            Severity = "High",
            DetailsJson = System.Text.Json.JsonSerializer.Serialize(new { LockdownId = lockdownId }),
            Timestamp = DateTime.UtcNow
        };
        _context.SecurityEvents.Add(securityEvent);

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "SecurityLockdownEnded",
            "SuperAdmin",
            "SecurityLockdown",
            lockdownId.ToString(),
            $"Ended security lockdown",
            null,
            new { LockdownId = lockdownId },
            endedBy);

        return true;
    }

    /// <summary>
    /// Get all security events with filtering.
    /// </summary>
    public async Task<(List<SecurityEventDto> Events, int TotalCount)> GetSecurityEventsAsync(SecurityEventQuery query)
    {
        var queryable = _context.SecurityEvents.AsQueryable();

        if (!string.IsNullOrEmpty(query.EventType))
            queryable = queryable.Where(e => e.EventType == query.EventType);

        if (!string.IsNullOrEmpty(query.Severity))
            queryable = queryable.Where(e => e.Severity == query.Severity);

        if (!string.IsNullOrEmpty(query.EmployeeId))
            queryable = queryable.Where(e => e.EmployeeId == query.EmployeeId);

        if (query.FromDate.HasValue)
            queryable = queryable.Where(e => e.OccurredAt >= query.FromDate.Value);

        if (query.ToDate.HasValue)
            queryable = queryable.Where(e => e.OccurredAt <= query.ToDate.Value);

        if (query.IsAcknowledged.HasValue)
            queryable = queryable.Where(e => e.IsAcknowledged == query.IsAcknowledged.Value);

        var totalCount = await queryable.CountAsync();

        var events = await queryable
            .OrderByDescending(e => e.OccurredAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(e => new SecurityEventDto
            {
                Id = e.Id,
                EventType = e.EventType,
                Severity = e.Severity,
                EmployeeId = e.EmployeeId,
                IpAddress = e.IpAddress,
                UserAgent = e.UserAgent,
                RequestPath = e.RequestPath,
                Details = e.DetailsJson,
                IsAcknowledged = e.IsAcknowledged,
                AcknowledgedBy = e.AcknowledgedBy,
                AcknowledgedAt = e.AcknowledgedAt,
                Timestamp = e.OccurredAt,
                EmployeeName = e.Employee != null ? e.Employee.DisplayName : null
            })
            .ToListAsync();

        return (events, totalCount);
    }

    /// <summary>
    /// Acknowledge a security event.
    /// </summary>
    public async Task<bool> AcknowledgeSecurityEventAsync(Guid eventId, string? notes, string acknowledgedBy)
    {
        var securityEvent = await _context.SecurityEvents.FindAsync(eventId);
        if (securityEvent == null) return false;

        securityEvent.IsAcknowledged = true;
        securityEvent.AcknowledgedBy = acknowledgedBy;
        securityEvent.AcknowledgedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "SecurityEventAcknowledged",
            "SuperAdmin",
            "SecurityEvent",
            eventId.ToString(),
            $"Acknowledged security event: {securityEvent.EventType}",
            null,
            new { EventType = securityEvent.EventType, Notes = notes },
            acknowledgedBy);

        return true;
    }

    /// <summary>
    /// Get all active security lockdowns.
    /// </summary>
    public async Task<List<SecurityLockdownDto>> GetActiveLockdownsAsync()
    {
        var lockdowns = await _context.SecurityLockdowns
            .Where(l => l.IsActive)
            .Include(l => l.Department)
            .OrderByDescending(l => l.StartedAt)
            .ToListAsync();

        return lockdowns.Select(l => new SecurityLockdownDto
        {
            Id = l.Id,
            LockdownType = l.LockdownType,
            DepartmentId = l.DepartmentId,
            DepartmentName = l.Department?.Name,
            TargetRole = l.TargetRole,
            Reason = l.Reason,
            IsActive = l.IsActive,
            StartedAt = l.StartedAt,
            InitiatedBy = l.InitiatedBy
        }).ToList();
    }

    /// <summary>
    /// Get all active user sessions.
    /// </summary>
    public async Task<List<UserSessionDto>> GetActiveSessionsAsync()
    {
        var sessions = await _context.UserSessions
            .Where(s => s.IsActive)
            .Include(s => s.Employee)
            .OrderByDescending(s => s.LastActivityAt)
            .ToListAsync();

        return sessions.Select(s => new UserSessionDto
        {
            Id = s.Id,
            EmployeeId = s.EmployeeId,
            EmployeeName = s.Employee != null ? $"{s.Employee.DisplayName} {s.Employee}" : null,
            IpAddress = s.IpAddress,
            UserAgent = s.UserAgent,
            StartedAt = s.StartedAt,
            LastActivityAt = s.LastActivityAt,
            ExpiresAt = s.ExpiresAt,
            IsActive = s.IsActive
        }).ToList();
    }

    /// <summary>
    /// Get active sessions for a specific user.
    /// </summary>
    public async Task<List<UserSessionDto>> GetUserSessionsAsync(string employeeId)
    {
        var sessions = await _context.UserSessions
            .Where(s => s.EmployeeId == employeeId)
            .OrderByDescending(s => s.LastActivityAt)
            .Take(50)
            .ToListAsync();

        return sessions.Select(s => new UserSessionDto
        {
            Id = s.Id,
            EmployeeId = s.EmployeeId,
            IpAddress = s.IpAddress,
            UserAgent = s.UserAgent,
            StartedAt = s.StartedAt,
            LastActivityAt = s.LastActivityAt,
            ExpiresAt = s.ExpiresAt,
            IsActive = s.IsActive
        }).ToList();
    }
}
