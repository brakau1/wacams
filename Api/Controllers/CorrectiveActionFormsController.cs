using Microsoft.AspNetCore.Mvc;
using WACAMS.Core.Disciplinary.Models;
using WACAMS.Core.Disciplinary.Repositories;
using WACAMS.Core.Disciplinary.Services;

namespace WACAMS.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CorrectiveActionFormsController : ControllerBase
{
    private readonly ICorrectiveActionFormService _formService;
    private readonly ICorrectiveActionRepository _correctiveActionRepository;
    private readonly ILogger<CorrectiveActionFormsController> _logger;

    public CorrectiveActionFormsController(
        ICorrectiveActionFormService formService,
        ICorrectiveActionRepository correctiveActionRepository,
        ILogger<CorrectiveActionFormsController> logger)
    {
        _formService = formService;
        _correctiveActionRepository = correctiveActionRepository;
        _logger = logger;
    }

    /// <summary>
    /// Generate corrective action form PDF and return as file download
    /// </summary>
    [HttpGet("{actionId}/download")]
    public async Task<IActionResult> DownloadForm(Guid actionId)
    {
        try
        {
            var action = await _correctiveActionRepository.GetByIdAsync(actionId);
            if (action == null)
            {
                return NotFound($"Corrective action {actionId} not found");
            }

            // TODO: Get employee details from employee service
            var employeeName = $"Employee {action.EmployeeId}";
            var department = "Department";
            var position = "Position";
            var issuedByManager = $"Manager {action.IssuedBy}";
            var managerPosition = "Manager";
            var followUpDate = action.DueDate ?? DateTime.UtcNow.AddDays(30);

            var pdfBytes = await _formService.GenerateFormAsync(
                action,
                employeeName,
                department,
                position,
                issuedByManager,
                managerPosition,
                followUpDate);

            var fileName = $"CorrectiveAction_{action.EmployeeId}_{action.IssuedAt:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating corrective action form for {ActionId}", actionId);
            return StatusCode(500, "Error generating form");
        }
    }

    /// <summary>
    /// Generate corrective action form PDF and upload to Azure Storage
    /// </summary>
    [HttpPost("{actionId}/generate")]
    public async Task<IActionResult> GenerateAndUploadForm(Guid actionId)
    {
        try
        {
            var action = await _correctiveActionRepository.GetByIdAsync(actionId);
            if (action == null)
            {
                return NotFound($"Corrective action {actionId} not found");
            }

            // TODO: Get employee details from employee service
            var employeeName = $"Employee {action.EmployeeId}";
            var department = "Department";
            var position = "Position";
            var issuedByManager = $"Manager {action.IssuedBy}";
            var managerPosition = "Manager";
            var followUpDate = action.DueDate ?? DateTime.UtcNow.AddDays(30);

            var fileUrl = await _formService.GenerateAndUploadFormAsync(
                action,
                employeeName,
                department,
                position,
                issuedByManager,
                managerPosition,
                followUpDate);

            return Ok(new { fileUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating and uploading corrective action form for {ActionId}", actionId);
            return StatusCode(500, "Error generating form");
        }
    }

    /// <summary>
    /// Generate form with custom parameters
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateFormWithParams([FromBody] GenerateFormRequest request)
    {
        try
        {
            var action = await _correctiveActionRepository.GetByIdAsync(request.ActionId);
            if (action == null)
            {
                return NotFound($"Corrective action {request.ActionId} not found");
            }

            var fileUrl = await _formService.GenerateAndUploadFormAsync(
                action,
                request.EmployeeName,
                request.Department,
                request.Position,
                request.IssuedByManager,
                request.ManagerPosition,
                request.FollowUpDate);

            return Ok(new { fileUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating corrective action form");
            return StatusCode(500, "Error generating form");
        }
    }
}

public class GenerateFormRequest
{
    public Guid ActionId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string IssuedByManager { get; set; } = string.Empty;
    public string ManagerPosition { get; set; } = string.Empty;
    public DateTime FollowUpDate { get; set; }
}
