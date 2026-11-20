using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WACAMS.Core.Entities;

namespace WACAMS.Infrastructure.Persistence.Configuration;

/// <summary>
/// FluentAPI configuration for the Department entity.
/// Configures Id PK, Name unique, Code unique, SupervisorId FK, AttendanceTargetPercentage decimal(5,2).
/// </summary>
public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        // Primary Key
        builder.HasKey(d => d.Id);

        // Unique indexes on Name and Code
        builder.HasIndex(d => d.Name)
            .IsUnique()
            .HasDatabaseName("IX_Departments_Name");

        builder.HasIndex(d => d.Code)
            .IsUnique()
            .HasDatabaseName("IX_Departments_Code");

        // Property configurations
        builder.Property(d => d.Id)
            .ValueGeneratedOnAdd();

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(d => d.Code)
            .IsRequired()
            .HasMaxLength(20);

        // SupervisorId is nullable — not all departments have a designated supervisor
        builder.Property(d => d.SupervisorId)
            .HasMaxLength(50);

        // TargetAttendancePercent as decimal(5,2) — supports up to 999.99%
        builder.Property(d => d.TargetAttendancePercent)
            .HasColumnType("decimal(5,2)")
            .HasDefaultValue(95m);

        // Relationships — SupervisorId references Employee.EmployeeId (nullable)
        // This is a soft reference; the Department entity does not have an Employee navigation property for SupervisorId.
    }
}
