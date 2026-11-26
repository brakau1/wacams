using Microsoft.EntityFrameworkCore;
using WACAMS.Core.Exceptions;
using WACAMS.Api.Data;
using WACAMS.Api.DTOs;
using WACAMS.Core.Entities;
using WACAMS.Api.Utils;
using WACAMS.Api.Middleware;

namespace WACAMS.Api.Services;

/// <summary>
/// CRUD operations, validation, manager hierarchy resolution, bulk import.
/// </summary>
public class EmployeeService
{
    private readonly WacamDbContext _context;
    private readonly AuditService _auditService;
    private readonly DepartmentAuthorizationService _departmentAuth;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(WacamDbContext context, AuditService auditService, DepartmentAuthorizationService departmentAuth, ILogger<EmployeeService> logger)
    {
        _context = context;
        _auditService = auditService;
        _departmentAuth = departmentAuth;
        _logger = logger;
    }

    /// <summary>
    /// List employees with filtering: departmentId, status, search keyword.
    /// Supports pagination via skip/take.
    /// </summary>
    public async Task<(List<EmployeeDto> Employees, int TotalCount)> ListAsync(
        string? searchKeyword = null,
        Guid? departmentId = null,
        bool? isActive = null,
        string? role = null,
        string? requestingEmployeeId = null,
        int page = 1,
        int pageSize = 50)
    {
        // Validate department access if requesting employee is provided
        if (!string.IsNullOrEmpty(requestingEmployeeId))
        {
            await _departmentAuth.ValidateDepartmentAccessAsync(requestingEmployeeId, departmentId);
        }

        var query = _context.Employees.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            var kw = searchKeyword.ToLowerInvariant();
            query = query.Where(e =>
                e.EmployeeId.Contains(kw) ||
                e.DisplayName.ToLowerInvariant().Contains(kw) ||
                e.Email.ToLowerInvariant().Contains(kw));
        }

        if (departmentId.HasValue)
            query = query.Where(e => e.DepartmentId == departmentId.Value);

        if (isActive.HasValue)
            query = query.Where(e => e.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(e => e.Role == role);

        var totalCount = await query.CountAsync();

        var employees = await query
            .OrderBy(e => e.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (employees.Select(MapToDto).ToList(), totalCount);
    }

    /// <summary>
    /// Get a single employee with full hierarchy info (manager + subordinates).
    /// </summary>
    public async Task<EmployeeHierarchyDto> GetByIdAsync(Guid id)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee is null)
            throw new NotFoundException($"Employee with ID {id} not found.");

