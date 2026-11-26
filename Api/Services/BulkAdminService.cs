using Microsoft.EntityFrameworkCore;
using WACAMS.Api.Data;
using WACAMS.Api.DTOs;
using WACAMS.Core.Entities;

namespace WACAMS.Api.Services;

/// <summary>
/// Service for bulk administration operations.
/// </summary>
public class BulkAdminService
{
    private readonly WacamDbContext _context;
    private readonly AuditService _auditService;

    public BulkAdminService(WacamDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    /// <summary>
    /// Bulk assign role to multiple users.
    /// </summary>
    public async Task<BulkOperationResultDto> BulkAssignRoleAsync(
        List<string> employeeIds,
        string roleName,
        string initiatedBy)
    {
        var result = new BulkOperationResultDto
        {
            OperationType = "RoleAssignment",
            TotalRecords = employeeIds.Count,
            Status = "Processing",
            InitiatedBy = initiatedBy,
            StartedAt = DateTime.UtcNow
        };

        try
        {
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == roleName);

            if (role == null)
            {
                result.Status = "Failed";
                result.ErrorMessage = $"Role '{roleName}' not found";
                return result;
            }

            var successCount = 0;
            var errorCount = 0;
            var errors = new List<string>();

            foreach (var employeeId in employeeIds)
            {
                try
                {
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.EmployeeId == employeeId);

                    if (user == null)
                    {
                        errors.Add($"User {employeeId} not found");
                        errorCount++;
                        continue;
                    }

                    var existingRole = await _context.UserRoles
                        .FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);

                    if (existingRole != null)
                    {
                        errors.Add($"User {employeeId} already has role {roleName}");
                        errorCount++;
                        continue;
                    }

                    var userRole = new UserRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = user.Id,
                        RoleId = role.Id,
                        AssignedAt = DateTime.UtcNow
                    };

                    _context.UserRoles.Add(userRole);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"User {employeeId}: {ex.Message}");
                    errorCount++;
                }
            }

            await _context.SaveChangesAsync();

            result.SuccessCount = successCount;
            result.ErrorCount = errorCount;
            result.Status = "Completed";
            result.ErrorMessage = errors.Any() ? string.Join("; ", errors.Take(10)) : null;
            result.CompletedAt = DateTime.UtcNow;

            await _auditService.LogActionAsync(
            initiatedBy,
            "BulkRoleAssignment",
            "UserRole",
            role.Id.ToString(),
            $"Bulk assigned role '{roleName}' to {successCount} users",
            System.Text.Json.JsonSerializer.Serialize(new { roleName, result.TotalRecords, successCount, errorCount }));
        }
        catch (Exception ex)
        {
            result.Status = "Failed";
            result.ErrorMessage = ex.Message;
            result.CompletedAt = DateTime.UtcNow;
        }

        return result;
    }

    /// <summary>
    /// Bulk remove role from multiple users.
    /// </summary>
    public async Task<BulkOperationResultDto> BulkRemoveRoleAsync(
        List<string> employeeIds,
        string roleName,
        string initiatedBy)
    {
        var result = new BulkOperationResultDto
        {
            OperationType = "RoleRemoval",
            TotalRecords = employeeIds.Count,
            Status = "Processing",
            InitiatedBy = initiatedBy,
            StartedAt = DateTime.UtcNow
        };

        try
        {
            var role = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == roleName);

            if (role == null)
            {
                result.Status = "Failed";
                result.ErrorMessage = $"Role '{roleName}' not found";
                return result;
            }

            var successCount = 0;
            var errorCount = 0;
            var errors = new List<string>();

            foreach (var employeeId in employeeIds)
            {
                try
                {
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.EmployeeId == employeeId);

                    if (user == null)
                    {
                        errors.Add($"User {employeeId} not found");
                        errorCount++;
                        continue;
                    }

                    var userRole = await _context.UserRoles
                        .FirstOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);

                    if (userRole == null)
                    {
                        errors.Add($"User {employeeId} does not have role {roleName}");
                        errorCount++;
                        continue;
                    }

                    _context.UserRoles.Remove(userRole);
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"User {employeeId}: {ex.Message}");
                    errorCount++;
                }
            }

            await _context.SaveChangesAsync();

            result.SuccessCount = successCount;
            result.ErrorCount = errorCount;
            result.Status = "Completed";
            result.ErrorMessage = errors.Any() ? string.Join("; ", errors.Take(10)) : null;
            result.CompletedAt = DateTime.UtcNow;

            await _auditService.LogActionAsync(
            initiatedBy,
            "BulkRoleRemoval",
            "UserRole",
            role.Id.ToString(),
            $"Bulk removed role '{roleName}' from {successCount} users",
            System.Text.Json.JsonSerializer.Serialize(new { roleName, result.TotalRecords, successCount, errorCount }));
        }
        catch (Exception ex)
        {
            result.Status = "Failed";
            result.ErrorMessage = ex.Message;
            result.CompletedAt = DateTime.UtcNow;
        }

        return result;
    }

    /// <summary>
    /// Bulk assign department to multiple users.
    /// </summary>
    public async Task<BulkOperationResultDto> BulkAssignDepartmentAsync(
        List<string> employeeIds,
        string departmentName,
        string initiatedBy)
    {
        var result = new BulkOperationResultDto
        {
            OperationType = "DepartmentAssignment",
            TotalRecords = employeeIds.Count,
            Status = "Processing",
            InitiatedBy = initiatedBy,
            StartedAt = DateTime.UtcNow
        };

        try
        {
            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Name == departmentName);

            if (department == null)
            {
                result.Status = "Failed";
                result.ErrorMessage = $"Department '{departmentName}' not found";
                return result;
            }

            var successCount = 0;
            var errorCount = 0;
            var errors = new List<string>();

            foreach (var employeeId in employeeIds)
            {
                try
                {
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.EmployeeId == employeeId);

                    if (user == null)
                    {
                        errors.Add($"User {employeeId} not found");
                        errorCount++;
                        continue;
                    }

                    User.Employee!.DepartmentId = department.Id;
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"User {employeeId}: {ex.Message}");
                    errorCount++;
                }
            }

            await _context.SaveChangesAsync();

            result.SuccessCount = successCount;
            result.ErrorCount = errorCount;
            result.Status = "Completed";
            result.ErrorMessage = errors.Any() ? string.Join("; ", errors.Take(10)) : null;
            result.CompletedAt = DateTime.UtcNow;

            await _auditService.LogActionAsync(
            initiatedBy,
            "BulkDepartmentAssignment",
            "User",
            department.Id.ToString(),
            $"Bulk assigned department '{departmentName}' to {successCount} users",
            System.Text.Json.JsonSerializer.Serialize(new { departmentName, result.TotalRecords, successCount, errorCount }));
        }
        catch (Exception ex)
        {
            result.Status = "Failed";
            result.ErrorMessage = ex.Message;
            result.CompletedAt = DateTime.UtcNow;
        }

        return result;
    }

    /// <summary>
    /// Bulk activate user accounts.
    /// </summary>
    public async Task<BulkOperationResultDto> BulkActivateUsersAsync(
        List<string> employeeIds,
        string initiatedBy)
    {
        var result = new BulkOperationResultDto
        {
            OperationType = "AccountActivation",
            TotalRecords = employeeIds.Count,
            Status = "Processing",
            InitiatedBy = initiatedBy,
            StartedAt = DateTime.UtcNow
        };

        try
        {
            var successCount = 0;
            var errorCount = 0;
            var errors = new List<string>();

            foreach (var employeeId in employeeIds)
            {
                try
                {
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.EmployeeId == employeeId);

                    if (user == null)
                    {
                        errors.Add($"User {employeeId} not found");
                        errorCount++;
                        continue;
                    }

                    user.IsActive = true;
                    user.IsLocked = false;
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"User {employeeId}: {ex.Message}");
                    errorCount++;
                }
            }

            await _context.SaveChangesAsync();

            result.SuccessCount = successCount;
            result.ErrorCount = errorCount;
            result.Status = "Completed";
            result.ErrorMessage = errors.Any() ? string.Join("; ", errors.Take(10)) : null;
            result.CompletedAt = DateTime.UtcNow;

            await _auditService.LogActionAsync(
            initiatedBy,
            "BulkAccountActivation",
            "User",
            "Bulk",
            $"Bulk activated {successCount} user accounts",
            System.Text.Json.JsonSerializer.Serialize(new { result.TotalRecords, successCount, errorCount }));
        }
        catch (Exception ex)
        {
            result.Status = "Failed";
            result.ErrorMessage = ex.Message;
            result.CompletedAt = DateTime.UtcNow;
        }

        return result;
    }

    /// <summary>
    /// Bulk deactivate user accounts.
    /// </summary>
    public async Task<BulkOperationResultDto> BulkDeactivateUsersAsync(
        List<string> employeeIds,
        string initiatedBy)
    {
        var result = new BulkOperationResultDto
        {
            OperationType = "AccountDeactivation",
            TotalRecords = employeeIds.Count,
            Status = "Processing",
            InitiatedBy = initiatedBy,
            StartedAt = DateTime.UtcNow
        };

        try
        {
            var successCount = 0;
            var errorCount = 0;
            var errors = new List<string>();

            foreach (var employeeId in employeeIds)
            {
                try
                {
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.EmployeeId == employeeId);

                    if (user == null)
                    {
                        errors.Add($"User {employeeId} not found");
                        errorCount++;
                        continue;
                    }

                    user.IsActive = false;
                    user.IsLocked = true;
                    successCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"User {employeeId}: {ex.Message}");
                    errorCount++;
                }
            }

            await _context.SaveChangesAsync();

            result.SuccessCount = successCount;
            result.ErrorCount = errorCount;
            result.Status = "Completed";
            result.ErrorMessage = errors.Any() ? string.Join("; ", errors.Take(10)) : null;
            result.CompletedAt = DateTime.UtcNow;

            await _auditService.LogActionAsync(
            initiatedBy,
            "BulkAccountDeactivation",
            "User",
            "Bulk",
            $"Bulk deactivated {successCount} user accounts",
            System.Text.Json.JsonSerializer.Serialize(new { result.TotalRecords, successCount, errorCount }));
        }
        catch (Exception ex)
        {
            result.Status = "Failed";
            result.ErrorMessage = ex.Message;
            result.CompletedAt = DateTime.UtcNow;
        }

        return result;
    }

    /// <summary>
    /// Import users from Excel/CSV file.
    /// </summary>
    public async Task<BulkImportResultDto> ImportUsersAsync(Stream fileStream, string fileName, string initiatedBy)
    {
        var result = new BulkImportResultDto
        {
            ImportId = Guid.NewGuid(),
            ImportType = "UserImport",
            FileName = fileName,
            Status = "Processing",
            StartedAt = DateTime.UtcNow
        };

        try
        {
            // Placeholder: Actual Excel parsing would go here
            // For now, return a completed result with zero records
            result.TotalRows = 0;
            result.SuccessCount = 0;
            result.ErrorCount = 0;
            result.Status = "Completed";
            result.CompletedAt = DateTime.UtcNow;

            await _auditService.LogActionAsync(
            initiatedBy,
            "BulkUserImport",
            "User",
            "Bulk",
            $"Bulk import from file '{fileName}'",
            System.Text.Json.JsonSerializer.Serialize(new { fileName, result.TotalRows, result.SuccessCount, result.ErrorCount }));
        }
        catch (Exception ex)
        {
            result.Status = "Failed";
            result.Errors.Add(new BulkImportError { Row = 0, Field = null, Message = ex.Message });
            result.CompletedAt = DateTime.UtcNow;
        }

        return result;
    }

    /// <summary>
    /// Get bulk import history.
    /// </summary>
    public async Task<List<BulkImportHistoryDto>> GetImportHistoryAsync(int limit = 50)
    {
        var history = await _context.BulkImportHistories
            .OrderByDescending(h => h.StartedAt)
            .Take(limit)
            .ToListAsync();

        return history.Select(h => new BulkImportHistoryDto
        {
            ImportId = h.ImportId,
            ImportType = h.ImportType,
            FileName = h.FileName,
            TotalRows = h.TotalRows,
            SuccessCount = h.SuccessCount,
            ErrorCount = h.ErrorCount,
            Status = h.Status,
            StartedAt = h.StartedAt,
            CompletedAt = h.CompletedAt,
            InitiatedBy = h.InitiatedBy
        }).ToList();
    }
}
