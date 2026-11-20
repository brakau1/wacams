using Microsoft.AspNetCore.Mvc;
using WACAMS.Core.Escalations.Interfaces;
using WACAMS.Core.Escalations.Models;

namespace WACAMS.Api.Controllers;

/// <summary>
/// API controller for escalation workflow management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EscalationsController : ControllerBase
{
    private readonly IEscalationService _escalationService;
    private readonly ILogger<EscalationsController> _logger;

    public EscalationsController(
        IEscalationService escalationService,
        ILogger<EscalationsController> logger)
    {
        _escalationService = escalationService;
        _logger = logger;
    }

    /// <summary>
    /// Get escalation by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Escalation>> GetEscalation(Guid id)
    {
        try
        {
            var escalation = await _escalationService.GetEscalationByIdAsync(id);
            return Ok(escalation);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get escalations by exception ID
    /// </summary>
    [HttpGet("exception/{exceptionId}")]
    public async Task<ActionResult<IEnumerable<Escalation>>> GetEscalationsByException(Guid exceptionId)
    {
        var escalations = await _escalationService.GetEscalationsByExceptionAsync(exceptionId);
        return Ok(escalations);
    }

    /// <summary>
    /// Create a new escalation
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Escalation>> CreateEscalation([FromBody] CreateEscalationRequest request)
    {
        try
        {
            var escalation = await _escalationService.CreateEscalationAsync(
                request.ExceptionId,
                request.CreatedBy);

            return CreatedAtAction(nameof(GetEscalation), new { id = escalation.Id }, escalation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating escalation for exception {ExceptionId}", request.ExceptionId);
            return StatusCode(500, new { message = "Error creating escalation", error = ex.Message });
        }
    }

    /// <summary>
    /// Acknowledge an escalation
    /// </summary>
    [HttpPost("{id}/acknowledge")]
    public async Task<ActionResult<Escalation>> AcknowledgeEscalation(
        Guid id,
        [FromBody] AcknowledgeEscalationRequest request)
    {
        try
        {
            var escalation = await _escalationService.AcknowledgeEscalationAsync(id, request.AcknowledgedBy);
            return Ok(escalation);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging escalation {EscalationId}", id);
            return StatusCode(500, new { message = "Error acknowledging escalation", error = ex.Message });
        }
    }

    /// <summary>
    /// Mark action taken on an escalation
    /// </summary>
    [HttpPost("{id}/action-taken")]
    public async Task<ActionResult<Escalation>> MarkActionTaken(
        Guid id,
        [FromBody] MarkActionTakenRequest request)
    {
        try
        {
            var escalation = await _escalationService.MarkActionTakenAsync(
                id,
                request.ActionNotes,
                request.UpdatedBy);
            return Ok(escalation);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking action taken on escalation {EscalationId}", id);
            return StatusCode(500, new { message = "Error marking action taken", error = ex.Message });
        }
    }

    /// <summary>
    /// Escalate to next level
    /// </summary>
    [HttpPost("{id}/escalate")]
    public async Task<ActionResult<Escalation>> Escalate(
        Guid id,
        [FromBody] EscalateRequest request)
    {
        try
        {
            var escalation = await _escalationService.EscalateAsync(id, request.EscalatedBy);
            return Ok(escalation);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error escalating {EscalationId}", id);
            return StatusCode(500, new { message = "Error escalating", error = ex.Message });
        }
    }

    /// <summary>
    /// Upload document for an escalation
    /// </summary>
    [HttpPost("{id}/upload-document")]
    public async Task<ActionResult<Escalation>> UploadDocument(
        Guid id,
        [FromBody] UploadDocumentRequest request)
    {
        try
        {
            var escalation = await _escalationService.UploadDocumentAsync(
                id,
                request.DocumentUrl,
                request.UploadedBy);
            return Ok(escalation);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document for escalation {EscalationId}", id);
            return StatusCode(500, new { message = "Error uploading document", error = ex.Message });
        }
    }

    /// <summary>
    /// Resolve an escalation
    /// </summary>
    [HttpPost("{id}/resolve")]
    public async Task<ActionResult<Escalation>> ResolveEscalation(
        Guid id,
        [FromBody] ResolveEscalationRequest request)
    {
        try
        {
            var escalation = await _escalationService.ResolveEscalationAsync(id, request.ResolvedBy);
            return Ok(escalation);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving escalation {EscalationId}", id);
            return StatusCode(500, new { message = "Error resolving escalation", error = ex.Message });
        }
    }
}

// Request DTOs
public record CreateEscalationRequest(Guid ExceptionId, string CreatedBy);
public record AcknowledgeEscalationRequest(string AcknowledgedBy);
public record MarkActionTakenRequest(string ActionNotes, string UpdatedBy);
public record EscalateRequest(string EscalatedBy);
public record UploadDocumentRequest(string DocumentUrl, string UploadedBy);
public record ResolveEscalationRequest(string ResolvedBy);
