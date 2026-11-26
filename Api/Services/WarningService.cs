using Microsoft.EntityFrameworkCore;
using WACAMS.Core.Exceptions;
using WACAMS.Api.Data;
using WACAMS.Api.DTOs;
using WACAMS.Api.Middleware;
using WACAMS.Core.Entities;
using WACAMS.Core.Enums;

namespace WACAMS.Api.Services;

/// <summary>
/// Generate warnings (validate investigation status first), signed form upload handler.
/// Progressive discipline: Counselling ? VerbalWarning ? WrittenWarning ? FinalWrittenWarning
/// No disciplinary action allowed until exception status is "Investigated" or "Closed".
/// </summary>
public class WarningService
{
    private readonly WacamDbContext _context;
    private readonly AuditService _auditService;
    private readonly EscalationService _escalationService;
    private readonly NotificationService _notificationService;
    private readonly ILogger<WarningService> _logger;
    private readonly IWebHostEnvironment _env;

    public WarningService(WacamDbContext context, AuditService auditService, EscalationService escalationService,
        NotificationService notificationService, ILogger<WarningService> logger, IWebHostEnvironment env)
    {
        _context = context;
        _auditService = auditService;
        _escalationService = escalationService;
        _notificationService = notificationService;
        _logger = logger;
        _env = env;
    }

    /// <summary>
    /// List warnings with filtering by employeeId, departmentId, warningType, date range, acknowledged status.
    /// Supports pagination.
    /// </summary>
    public async Task<(List<WarningLetterDto> Warnings, int TotalCount)> ListAsync(
        string? employeeId = null,
        Guid? departmentId = null,
        string? warningType = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool? acknowledged = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _context.WarningLetters
            .Include(wl => wl.Employee)
                .ThenInclude(e => e!.Department)
            .Include(wl => wl.Exception)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(employeeId))
            query = query.Where(wl => wl.EmployeeId == employeeId);

        if (departmentId.HasValue)
            query = query.Where(wl => wl.Employee!.DepartmentId == departmentId.Value);

        if (!string.IsNullOrWhiteSpace(warningType))
            query = query.Where(wl => wl.Type.ToString() == warningType);

        if (startDate.HasValue)
            query = query.Where(wl => wl.DateIssued >= startDate.Value.Date);

        if (endDate.HasValue)
            query = query.Where(wl => wl.DateIssued <= endDate.Value.Date.AddDays(1).AddTicks(-1));

        if (acknowledged.HasValue)
            query = query.Where(wl => wl.Signed == acknowledged.Value);

        var totalCount = await query.CountAsync();

