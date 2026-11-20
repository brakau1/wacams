using Microsoft.EntityFrameworkCore;
using WACAMS.Core.Entities;
using WACAMS.Core.Enums;

namespace WACAMS.Infrastructure.Data.SeedData;

/// <summary>
/// Seeds the Department table with standard organizational departments.
/// Each department has an attendance target percentage for KPI dashboards.
/// Production: 95%, Quality: 92%, Engineering: 90%, Maintenance: 88%, Logistics: 90%, Office: 95%, HR: 95%.
/// </summary>
public static class DepartmentSeeder
{
    public static async Task SeedAsync(WacamContext context)
    {
        if (await context.Departments.AnyAsync())
            return; // Already seeded

        var departments = new List<Department>
        {
            new()
            {
                Id = Guid.Parse("A1B2C3D4-0001-0001-A001-000000000001"),
                Name = "Production",
                Code = "PROD",
                SupervisorId = null, // Will be assigned when employees are seeded
                TargetAttendancePercent = 95m
            },
            new()
            {
                Id = Guid.Parse("A1B2C3D4-0001-0001-A001-000000000002"),
                Name = "Quality",
                Code = "QUAL",
                SupervisorId = null,
                TargetAttendancePercent = 92m
            },
            new()
            {
                Id = Guid.Parse("A1B2C3D4-0001-0001-A001-000000000003"),
                Name = "Engineering",
                Code = "ENGN",
                SupervisorId = null,
                TargetAttendancePercent = 90m
            },
            new()
            {
                Id = Guid.Parse("A1B2C3D4-0001-0001-A001-000000000004"),
                Name = "Maintenance",
                Code = "MAIN",
                SupervisorId = null,
                TargetAttendancePercent = 88m
            },
            new()
            {
                Id = Guid.Parse("A1B2C3D4-0001-0001-A001-000000000005"),
                Name = "Logistics",
                Code = "LOGS",
                SupervisorId = null,
                TargetAttendancePercent = 90m
            },
            new()
            {
                Id = Guid.Parse("A1B2C3D4-0001-0001-A001-000000000006"),
                Name = "Office",
                Code = "OFFC",
                SupervisorId = null,
                TargetAttendancePercent = 95m
            },
            new()
            {
                Id = Guid.Parse("A1B2C3D4-0001-0001-A001-000000000007"),
                Name = "HR",
                Code = "HR",
                SupervisorId = null,
                TargetAttendancePercent = 95m
            }
        };

        await context.Departments.AddRangeAsync(departments);
        await context.SaveChangesAsync();
    }
}
