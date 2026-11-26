using Microsoft.EntityFrameworkCore;
using WACAMS.Api.Data;
using WACAMS.Core.Entities;

namespace WACAMS.Api.Data.SeedData;

/// <summary>
/// Seeds permissions, roles, and role-permission mappings based on the RBAC matrix.
/// </summary>
public static class PermissionSeeder
{
    public static async Task SeedAsync(WacamDbContext context)
    {
        // Seed permissions if not exists
        if (!await context.Permissions.AnyAsync())
        {
            await SeedPermissionsAsync(context);
        }

        // Seed roles if not exists
        if (!await context.Roles.AnyAsync())
        {
            await SeedRolesAsync(context);
        }

        // Seed role-permission mappings if not exists
        if (!await context.RolePermissions.AnyAsync())
        {
            await SeedRolePermissionsAsync(context);
        }
    }

    private static async Task SeedPermissionsAsync(WacamDbContext context)
    {
        var permissions = new List<Permission>
        {
            // Attendance Module
            new() { Name = Permissions.Attendance.ViewOwn, Module = "Attendance", Description = "View own attendance records" },
            new() { Name = Permissions.Attendance.ViewTeam, Module = "Attendance", Description = "View team attendance records" },
            new() { Name = Permissions.Attendance.ViewDepartment, Module = "Attendance", Description = "View department attendance records" },
            new() { Name = Permissions.Attendance.ViewAll, Module = "Attendance", Description = "View all attendance records" },
            new() { Name = Permissions.Attendance.Edit, Module = "Attendance", Description = "Edit attendance records" },

            // Exceptions Module
            new() { Name = Permissions.Exceptions.ViewOwn, Module = "Exceptions", Description = "View own exceptions" },
            new() { Name = Permissions.Exceptions.ViewTeam, Module = "Exceptions", Description = "View team exceptions" },
            new() { Name = Permissions.Exceptions.ViewDepartment, Module = "Exceptions", Description = "View department exceptions" },
            new() { Name = Permissions.Exceptions.ViewAll, Module = "Exceptions", Description = "View all exceptions" },
            new() { Name = Permissions.Exceptions.Investigate, Module = "Exceptions", Description = "Investigate exceptions" },
            new() { Name = Permissions.Exceptions.Approve, Module = "Exceptions", Description = "Approve exceptions" },
            new() { Name = Permissions.Exceptions.Reject, Module = "Exceptions", Description = "Reject exceptions" },

            // Warnings Module
            new() { Name = Permissions.Warnings.ViewOwn, Module = "Warnings", Description = "View own warnings" },
            new() { Name = Permissions.Warnings.ViewTeam, Module = "Warnings", Description = "View team warnings" },
            new() { Name = Permissions.Warnings.ViewDepartment, Module = "Warnings", Description = "View department warnings" },
            new() { Name = Permissions.Warnings.ViewAll, Module = "Warnings", Description = "View all warnings" },
            new() { Name = Permissions.Warnings.Create, Module = "Warnings", Description = "Create warning letters" },
            new() { Name = Permissions.Warnings.Upload, Module = "Warnings", Description = "Upload warning documents" },
            new() { Name = Permissions.Warnings.Issue, Module = "Warnings", Description = "Issue warning letters" },

            // Escalations Module
            new() { Name = Permissions.Escalations.ViewOwn, Module = "Escalations", Description = "View own escalations" },
            new() { Name = Permissions.Escalations.ViewTeam, Module = "Escalations", Description = "View team escalations" },
            new() { Name = Permissions.Escalations.ViewDepartment, Module = "Escalations", Description = "View department escalations" },
            new() { Name = Permissions.Escalations.ViewAll, Module = "Escalations", Description = "View all escalations" },
            new() { Name = Permissions.Escalations.Manage, Module = "Escalations", Description = "Manage escalations" },

            // Employees Module
            new() { Name = Permissions.Employees.ViewOwn, Module = "Employees", Description = "View own employee record" },
            new() { Name = Permissions.Employees.ViewAll, Module = "Employees", Description = "View all employee records" },
            new() { Name = Permissions.Employees.Create, Module = "Employees", Description = "Create employee records" },
            new() { Name = Permissions.Employees.Edit, Module = "Employees", Description = "Edit employee records" },
            new() { Name = Permissions.Employees.Delete, Module = "Employees", Description = "Delete employee records" },
            new() { Name = Permissions.Employees.AssignRole, Module = "Employees", Description = "Assign roles to employees" },

            // Reports Module
            new() { Name = Permissions.Reports.ViewOwn, Module = "Reports", Description = "View own reports" },
            new() { Name = Permissions.Reports.ViewTeam, Module = "Reports", Description = "View team reports" },
            new() { Name = Permissions.Reports.ViewDepartment, Module = "Reports", Description = "View department reports" },
            new() { Name = Permissions.Reports.ViewAll, Module = "Reports", Description = "View all reports" },
            new() { Name = Permissions.Reports.Generate, Module = "Reports", Description = "Generate reports" },

            // System Module
            new() { Name = Permissions.System.ViewLogs, Module = "System", Description = "View system logs" },
            new() { Name = Permissions.System.ManageSettings, Module = "System", Description = "Manage system settings" },
            new() { Name = Permissions.System.ManageRoles, Module = "System", Description = "Manage roles" },
            new() { Name = Permissions.System.ManagePermissions, Module = "System", Description = "Manage permissions" },
            new() { Name = Permissions.System.ViewAuditTrail, Module = "System", Description = "View audit trail" }
        };

        await context.Permissions.AddRangeAsync(permissions);
        await context.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(WacamDbContext context)
    {
        var roles = new List<Role>
        {
            new() { Name = RoleNames.User, Description = "Basic user - view own records only" },
            new() { Name = "Operator", Description = "Operator - plant floor worker, view own attendance and records" },
            new() { Name = RoleNames.Supervisor, Description = "Supervisor - view team data, manage team members" },
            new() { Name = RoleNames.Manager, Description = "Manager - department-wide access, escalation management" },
            new() { Name = RoleNames.HR, Description = "Human Resources - attendance oversight, leave approvals, warning generation" },
            new() { Name = RoleNames.Admin, Description = "Administrator - full system access, user management" },
            new() { Name = RoleNames.SuperAdmin, Description = "Super Administrator - unrestricted system-wide access, infrastructure configuration, role assignment" }
        };

        await context.Roles.AddRangeAsync(roles);
        await context.SaveChangesAsync();
    }

    private static async Task SeedRolePermissionsAsync(WacamDbContext context)
    {
        var allPermissions = await context.Permissions.ToListAsync();
        var roles = await context.Roles.ToListAsync();

        var userRoleEntity = roles.First(r => r.Name == RoleNames.User);
        var operatorRole = roles.First(r => r.Name == "Operator");
        var hrRole = roles.First(r => r.Name == RoleNames.HR);
        var supervisorRole = roles.First(r => r.Name == RoleNames.Supervisor);
        var managerRole = roles.First(r => r.Name == RoleNames.Manager);
        var adminRole = roles.First(r => r.Name == RoleNames.Admin);
        var superAdminRole = roles.First(r => r.Name == RoleNames.SuperAdmin);

        var rolePermissions = new List<RolePermission>();

        // User permissions (minimal - own records only)
        rolePermissions.AddRange(new[]
        {
            new RolePermission { RoleId = userRoleEntity.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Attendance.ViewOwn).Id },
            new RolePermission { RoleId = userRoleEntity.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Exceptions.ViewOwn).Id },
            new RolePermission { RoleId = userRoleEntity.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Warnings.ViewOwn).Id },
            new RolePermission { RoleId = userRoleEntity.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Escalations.ViewOwn).Id },
            new RolePermission { RoleId = userRoleEntity.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Employees.ViewOwn).Id },
            new RolePermission { RoleId = userRoleEntity.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Reports.ViewOwn).Id }
        });

        // Operator permissions (plant floor workers - own records only)
        rolePermissions.AddRange(new[]
        {
            new RolePermission { RoleId = operatorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Attendance.ViewOwn).Id },
            new RolePermission { RoleId = operatorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Exceptions.ViewOwn).Id },
            new RolePermission { RoleId = operatorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Warnings.ViewOwn).Id },
            new RolePermission { RoleId = operatorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Escalations.ViewOwn).Id },
            new RolePermission { RoleId = operatorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Employees.ViewOwn).Id },
            new RolePermission { RoleId = operatorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Reports.ViewOwn).Id }
        });

        // HR permissions (own + employee management + warnings)
        rolePermissions.AddRange(new[]
        {
            new RolePermission { RoleId = hrRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Attendance.ViewOwn).Id },
            new RolePermission { RoleId = hrRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Attendance.ViewDepartment).Id },
            new RolePermission { RoleId = hrRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Exceptions.ViewOwn).Id },
            new RolePermission { RoleId = hrRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Exceptions.ViewDepartment).Id },
            new RolePermission { RoleId = hrRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Exceptions.Investigate).Id },
            new RolePermission { RoleId = hrRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Warnings.ViewOwn).Id },
            new RolePermission { RoleId = hrRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Warnings.ViewDepartment).Id },
            new RolePermission { RoleId = hrRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Warnings.Create).Id },
            new RolePermission { RoleId = hrRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Warnings.Upload).Id },
            new RolePermission { RoleId = hrRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Escalations.ViewOwn).Id },
            new RolePermission { RoleId = hrRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Escalations.ViewDepartment).Id },
            new RolePermission { RoleId = hrRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Employees.ViewOwn).Id },
            new RolePermission { RoleId = hrRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Employees.ViewAll).Id },
            new RolePermission { RoleId = hrRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Employees.Edit).Id },
            new RolePermission { RoleId = hrRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Reports.ViewOwn).Id },
            new RolePermission { RoleId = hrRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Reports.ViewDepartment).Id },
            new RolePermission { RoleId = hrRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Reports.Generate).Id }
        });

        // Supervisor permissions (team-level access + approvals)
        rolePermissions.AddRange(new[]
        {
            new RolePermission { RoleId = supervisorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Attendance.ViewOwn).Id },
            new RolePermission { RoleId = supervisorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Attendance.ViewTeam).Id },
            new RolePermission { RoleId = supervisorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Attendance.Edit).Id },
            new RolePermission { RoleId = supervisorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Exceptions.ViewOwn).Id },
            new RolePermission { RoleId = supervisorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Exceptions.ViewTeam).Id },
            new RolePermission { RoleId = supervisorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Exceptions.Investigate).Id },
            new RolePermission { RoleId = supervisorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Exceptions.Approve).Id },
            new RolePermission { RoleId = supervisorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Exceptions.Reject).Id },
            new RolePermission { RoleId = supervisorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Warnings.ViewOwn).Id },
            new RolePermission { RoleId = supervisorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Warnings.ViewTeam).Id },
            new RolePermission { RoleId = supervisorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Warnings.Create).Id },
            new RolePermission { RoleId = supervisorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Warnings.Upload).Id },
            new RolePermission { RoleId = supervisorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Warnings.Issue).Id },
            new RolePermission { RoleId = supervisorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Escalations.ViewOwn).Id },
            new RolePermission { RoleId = supervisorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Escalations.ViewTeam).Id },
            new RolePermission { RoleId = supervisorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Escalations.Manage).Id },
            new RolePermission { RoleId = supervisorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Employees.ViewOwn).Id },
            new RolePermission { RoleId = supervisorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Employees.ViewAll).Id },
            new RolePermission { RoleId = supervisorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Reports.ViewOwn).Id },
            new RolePermission { RoleId = supervisorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Reports.ViewTeam).Id },
            new RolePermission { RoleId = supervisorRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Reports.Generate).Id }
        });

        // Manager permissions (department-level access)
        rolePermissions.AddRange(new[]
        {
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Attendance.ViewOwn).Id },
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Attendance.ViewTeam).Id },
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Attendance.ViewDepartment).Id },
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Attendance.Edit).Id },
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Exceptions.ViewOwn).Id },
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Exceptions.ViewTeam).Id },
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Exceptions.ViewDepartment).Id },
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Exceptions.Investigate).Id },
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Exceptions.Approve).Id },
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Exceptions.Reject).Id },
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Warnings.ViewOwn).Id },
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Warnings.ViewTeam).Id },
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Warnings.ViewDepartment).Id },
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Warnings.Create).Id },
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Warnings.Upload).Id },
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Warnings.Issue).Id },
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Escalations.ViewOwn).Id },
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Escalations.ViewTeam).Id },
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Escalations.ViewDepartment).Id },
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Escalations.Manage).Id },
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Employees.ViewOwn).Id },
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Employees.ViewAll).Id },
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Employees.Edit).Id },
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Reports.ViewOwn).Id },
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Reports.ViewTeam).Id },
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Reports.ViewDepartment).Id },
            new RolePermission { RoleId = managerRole.Id, PermissionId = allPermissions.First(p => p.Name == Permissions.Reports.Generate).Id }
        });

        // Admin permissions (all permissions)
        foreach (var permission in allPermissions)
        {
            rolePermissions.Add(new RolePermission
            {
                RoleId = adminRole.Id,
                PermissionId = permission.Id
            });
        }

        // SuperAdmin permissions (all permissions — unrestricted)
        foreach (var permission in allPermissions)
        {
            rolePermissions.Add(new RolePermission
            {
                RoleId = superAdminRole.Id,
                PermissionId = permission.Id
            });
        }

        await context.RolePermissions.AddRangeAsync(rolePermissions);
        await context.SaveChangesAsync();
    }
}
