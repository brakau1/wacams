using Microsoft.AspNetCore.Authorization;
using WACAMS.Core.Exceptions;
using Microsoft.AspNetCore.Mvc;
using WACAMS.Core.Exceptions;
using WACAMS.Api.DTOs;
using WACAMS.Core.Exceptions;
using WACAMS.Api.Services;
using WACAMS.Core.Exceptions;
using WACAMS.Api.Middleware;
using WACAMS.Core.Exceptions;
using ErrorResponse = WACAMS.Api.DTOs.ErrorResponse;
using WACAMS.Core.Exceptions;

namespace WACAMS.Api.Controllers;

/// <summary>
/// Employee CRUD + search + bulk upload + hierarchy view.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EmployeesController : ControllerBase
{
    private readonly EmployeeService _employeeService;
    private readonly ILogger<EmployeesController> _logger;

    public EmployeesController(EmployeeService employeeService, ILogger<EmployeesController> logger)
    {
        _employeeService = employeeService;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/employees — List/search employees with filters and pagination.
    /// Query params: searchKeyword, departmentId, isActive, role, page, pageSize.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof((List<EmployeeDto>, int)), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] string? searchKeyword,
        [FromQuery] Guid? departmentId,
        [FromQuery] bool? isActive,
        [FromQuery] string? role,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var requestingEmployeeId = User.FindFirst("employeeId")?.Value;
        var (employees, totalCount) = await _employeeService.ListAsync(searchKeyword, departmentId, isActive, role, requestingEmployeeId, page, pageSize);
        return Ok(new { employees, totalCount, page, pageSize });
    }

    /// <summary>
    /// GET /api/employees/{id} — Get employee detail with full hierarchy (manager + subordinates).
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(EmployeeHierarchyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _employeeService.GetByIdAsync(id);
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = "NotFound", message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/employees/by-id/{employeeId} — Get employee by master key (EmployeeId).
    /// </summary>
    [HttpGet("by-id/{employeeId}")]
    [Authorize]
    [ProducesResponseType(typeof(EmployeeHierarchyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByEmployeeId([FromRoute] string employeeId)
    {
        try
        {
            var result = await _employeeService.GetByEmployeeIdAsync(employeeId);
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = "NotFound", message = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/employees/bulk — Bulk upload employees from a list.
    /// Admin/SuperAdmin only.
    /// </summary>
    [HttpPost("bulk")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(BulkResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> BulkUpload([FromBody] BulkUploadRequest request)
    {
        var result = await _employeeService.BulkUploadAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// PUT /api/employees/{id} — Update an existing employee.
    /// Admin/Supervisor/SuperAdmin only.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin,Supervisor")]
    [ProducesResponseType(typeof(EmployeeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployeeRequest request)
    {
        try
        {
            var result = await _employeeService.UpdateAsync(id, request);
            return Ok(result);
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = "NotFound", message = ex.Message });
        }
        catch (ConflictException ex)
        {
            return Conflict(new { error = "Conflict", message = ex.Message });
        }
    }

    /// <summary>
    /// DELETE /api/employees/{id}/disable — Soft-delete (disable) an employee.
    /// Admin/SuperAdmin only.
    /// </summary>
    [HttpDelete("{id:guid}/disable")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Disable(Guid id)
    {
        try
        {
            await _employeeService.DisableAsync(id);
            return Ok(new { message = $"Employee {id} has been disabled." });
        }
        catch (NotFoundException ex)
        {
            return NotFound(new { error = "NotFound", message = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/employees/{employeeId}/hierarchy — Get the manager chain for an employee.
    /// </summary>
    [HttpGet("{employeeId}/hierarchy")]
    [Authorize]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHierarchy(string employeeId)
    {
        var chain = await _employeeService.GetManagerChainAsync(employeeId);
        return Ok(chain);
    }

    /// <summary>
    /// GET /api/employees/{employeeId}/subordinates — Get all direct subordinates of an employee.
    /// </summary>
    [HttpGet("{employeeId}/subordinates")]
    [Authorize]
    [ProducesResponseType(typeof(List<EmployeeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubordinates(string employeeId)
    {
        var subordinates = await _employeeService.GetSubordinatesAsync(employeeId);
        return Ok(subordinates);
    }
}
