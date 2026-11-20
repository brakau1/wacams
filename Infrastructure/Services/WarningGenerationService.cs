using Microsoft.EntityFrameworkCore;
using WACAMS.Core.Constants;
using WACAMS.Core.Entities;
using WACAMS.Core.Enums;
using WACAMS.Core.Interfaces;
using WACAMS.Core.Services;
using WACAMS.Infrastructure.Data;

namespace WACAMS.Infrastructure.Services;

/// <summary>
/// Domain service implementing IWarningGenerationService.
/// Generates progressive disciplinary warning letters.
/// ALL methods enforce the rule that a warning can ONLY be generated after an
/// AttendanceException has status "Investigated" or "Closed". If no such exception exists,
/// the method returns null and does not create any warning letter.
/// </summary>
public class WarningGenerationService : IWarningGenerationService
{
    private readonly WacamContext _context;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IEscalationService _escalationService;

    public WarningGenerationService(
        WacamContext context,
        IEmployeeRepository employeeRepository,
        IEscalationService escalationService)
    {
        _context = context;
        _employeeRepository = employeeRepository;
        _escalationService = escalationService;
    }

    /// <inheritdoc />
    public async Task<WarningLetter?> GenerateCounselling(
        string employeeId,
        string reason,
        string issuedBy,
        CancellationToken cancellationToken = default)
    {
        if (!await VerifyInvestigationComplete(employeeId, cancellationToken))
            return null;

        var now = DateTime.UtcNow;
        var warning = new WarningLetter
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            Type = WarningType.Counselling,
            DateIssued = now,
            Reason = reason,
            IssuedBy = issuedBy,
            Signed = false,
            SignedFileName = null,
            SignedFilePath = null,
            Acknowledgements = null,
            NextReviewDate = now.AddDays(30)
        };

        await _context.WarningLetters.AddAsync(warning, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return warning;
    }

    /// <inheritdoc />
    public async Task<WarningLetter?> GenerateVerbalWarning(
        string employeeId,
        string reason,
        IEnumerable<WarningLetter> priorWarnings,
        string issuedBy,
        CancellationToken cancellationToken = default)
    {
        if (!await VerifyInvestigationComplete(employeeId, cancellationToken))
            return null;

        // Check threshold: 5+ exceptions this month OR existing active Counselling
        var thresholds = await GetThresholdStatus(employeeId, cancellationToken);
        if (!thresholds.ShouldGenerate && !priorWarnings.Any(w => w.Type == WarningType.Counselling))
            return null;

        var now = DateTime.UtcNow;
        var warning = new WarningLetter
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            Type = WarningType.VerbalWarning,
            DateIssued = now,
            Reason = reason,
            IssuedBy = issuedBy,
            Signed = false,
            SignedFileName = null,
            SignedFilePath = null,
            Acknowledgements = null,
            NextReviewDate = now.AddDays(30)
        };

        await _context.WarningLetters.AddAsync(warning, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return warning;
    }

    /// <inheritdoc />
    public async Task<WarningLetter?> GenerateWrittenWarning(
        string employeeId,
        string reason,
        IEnumerable<WarningLetter> priorWarnings,
        string issuedBy,
        CancellationToken cancellationToken = default)
    {
        if (!await VerifyInvestigationComplete(employeeId, cancellationToken))
            return null;

        // Check threshold: 7+ exceptions this month OR existing active Verbal Warning
        var thresholds = await GetThresholdStatus(employeeId, cancellationToken);
        if (!thresholds.ShouldGenerate && !priorWarnings.Any(w => w.Type == WarningType.VerbalWarning))
            return null;

        var now = DateTime.UtcNow;
        var warning = new WarningLetter
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            Type = WarningType.WrittenWarning,
            DateIssued = now,
            Reason = reason,
            IssuedBy = issuedBy,
            Signed = false,
            SignedFileName = null,
            SignedFilePath = null,
            Acknowledgements = null,
            NextReviewDate = now.AddDays(60)
        };

        await _context.WarningLetters.AddAsync(warning, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return warning;
    }

