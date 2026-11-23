using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Wacam.Api.DTOs;
using Wacam.Api.Middleware;
using Wacam.Api.Services;

namespace Wacam.Api.Controllers;

/// <summary>
/// Department CRUD — update targets, get hierarchy.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class DepartmentsController : ControllerBase
{
    private readonly WacamDbContext _context;
    private readonly AuditService _auditService;
    private readonly ILogger<DepartmentsController> _logger;

    public DepartmentsController(WacamDbContext context, AuditService auditService, ILogger<DepartmentsController> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/departments — List all departments with hierarchy.
    /// Returns top-level departments (ParentId = null) with their children.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(List<DepartmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get()
    {
        var depts = await _context.Departments.ToListAsync();
        var result = new List<DepartmentDto>();

        foreach (var dept in depts)
        {
            var parentName = string.Empty;
            if (dept.ParentId.HasValue)
            {
                var parent = depts.FirstOrDefault(d => d.Id == dept.ParentId);
                parentName = parent?.Name ?? string.Empty;
            }

            result.Add(new DepartmentDto
            {
                Id = dept.Id,
                Name = dept.Name,
                Description = dept.Description,
                ParentId = dept.ParentId,
                ParentName = parentName,
                TargetAttendancePercent = dept.TargetAttendancePercent,
                CreatedAt = dept.CreatedAt,
                UpdatedAt = dept.UpdatedAt
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// POST /api/departments — Create a new department.
    /// Admin/SuperAdmin only.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(DepartmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest request)
    {
        // Validate parent exists if provided
        if (request.ParentId.HasValue)
        {
            var parentExists = await _context.Departments.AnyAsync(d => d.Id == request.ParentId.Value);
            if (!parentExists)
                return BadRequest(new { error = "InvalidParent", message = $"Parent department with ID {request.ParentId.Value} not found." });
        }

        var dept = new Department
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            ParentId = request.ParentId,
            TargetAttendancePercent = request.TargetAttendancePercent,
            CreatedAt = DateTime.UtcNow
        };

        _context.Departments.Add(dept);
        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(
            actorId: User.FindFirst("employeeId")?.Value,
            action: "CreateDepartment",
            entityType: "Department",
            entityId: dept.Id.ToString(),
            httpEndpoint: "POST /api/departments",
            changesJson: $"{{\"Name\":\"{dept.Name}\",\"TargetAttendancePercent\":{dept.TargetAttendancePercent}}}");

        _logger.LogInformation("Department created: {Name} (Id={Id})", dept.Name, dept.Id);

        return CreatedAtAction(nameof(Get), new { }, MapToDto(dept));
    }

    /// <summary>
    /// PUT /api/departments/{id} — Update a department.
    /// Admin/SuperAdmin only.
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(DepartmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDepartmentRequest request)
    {
        var dept = await _context.Departments.FindAsync(id);
        if (dept is null)
            return NotFound(new { error = "NotFound", message = $"Department with ID {id} not found." });

        if (request.ParentId.HasValue)
        {
            var parentExists = await _context.Departments.AnyAsync(d => d.Id == request.ParentId.Value);
            if (!parentExists)
                return BadRequest(new { error = "InvalidParent", message = $"Parent department with ID {request.ParentId.Value} not found." });
        }

        // Prevent circular reference
        if (request.ParentId.HasValue && request.ParentId.Value == id)
            return BadRequest(new { error = "CircularReference", message = "A department cannot be its own parent." });

        if (request.Name != null)
            dept.Name = request.Name.Trim();
        if (request.Description != null)
            dept.Description = request.Description.Trim();
        if (request.ParentId.HasValue)
            dept.ParentId = request.ParentId.Value;
        if (request.TargetAttendancePercent.HasValue)
            dept.TargetAttendancePercent = request.TargetAttendancePercent.Value;

        dept.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(
            actorId: User.FindFirst("employeeId")?.Value,
            action: "UpdateDepartment",
            entityType: "Department",
            entityId: id.ToString(),
            httpEndpoint: $"PUT /api/departments/{id}",
            changesJson: $"{{\"UpdatedAt\":\"{dept.UpdatedAt:yyyy-MM-dd HH:mm}\"}}");

        return Ok(MapToDto(dept));
    }

    /// <summary>
    /// DELETE /api/departments/{id} — Delete a department (only if it has no employees).
    /// Admin/SuperAdmin only.
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        var dept = await _context.Departments
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dept is null)
            return NotFound(new { error = "NotFound", message = $"Department with ID {id} not found." });

        if (dept.Employees?.Any() == true)
            return Conflict(new { error = "HasEmployees", message = "Cannot delete department with assigned employees." });

        _context.Departments.Remove(dept);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Department deleted: Id={Id}, Name={Name}", id, dept.Name);

        return Ok(new { message = $"Department '{dept.Name}' deleted successfully." });
    }

    private static DepartmentDto MapToDto(Department dept)
    {
        return new DepartmentDto
        {
            Id = dept.Id,
            Name = dept.Name,
            Description = dept.Description,
            ParentId = dept.ParentId,
            ParentName = null,
            TargetAttendancePercent = dept.TargetAttendancePercent,
            CreatedAt = dept.CreatedAt,
            UpdatedAt = dept.UpdatedAt
        };
    }
}
