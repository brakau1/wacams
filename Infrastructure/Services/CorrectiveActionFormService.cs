using Microsoft.Extensions.Logging;
using WACAMS.Core.Disciplinary.Models;
using WACAMS.Core.Disciplinary.Services;
using WACAMS.Core.Interfaces;
using Infrastructure.Services.Pdf;

namespace WACAMS.Infrastructure.Services;

/// <summary>
/// Service implementation for generating corrective action form PDFs
/// </summary>
public class CorrectiveActionFormService : ICorrectiveActionFormService
{
    private readonly IFileStorageService _storageService;
    private readonly ILogger<CorrectiveActionFormService> _logger;
    private readonly CorrectiveActionFormPdfGenerator _pdfGenerator;
    private readonly CorrectiveActionNarrativeGenerator _narrativeGenerator;

    public CorrectiveActionFormService(
        IFileStorageService storageService,
        ILogger<CorrectiveActionFormService> logger)
    {
        _storageService = storageService;
        _logger = logger;
        _pdfGenerator = new CorrectiveActionFormPdfGenerator();
        _narrativeGenerator = new CorrectiveActionNarrativeGenerator();
    }

    public async Task<string> GenerateAndUploadFormAsync(
        CorrectiveAction action,
        string employeeName,
        string department,
        string position,
        string issuedByManager,
        string managerPosition,
        DateTime followUpDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating corrective action form for action {ActionId}", action.Id);

            // Generate the PDF
            var pdfBytes = await GenerateFormAsync(
                action,
                employeeName,
                department,
                position,
                issuedByManager,
                managerPosition,
                followUpDate);

            // Generate filename
            var fileName = GenerateFileName(action, employeeName);

            // Upload to storage
            var fileUrl = await _storageService.UploadFileAsync(
                pdfBytes,
                "corrective-action-forms",
                fileName,
                "application/pdf",
                cancellationToken);

            _logger.LogInformation("Corrective action form uploaded successfully: {FileUrl}", fileUrl);

            return fileUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate and upload corrective action form for action {ActionId}", action.Id);
            throw;
        }
    }

    public async Task<byte[]> GenerateFormAsync(
        CorrectiveAction action,
        string employeeName,
        string department,
        string position,
        string issuedByManager,
        string managerPosition,
        DateTime followUpDate)
    {
        try
        {
            _logger.LogInformation("Generating corrective action form PDF for action {ActionId}", action.Id);

            // Generate narratives
            var (situation, reasons, causes, actionTaken, sanction) = _narrativeGenerator.GenerateNarratives(
                action,
                employeeName,
                department);

            // Create form data
            var formData = new CorrectiveActionFormData
            {
                // Employee Information
                EmployeeId = action.EmployeeId,
                EmployeeName = employeeName,
                Department = department,
                Position = position,
                
                // Incident Details
                IncidentDate = action.IssuedAt,
                OffenceType = GetOffenceTypeDescription(action.OffenceType),
                IncidentDescription = situation,
                
                // Disciplinary Information
                DisciplinaryLevel = GetDisciplinaryLevelDescription(action.Level),
                OffenceNumber = action.OffenceNumber,
                IssuedDate = action.IssuedAt,
                IssuedBy = issuedByManager,
                IssuerPosition = managerPosition,
                
                // Form Sections
                SituationDescription = situation,
                EmployeeReasons = reasons,
                RootCauses = causes,
                ActionTaken = actionTaken,
                RecommendedSanction = sanction,
                FollowUpDate = followUpDate
            };

            // Generate PDF
            var pdfBytes = _pdfGenerator.Generate(formData);

            _logger.LogInformation("Corrective action form PDF generated successfully: {Size} bytes", pdfBytes.Length);

            return await Task.FromResult(pdfBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate corrective action form PDF for action {ActionId}", action.Id);
            throw;
        }
    }

    private string GenerateFileName(CorrectiveAction action, string employeeName)
    {
        // Sanitize employee name for filename
        var sanitizedEmployeeName = SanitizeFileName(employeeName);
        var date = action.IssuedAt.ToString("yyyyMMdd");
        var offenceType = action.OffenceType.ToString();
        
        return $"CorrectiveAction_{sanitizedEmployeeName}_{date}_{offenceType}.pdf";
    }

    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Replace(" ", "_");
    }

    private string GetOffenceTypeDescription(OffenceType offenceType)
    {
        return offenceType switch
        {
            OffenceType.LateArrival => "Late Arrival",
            OffenceType.EarlyDeparture => "Early Departure",
            OffenceType.UnplannedAbsence => "Unplanned Absence",
            OffenceType.AWOL => "Absent Without Leave (AWOL)",
            OffenceType.UnauthorizedAbsence => "Unauthorized Absence",
            OffenceType.MissingClockIn => "Missing Clock In",
            OffenceType.MissingClockOut => "Missing Clock Out",
            _ => offenceType.ToString()
        };
    }

    private string GetDisciplinaryLevelDescription(DisciplinaryLevel level)
    {
        return level switch
        {
            DisciplinaryLevel.Counselling => "Counselling",
            DisciplinaryLevel.VerbalWarning => "Verbal Warning",
            DisciplinaryLevel.WrittenWarning => "Written Warning",
            DisciplinaryLevel.FinalWrittenWarning => "Final Written Warning",
            DisciplinaryLevel.DismissalHearing => "Dismissal Hearing",
            DisciplinaryLevel.Dismissal => "Dismissal",
            _ => level.ToString()
        };
    }
}
