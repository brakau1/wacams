using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wacam.Api.DTOs;
using Wacam.Api.Services;

namespace Wacam.Api.Controllers;

/// <summary>
/// SuperAdmin API endpoints for permission template management.
/// All operations require SuperAdmin role and are audit logged.
/// </summary>
[ApiController]
[Route("api/admin/permission-templates")]
[Authorize(Policy = "RequireSuperAdmin")]
public class PermissionTemplateController : ControllerBase
{
    private readonly PermissionTemplateService _templateService;
    private readonly ILogger<PermissionTemplateController> _logger;

    public PermissionTemplateController(
        PermissionTemplateService templateService,
        ILogger<PermissionTemplateController> logger)
    {
        _templateService = templateService;
        _logger = logger;
    }

    private string GetCurrentEmployeeId()
    {
        return User.FindFirst("employeeId")?.Value ?? "system";
    }

    /// <summary>
    /// GET /api/admin/permission-templates - Get all permission templates
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<PermissionTemplateDto>>> GetAllTemplates()
    {
        var templates = await _templateService.GetAllTemplatesAsync();
        return Ok(templates);
    }

    /// <summary>
    /// GET /api/admin/permission-templates/{id} - Get template by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<PermissionTemplateDto>> GetTemplateById(Guid id)
    {
        var template = await _templateService.GetTemplateByIdAsync(id);
        if (template == null)
        {
            return NotFound(new { message = $"Template {id} not found" });
        }
        return Ok(template);
    }

    /// <summary>
    /// POST /api/admin/permission-templates - Create a new permission template
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<PermissionTemplateDto>> CreateTemplate([FromBody] CreatePermissionTemplateRequest request)
    {
        try
        {
            var createdBy = GetCurrentEmployeeId();
            var template = await _templateService.CreateTemplateAsync(request, createdBy);
            return CreatedAtAction(nameof(GetTemplateById), new { id = template.Id }, template);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// PUT /api/admin/permission-templates/{id} - Update an existing permission template
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<PermissionTemplateDto>> UpdateTemplate(Guid id, [FromBody] UpdatePermissionTemplateRequest request)
    {
        try
        {
            var modifiedBy = GetCurrentEmployeeId();
            var template = await _templateService.UpdateTemplateAsync(id, request, modifiedBy);
            if (template == null)
            {
                return NotFound(new { message = $"Template {id} not found" });
            }
            return Ok(template);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/admin/permission-templates/{id}/clone - Clone an existing permission template
    /// </summary>
    [HttpPost("{id}/clone")]
    public async Task<ActionResult<PermissionTemplateDto>> CloneTemplate(Guid id, [FromBody] ClonePermissionTemplateRequest request)
    {
        try
        {
            var clonedBy = GetCurrentEmployeeId();
            var template = await _templateService.CloneTemplateAsync(id, request, clonedBy);
            if (template == null)
            {
                return NotFound(new { message = $"Template {id} not found" });
            }
            return CreatedAtAction(nameof(GetTemplateById), new { id = template.Id }, template);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/admin/permission-templates/{id}/deactivate - Deactivate a permission template
    /// </summary>
    [HttpPost("{id}/deactivate")]
    public async Task<IActionResult> DeactivateTemplate(Guid id)
    {
        try
        {
            var deactivatedBy = GetCurrentEmployeeId();
            var success = await _templateService.DeactivateTemplateAsync(id, deactivatedBy);
            if (!success)
            {
                return NotFound(new { message = $"Template {id} not found" });
            }
            return Ok(new { message = "Template deactivated successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/admin/permission-templates/{id}/assign - Assign template to employees
    /// </summary>
    [HttpPost("{id}/assign")]
    public async Task<IActionResult> AssignTemplate(Guid id, [FromBody] AssignTemplateRequest request)
    {
        try
        {
            var assignedBy = GetCurrentEmployeeId();
            var count = await _templateService.AssignTemplateAsync(id, request, assignedBy);
            return Ok(new { message = $"Template assigned to {count} employees", count });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/admin/permission-templates/{id}/versions - Get template version history
    /// </summary>
    [HttpGet("{id}/versions")]
    public async Task<ActionResult<List<PermissionTemplateVersionDto>>> GetTemplateVersions(Guid id)
    {
        var versions = await _templateService.GetTemplateVersionsAsync(id);
        return Ok(versions);
    }
}
