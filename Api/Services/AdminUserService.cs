using Microsoft.EntityFrameworkCore;
using WACAMS.Api.Data;
using WACAMS.Core.Entities;

namespace WACAMS.Api.Services;

/// <summary>
/// Service for managing admin users who have full system access regardless of their role.
/// </summary>
public class AdminUserService
{
    private readonly WacamDbContext _context;
    private readonly ILogger<AdminUserService> _logger;

    public AdminUserService(WacamDbContext context, ILogger<AdminUserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Check if a user has admin access (either by role or by being in the admin list)
    /// </summary>
    public async Task<bool> IsAdminAsync(string email)
    {
        return await _context.AdminUsers
            .AnyAsync(a => a.Email == email && a.IsActive);
    }

    /// <summary>
    /// Get all admin users
    /// </summary>
    public async Task<List<AdminUser>> GetAllAdminsAsync()
    {
        return await _context.AdminUsers
            .OrderByDescending(a => a.GrantedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Add a user to the admin list
    /// </summary>
    public async Task<AdminUser> AddAdminAsync(string email, string displayName, string reason, string grantedBy)
    {
        // Check if already exists
        var existing = await _context.AdminUsers.FirstOrDefaultAsync(a => a.Email == email);
        if (existing != null)
        {
            if (existing.IsActive)
            {
                throw new InvalidOperationException($"User {email} is already an admin");
            }
            
            // Re-activate if previously deactivated
            existing.IsActive = true;
            existing.Reason = reason;
            existing.GrantedBy = grantedBy;
            existing.GrantedAt = DateTime.UtcNow;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var admin = new AdminUser
            {
                Email = email,
                DisplayName = displayName,
                Reason = reason,
                GrantedBy = grantedBy,
                GrantedAt = DateTime.UtcNow,
                IsActive = true
            };
            _context.AdminUsers.Add(admin);
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Admin access granted to {Email} by {GrantedBy}", email, grantedBy);

        return (await _context.AdminUsers.FirstOrDefaultAsync(a => a.Email == email))!;
    }

    /// <summary>
    /// Remove a user from the admin list (soft delete)
    /// </summary>
    public async Task<bool> RemoveAdminAsync(string email, string removedBy)
    {
        var admin = await _context.AdminUsers.FirstOrDefaultAsync(a => a.Email == email);
        if (admin == null)
        {
            return false;
        }

        admin.IsActive = false;
        admin.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        
        _logger.LogInformation("Admin access removed from {Email} by {RemovedBy}", email, removedBy);
        return true;
    }

    /// <summary>
    /// Get admin user details
    /// </summary>
    public async Task<AdminUser?> GetAdminByEmailAsync(string email)
    {
        return await _context.AdminUsers.FirstOrDefaultAsync(a => a.Email == email);
    }
}
