using WACAMS.Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Wacam.Api.Data;
using Wacam.Api.DTOs;
using WACAMS.Core.Entities;
using WACAMS.Core.DTOs;
using WACAMS.Core.Enums;
using WACAMS.Core.Interfaces;

namespace Wacam.Api.Services;

/// <summary>
/// Hierarchical escalation following managerId chain, auto-create on final warning.
/// Each manager has 24 hours to act before auto-escalating to the next level.
/// </summary>
public class EscalationService
{
    private readonly WacamDbContext _context;
    private readonly AuditService _auditService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<EscalationService> _logger;

    public EscalationService(WacamDbContext context, AuditService auditService,
        INotificationService notificationService, ILogger<EscalationService> logger)
    {
        _context = context;
        _auditService = auditService;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// List escalations with filtering by employeeId, status, level, departmentId.
    /// Supports pagination.
    /// </summary>
    public async Task<(List<EscalationDto> Escalations, int TotalCount)> ListAsync(
        string? employeeId = null,
        string? status = null,
        int? level = null,
        int? departmentId = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _context.Escalations
            .Include(e => e.Employee)
                .ThenInclude(emp => emp!.Department)
            .Include(e => e.WarningLetter)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(employeeId))
            query = query.Where(e => e.EmployeeId == employeeId);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(e => e.Status == status);

        if (level.HasValue)
            query = query.Where(e => e.Level == level.Value);

        if (departmentId.HasValue)
            query = query.Where(e => e.Employee!.DepartmentId == departmentId.Value);

        var totalCount = await query.CountAsync();

        var escalations = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (escalations.Select(MapToDto).ToList(), totalCount);
    }

    /// <summary>
    /// Get the full escalation chain for an employee.
    /// Builds the chain from direct supervisor up through the management hierarchy.
    /// </summary>
    public async Task<EscalationChainResponse> GetEscalationChainAsync(string employeeId)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
        if (employee is null)
            throw new NotFoundException($"Employee '{employeeId}' not found.");

        var chain = new List<EscalationLevelDto>();
        var currentManagerId = employee.ManagerId;
        var level = 1;

        while (!string.IsNullOrEmpty(currentManagerId))
        {
            var manager = await _context.Employees.FirstOrDefaultAsync(
                m => m.EmployeeId == currentManagerId && m.IsActive);

            if (manager is null)
                break;

            chain.Add(new EscalationLevelDto
            {
                Level = level,
                ManagerId = manager.EmployeeId,
                ManagerName = manager.DisplayName,
                Role = manager.Role
            });

            currentManagerId = manager.ManagerId;
            level++;
        }

