using WACAMS.Core.Entities;

namespace WACAMS.Tests.TestData;

/// <summary>
/// Test data factory for Department entities.
/// Covers 5 departments with parent/child hierarchy relationships and department-specific settings.
/// </summary>
public static class DepartmentFixtures
{
    /// <summary>
    /// Creates all 5 departments with realistic settings for a manufacturing environment.
    /// Hierarchy: Root → Sub-departments (e.g., Production → Assembly, Painting).
    /// </summary>
    public static List<Department> CreateAllDepartments()
    {
        return new List<Department>
        {
            // Top-level: Production (ID 1)
            new()
            {
                Id = 1,
                Name = "Production",
                Description = "Main production line operations including painting, assembly, and quality control.",
                ParentId = null,
                TargetAttendancePercent = 95.0m,
                CreatedAt = DateTime.UtcNow.AddDays(-365),
            },

            // Top-level: Quality (ID 2)
            new()
            {
                Id = 2,
                Name = "Quality Assurance",
                Description = "Quality inspection, testing, and compliance verification.",
                ParentId = null,
                TargetAttendancePercent = 95.0m,
                CreatedAt = DateTime.UtcNow.AddDays(-365),
            },

            // Top-level: Logistics (ID 3)
            new()
            {
                Id = 3,
                Name = "Logistics & Warehouse",
                Description = "Material handling, warehouse management, and shipping operations.",
                ParentId = null,
                TargetAttendancePercent = 93.0m,
                CreatedAt = DateTime.UtcNow.AddDays(-365),
            },

            // Top-level: Maintenance (ID 4)
            new()
            {
                Id = 4,
                Name = "Maintenance & Engineering",
                Description = "Equipment maintenance, repairs, and engineering support.",
                ParentId = null,
                TargetAttendancePercent = 94.0m,
                CreatedAt = DateTime.UtcNow.AddDays(-365),
            },

            // Top-level: Admin & HR (ID 5)
            new Department
            {
                Id = 5,
                Name = "Administration & Human Resources",
                Description = "HR management, payroll, admin services, and office support.",
                ParentId = null,
                TargetAttendancePercent = 96.0m,
                CreatedAt = DateTime.UtcNow.AddDays(-365),
            },

            // Child: Painting (under Production, ID 6)
            new()
            {
                Id = 6,
                Name = "Painting Line",
                Description = "Vehicle panel painting operations — primary production area.",
                ParentId = 1,
                TargetAttendancePercent = 96.0m,
                CreatedAt = DateTime.UtcNow.AddDays(-300),
            },

            // Child: Assembly (under Production, ID 7)
            new()
            {
                Id = 7,
                Name = "Assembly Line",
                Description = "Final assembly and integration of painted components.",
                ParentId = 1,
                TargetAttendancePercent = 95.0m,
                CreatedAt = DateTime.UtcNow.AddDays(-300),
            },

            // Child: Incoming QC (under Quality, ID 8)
            new()
            {
                Id = 8,
                Name = "Incoming Quality Control",
                Description = "Inspection of incoming raw materials and components.",
                ParentId = 2,
                TargetAttendancePercent = 95.0m,
                CreatedAt = DateTime.UtcNow.AddDays(-200),
            },

            // Child: Outgoing QC (under Quality, ID 9)
            new()
            {
                Id = 9,
                Name = "Outgoing Quality Control",
                Description = "Final inspection of finished products before shipment.",
                ParentId = 2,
                TargetAttendancePercent = 95.0m,
                CreatedAt = DateTime.UtcNow.AddDays(-200),
            },
        };
    }

    /// <summary>
    /// Gets the Production department (root level).
    /// </summary>
    public static Department GetProductionDepartment() => CreateAllDepartments().First(d => d.Id == 1);

    /// <summary>
    /// Gets the Painting Line sub-department.
    /// </summary>
    public static Department GetPaintingLineDepartment() => CreateAllDepartments().First(d => d.Id == 6);

    /// <summary>
    /// Gets a department with a lower attendance target (for testing threshold alerts).
    /// </summary>
    public static Department GetLowTargetDepartment() => CreateAllDepartments().First(d => d.Id == 3);
}
