using Microsoft.EntityFrameworkCore;
using WACAMS.Core.Constants;
using WACAMS.Core.Entities;

namespace WACAMS.Infrastructure.Data.SeedData;

/// <summary>
/// Seeds the ShiftSchedule table with default shift configurations.
/// Creates one-shift-per-code template entries that serve as the baseline for all employee schedules.
/// Individual employee schedules can override these defaults via specific date entries.
/// </summary>
public static class ShiftConfigSeeder
{
    public static async Task SeedAsync(WacamContext context)
    {
        if (await context.ShiftSchedules.AnyAsync())
            return; // Already seeded

        var shifts = new List<ShiftSchedule>
        {
            new()
            {
                Id = Guid.Parse("C3D4E5F6-0001-0001-C001-000000000001"),
                EmployeeId = "000000", // System-wide template (applies to all employees by default)
                Date = DateTime.UtcNow.Date,
                ShiftCode = ShiftConstants.Morning.Code,
                StartTime = ShiftConstants.Morning.StartTime,
                EndTime = ShiftConstants.Morning.EndTime,
                Cancelled = false,
                CancellationReason = null
            },
            new()
            {
                Id = Guid.Parse("C3D4E5F6-0001-0001-C001-000000000002"),
                EmployeeId = "000000",
                Date = DateTime.UtcNow.Date,
                ShiftCode = ShiftConstants.Afternoon.Code,
                StartTime = ShiftConstants.Afternoon.StartTime,
                EndTime = ShiftConstants.Afternoon.EndTime,
                Cancelled = false,
                CancellationReason = null
            },
            new()
            {
                Id = Guid.Parse("C3D4E5F6-0001-0001-C001-000000000003"),
                EmployeeId = "000000",
                Date = DateTime.UtcNow.Date,
                ShiftCode = ShiftConstants.Night.Code,
                StartTime = ShiftConstants.Night.StartTime,
                EndTime = ShiftConstants.Night.EndTime,
                Cancelled = false,
                CancellationReason = null
            },
            new()
            {
                Id = Guid.Parse("C3D4E5F6-0001-0001-C001-000000000004"),
                EmployeeId = "000000",
                Date = DateTime.UtcNow.Date,
                ShiftCode = ShiftConstants.LongDay.Code,
                StartTime = ShiftConstants.LongDay.StartTime,
                EndTime = ShiftConstants.LongDay.EndTime,
                Cancelled = false,
                CancellationReason = null
            },
            new()
            {
                Id = Guid.Parse("C3D4E5F6-0001-0001-C001-000000000005"),
                EmployeeId = "000000",
                Date = DateTime.UtcNow.Date,
                ShiftCode = ShiftConstants.Day.Code,
                StartTime = ShiftConstants.Day.StartTime,
                EndTime = ShiftConstants.Day.EndTime,
                Cancelled = false,
                CancellationReason = null
            }
        };

        await context.ShiftSchedules.AddRangeAsync(shifts);
        await context.SaveChangesAsync();
    }
}
