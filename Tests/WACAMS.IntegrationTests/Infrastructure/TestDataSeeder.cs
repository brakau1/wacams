using Wacam.Api.Data;
using WACAMS.Core.Entities;
using WACAMS.Api.Data;

namespace WACAMS.IntegrationTests.Infrastructure;

/// <summary>
/// Seeds the test database with realistic employee, department, attendance, warning,
/// escalation, exception, and RBAC data for integration tests.
/// </summary>
public static class TestDataSeeder
{
    public static void SeedTestData(WacamDbContext context)
    {
        // ── Departments ──
        var productionDept = new Department
        {
            Id = 1,
            Name = "Production",
            Description = "Paint line production department",
            TargetAttendancePercent = 95.0m,
            CreatedAt = DateTime.UtcNow
        };

        var engineeringDept = new Department
        {
            Id = 2,
            Name = "Engineering",
            Description = "Engineering department",
            TargetAttendancePercent = 95.0m,
            CreatedAt = DateTime.UtcNow
        };

        context.Departments.AddRange(productionDept, engineeringDept);

        // ── Employees ──
        // Production Dept hierarchy: Operator → TeamLeader → Supervisor → Manager
        var empOperator1 = new Employee
        {
            EmployeeId = "708513",
            DisplayName = "Lesedi Mashile",
            Email = "lesedi@yfpoza.com",
            PhoneNumber = "0831234567",
            DepartmentId = 1,
            Role = "Operator",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
            IsActive = true,
            ManagerId = "707995",
            CreatedAt = DateTime.UtcNow
        };

        var empOperator2 = new Employee
        {
            EmployeeId = "708155",
            DisplayName = "Tshepiso Tau",
            Email = "tshepiso@yfpoza.com",
            DepartmentId = 1,
            Role = "Operator",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
            IsActive = true,
            ManagerId = "707995",
            CreatedAt = DateTime.UtcNow
        };

        var empOperator3 = new Employee
        {
            EmployeeId = "802987",
            DisplayName = "Kagiso Modiba",
            Email = "kagiso@yfpoza.com",
            DepartmentId = 1,
            Role = "Operator",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
            IsActive = true,
            ManagerId = "707995",
            CreatedAt = DateTime.UtcNow
        };

        var empTeamLeader = new Employee
        {
            EmployeeId = "707995",
            DisplayName = "George Ntseke",
            Email = "george@yfpoza.com",
            DepartmentId = 1,
            Role = "TeamLeader",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
            IsActive = true,
            ManagerId = "800001",
            CreatedAt = DateTime.UtcNow
        };

        var empSupervisor = new Employee
        {
            EmployeeId = "800001",
            DisplayName = "Jeremiah Bezuidenhout",
            Email = "jeremiah@yfpoza.com",
            DepartmentId = 1,
            Role = "Supervisor",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
            IsActive = true,
            ManagerId = "800010",
            CreatedAt = DateTime.UtcNow
        };

        var empManager = new Employee
        {
            EmployeeId = "800010",
            DisplayName = "Production Manager",
            Email = "pm@yfpoza.com",
            DepartmentId = 1,
            Role = "Admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
            IsActive = true,
            ManagerId = null,
            CreatedAt = DateTime.UtcNow
        };

        // Engineering dept employee (for cross-department security testing)
        var empEngineering = new Employee
        {
            EmployeeId = "900001",
            DisplayName = "Eng Worker",
            Email = "eng@yfpoza.com",
            DepartmentId = 2,
            Role = "Operator",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
            IsActive = true,
            ManagerId = "800020",
            CreatedAt = DateTime.UtcNow
        };

        var empEngManager = new Employee
        {
            EmployeeId = "800020",
            DisplayName = "Eng Manager",
            Email = "engmgr@yfpoza.com",
            DepartmentId = 2,
            Role = "Supervisor",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test@1234"),
            IsActive = true,
            ManagerId = null,
            CreatedAt = DateTime.UtcNow
        };

        context.Employees.AddRange(
            empOperator1, empOperator2, empOperator3,
            empTeamLeader, empSupervisor, empManager,
            empEngineering, empEngManager
        );

        context.SaveChanges();

        // ── Attendance Records ──
        var baseDate = new DateTime(2026, 11, 10); // A Tuesday

        // Normal clock-in/clock-out (on time)
        context.AttendanceRecords.AddRange(
            new AttendanceRecord
            {
                EmployeeId = "708513", Date = baseDate, ShiftCode = "ES",
                ClockIn = new TimeSpan(5, 50, 0), ExpectedClockIn = new TimeSpan(6, 0, 0),
                ClockOut = new TimeSpan(14, 10, 0), ExpectedClockOut = new TimeSpan(14, 0, 0),
                IsLate = false, MinutesLate = 0, IsEarlyDeparture = false, MinutesEarly = 0,
                CreatedAt = DateTime.UtcNow
            },
            new AttendanceRecord
            {
                EmployeeId = "708513", Date = baseDate.AddDays(1), ShiftCode = "ES",
                ClockIn = new TimeSpan(5, 50, 0), ExpectedClockIn = new TimeSpan(6, 0, 0),
                ClockOut = new TimeSpan(14, 10, 0), ExpectedClockOut = new TimeSpan(14, 0, 0),
                IsLate = false, MinutesLate = 0, IsEarlyDeparture = false, MinutesEarly = 0,
                CreatedAt = DateTime.UtcNow
            },
            new AttendanceRecord
            {
                EmployeeId = "708513", Date = baseDate.AddDays(2), ShiftCode = "ES",
                ClockIn = new TimeSpan(5, 50, 0), ExpectedClockIn = new TimeSpan(6, 0, 0),
                ClockOut = new TimeSpan(14, 10, 0), ExpectedClockOut = new TimeSpan(14, 0, 0),
                IsLate = false, MinutesLate = 0, IsEarlyDeparture = false, MinutesEarly = 0,
                CreatedAt = DateTime.UtcNow
            }
        );

        // Late clock-in
        context.AttendanceRecords.AddRange(
            new AttendanceRecord
            {
                EmployeeId = "708155", Date = baseDate, ShiftCode = "ES",
                ClockIn = new TimeSpan(6, 25, 0), ExpectedClockIn = new TimeSpan(6, 0, 0),
                IsLate = true, MinutesLate = 25,
                CreatedAt = DateTime.UtcNow
            },
            new AttendanceRecord
            {
                EmployeeId = "708155", Date = baseDate.AddDays(1), ShiftCode = "ES",
                ClockIn = new TimeSpan(6, 30, 0), ExpectedClockIn = new TimeSpan(6, 0, 0),
                IsLate = true, MinutesLate = 30,
                CreatedAt = DateTime.UtcNow
            }
        );

        // Early departure
        context.AttendanceRecords.Add(new AttendanceRecord
        {
            EmployeeId = "802987", Date = baseDate, ShiftCode = "ES",
            ClockIn = new TimeSpan(5, 55, 0), ExpectedClockIn = new TimeSpan(6, 0, 0),
            ClockOut = new TimeSpan(13, 40, 0), ExpectedClockOut = new TimeSpan(14, 0, 0),
            IsLate = false, MinutesLate = 0,
            IsEarlyDeparture = true, MinutesEarly = 20,
            CreatedAt = DateTime.UtcNow
        });

        // Missing clock-out
        context.AttendanceRecords.Add(new AttendanceRecord
        {
            EmployeeId = "802987", Date = baseDate.AddDays(1), ShiftCode = "ES",
            ClockIn = new TimeSpan(5, 50, 0), ExpectedClockIn = new TimeSpan(6, 0, 0),
            ClockOut = null, ExpectedClockOut = new TimeSpan(14, 0, 0),
            IsLate = false, MinutesLate = 0,
            IsEarlyDeparture = false, MinutesEarly = 0,
            CreatedAt = DateTime.UtcNow
        });

        // No record at all (absenteeism) — no record created for 708155 on day 3

        context.SaveChanges();

        // ── Attendance Exceptions ──
        var excLate = new AttendanceException
        {
            EmployeeId = "708155", Date = baseDate, ExceptionType = "LateArrival",
            ExpectedClockIn = new TimeSpan(6, 0, 0), ActualClockIn = new TimeSpan(6, 25, 0),
            MinutesLate = 25, LostProductionMinutes = 25,
            Status = "Investigated", InvestigationNotes = "Confirmed late arrival.",
            InvestigatedBy = "707995", InvestigatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        var excLate2 = new AttendanceException
        {
            EmployeeId = "708155", Date = baseDate.AddDays(1), ExceptionType = "LateArrival",
            ExpectedClockIn = new TimeSpan(6, 0, 0), ActualClockIn = new TimeSpan(6, 30, 0),
            MinutesLate = 30, LostProductionMinutes = 30,
            Status = "Investigated", InvestigationNotes = "Second late this week.",
            InvestigatedBy = "707995", InvestigatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        var excEarly = new AttendanceException
        {
            EmployeeId = "802987", Date = baseDate, ExceptionType = "EarlyDeparture",
            ExpectedClockOut = new TimeSpan(14, 0, 0), ActualClockOut = new TimeSpan(13, 40, 0),
            MinutesEarly = 20, LostProductionMinutes = 20,
            Status = "Investigated", InvestigationNotes = "Left early without notice.",
            InvestigatedBy = "707995", InvestigatedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        var excMissingCO = new AttendanceException
        {
            EmployeeId = "802987", Date = baseDate.AddDays(1), ExceptionType = "MissingClockOut",
            ExpectedClockOut = new TimeSpan(14, 0, 0),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        context.AttendanceExceptions.AddRange(excLate, excLate2, excEarly, excMissingCO);
        context.SaveChanges();

        // ── Warning Letters (Progressive discipline chain) ──
        // 708155 has Counselling (from first LateArrival)
        var warningCounselling = new WarningLetter
        {
            EmployeeId = "708155", ExceptionId = excLate.Id,
            WarningType = "Counselling",
            IssueDate = DateTime.UtcNow.AddDays(-10),
            Description = "Late arrival on 2026-11-10.",
            IssuedBy = "707995",
            Acknowledged = true, AcknowledgedAt = DateTime.UtcNow.AddDays(-9),
            CreatedAt = DateTime.UtcNow
        };

        // 708155 has VerbalWarning (from second LateArrival)
        var warningVerbal = new WarningLetter
        {
            EmployeeId = "708155", ExceptionId = excLate2.Id,
            WarningType = "VerbalWarning",
            IssueDate = DateTime.UtcNow.AddDays(-5),
            Description = "Second late arrival within 90 days.",
            IssuedBy = "707995",
            Acknowledged = true, AcknowledgedAt = DateTime.UtcNow.AddDays(-4),
            CreatedAt = DateTime.UtcNow
        };

        // 802987 has Counselling (from EarlyDeparture)
        var warningCounsellingEarly = new WarningLetter
        {
            EmployeeId = "802987", ExceptionId = excEarly.Id,
            WarningType = "Counselling",
            IssueDate = DateTime.UtcNow.AddDays(-8),
            Description = "Early departure without notice.",
            IssuedBy = "707995",
            Acknowledged = true, AcknowledgedAt = DateTime.UtcNow.AddDays(-7),
            CreatedAt = DateTime.UtcNow
        };

        context.WarningLetters.AddRange(warningCounselling, warningVerbal, warningCounsellingEarly);
        context.SaveChanges();

        // ── Escalations ──
        var escalation1 = new Escalation
        {
            EmployeeId = "708155", WarningLetterId = warningVerbal.Id,
            Level = 1, ResponsibleManagerId = "800001",
            Status = "Acknowledged",
            Notes = "Escalation acknowledged by supervisor.",
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-4)
        };

        context.Escalations.Add(escalation1);
        context.SaveChanges();

        // ── GIS Sync Logs ──
        context.GISSyncLogs.AddRange(
            new GISSyncLog
            {
                SyncType = "Employees", Success = true,
                RecordsProcessed = 126, ErrorMessage = null,
                SyncedAt = DateTime.UtcNow
            },
            new GISSyncLog
            {
                SyncType = "Attendance", Success = true,
                RecordsProcessed = 500, ErrorMessage = null,
                SyncedAt = DateTime.UtcNow
            }
        );

        // ── System Settings ──
        context.SystemSettings.AddRange(
            new SystemSettings { Id = 100, SettingKey = "TestSetting", SettingValue = "true", DataType = "Boolean", CreatedAt = DateTime.UtcNow }
        );

        context.SaveChanges();
    }
}
