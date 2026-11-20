using Microsoft.EntityFrameworkCore;
using Wacam.Api.Data;
using Wacam.Api.DTOs;
using WACAMS.Core.Entities;

namespace Wacam.Api.Services;

/// <summary>
/// Create/update/investigate exceptions, status transitions, audit log.
/// Status flow: Pending → Investigated → Closed
/// </summary>
public class ExceptionService
{
    private readonly WacamDbContext _context;
    private readonly AuditService _auditService;
    private readonly DepartmentAuthorizationService _departmentAuth;
    private readonly ILogger<ExceptionService> _logger;

    public ExceptionService(
        WacamDbContext context,
        AuditService auditService,
        DepartmentAuthorizationService departmentAuth,
        ILogger<ExceptionService> logger)
    {
        _context = context;
        _auditService = auditService;
        _departmentAuth = departmentAuth;
        _logger = logger;
    }

    /// <summary>
    /// List exceptions with filtering by employeeId, date range, type, status, departmentId.
    /// Supports pagination.
    /// </summary>
    public async Task<(List<AttendanceExceptionDto> Exceptions, int TotalCount)> ListAsync(
        string? employeeId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? exceptionType = null,
        string? status = null,
        int? departmentId = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _context.AttendanceExceptions
            .Include(ae => ae.Employee)
                .ThenInclude(e => e!.Department)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(employeeId))
            query = query.Where(ae => ae.EmployeeId == employeeId);

        if (startDate.HasValue)
            query = query.Where(ae => ae.Date >= startDate.Value.Date);

        if (endDate.HasValue)
            query = query.Where(ae => ae.Date <= endDate.Value.Date.AddDays(1).AddTicks(-1));