        return new EscalationChainResponse
        {
            EmployeeId = employeeId,
            DisplayName = employee.DisplayName,
            Chain = chain
        };
    }

    /// <summary>
    /// Auto-create an escalation when FinalWrittenWarning is generated.
    /// Creates escalation at Level 1 (direct supervisor).
    /// </summary>
    public async Task AutoCreateEscalationAsync(int warningLetterId, string employeeId)
    {
        var warningLetter = await _context.WarningLetters
            .Include(wl => wl.Employee)
            .FirstOrDefaultAsync(wl => wl.Id == warningLetterId);

        if (warningLetter is null || warningLetter.WarningType != "FinalWrittenWarning")
            return;

        var employee = warningLetter.Employee;
        if (employee?.ManagerId is null)
        {
            _logger.LogWarning("Cannot auto-create escalation for {EmployeeId} — no manager assigned.", employeeId);
            return;
        }

        // Check if an active escalation already exists for this warning
        var existing = await _context.Escalations.AnyAsync(
            e => e.WarningLetterId == warningLetterId);

        if (existing)
        {
            _logger.LogInformation("Escalation already exists for WarningLetterId={Id}, skipping auto-create.", warningLetterId);
            return;
        }

        var escalation = new Escalation
        {
            EmployeeId = employeeId,
            WarningLetterId = warningLetterId,
            Level = 1,
            ResponsibleManagerId = employee.ManagerId,
            Status = "Pending",
            Notes = $"Auto-created escalation due to FinalWrittenWarning (Id={warningLetterId}).",
            CreatedAt = DateTime.UtcNow
        };

        _context.Escalations.Add(escalation);
        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(
            actorId: null,
            action: "AutoEscalationCreated",
            entityType: "Escalation",
            entityId: escalation.Id.ToString(),
            httpEndpoint: "SYSTEM/AUTO-ESCALATION",
            changesJson: $"{{\"WarningLetterId\":{warningLetterId},\"EmployeeId\":\"{employeeId}\",\"Level\":1,\"ResponsibleManagerId\":\"{escalation.ResponsibleManagerId}\"}}");

        _logger.LogInformation("Auto-escalation created: Id={Id}, Level=1, Manager={ManagerId}",
            escalation.Id, escalation.ResponsibleManagerId);

        // Notify the responsible manager
        try
        {
            await SendEscalationNotificationAsync(escalation);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send escalation notification for EscalationId={Id}", escalation.Id);
        }
    }

    /// <summary>
    /// Progress an escalation — either acknowledge at current level or escalate to next level.
    /// If resolved, mark as Resolved. Otherwise increment Level and assign to next manager in chain.
    /// </summary>
    public async Task<EscalationDto> ProgressEscalationAsync(int escalationId, ProgressEscalationRequest request, string actedBy)
    {
        var escalation = await _context.Escalations
            .Include(e => e.Employee)
            .Include(e => e.WarningLetter)
            .FirstOrDefaultAsync(e => e.Id == escalationId);

        if (escalation is null)
            throw new NotFoundException($"Escalation with ID {escalationId} not found.");

        if (escalation.Status == "Resolved")
            throw new InvalidOperationException("Escalation is already resolved.");

        escalation.Notes = $"{escalation.Notes}{Environment.NewLine}[{DateTime.UtcNow:yyyy-MM-dd HH:mm}] {actedBy}: {request.Notes}".Trim();
        escalation.UpdatedAt = DateTime.UtcNow;

        // Check if we should resolve or escalate further
        var chain = await BuildFullChainAsync(escalation.EmployeeId);

        if (chain.Count <= escalation.Level)
        {
            // We've reached the top of the chain — resolve
            escalation.Status = "Resolved";
            escalation.ResolvedAt = DateTime.UtcNow;
        }
        else
        {
            // Move to next level
            escalation.Level = chain[escalation.Level].Level;
            escalation.ResponsibleManagerId = chain[escalation.Level].ManagerId;
            escalation.Status = "Pending";

            await SendEscalationNotificationAsync(escalation);
        }

        await _context.SaveChangesAsync();

        var oldLevel = escalation.Level - (escalation.Status == "Resolved" ? 0 : 1);
        var status = escalation.Status;
        await _auditService.LogActionAsync(
            actorId: actedBy,
            action: "ProgressEscalation",
            entityType: "Escalation",
            entityId: escalationId.ToString(),
            httpEndpoint: $"PUT /api/escalations/{escalationId}/progress",
            changesJson: $"{{\"OldLevel\":{oldLevel},\"NewLevel\":{escalation.Level},\"Status\":\"{status}\"}}");

        _logger.LogInformation("Escalation progressed: Id={Id}, Level={Level}, Status={Status}",
            escalationId, escalation.Level, escalation.Status);

        return MapToDto(escalation);
    }

    /// <summary>
    /// Scan for overdue escalations where the manager hasn't acted within 24 hours.
    /// Auto-escalates to the next level in the chain.
    /// This would be called by a scheduled background job.
    /// </summary>
    public async Task ScanOverdueEscalationsAsync()
    {
        var pendingEscalations = await _context.Escalations
            .Where(e => e.Status == "Pending" && e.CreatedAt <= DateTime.UtcNow.AddHours(-24))
            .ToListAsync();

        foreach (var esc in pendingEscalations)
        {
            try
            {
                var chain = await BuildFullChainAsync(esc.EmployeeId);

                if (chain.Count <= esc.Level)
                {
                    // Top of chain reached — resolve
                    esc.Status = "Resolved";
                    esc.ResolvedAt = DateTime.UtcNow;
                    esc.Notes += Environment.NewLine + "[AUTO] Reached end of escalation chain — resolved.";
                }
                else
                {
                    var nextLevel = chain[esc.Level];
                    esc.Level = nextLevel.Level;
                    esc.ResponsibleManagerId = nextLevel.ManagerId;
                    esc.Notes += Environment.NewLine + $"[AUTO] Escalated from Level {esc.Level - 1} to Level {nextLevel.Level} (Manager: {nextLevel.ManagerId}) due to 24-hour timeout.";
                }

                await _context.SaveChangesAsync();

                await _auditService.LogActionAsync(
                    actorId: "system",
                    action: "AutoEscalationTimeout",
                    entityType: "Escalation",
                    entityId: esc.Id.ToString(),
                    httpEndpoint: "SYSTEM/AUTO-TIMEOUT",
                    changesJson: $"{{\"Level\":{esc.Level},\"Status\":\"{esc.Status}\",\"ResponsibleManagerId\":\"{esc.ResponsibleManagerId}\"}}");

                _logger.LogInformation("Auto-escalated overdue EscalationId={Id} to Level {Level}", esc.Id, esc.Level);

                // Notify new responsible manager
                if (esc.Status != "Resolved")
                {
                    try
                    {
                        await SendEscalationNotificationAsync(esc);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to notify escalation for EscalationId={Id}", esc.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing overdue EscalationId={Id}", esc.Id);
            }
        }
    }

    private async Task<List<EscalationLevelDto>> BuildFullChainAsync(string employeeId)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
        if (employee is null) return new List<EscalationLevelDto>();

        var chain = new List<EscalationLevelDto>();
        var currentManagerId = employee.ManagerId;
        var level = 1;

        while (!string.IsNullOrEmpty(currentManagerId))
        {
            var manager = await _context.Employees.FirstOrDefaultAsync(
                m => m.EmployeeId == currentManagerId && m.IsActive);

            if (manager is null) break;

            chain.Add(new EscalationLevelDto
            {
                Level = level,
                ManagerId = manager.EmployeeId,
                ManagerName = manager.DisplayName,
                Role = manager.Role
            });

            currentManagerId = manager.ManagerId;
            level++;
        }

        return chain;
    }

    /// <summary>
    /// Send notification for an escalation to the responsible manager
    /// </summary>
    private async Task SendEscalationNotificationAsync(Escalation escalation)
    {
        try
        {
            // Get manager details
            var manager = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == escalation.ResponsibleManagerId);

            if (manager == null)
            {
                _logger.LogWarning("Manager {ManagerId} not found for escalation notification", escalation.ResponsibleManagerId);
                return;
            }

            // Get employee details
            var employee = await _context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EmployeeId == escalation.EmployeeId);

            if (employee == null)
            {
                _logger.LogWarning("Employee {EmployeeId} not found for escalation notification", escalation.EmployeeId);
                return;
            }

            var request = new CreateNotificationRequest
            {
                RecipientEmployeeId = escalation.ResponsibleManagerId,
                EventType = NotificationEventType.EscalationRequired,
                Subject = $"Escalation Level {escalation.Level} - {employee.DisplayName}",
                Body = $"An attendance issue has been escalated to Level {escalation.Level}. Employee: {employee.DisplayName}. Please review immediately.",
                Priority = NotificationPriority.High,
                RelatedEntityId = escalation.Id.ToString(),
                RelatedEntityType = "Escalation",
                TemplateParameters = new Dictionary<string, string>
                {
                    ["ManagerName"] = manager.DisplayName,
                    ["EmployeeName"] = employee.DisplayName,
                    ["EmployeeId"] = employee.EmployeeId,
                    ["DepartmentName"] = employee.Department?.Name ?? "Unknown",
                    ["EscalationLevel"] = escalation.Level.ToString(),
                    ["WarningType"] = escalation.WarningLetter?.WarningType ?? "Unknown",
                    ["AppUrl"] = "http://localhost:5173",
                    ["EscalationId"] = escalation.Id.ToString()
                }
            };

            await _notificationService.SendNotificationAsync(request);

            _logger.LogInformation("Notification sent for escalation {EscalationId}", escalation.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification for escalation {EscalationId}", escalation.Id);
        }
    }

    private static EscalationDto MapToDto(Escalation escalation)
    {
        var managerName = escalation.ResponsibleManagerId;
        if (!string.IsNullOrEmpty(escalation.ResponsibleManagerId))
        {
            var mgr = _context.Employees.AsNoTracking().FirstOrDefault(m => m.EmployeeId == escalation.ResponsibleManagerId);
            if (mgr != null) managerName = mgr.DisplayName;
        }

        return new EscalationDto
        {
            Id = escalation.Id,
            EmployeeId = escalation.EmployeeId,
            DisplayName = escalation.Employee?.DisplayName ?? string.Empty,
            WarningLetterId = escalation.WarningLetterId,
            WarningType = escalation.WarningLetter?.WarningType ?? string.Empty,
            Level = escalation.Level,
            ResponsibleManagerId = escalation.ResponsibleManagerId,
            ResponsibleManagerName = managerName,
            Status = escalation.Status,
            Notes = escalation.Notes,
            CreatedAt = escalation.CreatedAt,
            UpdatedAt = escalation.UpdatedAt,
            ResolvedAt = escalation.ResolvedAt
        };
    }
}
