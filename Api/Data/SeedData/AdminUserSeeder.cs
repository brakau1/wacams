using Microsoft.EntityFrameworkCore;
using WACAMS.Api.Data;
using WACAMS.Core.Entities;

namespace WACAMS.Api.Data.SeedData;

/// <summary>
/// Seeds the initial SuperAdmin user account.
/// Password is NOT hardcoded — must be set via environment variable or first-login setup.
/// The seeded account has MustChangePassword = true to enforce secure onboarding.
/// </summary>
public static class AdminUserSeeder
{
    public static async Task SeedAsync(WacamDbContext context)
    {
        // Seed SuperAdmin user if not exists
        if (!await context.Users.AnyAsync(u => u.EmployeeId == "803341"))
        {
            var superAdminUser = new User
            {
                Id = Guid.NewGuid(),
                EmployeeId = "803341",
                Email = "brakau1@yfpo.com",
                DisplayName = "Botlhale Rakau",
                IsActive = true,
                MustChangePassword = true, // Force password change on first login
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.Users.Add(superAdminUser);
            await context.SaveChangesAsync();

            // Assign SuperAdmin role
            var superAdminRole = await context.Roles
                .FirstOrDefaultAsync(r => r.Name == RoleNames.SuperAdmin);

            if (superAdminRole != null)
            {
                var userRoleAssignment = new UserRole
                {
                    Id = Guid.NewGuid(),
                    UserId = superAdminUser.Id,
                    RoleId = superAdminRole.Id,
                    AssignedAt = DateTime.UtcNow
                };

                context.UserRoles.Add(userRoleAssignment);
                await context.SaveChangesAsync();
            }
        }
    }
}
