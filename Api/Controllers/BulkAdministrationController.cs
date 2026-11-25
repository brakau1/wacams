using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WACAMS.Api.DTOs;
using WACAMS.Api.Services;

namespace WACAMS.Api.Controllers;

/// <summary>
/// SuperAdmin API endpoints for bulk administration operations.
/// All operations require SuperAdmin role and are audit logged.
/// </summary>
[ApiController]
[Route("api/admin/bulk")]
[Authorize(Policy = "RequireSuperAdmin")]
public class BulkAdministrationController : ControllerBase
{
    private readonly BulkAdminService _bulkAdminService;
    private readonly ILogger<BulkAdministrationController> _logger;

    public BulkAdministrationController(
        BulkAdminService bulkAdminService,
        ILogger<BulkAdministrationController> logger)
    {
        _bulkAdminService = bulkAdminService;
        _logger = logger;
    }

    private string GetCurrentEmployeeId()
    {
        return User.FindFirst("employeeId")?.Value ?? "system";
    }

    /// <summary>
    /// POST /api/admin/bulk/import-users - Bulk import users from Excel/CSV file
    /// </summary>
    [HttpPost("import-users")]
    public async Task<ActionResult<BulkImportResultDto>> ImportUsers(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded" });
            }

            var initiatedBy = GetCurrentEmployeeId();
            using var stream = file.OpenReadStream();
            var result = await _bulkAdminService.ImportUsersAsync(stream, file.FileName, initiatedBy);
            
            if (result.Status == "Failed")
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing users");
            return StatusCode(500, new { message = "Error importing users", error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/admin/bulk/assign-role - Bulk assign role to multiple users
    /// </summary>
    [HttpPost("assign-role")]
    public async Task<ActionResult<BulkOperationResultDto>> BulkAssignRole([FromBody] BulkRoleAssignmentRequest request)
    {
        try
        {
            var initiatedBy = GetCurrentEmployeeId();
            var result = await _bulkAdminService.BulkAssignRoleAsync(request.EmployeeIds, request.Role, initiatedBy);
            
            if (result.Status == "Failed")
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk assigning roles");
            return StatusCode(500, new { message = "Error bulk assigning roles", error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/admin/bulk/remove-role - Bulk remove role from multiple users
    /// </summary>
    [HttpPost("remove-role")]
    public async Task<ActionResult<BulkOperationResultDto>> BulkRemoveRole([FromBody] BulkRoleAssignmentRequest request)
    {
        try
        {
            var initiatedBy = GetCurrentEmployeeId();
            var result = await _bulkAdminService.BulkRemoveRoleAsync(request.EmployeeIds, request.Role, initiatedBy);
            
            if (result.Status == "Failed")
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk removing roles");
            return StatusCode(500, new { message = "Error bulk removing roles", error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/admin/bulk/assign-department - Bulk assign department to multiple users
    /// </summary>
    [HttpPost("assign-department")]
    public async Task<ActionResult<BulkOperationResultDto>> BulkAssignDepartment([FromBody] BulkDepartmentAssignmentRequest request)
    {
        try
        {
            var initiatedBy = GetCurrentEmployeeId();
            var result = await _bulkAdminService.BulkAssignDepartmentAsync(request.EmployeeIds, request.DepartmentName, initiatedBy);
            
            if (result.Status == "Failed")
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk assigning departments");
            return StatusCode(500, new { message = "Error bulk assigning departments", error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/admin/bulk/activate-users - Bulk activate user accounts
    /// </summary>
    [HttpPost("activate-users")]
    public async Task<ActionResult<BulkOperationResultDto>> BulkActivateUsers([FromBody] BulkOperationRequest request)
    {
        try
        {
            var initiatedBy = GetCurrentEmployeeId();
            var result = await _bulkAdminService.BulkActivateUsersAsync(request.EmployeeIds, initiatedBy);
            
            if (result.Status == "Failed")
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk activating users");
            return StatusCode(500, new { message = "Error bulk activating users", error = ex.Message });
        }
    }

    /// <summary>
    /// POST /api/admin/bulk/deactivate-users - Bulk deactivate user accounts
    /// </summary>
    [HttpPost("deactivate-users")]
    public async Task<ActionResult<BulkOperationResultDto>> BulkDeactivateUsers([FromBody] BulkOperationRequest request)
    {
        try
        {
            var initiatedBy = GetCurrentEmployeeId();
            var result = await _bulkAdminService.BulkDeactivateUsersAsync(request.EmployeeIds, initiatedBy);
            
            if (result.Status == "Failed")
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk deactivating users");
            return StatusCode(500, new { message = "Error bulk deactivating users", error = ex.Message });
        }
    }

    /// <summary>
    /// GET /api/admin/bulk/import-history - Get bulk import history
    /// </summary>
    [HttpGet("import-history")]
    public async Task<ActionResult<List<BulkImportHistoryDto>>> GetImportHistory([FromQuery] int limit = 50)
    {
        try
        {
            var history = await _bulkAdminService.GetImportHistoryAsync(limit);
            return Ok(history);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving import history");
            return StatusCode(500, new { message = "Error retrieving import history", error = ex.Message });
        }
    }
}