        var warnings = await query
            .OrderByDescending(wl => wl.DateIssued)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (warnings.Select(MapToDto).ToList(), totalCount);
    }

    /// <summary>
    /// Generate a warning letter from an attendedce exception.
    /// GUARD: Exception must have Status == "Investigated" or "Closed" before any warning can be created.
    /// Auto-triggers escalation when FinalWrittenWarning is generated.
    /// </summary>
    public async Task<WarningLetterDto> GenerateWarningAsync(GenerateWarningRequest request, string issuedBy)
    {
        // -- VALIDATION: Check exception exists and is investigated/closed --
        var exception = await _context.AttendanceExceptions
            .FirstOrDefaultAsync(ae => ae.Id == request.ExceptionId && ae.EmployeeId == request.EmployeeId);

        if (exception is null)
            throw new NotFoundException($"Exception with ID {request.ExceptionId} not found for employee {request.EmployeeId}.");

        // Core business rule: NO disciplinary action until investigation is complete
        if (exception.InvestigationStatus != InvestigationStatus.Investigated && 
            exception.InvestigationStatus != InvestigationStatus.Closed)
            throw new InvalidOperationException(
                $"Cannot generate warning. Exception status is '{exception.InvestigationStatus}'. " +
                "Disciplinary action requires the exception to be in 'Investigated' or 'Closed' status.");

        // Validate warning type
        var validTypes = new[] { "Counselling", "VerbalWarning", "WrittenWarning", "FinalWrittenWarning" };
        if (!validTypes.Contains(request.Type))
            throw new ArgumentException($"Invalid warning type: '{request.Type}'. Must be one of: {string.Join(", ", validTypes)}");

        // Validate progressive discipline order
        await ValidateProgressiveDisciplineAsync(request.EmployeeId, request.Type);

        // Get issuing manager info
        var issuer = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == issuedBy);
        var issuerName = issuer?.DisplayName ?? "System";

        var warningLetter = new WarningLetter
        {
            EmployeeId = request.EmployeeId,
            ExceptionId = request.ExceptionId,
            Type = Enum.Parse<WarningType>(request.Type),
            DateIssued = DateTime.UtcNow,
            Reason = request.Reason.Trim(),
            IssuedBy = issuedBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.WarningLetters.Add(warningLetter);
        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(
            performedBy: issuedBy,
            action: "GenerateWarning",
            entityType: "WarningLetter",
            entityId: warningLetter.Id.ToString(),
            httpEndpoint: "POST /api/warnings/generate",
            changesJson: $"{{\"EmployeeId\":\"{warningLetter.EmployeeId}\",\"Type\":\"{warningLetter.Type}\",\"ExceptionId\":{warningLetter.ExceptionId},\"IssuedBy\":\"{issuedBy}\"}}");

        _logger.LogInformation("Warning generated: Type={Type}, Employee={EmployeeId}, Id={Id}",
            warningLetter.Type, warningLetter.EmployeeId, warningLetter.Id);

        // -- AUTO-ESCALATION: When FinalWrittenWarning is generated --
        if (request.Type == "FinalWrittenWarning")
        {
            try
            {
                await _escalationService.AutoCreateEscalationAsync(warningLetter.Id, request.EmployeeId);
                _logger.LogInformation("Auto-escalation triggered for FinalWrittenWarning Id={Id}", warningLetter.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-create escalation for FinalWrittenWarning Id={Id}", warningLetter.Id);
            }
        }

        // Notify responsible parties
        try
        {
            await _notificationService.NotifyWarningGeneratedAsync(
                warningLetter.EmployeeId, warningLetter.Type.ToString(), warningLetter.Id, issuedBy);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send warning notification for EmployeeId={EmployeeId}", request.EmployeeId);
        }

        return MapToDto(warningLetter, exception, issuerName);
    }

    /// <summary>
    /// Upload a signed warning form (PDF) via multipart/form-data.
    /// Saves to wwwroot/signatures/{employeeId}_{warningId}_{timestamp}.pdf
    /// </summary>
    public async Task<string> UploadSignedFormAsync(Guid warningId, IFormFile file)
    {
        var warning = await _context.WarningLetters.FindAsync(warningId);
        if (warning is null)
            throw new NotFoundException($"Warning with ID {warningId} not found.");

        // Validate file
        if (file == null || file.Length == 0)
            throw new ArgumentException("No file uploaded.");

        if (file.ContentType != "application/pdf")
            throw new ArgumentException("Only PDF files are accepted for signed forms.");

        if (file.Length > 10 * 1024 * 1024) // 10MB limit
            throw new ArgumentException("File size exceeds 10MB limit.");

        // Create signatures directory if it doesn't exist
        var uploadsPath = Path.Combine(_env.ContentRootPath, "wwwroot", "signatures");
        Directory.CreateDirectory(uploadsPath);

        var fileName = $"{warning.EmployeeId}_{warningId}_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf";
        var fullPath = Path.Combine(uploadsPath, fileName);

        using var stream = new FileStream(fullPath, FileMode.Create);
        await file.CopyToAsync(stream);

        // Store relative path in DB
        var relativePath = $"/signatures/{fileName}";
        warning.SignedFilePath = relativePath;
        warning.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(
            performedBy: warning.IssuedBy,
            action: "UploadSignedForm",
            entityType: "WarningLetter",
            entityId: warningId.ToString(),
            httpEndpoint: $"POST /api/warnings/{warningId}/upload-signed",
            changesJson: $"{{\"FileName\":\"{fileName}\",\"FileSize\":{file.Length}}}");

        _logger.LogInformation("Signed form uploaded for WarningId={Id}: {FileName}", warningId, fileName);

        return relativePath;
    }

    /// <summary>
    /// Mark a warning as acknowledged by the employee.
    /// </summary>
    public async Task<WarningLetterDto> AcknowledgeAsync(Guid warningId, string employeeId)
    {
        var warning = await _context.WarningLetters
            .Include(wl => wl.Exception)
            .Include(wl => wl.Employee)
            .FirstOrDefaultAsync(wl => wl.Id == warningId && wl.EmployeeId == employeeId);

        if (warning is null)
            throw new NotFoundException($"Warning with ID {warningId} not found for employee {employeeId}.");

        warning.Signed = true;
        warning.SignedAt = DateTime.UtcNow;
        warning.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(
            performedBy: employeeId,
            action: "AcknowledgeWarning",
            entityType: "WarningLetter",
            entityId: warningId.ToString(),
            httpEndpoint: $"PUT /api/warnings/{warningId}/acknowledge",
            changesJson: "{\"Acknowledged\":true}");

        _logger.LogInformation("Warning acknowledged: Id={Id}, Employee={EmployeeId}", warningId, employeeId);

        return MapToDto(warning, warning.Exception!, warning.Employee!.DisplayName);
    }

    /// <summary>
    /// Count how many warnings of each type an employee has — used for progressive discipline validation.
    /// </summary>
    public async Task<Dictionary<string, int>> GetWarningCountsAsync(string employeeId)
    {
        var warnings = await _context.WarningLetters
            .Where(wl => wl.EmployeeId == employeeId)
            .GroupBy(wl => wl.Type)
            .Select(g => new { Type = g.Key.ToString(), Count = g.Count() })
            .ToListAsync();

        return warnings.ToDictionary(w => w.Type, w => w.Count);
    }

    private async Task ValidateProgressiveDisciplineAsync(string employeeId, string requestedType)
    {
        var counts = await GetWarningCountsAsync(employeeId);
        var counsellingCount = counts.GetValueOrDefault("Counselling", 0);
        var verbalCount = counts.GetValueOrDefault("VerbalWarning", 0);
        var writtenCount = counts.GetValueOrDefault("WrittenWarning", 0);

        switch (requestedType)
        {
            case "Counselling":
                // Can always issue counselling
                break;

            case "VerbalWarning":
                if (counsellingCount == 0)
                    throw new InvalidOperationException(
                        "A VerbalWarning requires at least one Counselling record on file.");
                break;

            case "WrittenWarning":
                if (verbalCount == 0)
                    throw new InvalidOperationException(
                        "A WrittenWarning requires at least one VerbalWarning record on file.");
                break;

            case "FinalWrittenWarning":
                if (writtenCount == 0)
                    throw new InvalidOperationException(
                        "A FinalWrittenWarning requires at least one WrittenWarning record on file.");
                break;
        }
    }

    private static WarningLetterDto MapToDto(WarningLetter warning, AttendanceException exception, string issuerName)
    {
        return new WarningLetterDto
        {
            Id = warning.Id,
            EmployeeId = warning.EmployeeId,
            DisplayName = warning.Employee?.DisplayName ?? string.Empty,
            DepartmentName = warning.Employee?.Department?.Name ?? string.Empty,
            ExceptionId = warning.ExceptionId,
            ExceptionType = exception.ExceptionType.ToString(),
            WarningType = warning.Type.ToString(),
            IssueDate = warning.DateIssued,
            Description = warning.Reason,
            IssuedBy = issuerName,
            SignedFormPath = warning.SignedFilePath,
            Acknowledged = warning.Signed,
            AcknowledgedAt = warning.SignedAt,
            CreatedAt = warning.CreatedAt
        };
    }
}