        var hierarchy = BuildHierarchy(employee);
        return hierarchy;
    }

    /// <summary>
    /// Get employee by EmployeeId (master key) with hierarchy.
    /// </summary>
    public async Task<EmployeeHierarchyDto> GetByEmployeeIdAsync(string employeeId)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

        if (employee is null)
            throw new NotFoundException($"Employee with ID '{employeeId}' not found.");

        return BuildHierarchy(employee);
    }

    /// <summary>
    /// Create a new employee (used for programmatic creation — registration goes through AuthService).
    /// </summary>
    public async Task<EmployeeDto> CreateAsync(CreateEmployeeRequest request)
    {
        // Validate unique EmployeeId
        if (await _context.Employees.AnyAsync(e => e.EmployeeId == request.EmployeeId))
            throw new ConflictException($"Employee with ID '{request.EmployeeId}' already exists.");

        // Validate department
        var department = await _context.Departments.FindAsync(request.DepartmentId);
        if (department is null)
            throw new NotFoundException($"Department with ID {request.DepartmentId} not found.");

        // Validate manager
        if (!string.IsNullOrEmpty(request.ManagerId))
        {
            if (!await _context.Employees.AnyAsync(e => e.EmployeeId == request.ManagerId))
                throw new NotFoundException($"Manager with EmployeeId '{request.ManagerId}' not found.");
        }

        var employee = new Employee
        {
            EmployeeId = request.EmployeeId.Trim(),
            DisplayName = request.DisplayName.Trim(),
            Email = request.Email.ToLowerInvariant().Trim(),
            Phone = request.Phone?.Trim(),
            ManagerId = request.ManagerId?.Trim(),
            DepartmentId = request.DepartmentId,
            Role = request.Role,
            PasswordHash = PasswordHasher.HashPassword(request.Password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(action: "CreateEmployee",
            entityType: "Employee",
            entityId: employee.Id.ToString(),
            httpEndpoint: "POST /api/employees",
            changesJson: $"{{\"EmployeeId\":\"{employee.EmployeeId}\",\"DisplayName\":\"{employee.DisplayName}\"}}");

        _logger.LogInformation("Employee created: {EmployeeId} ({DisplayName})", employee.EmployeeId, employee.DisplayName);

        return MapToDto(employee);
    }

    /// <summary>
    /// Update an existing employee. Only non-sensitive fields can be changed via this method.
    /// Password changes go through AuthService.ChangePasswordAsync.
    /// </summary>
    public async Task<EmployeeDto> UpdateAsync(Guid id, UpdateEmployeeRequest request)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee is null)
            throw new NotFoundException($"Employee with ID {id} not found.");

        // Capture old values for audit
        var oldValues = new
        {
            employee.DisplayName,
            employee.Email,
            employee.Phone,
            employee.ManagerId,
            employee.DepartmentId,
            employee.Role,
            employee.IsActive
        };

        // Apply updates
        if (request.DisplayName != null)
            employee.DisplayName = request.DisplayName.Trim();
        if (request.Email != null)
            employee.Email = request.Email.ToLowerInvariant().Trim();
        if (request.Phone != null)
            employee.Phone = request.Phone.Trim();
        if (request.ManagerId != null)
        {
            // Validate manager exists
            if (!await _context.Employees.AnyAsync(e => e.EmployeeId == request.ManagerId))
                throw new NotFoundException($"Manager with EmployeeId '{request.ManagerId}' not found.");
            employee.ManagerId = request.ManagerId.Trim();
        }
        if (request.DepartmentId.HasValue)
        {
            if (!await _context.Departments.AnyAsync(d => d.Id == request.DepartmentId.Value))
                throw new NotFoundException($"Department with ID {request.DepartmentId.Value} not found.");
            employee.DepartmentId = request.DepartmentId.Value;
        }
        if (request.Role != null)
            employee.Role = request.Role;
        if (request.IsActive.HasValue)
            employee.IsActive = request.IsActive.Value;

        employee.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(action: "UpdateEmployee",
            entityType: "Employee",
            entityId: id.ToString(),
            httpEndpoint: $"PUT /api/employees/{id}",
            changesJson: SerializeChanges(oldValues, new
            {
                employee.DisplayName,
                employee.Email,
                employee.Phone,
                employee.ManagerId,
                employee.DepartmentId,
                employee.Role,
                employee.IsActive
            }));

        _logger.LogInformation("Employee updated: {EmployeeId}", employee.EmployeeId);

        return MapToDto(employee);
    }

    /// <summary>
    /// Bulk upload employees from a list. Returns count of created and errors.
    /// </summary>
    public async Task<BulkResult> BulkUploadAsync(BulkUploadRequest request)
    {
        var created = 0;
        var errors = new List<string>();
        var batch = new List<Employee>();

        foreach (var item in request.Employees)
        {
            try
            {
                if (await _context.Employees.AnyAsync(e => e.EmployeeId == item.EmployeeId))
                {
                    errors.Add($"Duplicate EmployeeId: {item.EmployeeId}");
                    continue;
                }

                var deptExists = await _context.Departments.AnyAsync(d => d.Id == item.DepartmentId);
                if (!deptExists)
                {
                    errors.Add($"Invalid DepartmentId {item.DepartmentId} for {item.DisplayName}");
                    continue;
                }

                if (!string.IsNullOrEmpty(item.ManagerId))
                {
                    if (!await _context.Employees.AnyAsync(e => e.EmployeeId == item.ManagerId))
                    {
                        errors.Add($"ManagerId {item.ManagerId} not found for {item.DisplayName}");
                        continue;
                    }
                }

                batch.Add(new Employee
                {
                    EmployeeId = item.EmployeeId.Trim(),
                    DisplayName = item.DisplayName.Trim(),
                    Email = item.Email.ToLowerInvariant().Trim(),
                    Phone = item.Phone?.Trim(),
                    ManagerId = item.ManagerId?.Trim(),
                    DepartmentId = item.DepartmentId,
                    Role = item.Role,
                    PasswordHash = PasswordHasher.HashPassword(item.Password),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                errors.Add($"{item.EmployeeId}: {ex.Message}");
            }
        }

        if (batch.Count > 0)
        {
            await _context.Employees.AddRangeAsync(batch);
            await _context.SaveChangesAsync();
            created = batch.Count;
        }

        await _auditService.LogActionAsync(action: "BulkUploadEmployees",
            entityType: "Employee",
            entityId: "bulk",
            httpEndpoint: "POST /api/employees/bulk",
            changesJson: $"{{\"Created\":{created},\"Errors\":{errors.Count}}}");

        return new BulkResult { Created = created, Errors = errors };
    }

    /// <summary>
    /// Soft-delete (disable) an employee.
    /// </summary>
    public async Task DisableAsync(Guid id)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == id);
        if (employee is null)
            throw new NotFoundException($"Employee with ID {id} not found.");

        employee.IsActive = false;
        employee.RefreshToken = null;
        employee.RefreshTokenExpiry = null;
        employee.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Employee disabled: {EmployeeId}", employee.EmployeeId);
    }

    /// <summary>
    /// Resolve the manager chain (hierarchy path) for an employee.
    /// Returns list of EmployeeIds from top-level manager down to self.
    /// </summary>
    public async Task<List<string>> GetManagerChainAsync(string employeeId)
    {
        var chain = new List<string>();
        var currentId = employeeId;

        while (!string.IsNullOrEmpty(currentId))
        {
            chain.Insert(0, currentId); // Prepend to maintain top-down order
            var emp = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == currentId);
            currentId = emp?.ManagerId;
        }

        return chain;
    }

    /// <summary>
    /// Get all direct subordinates of a given employee (by their EmployeeId).
    /// </summary>
    public async Task<List<EmployeeDto>> GetSubordinatesAsync(string employeeId)
    {
        var subordinates = await _context.Employees
            .Where(e => e.ManagerId == employeeId && e.IsActive)
            .OrderBy(e => e.DisplayName)
            .ToListAsync();

        return subordinates.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Get manager by manager's EmployeeId (not surrogate PK).
    /// </summary>
    public async Task<Employee?> GetManagerByEmployeeIdAsync(string managerId)
    {
        return await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == managerId && e.IsActive);
    }

    private EmployeeHierarchyDto BuildHierarchy(Employee employee)
    {
        var dto = MapToDto(employee);

        // Get manager
        EmployeeDto? manager = null;
        if (!string.IsNullOrEmpty(employee.ManagerId))
        {
            var mgr = _context.Employees
                .Include(e => e.Department)
                .FirstOrDefault(e => e.EmployeeId == employee.ManagerId);
            if (mgr != null)
                manager = MapToDto(mgr);
        }

        // Get subordinates
        var subordinates = _context.Employees
            .Where(e => e.ManagerId == employee.EmployeeId && e.IsActive)
            .OrderBy(e => e.DisplayName)
            .Select(MapToDto)
            .ToList();

        // Build hierarchy path
        var hierarchyPath = GetManagerChainAsync(employee.EmployeeId).GetAwaiter().GetResult();

        return new EmployeeHierarchyDto
        {
            Employee = dto,
            Manager = manager,
            Subordinates = subordinates,
            HierarchyPath = hierarchyPath
        };
    }

    private static EmployeeDto MapToDto(Employee employee)
    {
        return new EmployeeDto
        {
            Id = employee.Id,
            EmployeeId = employee.EmployeeId,
            DisplayName = employee.DisplayName,
            Email = employee.Email,
            PhoneNumber = employee.Phone,
            ManagerId = employee.ManagerId,
            ManagerName = null, // Will be populated separately if needed
            DepartmentId = employee.DepartmentId,
            DepartmentName = employee.Department?.Name ?? string.Empty,
            Role = employee.Role,
            IsActive = employee.IsActive,
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt
        };
    }

    private string SerializeChanges(object oldValues, object newValues)
    {
        var oldDict = oldValues.GetType().GetProperties()
            .ToDictionary(p => p.Name, p => p.GetValue(oldValues)?.ToString());
        var newDict = newValues.GetType().GetProperties()
            .ToDictionary(p => p.Name, p => p.GetValue(newValues)?.ToString());

        var changes = oldDict.Keys.Select(key =>
            $"{key}:{oldDict[key]}?{newDict[key]}").ToList();

        return string.Join("|", changes);
    }
}

public record BulkResult(int Created, List<string> Errors);
