using Microsoft.EntityFrameworkCore;
using WACAMS.Core.Entities;
using RoleEnum = WACAMS.Core.Enums.Role;

namespace WACAMS.Infrastructure.Data.SeedData;

/// <summary>
/// Seeds the User table with default system roles.
/// Creates a SuperAdmin account and placeholder accounts for each role level.
/// Default password hash uses PBKDF2-compatible format (to be replaced by auth service).
/// </summary>
public static class RoleSeeder
{
    public static async Task SeedAsync(WacamContext context)
    {
        if (await context.Users.AnyAsync())
            return; // Already seeded

        var roles = new List<User>
        {
            new()
            {
                Id = Guid.Parse("B2C3D4E5-0001-0001-B001-000000000001"),
                EmployeeId = "000000", // System admin placeholder
                Username = "superadmin",
                PasswordHash = "$2a$11$placeholder_hash_for_superadmin", // Replace with actual hash via auth service
                Role = RoleEnum.SuperAdmin,
                IsActive = true,
                LastLoginAt = null
            },
            new()
            {
                Id = Guid.Parse("B2C3D4E5-0001-0001-B001-000000000002"),
                EmployeeId = "000001",
                Username = "admin",
                PasswordHash = "$2a$11$placeholder_hash_for_admin",
                Role = RoleEnum.Admin,
                IsActive = true,
                LastLoginAt = null
            },
            new()
            {
                Id = Guid.Parse("B2C3D4E5-0001-0001-B001-000000000003"),
                EmployeeId = "000002",
                Username = "supervisor",
                PasswordHash = "$2a$11$placeholder_hash_for_supervisor",
                Role = RoleEnum.Supervisor,
                IsActive = true,
                LastLoginAt = null
            },
            new()
            {
                Id = Guid.Parse("B2C3D4E5-0001-0001-B001-000000000004"),
                EmployeeId = "000003",
                Username = "manager",
                PasswordHash = "$2a$11$placeholder_hash_for_manager",
                Role = RoleEnum.Manager,
                IsActive = true,
                LastLoginAt = null
            },
            new()
            {
                Id = Guid.Parse("B2C3D4E5-0001-0001-B001-000000000005"),
                EmployeeId = "000004",
                Username = "hr_user",
                PasswordHash = "$2a$11$placeholder_hash_for_hr",
                Role = RoleEnum.HR,
                IsActive = true,
                LastLoginAt = null
            },
            new()
            {
                Id = Guid.Parse("B2C3D4E5-0001-0001-B001-000000000007"),
                EmployeeId = "000006",
                Username = "operator",
                PasswordHash = "$2a$11$placeholder_hash_for_operator",
                Role = RoleEnum.Operator,
                IsActive = true,
                LastLoginAt = null
            },
            new()
            {
                Id = Guid.Parse("B2C3D4E5-0001-0001-B001-000000000006"),
                EmployeeId = "000005",
                Username = "user",
                PasswordHash = "$2a$11$placeholder_hash_for_user",
                Role = RoleEnum.User,
                IsActive = true,
                LastLoginAt = null
            }
        };

        await context.Users.AddRangeAsync(roles);
        await context.SaveChangesAsync();
    }
}
