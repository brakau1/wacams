using Microsoft.AspNetCore.Authorization;

namespace WACAMS.Api.Policies;

/// <summary>
/// Defines authorization policies for permission-based access control.
/// </summary>
public static class AuthorizationPolicies
{
    public static void Configure(AuthorizationOptions options)
    {
        // SuperAdmin policy — unrestricted system access
        options.AddPolicy("RequireSuperAdmin", policy =>
            policy.RequireRole("SuperAdmin"));

        // Attendance policies
        options.AddPolicy("Attendance.ViewOwn", policy =>
            policy.RequireClaim("permission", "attendance.view.own"));

        options.AddPolicy("Attendance.ViewTeam", policy =>
            policy.RequireClaim("permission", "attendance.view.team"));

        options.AddPolicy("Attendance.ViewDepartment", policy =>
            policy.RequireClaim("permission", "attendance.view.department"));

        options.AddPolicy("Attendance.ViewAll", policy =>
            policy.RequireClaim("permission", "attendance.view.all"));

        options.AddPolicy("Attendance.Edit", policy =>
            policy.RequireClaim("permission", "attendance.edit"));

        // Exceptions policies
        options.AddPolicy("Exceptions.ViewOwn", policy =>
            policy.RequireClaim("permission", "exceptions.view.own"));

        options.AddPolicy("Exceptions.ViewTeam", policy =>
            policy.RequireClaim("permission", "exceptions.view.team"));

        options.AddPolicy("Exceptions.ViewDepartment", policy =>
            policy.RequireClaim("permission", "exceptions.view.department"));

        options.AddPolicy("Exceptions.ViewAll", policy =>
            policy.RequireClaim("permission", "exceptions.view.all"));

        options.AddPolicy("Exceptions.Investigate", policy =>
            policy.RequireClaim("permission", "exceptions.investigate"));

        options.AddPolicy("Exceptions.Approve", policy =>
            policy.RequireClaim("permission", "exceptions.approve"));

        options.AddPolicy("Exceptions.Reject", policy =>
            policy.RequireClaim("permission", "exceptions.reject"));

        // Warnings policies
        options.AddPolicy("Warnings.ViewOwn", policy =>
            policy.RequireClaim("permission", "warnings.view.own"));

        options.AddPolicy("Warnings.ViewTeam", policy =>
            policy.RequireClaim("permission", "warnings.view.team"));

        options.AddPolicy("Warnings.ViewDepartment", policy =>
            policy.RequireClaim("permission", "warnings.view.department"));

        options.AddPolicy("Warnings.ViewAll", policy =>
            policy.RequireClaim("permission", "warnings.view.all"));

        options.AddPolicy("Warnings.Create", policy =>
            policy.RequireClaim("permission", "warnings.create"));

        options.AddPolicy("Warnings.Upload", policy =>
            policy.RequireClaim("permission", "warnings.upload"));

        options.AddPolicy("Warnings.Issue", policy =>
            policy.RequireClaim("permission", "warnings.issue"));

        // Escalations policies
        options.AddPolicy("Escalations.ViewOwn", policy =>
            policy.RequireClaim("permission", "escalations.view.own"));

        options.AddPolicy("Escalations.ViewTeam", policy =>
            policy.RequireClaim("permission", "escalations.view.team"));

        options.AddPolicy("Escalations.ViewDepartment", policy =>
            policy.RequireClaim("permission", "escalations.view.department"));

        options.AddPolicy("Escalations.ViewAll", policy =>
            policy.RequireClaim("permission", "escalations.view.all"));

        options.AddPolicy("Escalations.Manage", policy =>
            policy.RequireClaim("permission", "escalations.manage"));

        // Employees policies
        options.AddPolicy("Employees.ViewOwn", policy =>
            policy.RequireClaim("permission", "employees.view.own"));

        options.AddPolicy("Employees.ViewAll", policy =>
            policy.RequireClaim("permission", "employees.view.all"));

        options.AddPolicy("Employees.Create", policy =>
            policy.RequireClaim("permission", "employees.create"));

        options.AddPolicy("Employees.Edit", policy =>
            policy.RequireClaim("permission", "employees.edit"));

        options.AddPolicy("Employees.Delete", policy =>
            policy.RequireClaim("permission", "employees.delete"));

        options.AddPolicy("Employees.AssignRole", policy =>
            policy.RequireClaim("permission", "employees.assign.role"));

        // Reports policies
        options.AddPolicy("Reports.ViewOwn", policy =>
            policy.RequireClaim("permission", "reports.view.own"));

        options.AddPolicy("Reports.ViewTeam", policy =>
            policy.RequireClaim("permission", "reports.view.team"));

        options.AddPolicy("Reports.ViewDepartment", policy =>
            policy.RequireClaim("permission", "reports.view.department"));

        options.AddPolicy("Reports.ViewAll", policy =>
            policy.RequireClaim("permission", "reports.view.all"));

        options.AddPolicy("Reports.Generate", policy =>
            policy.RequireClaim("permission", "reports.generate"));

        // System policies
        options.AddPolicy("System.ViewLogs", policy =>
            policy.RequireClaim("permission", "system.view.logs"));

        options.AddPolicy("System.ManageSettings", policy =>
            policy.RequireClaim("permission", "system.manage.settings"));

        options.AddPolicy("System.ManageRoles", policy =>
            policy.RequireClaim("permission", "system.manage.roles"));

        options.AddPolicy("System.ManagePermissions", policy =>
            policy.RequireClaim("permission", "system.manage.permissions"));

        options.AddPolicy("System.ViewAuditTrail", policy =>
            policy.RequireClaim("permission", "system.view.audit"));
    }
}