        if (!string.IsNullOrWhiteSpace(exceptionType))
        {
            if (Enum.TryParse<WACAMS.Core.Enums.ExceptionType>(exceptionType, true, out var parsedType))
                query = query.Where(ae => ae.ExceptionType == parsedType);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<WACAMS.Core.Enums.InvestigationStatus>(status, true, out var parsedStatus))
                query = query.Where(ae => ae.InvestigationStatus == parsedStatus);
        }

        if (departmentId.HasValue)
            query = query.Where(ae => ae.Employee!.DepartmentId == departmentId.Value);

        var totalCount = await query.CountAsync();

        var exceptions = await query
            .OrderByDescending(ae => ae.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (exceptions.Select(MapToDto).ToList(), totalCount);
    }

    /// <summary>
    /// Create a new attendance exception manually or automatically.
    /// Valid types: LateArrival, EarlyDeparture, UnplannedAbsenteeism, Awol,
    /// SickLeave, FamilyResponsibilityLeave, ShiftCancellation, WorkshopAttendance,
    /// Training, MissingClockIn, MissingClockOut, UnscheduledClockIn, UnscheduledClockOut
    /// </summary>
    public async Task<AttendanceExceptionDto> CreateAsync(CreateExceptionRequest request)
    {
        // Validate employee exists
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId);
        if (employee is null)
            throw new NotFoundException($"Employee '{request.EmployeeId}' not found.");

        // Validate exception type
        var validTypes = new[]
        {
            "LateArrival", "EarlyDeparture", "UnplannedAbsenteeism", "Awol",
            "SickLeave", "FamilyResponsibilityLeave", "ShiftCancellation",
            "WorkshopAttendance", "Training", "MissingClockIn", "MissingClockOut",
            "UnscheduledClockIn", "UnscheduledClockOut"
        };

        if (!validTypes.Contains(request.ExceptionType))
            throw new ArgumentException($"Invalid exception type: '{request.ExceptionType}'. Must be one of: {string.Join(", ", validTypes)}");

        // Parse exception type
        if (!Enum.TryParse<WACAMS.Core.Enums.ExceptionType>(request.ExceptionType, true, out var exceptionTypeEnum))
            throw new ArgumentException($"Invalid exception type: '{request.ExceptionType}'.");

        // Check for duplicate exception on same date/type/employee
        var existing = await _context.AttendanceExceptions.AnyAsync(
            ae => ae.EmployeeId == request.EmployeeId &&
                  ae.Date.Date == request.Date.Date &&
                  ae.ExceptionType == exceptionTypeEnum);

        if (existing)
            throw new ConflictException($"Exception '{request.ExceptionType}' already recorded for {request.EmployeeId} on {request.Date:yyyy-MM-dd}.");

        var exception = new AttendanceException
        {
            EmployeeId = request.EmployeeId,
            Date = request.Date.Date,
            ExceptionType = exceptionTypeEnum,
            ExpectedClockIn = request.ExpectedClockIn,
            ActualClockIn = request.ActualClockIn,
            ExpectedClockOut = request.ExpectedClockOut,
            ActualClockOut = request.ActualClockOut,
            MinutesLate = request.MinutesLate,
            MinutesEarly = request.MinutesEarly,
            LostProductionMinutes = request.LostProductionMinutes,
            InvestigationStatus = WACAMS.Core.Enums.InvestigationStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        _context.AttendanceExceptions.Add(exception);
        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(
            actorId: null,
            action: "CreateException",
            entityType: "AttendanceException",
            entityId: exception.Id.ToString(),
            httpEndpoint: "POST /api/exceptions",
            changesJson: $"{{\"EmployeeId\":\"{exception.EmployeeId}\",\"Date\":\"{exception.Date:yyyy-MM-dd}\",\"Type\":\"{exception.ExceptionType}\",\"Status\":\"{exception.InvestigationStatus}\"}}");

        _logger.LogInformation("Exception created: Type={Type}, Employee={EmployeeId}, Date={Date}",
            exception.ExceptionType, exception.EmployeeId, exception.Date);

        return MapToDto(exception, employee);
    }

    /// <summary>
    /// Auto-create exception (called by AttendanceService for automatic detection).
    /// Skips duplicate check to avoid circular logic.
    /// </summary>
    public async Task CreateAutoExceptionAsync(CreateExceptionRequest request)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId);
        if (employee is null) return;

        if (!Enum.TryParse<WACAMS.Core.Enums.ExceptionType>(request.ExceptionType, true, out var exceptionTypeEnum))
        {
            _logger.LogWarning("Invalid exception type for auto-create: {Type}", request.ExceptionType);
            return;
        }

        var exception = new AttendanceException
        {
            EmployeeId = request.EmployeeId,
            Date = request.Date.Date,
            ExceptionType = exceptionTypeEnum,
            ExpectedClockIn = request.ExpectedClockIn,
            ActualClockIn = request.ActualClockIn,
            ExpectedClockOut = request.ExpectedClockOut,
            ActualClockOut = request.ActualClockOut,
            MinutesLate = request.MinutesLate,
            MinutesEarly = request.MinutesEarly,
            LostProductionMinutes = request.LostProductionMinutes,
            InvestigationStatus = WACAMS.Core.Enums.InvestigationStatus.Open,
            CreatedAt = DateTime.UtcNow
        };

        _context.AttendanceExceptions.Add(exception);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Auto-created exception: Type={Type}, Employee={EmployeeId}, Date={Date}",
            exception.ExceptionType, exception.EmployeeId, exception.Date);
    }

    /// <summary>
    /// Investigate an exception — transitions from Pending → Investigated.
    /// Must have access rights (Supervisor, Admin, SuperAdmin, HR).
    /// </summary>
    public async Task<AttendanceExceptionDto> InvestigateAsync(int exceptionId, InvestigateExceptionRequest request, string investigatedBy)
    {
        var exception = await _context.AttendanceExceptions
            .Include(ae => ae.Employee)
            .FirstOrDefaultAsync(ae => ae.Id == exceptionId);

        if (exception is null)
            throw new NotFoundException($"Exception with ID {exceptionId} not found.");

        if (exception.InvestigationStatus != WACAMS.Core.Enums.InvestigationStatus.Open)
            throw new InvalidOperationException($"Exception is already '{exception.InvestigationStatus}'. Cannot investigate.");

        exception.InvestigationStatus = WACAMS.Core.Enums.InvestigationStatus.Investigated;
        exception.InvestigationNotes = request.InvestigationNotes?.Trim();
        exception.InvestigatedBy = investigatedBy;
        exception.InvestigatedAt = DateTime.UtcNow;
        exception.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(
            actorId: investigatedBy,
            action: "InvestigateException",
            entityType: "AttendanceException",
            entityId: exceptionId.ToString(),
            httpEndpoint: $"PUT /api/exceptions/{exceptionId}/investigate",
            changesJson: $"{{\"OldStatus\":\"Pending\",\"NewStatus\":\"Investigated\",\"Investigator\":\"{investigatedBy}\"}}");

        _logger.LogInformation("Exception investigated: Id={Id}, Employee={EmployeeId}, Status→Investigated",
            exceptionId, exception.EmployeeId);

        return MapToDto(exception, exception.Employee!);
    }

    /// <summary>
    /// Bulk investigate multiple exceptions at once.
    /// </summary>
    public async Task<int> BulkInvestigateAsync(List<int> exceptionIds, InvestigateExceptionRequest request, string investigatedBy)
    {
        var exceptions = await _context.AttendanceExceptions
            .Where(ae => exceptionIds.Contains(ae.Id) && ae.InvestigationStatus == WACAMS.Core.Enums.InvestigationStatus.Open)
            .ToListAsync();

        var count = 0;
        foreach (var exc in exceptions)
        {
            exc.InvestigationStatus = WACAMS.Core.Enums.InvestigationStatus.Investigated;
            exc.InvestigationNotes = request.InvestigationNotes?.Trim();
            exc.InvestigatedBy = investigatedBy;
            exc.InvestigatedAt = DateTime.UtcNow;
            exc.UpdatedAt = DateTime.UtcNow;
            count++;
        }

        if (count > 0)
            await _context.SaveChangesAsync();

        if (count > 0)
        {
            await _auditService.LogActionAsync(
                actorId: investigatedBy,
                action: "BulkInvestigateExceptions",
                entityType: "AttendanceException",
                entityId: string.Join(",", exceptionIds),
                httpEndpoint: "PUT /api/exceptions/bulk-investigate",
                changesJson: $"{{\"Count\":{count}}}");
        }

        return count;
    }

    /// <summary>
    /// Close an investigated exception.
    /// Transitions: Investigated → Closed.
    /// </summary>
    public async Task<AttendanceExceptionDto> CloseAsync(int exceptionId, string closedBy)
    {
        var exception = await _context.AttendanceExceptions
            .Include(ae => ae.Employee)
            .FirstOrDefaultAsync(ae => ae.Id == exceptionId);

        if (exception is null)
            throw new NotFoundException($"Exception with ID {exceptionId} not found.");

        if (exception.InvestigationStatus != WACAMS.Core.Enums.InvestigationStatus.Investigated)
            throw new InvalidOperationException($"Cannot close exception with status '{exception.InvestigationStatus}'. Must be 'Investigated' first.");

        exception.InvestigationStatus = WACAMS.Core.Enums.InvestigationStatus.Closed;
        exception.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(
            actorId: closedBy,
            action: "CloseException",
            entityType: "AttendanceException",
            entityId: exceptionId.ToString(),
            httpEndpoint: $"PUT /api/exceptions/{exceptionId}/close",
            changesJson: $"{{\"OldStatus\":\"Investigated\",\"NewStatus\":\"Closed\"}}");

        _logger.LogInformation("Exception closed: Id={Id}", exceptionId);

        return MapToDto(exception, exception.Employee!);
    }

    private static AttendanceExceptionDto MapToDto(AttendanceException exception, Employee employee)
    {
        return new AttendanceExceptionDto
        {
            Id = exception.Id,
            EmployeeId = exception.EmployeeId,
            DisplayName = employee.DisplayName,
            DepartmentName = employee.Department?.Name ?? string.Empty,
            Date = exception.Date,
            ExceptionType = exception.ExceptionType,
            ExpectedClockIn = exception.ExpectedClockIn,
            ActualClockIn = exception.ActualClockIn,
            ExpectedClockOut = exception.ExpectedClockOut,
            ActualClockOut = exception.ActualClockOut,
            MinutesLate = exception.MinutesLate,
            MinutesEarly = exception.MinutesEarly,
            LostProductionMinutes = exception.LostProductionMinutes,
            InvestigationStatus = exception.InvestigationStatus,
            InvestigationNotes = exception.InvestigationNotes,
            InvestigatedBy = exception.InvestigatedBy,
            InvestigatedAt = exception.InvestigatedAt,
            CreatedAt = exception.CreatedAt
        };
    }
}
