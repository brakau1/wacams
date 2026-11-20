using Microsoft.EntityFrameworkCore;
using Wacam.Api.Data;
using Wacam.Api.DTOs;
using WACAMS.Core.Entities;
using Wacam.Api.Utils;

namespace Wacam.Api.Services;

/// <summary>
/// JWT token generation/validation, password hashing (BCrypt), refresh tokens.
/// </summary>
public class AuthService
{
    private readonly WacamDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly PermissionService _permissionService;

    // Lockout policy constants
    private const int MaxFailedAttempts = 5;
    private const int LockoutWindowMinutes = 30;
    private const int LockoutDurationMinutes = 30;

    public AuthService(WacamDbContext context, IConfiguration configuration, ILogger<AuthService> logger, PermissionService permissionService)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
        _permissionService = permissionService;
    }

    /// <summary>
    /// Authenticate employee by EmployeeId and password. Returns JWT access + refresh tokens.
    /// Enforces account lockout: 5 failed attempts within 30 minutes → 30-minute lockout.
    /// </summary>
    public async Task<LoginResponse> LoginAsync(LoginRequest request, string? ipAddress = null)
    {
        // 1. Look up employee (including locked accounts so we can return a specific message)
        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId);

        if (employee is null || !employee.IsActive)
        {
            _logger.LogWarning("Failed login attempt for unknown/inactive EmployeeId: {EmployeeId}", request.EmployeeId);
            throw new UnauthorizedAccessException("Invalid employee ID or password.");
        }

        // 2. Check if account is currently locked
        if (employee.LockoutEnd.HasValue && employee.LockoutEnd.Value > DateTime.UtcNow)
        {
            var minutesRemaining = (int)Math.Ceiling((employee.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes);
            _logger.LogWarning(
                "Login attempt for locked account {EmployeeId}. Lockout ends {LockoutEnd} ({MinutesRemaining} min remaining).",
                request.EmployeeId, employee.LockoutEnd.Value, minutesRemaining);

            throw new AccountLockedException(
                $"Account is locked due to too many failed login attempts. Try again in {minutesRemaining} minutes.");
        }

        // 3. If lockout has expired, auto-unlock
        if (employee.LockoutEnd.HasValue && employee.LockoutEnd.Value <= DateTime.UtcNow)
        {
            await LogAccountUnlockedAsync(employee, ipAddress, "AutoUnlock_Expired");
            employee.LockoutEnd = null;
            employee.FailedLoginCount = 0;
            employee.LastFailedLoginAt = null;
            await _context.SaveChangesAsync();
        }

        // 4. Verify password
        if (!PasswordHasher.VerifyPassword(request.Password, employee.PasswordHash))
        {
            await RecordFailedLoginAsync(employee, ipAddress);
            throw new UnauthorizedAccessException("Invalid employee ID or password.");
        }

        // 5. Successful login — reset lockout counters
        await ResetLockoutCountersAsync(employee, ipAddress);

        // 6. Load user permissions
        var permissions = await _permissionService.GetUserPermissionsAsync(employee.Id);

        var (accessToken, refreshToken, expiresAt) = TokenGenerator.GenerateTokens(
            employee.EmployeeId,
            employee.DisplayName,
            employee.Email,
            employee.Role,
            permissions);

        // Store refresh token in DB
        employee.RefreshToken = refreshToken;
        employee.RefreshTokenExpiry = DateTime.UtcNow.AddDays(GetRefreshTokenDays());
        await _context.SaveChangesAsync();

        _logger.LogInformation("Successful login for EmployeeId: {EmployeeId} ({DisplayName})",
            employee.EmployeeId, employee.DisplayName);

        // Log successful login security event
        await LogSecurityEventAsync(
            eventType: "LoginSuccess",
            severity: "Low",
            employeeId: employee.EmployeeId,
            ipAddress: ipAddress,
            details: new
            {
                Role = employee.Role,
                DisplayName = employee.DisplayName
            });

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            Employee = MapToEmployeeDto(employee)
        };
    }

    /// <summary>
    /// Record a failed login attempt. If threshold reached, lock the account.
    /// Uses a 30-minute sliding window: only counts failures within the window.
    /// </summary>
    private async Task RecordFailedLoginAsync(Employee employee, string? ipAddress)
    {
        var now = DateTime.UtcNow;

        // If last failure was outside the sliding window, reset counter
        if (employee.LastFailedLoginAt.HasValue &&
            (now - employee.LastFailedLoginAt.Value).TotalMinutes > LockoutWindowMinutes)
        {
            employee.FailedLoginCount = 0;
        }

        employee.FailedLoginCount++;
        employee.LastFailedLoginAt = now;

        _logger.LogWarning(
            "Failed login #{FailedCount}/{MaxAttempts} for EmployeeId: {EmployeeId} from IP: {IpAddress}",
            employee.FailedLoginCount, MaxFailedAttempts, employee.EmployeeId, ipAddress ?? "unknown");

        // Log security event for failed login
        await LogSecurityEventAsync(
            eventType: "LoginFailed",
            severity: employee.FailedLoginCount >= MaxFailedAttempts ? "Critical" : "Medium",
            employeeId: employee.EmployeeId,
            ipAddress: ipAddress,
            details: new
            {
                FailedCount = employee.FailedLoginCount,
                MaxAttempts = MaxFailedAttempts,
                WindowMinutes = LockoutWindowMinutes
            });

        // Lock account if threshold reached
        if (employee.FailedLoginCount >= MaxFailedAttempts)
        {
            employee.LockoutEnd = now.AddMinutes(LockoutDurationMinutes);

            _logger.LogWarning(
                "Account LOCKED for EmployeeId: {EmployeeId} after {FailedCount} failed attempts. Lockout until {LockoutEnd}.",
                employee.EmployeeId, employee.FailedLoginCount, employee.LockoutEnd.Value);

            await LogAccountLockedAsync(employee, ipAddress);
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Reset lockout counters after successful login.
    /// Logs AccountUnlocked security event if the account was previously locked.
    /// </summary>
    private async Task ResetLockoutCountersAsync(Employee employee, string? ipAddress)
    {
        var wasLocked = employee.LockoutEnd.HasValue || employee.FailedLoginCount > 0;

        employee.FailedLoginCount = 0;
        employee.LastFailedLoginAt = null;
        employee.LockoutEnd = null;

        if (wasLocked)
        {
            await LogAccountUnlockedAsync(employee, ipAddress, "SuccessfulLogin");
        }
    }

    /// <summary>
    /// Log AccountLocked security event. For SuperAdmin accounts, severity is always Critical.
    /// </summary>
    private async Task LogAccountLockedAsync(Employee employee, string? ipAddress)
    {
        var isSuperAdmin = string.Equals(employee.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
        var severity = isSuperAdmin ? "Critical" : "High";

        await LogSecurityEventAsync(
            eventType: "AccountLocked",
            severity: severity,
            employeeId: employee.EmployeeId,
            ipAddress: ipAddress,
            details: new
            {
                FailedCount = employee.FailedLoginCount,
                LockoutEnd = employee.LockoutEnd,
                LockoutDurationMinutes = LockoutDurationMinutes,
                IsSuperAdmin = isSuperAdmin,
                Role = employee.Role
            });
    }

    /// <summary>
    /// Log AccountUnlocked security event.
    /// </summary>
    private async Task LogAccountUnlockedAsync(Employee employee, string? ipAddress, string reason)
    {
        await LogSecurityEventAsync(
            eventType: "AccountUnlocked",
            severity: "Medium",
            employeeId: employee.EmployeeId,
            ipAddress: ipAddress,
            details: new
            {
                Reason = reason,
                Role = employee.Role
            });
    }

    /// <summary>
    /// Persist a SecurityEvent to the database.
    /// </summary>
    private async Task LogSecurityEventAsync(string eventType, string severity, string? employeeId, string? ipAddress, object? details)
    {
        try
        {
            var securityEvent = new SecurityEvent
            {
                Id = Guid.NewGuid(),
                EventType = eventType,
                Severity = severity,
                EmployeeId = employeeId,
                IpAddress = ipAddress,
                DetailsJson = details != null
                    ? System.Text.Json.JsonSerializer.Serialize(details)
                    : null,
                OccurredAt = DateTime.UtcNow
            };

            _context.SecurityEvents.Add(securityEvent);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Security event logging must never break the authentication flow
            _logger.LogError(ex, "Failed to log security event {EventType} for {EmployeeId}", eventType, employeeId);
        }
    }

    /// <summary>
    /// Register a new employee. Only Admin/SuperAdmin roles allowed to call this endpoint.
    /// </summary>
    public async Task<EmployeeDto> RegisterAsync(RegisterRequest request)
    {
        // Check for duplicate EmployeeId
        if (await _context.Employees.AnyAsync(e => e.EmployeeId == request.EmployeeId))
            throw new ConflictException($"Employee with ID '{request.EmployeeId}' already exists.");

        // Validate department exists
        var department = await _context.Departments.FindAsync(request.DepartmentId);
        if (department is null)
            throw new NotFoundException($"Department with ID {request.DepartmentId} not found.");

        // Validate manager if provided
        if (!string.IsNullOrEmpty(request.ManagerId))
        {
            var managerExists = await _context.Employees.AnyAsync(e => e.EmployeeId == request.ManagerId);
            if (!managerExists)
                throw new NotFoundException($"Manager with EmployeeId '{request.ManagerId}' not found.");
        }

        var newEmployee = new Employee
        {
            EmployeeId = request.EmployeeId.Trim(),
            DisplayName = request.DisplayName.Trim(),
            Email = request.Email.ToLowerInvariant().Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            ManagerId = request.ManagerId?.Trim(),
            DepartmentId = request.DepartmentId,
            Role = request.Role,
            PasswordHash = PasswordHasher.HashPassword(request.Password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Employees.Add(newEmployee);
        await _context.SaveChangesAsync();

        _logger.LogInformation("New employee registered: {EmployeeId} ({DisplayName})",
            newEmployee.EmployeeId, newEmployee.DisplayName);

        return MapToEmployeeDto(newEmployee);
    }

    /// <summary>
    /// Refresh an expired access token using a valid refresh token.
    /// </summary>
    public async Task<LoginResponse> RefreshTokenAsync(string employeeId, string refreshToken)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

        if (employee is null || string.IsNullOrEmpty(employee.RefreshToken))
            throw new UnauthorizedAccessException("Invalid refresh token.");

        if (employee.RefreshToken != refreshToken)
            throw new UnauthorizedAccessException("Refresh token mismatch.");

        if (employee.RefreshTokenExpiry.HasValue && employee.RefreshTokenExpiry.Value < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token has expired.");

        var (accessToken, newRefreshToken, expiresAt) = TokenGenerator.GenerateTokens(
            employee.EmployeeId,
            employee.DisplayName,
            employee.Email,
            employee.Role);

        employee.RefreshToken = newRefreshToken;
        employee.RefreshTokenExpiry = DateTime.UtcNow.AddDays(GetRefreshTokenDays());
        await _context.SaveChangesAsync();

        _logger.LogInformation("Token refreshed for EmployeeId: {EmployeeId}", employeeId);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = expiresAt,
            Employee = MapToEmployeeDto(employee)
        };
    }

    /// <summary>
    /// Logout — invalidate the current refresh token.
    /// </summary>
    public async Task LogoutAsync(string employeeId)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
        if (employee is not null)
        {
            employee.RefreshToken = null;
            employee.RefreshTokenExpiry = null;
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Logout for EmployeeId: {EmployeeId}", employeeId);
    }

    /// <summary>
    /// Change password for the authenticated user.
    /// </summary>
    public async Task ChangePasswordAsync(string employeeId, ChangePasswordRequest request)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
        if (employee is null)
            throw new NotFoundException("Employee not found.");

        if (!PasswordHasher.VerifyPassword(request.CurrentPassword, employee.PasswordHash))
            throw new ArgumentException("Current password is incorrect.");

        if (request.NewPassword.Length < 8)
            throw new ArgumentException("New password must be at least 8 characters long.");

        employee.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
        employee.RefreshToken = null; // Invalidate existing sessions
        employee.RefreshTokenExpiry = null;
        employee.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Password changed for EmployeeId: {EmployeeId}", employeeId);
    }

    private int GetRefreshTokenDays()
    {
        var configValue = _configuration.GetValue<int?>("Jwt:RefreshTokenDays");
        return configValue ?? 7;
    }

    private static EmployeeDto MapToEmployeeDto(Employee employee)
    {
        return new EmployeeDto
        {
            Id = employee.Id,
            EmployeeId = employee.EmployeeId,
            DisplayName = employee.DisplayName,
            Email = employee.Email,
            PhoneNumber = employee.PhoneNumber,
            ManagerId = employee.ManagerId,
            DepartmentId = employee.DepartmentId,
            Role = employee.Role,
            IsActive = employee.IsActive,
            CreatedAt = employee.CreatedAt,
            UpdatedAt = employee.UpdatedAt
        };
    }
}

/// <summary>
/// Exception thrown when an account is locked due to too many failed login attempts.
/// </summary>
public class AccountLockedException : Exception
{
    public AccountLockedException(string message) : base(message) { }
    public AccountLockedException(string message, Exception innerException) : base(message, innerException) { }
}
