using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WACAMS.Core.Entities;

namespace WACAMS.Infrastructure.Persistence.Configuration;

/// <summary>
/// FluentAPI configuration for the Employee entity.
/// Configures primary key, unique index on EmployeeId, foreign keys, and relationships.
/// </summary>
public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        // Primary Key
        builder.HasKey(e => e.Id);

        // Unique index on EmployeeId (master business key)
        builder.HasIndex(e => e.EmployeeId)
            .IsUnique()
            .HasDatabaseName("IX_Employees_EmployeeId");

        // Property configurations
        builder.Property(e => e.Id)
            .ValueGeneratedOnAdd();

        builder.Property(e => e.EmployeeId)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnName("EmployeeId");

        builder.Property(e => e.DisplayName)
            .IsRequired()
            .HasMaxLength(150)
            .HasColumnName("DisplayName");

        builder.Property(e => e.Email)
            .HasMaxLength(200)
            .HasColumnName("Email");

        builder.Property(e => e.Phone)
            .HasMaxLength(20)
            .HasColumnName("Phone");

        // Department Foreign Key
        builder.Property(e => e.DepartmentId)
            .IsRequired();

        builder.HasOne(e => e.Department)
            .WithMany(d => d.Employees)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Employees_Departments_DepartmentId");

        // ManagerId — self-referencing FK (nullable for top-level managers)
        builder.Property(e => e.ManagerId)
            .HasMaxLength(50)
            .HasColumnName("ManagerId");

        // Status enum
        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasColumnName("Status")
            .HasDefaultValue(Core.Enums.EmployeeStatus.Active);

        // HireDate
        builder.Property(e => e.HireDate)
            .IsRequired()
            .HasColumnName("HireDate");

        // ShiftCode
        builder.Property(e => e.ShiftCode)
            .HasMaxLength(10)
            .HasColumnName("ShiftCode");

        // Indexes for common queries
        builder.HasIndex(e => e.DepartmentId)
            .HasDatabaseName("IX_Employees_DepartmentId");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_Employees_Status");

        // Relationships
        builder.HasMany(e => e.DirectReports)
            .WithOne()
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_Employees_Managers_ManagerId");

        // One-to-one relationship between Employee and User (configured in UserConfiguration)
    }
}