    /// <inheritdoc />
    public async Task<WarningLetter?> GenerateFinalWrittenWarning(
        string employeeId,
        string reason,
        IEnumerable<WarningLetter> priorWarnings,
        string issuedBy,
        CancellationToken cancellationToken = default)
    {
        if (!await VerifyInvestigationComplete(employeeId, cancellationToken))
            return null;

        // Check threshold: 9+ exceptions this month OR existing active Written Warning
        var thresholds = await GetThresholdStatus(employeeId, cancellationToken);
        if (!thresholds.ShouldGenerate && !priorWarnings.Any(w => w.Type == WarningType.WrittenWarning))
            return null;

        var now = DateTime.UtcNow;
        var warning = new WarningLetter
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            Type = WarningType.FinalWrittenWarning,
            DateIssued = now,
            Reason = reason,
            IssuedBy = issuedBy,
            Signed = false,
            SignedFileName = null,
            SignedFilePath = null,
            Acknowledgements = null,
            NextReviewDate = now.AddDays(90)
        };

        await _context.WarningLetters.AddAsync(warning, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Final Written Warning automatically triggers escalation check
        var shouldEscalate = await CheckEscalationTrigger(warning.Id, cancellationToken);
        if (shouldEscalate)
        {
            // Fetch employee to get manager hierarchy for escalation
            var employee = await _employeeRepository.GetByEmployeeIdAsync(employeeId, cancellationToken);
            if (employee != null && !string.IsNullOrEmpty(employee.ManagerId))
            {
                await _escalationService.CreateEscalation(
                    warning.Id,
                    employeeId,
                    employee.ManagerId!,
                    1,
                    $"Final Written Warning issued — immediate action required: {reason}",
                    "System",
                    cancellationToken);
            }
        }

        return warning;
    }

    /// <inheritdoc />
    public async Task<bool> CheckEscalationTrigger(
        Guid warningLetterId,
        CancellationToken cancellationToken = default)
    {
        var warning = await _context.WarningLetters.FindAsync([warningLetterId], cancellationToken);
        if (warning == null || warning.Type != WarningType.FinalWrittenWarning)
            return false;

        // Check if 24 hours have passed since issuance without action
        var timeSinceIssue = DateTime.UtcNow - warning.DateIssued;
        if (timeSinceIssue.TotalHours < Core.Constants.WarningThresholds.EscalationActionWindowHours)
            return false;

        // Check if any escalations already exist for this warning
        var existingEscalations = await _context.Escalations
            .AnyAsync(e => e.WarningLetterId == warningLetterId, cancellationToken);

        // Only trigger if no escalations yet and warning is still unsigned
        return !existingEscalations && !warning.Signed;
    }

    #region Private Helpers

    /// <summary>
    /// Verifies that at least one AttendanceException for the employee has status
    /// "Investigated" or "Closed" — required precondition for generating any warning.
    /// </summary>
    private async Task<bool> VerifyInvestigationComplete(string employeeId, CancellationToken cancellationToken)
    {
        return await _context.AttendanceExceptions.AsNoTracking()
            .AnyAsync(ae => ae.EmployeeId == employeeId &&
                           (ae.InvestigationStatus == InvestigationStatus.Investigated ||
                            ae.InvestigationStatus == InvestigationStatus.Closed),
                       cancellationToken);
    }

    /// <summary>
    /// Checks monthly exception count thresholds against current month.
    /// Returns ShouldGenerate=true if the count meets the threshold for any warning level.
    /// </summary>
    private async Task<(bool ShouldGenerate, int ExceptionCount)> GetThresholdStatus(
        string employeeId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var count = await _context.AttendanceExceptions.AsNoTracking()
            .Where(ae => ae.EmployeeId == employeeId && ae.Date >= monthStart && ae.Date <= monthEnd)
            .CountAsync(cancellationToken);

        var shouldGenerate = count >= WarningThresholds.CounsellingExceptionCount;
        return (shouldGenerate, count);
    }

    #endregion
}
