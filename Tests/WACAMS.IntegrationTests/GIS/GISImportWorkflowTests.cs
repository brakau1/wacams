using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wacam.Api.Data;
using WACAMS.Api.Integration.GIS.DTOs;
using WACAMS.Api.Integration.GIS.Models;
using WACAMS.Api.Integration.GIS.Services;
using WACAMS.Core.Entities;
using WACAMS.Core.Enums;
using WACAMS.IntegrationTests.Infrastructure;
using WACAMS.Api.Data;

namespace WACAMS.IntegrationTests.GIS;

/// <summary>
/// Integration tests for the complete GIS data import flow: employee sync, attendance records,
/// shift schedule mapping, duplicate detection, error logging, and import history tracking.
/// </summary>
public class GISImportWorkflowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly WacamDbContext _context;
    private readonly TestWebApplicationFactory _factory;

    public GISImportWorkflowTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _context = factory.Services.GetRequiredService<WacamDbContext>();
    }

    #region Complete GIS Data Import Flow

    [Fact]
    public async Task SyncEmployeeRealTime_ShouldUpsertEmployeeData()
    {
        // Arrange
        var scope = _factory.Services.CreateScope();
        var gisService = scope.ServiceProvider.GetRequiredService<GISIntegrationService>();

        var employeeDto = new GISEmployeeDTO
        {
            EmployeeId = "GIS-TEST-001",
            DisplayName = "Test GIS Employee",
            Email = "test.gis@yfpoza.com",
            DepartmentCode = "PROD",
            DepartmentName = "Production",
            EmploymentStatus = "Active",
            Gender = "Male",
            Nationality = "South African",
            Phone = "0831112222",
            ManagerId = "707995"
        };

        // Act
        var result = await gisService.SyncEmployeeRealTimeAsync(employeeDto);

        // Assert
        result.Success.Should().BeTrue();
        result.RecordsProcessed.Should().Be(1);
        result.RecordsCreated.Should().Be(1);
        result.Errors.Should().BeEmpty();

        // Verify in database
        var dbEmployee = await _context.GISEmployees.FindAsync("GIS-TEST-001");
        dbEmployee.Should().NotBeNull();
        dbEmployee!.DisplayName.Should().Be("Test GIS Employee");
        dbEmployee.DepartmentCode.Should().Be("PROD");
    }

    [Fact]
    public async Task SyncAttendanceRecordRealTime_ShouldUpsertAttendanceData()
    {
        // Arrange
        var scope = _factory.Services.CreateScope();
        var gisService = scope.ServiceProvider.GetRequiredService<GISIntegrationService>();

        var recordDto = new GISAttendanceRecordDTO
        {
            EmployeeId = "GIS-TEST-001",
            AttendanceDate = new DateTime(2026, 11, 15),
            ShiftType = "Morning",
            ExpectedClockIn = new TimeSpan(6, 0, 0),
            ExpectedClockOut = new TimeSpan(14, 0, 0),
            ActualClockIn = new TimeSpan(5, 55, 0),
            ActualClockOut = new TimeSpan(14, 10, 0),
            Status = "Present",
            Source = "GIS_iWork"
        };

        // Act
        var result = await gisService.SyncAttendanceRecordRealTimeAsync(recordDto);

        // Assert
        result.Success.Should().BeTrue();

        var dbRecord = await _context.GISAttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == "GIS-TEST-001" && r.AttendanceDate.Date == new DateTime(2026, 11, 15).Date);

        dbRecord.Should().NotBeNull();
        dbRecord!.ActualClockIn.Should().Be(new TimeSpan(5, 55, 0));
        dbRecord.Status.Should().Be("Present");
    }

    [Fact]
    public async Task SyncShiftSchedule_ShouldStoreScheduleData()
    {
        // Arrange
        var scope = _factory.Services.CreateScope();
        var gisService = scope.ServiceProvider.GetRequiredService<GISIntegrationService>();

        var scheduleDto = new GISShiftScheduleDTO
        {
            EmployeeId = "GIS-TEST-001",
            ScheduleDate = new DateTime(2026, 11, 16),
            ShiftType = "Afternoon",
            ShiftCode = "MS",
            ExpectedClockIn = new TimeSpan(14, 0, 0),
            ExpectedClockOut = new TimeSpan(22, 0, 0),
            IsWorkingDay = true
        };

        // Act — This goes through the repository directly since there's no service method for shifts
        var repo = scope.ServiceProvider.GetRequiredService<WACAMS.Api.Integration.GIS.Repositories.IGISIntegrationRepository>();
        var added = await repo.AddShiftScheduleAsync(scheduleDto);

        // Assert
        added.Should().BeTrue();

        var dbSchedule = await _context.GISShiftSchedules
            .FirstOrDefaultAsync(s => s.EmployeeId == "GIS-TEST-001" && s.ScheduleDate.Date == new DateTime(2026, 11, 16).Date);

        dbSchedule.Should().NotBeNull();
        dbSchedule!.ShiftType.Should().Be("Afternoon");
        dbSchedule.ShiftCode.Should().Be("MS");
    }

    [Fact]
    public async Task SyncLeaveRecord_ShouldStoreLeaveData()
    {
        // Arrange
        var scope = _factory.Services.CreateScope();
        var gisService = scope.ServiceProvider.GetRequiredService<GISIntegrationService>();

        var leaveDto = new GISLeaveRecordDTO
        {
            EmployeeId = "GIS-TEST-001",
            LeaveDate = new DateTime(2026, 11, 17),
            LeaveType = "Sick Leave",
            LeaveReason = "Medical appointment",
            IsApproved = true,
            ApprovedBy = "707995"
        };

        // Act — Direct repository call
        var repo = scope.ServiceProvider.GetRequiredService<WACAMS.Api.Integration.GIS.Repositories.IGISIntegrationRepository>();
        var added = await repo.AddLeaveRecordAsync(leaveDto);

        // Assert
        added.Should().BeTrue();

        var dbLeave = await _context.GISLeaveRecords
            .FirstOrDefaultAsync(l => l.EmployeeId == "GIS-TEST-001" && l.LeaveDate.Date == new DateTime(2026, 11, 17).Date);

        dbLeave.Should().NotBeNull();
        dbLeave!.LeaveType.Should().Be("Sick Leave");
        dbLeave.IsApproved.Should().BeTrue();
    }

    #endregion

    #region Shift Code Mapping and Assignment

    [Fact]
    public async Task ShiftSchedule_MorningShift_ShouldMapCorrectly()
    {
        // Arrange
        var schedule = new GISShiftScheduleDTO
        {
            EmployeeId = "GIS-TEST-001",
            ScheduleDate = new DateTime(2026, 11, 16),
            ShiftType = "Morning",
            ShiftCode = "ES",
            ExpectedClockIn = new TimeSpan(6, 0, 0),
            ExpectedClockOut = new TimeSpan(14, 0, 0),
            IsWorkingDay = true
        };

        // Act
        var repo = _factory.Services.GetRequiredService<WACAMS.Api.Integration.GIS.Repositories.IGISIntegrationRepository>();
        var added = await repo.AddShiftScheduleAsync(schedule);
        added.Should().BeTrue();

        // Assert
        var dbSchedule = await _context.GISShiftSchedules.FindAsync(schedule.Id);
        dbSchedule.Should().NotBeNull();
        dbSchedule!.ShiftType.Should().Be("Morning");
        dbSchedule.ExpectedClockIn.Should().Be(new TimeSpan(6, 0, 0));
        dbSchedule.ExpectedClockOut.Should().Be(new TimeSpan(14, 0, 0));
    }

    [Fact]
    public async Task ShiftSchedule_AfternoonShift_ShouldMapCorrectly()
    {
        // Arrange
        var schedule = new GISShiftScheduleDTO
        {
            EmployeeId = "GIS-TEST-001",
            ScheduleDate = new DateTime(2026, 11, 16),
            ShiftType = "Afternoon",
            ShiftCode = "MS",
            ExpectedClockIn = new TimeSpan(14, 0, 0),
            ExpectedClockOut = new TimeSpan(22, 0, 0),
            IsWorkingDay = true
        };

        // Act
        var repo = _factory.Services.GetRequiredService<WACAMS.Api.Integration.GIS.Repositories.IGISIntegrationRepository>();
        await repo.AddShiftScheduleAsync(schedule);

        // Assert
        var dbSchedule = await _context.GISShiftSchedules
            .FirstOrDefaultAsync(s => s.EmployeeId == "GIS-TEST-001" && s.ScheduleDate.Date == new DateTime(2026, 11, 16).Date);

        dbSchedule.Should().NotBeNull();
        dbSchedule!.ExpectedClockIn.Should().Be(new TimeSpan(14, 0, 0));
        dbSchedule.ExpectedClockOut.Should().Be(new TimeSpan(22, 0, 0));
    }

    [Fact]
    public async Task ShiftSchedule_NightShift_ShouldMapCorrectly()
    {
        // Arrange
        var schedule = new GISShiftScheduleDTO
        {
            EmployeeId = "GIS-TEST-001",
            ScheduleDate = new DateTime(2026, 11, 16),
            ShiftType = "Night",
            ShiftCode = "NS",
            ExpectedClockIn = new TimeSpan(22, 0, 0),
            ExpectedClockOut = new TimeSpan(6, 0, 0),
            IsWorkingDay = true
        };

        // Act
        var repo = _factory.Services.GetRequiredService<WACAMS.Api.Integration.GIS.Repositories.IGISIntegrationRepository>();
        await repo.AddShiftScheduleAsync(schedule);

        // Assert
        var dbSchedule = await _context.GISShiftSchedules
            .FirstOrDefaultAsync(s => s.EmployeeId == "GIS-TEST-001" && s.ScheduleDate.Date == new DateTime(2026, 11, 16).Date);

        dbSchedule.Should().NotBeNull();
        dbSchedule!.ShiftType.Should().Be("Night");
        dbSchedule.ExpectedClockIn.Should().Be(new TimeSpan(22, 0, 0));
        dbSchedule.ExpectedClockOut.Should().Be(new TimeSpan(6, 0, 0));
    }

    #endregion

    #region Duplicate Detection and Handling

    [Fact]
    public async Task UpsertEmployee_Duplicate_ShouldUpdateInsteadOfCreate()
    {
        // Arrange — First insert
        var scope = _factory.Services.CreateScope();
        var gisService = scope.ServiceProvider.GetRequiredService<GISIntegrationService>();

        var empDto = new GISEmployeeDTO
        {
            EmployeeId = "GIS-DUP-001",
            DisplayName = "Duplicate Test Employee",
            Email = "dup.test@yfpoza.com",
            DepartmentCode = "ENG",
            DepartmentName = "Engineering",
            EmploymentStatus = "Active"
        };

        var result1 = await gisService.SyncEmployeeRealTimeAsync(empDto);
        result1.Success.Should().BeTrue();
        result1.RecordsCreated.Should().Be(1);

        // Act — Upsert same employee with updated data
        empDto.DisplayName = "Duplicate Test Employee Updated";
        empDto.Email = "updated.dup@yfpoza.com";
        var result2 = await gisService.SyncEmployeeRealTimeAsync(empDto);

        // Assert
        result2.Success.Should().BeTrue();
        result2.RecordsCreated.Should().Be(0); // Not created again
        result2.RecordsProcessed.Should().Be(1);

        var dbEmp = await _context.GISEmployees.FindAsync("GIS-DUP-001");
        dbEmp.Should().NotBeNull();
        dbEmp!.DisplayName.Should().Be("Duplicate Test Employee Updated");
        dbEmp.Email.Should().Be("updated.dup@yfpoza.com");
    }

    [Fact]
    public async Task AttendanceRecord_Duplicate_ShouldBeHandledUniquely()
    {
        // Arrange — Unique constraint on (EmployeeId, AttendanceDate, ShiftType)
        var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<WACAMS.Api.Integration.GIS.Repositories.IGISIntegrationRepository>();

        var record = new GISAttendanceRecordDTO
        {
            EmployeeId = "GIS-TEST-001",
            AttendanceDate = new DateTime(2026, 11, 20),
            ShiftType = "Morning",
            Status = "Present"
        };

        // Insert first time
        var added1 = await repo.UpsertAttendanceRecordAsync(record);
        added1.Should().BeTrue();

        // Insert same key again
        record.Status = "Late";
        var added2 = await repo.UpsertAttendanceRecordAsync(record);
        added2.Should().BeTrue(); // Should upsert, not fail

        // Assert — Only one record should exist for this key combination
        var count = await _context.GISAttendanceRecords.CountAsync(
            r => r.EmployeeId == "GIS-TEST-001" &&
                 r.AttendanceDate.Date == new DateTime(2026, 11, 20).Date &&
                 r.ShiftType == "Morning");

        count.Should().Be(1);
    }

    [Fact]
    public async Task ShiftSchedule_Duplicate_ShouldBeHandledUniquely()
    {
        // Arrange — Unique constraint on (EmployeeId, ScheduleDate)
        var repo = _factory.Services.GetRequiredService<WACAMS.Api.Integration.GIS.Repositories.IGISIntegrationRepository>();

        var schedule = new GISShiftScheduleDTO
        {
            EmployeeId = "GIS-TEST-001",
            ScheduleDate = new DateTime(2026, 11, 21),
            ShiftType = "Morning",
            ShiftCode = "ES",
            IsWorkingDay = true
        };

        // Insert twice
        var added1 = await repo.AddShiftScheduleAsync(schedule);
        added1.Should().BeTrue();

        schedule.ShiftType = "Afternoon";
        var added2 = await repo.AddShiftScheduleAsync(schedule);
        added2.Should().BeTrue();

        // Assert — Only one record per employee per date
        var count = await _context.GISShiftSchedules.CountAsync(
            s => s.EmployeeId == "GIS-TEST-001" &&
                 s.ScheduleDate.Date == new DateTime(2026, 11, 21).Date);

        count.Should().Be(1);
    }

    #endregion

    #region Error Logging for Invalid Records

    [Fact]
    public async Task SyncWithException_ShouldLogErrorToSyncLog()
    {
        // Arrange
        var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<WACAMS.Api.Integration.GIS.Repositories.IGISIntegrationRepository>();

        // Act — Record a failed sync attempt
        await repo.RecordSyncAttemptAsync("Employees_TestError", false, 0, "Simulated connection timeout");

        // Assert
        var log = await _context.GISSyncLogs
            .OrderByDescending(l => l.SyncedAt)
            .FirstOrDefaultAsync();

        log.Should().NotBeNull();
        log!.Success.Should().BeFalse();
        log.ErrorMessage.Should().Contain("connection timeout");
        log.SyncType.Should().Be("Employees_TestError");
    }

    [Fact]
    public async Task MultipleSyncErrors_ShouldEachBeLogged()
    {
        // Arrange
        var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<WACAMS.Api.Integration.GIS.Repositories.IGISIntegrationRepository>();

        // Act — Record multiple failed sync attempts
        await repo.RecordSyncAttemptAsync("Attendance_Error1", false, 0, "Error 1");
        await repo.RecordSyncAttemptAsync("Attendance_Error2", false, 0, "Error 2");
        await repo.RecordSyncAttemptAsync("Employees_Success", true, 10, null);

        // Assert
        var logs = await _context.GISSyncLogs
            .Where(l => l.SyncType.Contains("Error") || l.SyncType == "Employees_Success")
            .ToListAsync();

        logs.Should().HaveCount(3);
        logs.Any(l => !l.Success && l.ErrorMessage?.Contains("Error 1") == true).Should().BeTrue();
        logs.Any(l => !l.Success && l.ErrorMessage?.Contains("Error 2") == true).Should().BeTrue();
        logs.Any(l => l.Success && l.RecordsProcessed == 10).Should().BeTrue();
    }

    #endregion

    #region Import History Tracking

    [Fact]
    public async Task SyncHistory_ShouldTrackAllAttempts()
    {
        // Arrange — Seeder already has 2 sync logs
        var initialCount = await _context.GISSyncLogs.CountAsync();
        initialCount.Should().BeGreaterOrEqualTo(2);

        // Act — Add more sync logs
        var scope = _factory.Services.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<WACAMS.Api.Integration.GIS.Repositories.IGISIntegrationRepository>();

        await repo.RecordSyncAttemptAsync("Employees_Batch1", true, 50, null);
        await repo.RecordSyncAttemptAsync("Employees_Batch2", true, 30, null);
        await repo.RecordSyncAttemptAsync("Attendance_Batch1", false, 0, "API rate limit");

        // Assert
        var totalLogs = await _context.GISSyncLogs.CountAsync();
        totalLogs.Should().BeGreaterOrEqualTo(initialCount + 3);

        // Verify specific entries
        var batch1 = await _context.GISSyncLogs.FirstOrDefaultAsync(l => l.SyncType == "Employees_Batch1");
        batch1.Should().NotBeNull();
        batch1!.RecordsProcessed.Should().Be(50);
        batch1.Success.Should().BeTrue();
    }

    [Fact]
    public async Task SyncHistory_ByType_ShouldReturnCorrectSubset()
    {
        // Act
        var employeeLogs = await _context.GISSyncLogs
            .Where(l => l.SyncType.Contains("Employee"))
            .ToListAsync();

        var attendanceLogs = await _context.GISSyncLogs
            .Where(l => l.SyncType.Contains("Attendance"))
            .ToListAsync();

        // Assert
        employeeLogs.Should().NotBeEmpty();
        attendanceLogs.Should().NotBeEmpty();

        foreach (var log in employeeLogs)
            log.SyncType.Should().Contain("Employee");

        foreach (var log in attendanceLogs)
            log.SyncType.Should().Contain("Attendance");
    }

    #endregion

    #region Exception Detection via GIS Service

    [Fact]
    public async Task DetectLateArrivals_ShouldIdentifyLateness()
    {
        // Arrange — Create a GIS attendance record that is late
        var scope = _factory.Services.CreateScope();
        var gisService = scope.ServiceProvider.GetRequiredService<GISIntegrationService>();

        var lateRecord = new GISAttendanceRecord
        {
            EmployeeId = "GIS-TEST-001",
            AttendanceDate = new DateTime(2026, 11, 22),
            ShiftType = "Morning",
            ExpectedClockIn = new TimeSpan(6, 0, 0),
            ExpectedClockOut = new TimeSpan(14, 0, 0),
            ActualClockIn = new TimeSpan(6, 35, 0), // 35 min late
            ActualClockOut = null,
            Status = "Late",
            Source = "GIS_iWork"
        };

        _context.GISAttendanceRecords.Add(lateRecord);
        await _context.SaveChangesAsync();

        // Act
        var exceptions = await gisService.DetectLateArrivalsAsync(new DateTime(2026, 11, 22));

        // Assert
        exceptions.Should().NotBeEmpty();
        var lateExc = exceptions.First(e => e.ExceptionType == "LateArrival");
        lateExc.EmployeeId.Should().Be("GIS-TEST-001");
        lateExc.MinutesLost.Should().Be(35);
        lateExc.Severity.Should().Be("Medium"); // >15 but <=30 would be Medium, >30 is High
    }

    [Fact]
    public async Task DetectEarlyDepartures_ShouldIdentifyEarlyLeaving()
    {
        // Arrange
        var scope = _factory.Services.CreateScope();
        var gisService = scope.ServiceProvider.GetRequiredService<GISIntegrationService>();

        var earlyRecord = new GISAttendanceRecord
        {
            EmployeeId = "GIS-TEST-001",
            AttendanceDate = new DateTime(2026, 11, 23),
            ShiftType = "Morning",
            ExpectedClockIn = new TimeSpan(6, 0, 0),
            ExpectedClockOut = new TimeSpan(14, 0, 0),
            ActualClockIn = new TimeSpan(5, 55, 0),
            ActualClockOut = new TimeSpan(13, 20, 0), // 40 min early
            Status = "Early",
            Source = "GIS_iWork"
        };

        _context.GISAttendanceRecords.Add(earlyRecord);
        await _context.SaveChangesAsync();

        // Act
        var exceptions = await gisService.DetectEarlyDeparturesAsync(new DateTime(2026, 11, 23));

        // Assert
        exceptions.Should().NotBeEmpty();
        var earlyExc = exceptions.First(e => e.ExceptionType == "EarlyDeparture");
        earlyExc.EmployeeId.Should().Be("GIS-TEST-001");
        earlyExc.MinutesLost.Should().Be(40);
    }

    [Fact]
    public async Task DetectMissingTransactions_ShouldFindMissingClockOut()
    {
        // Arrange
        var scope = _factory.Services.CreateScope();
        var gisService = scope.ServiceProvider.GetRequiredService<GISIntegrationService>();

        var record = new GISAttendanceRecord
        {
            EmployeeId = "GIS-TEST-001",
            AttendanceDate = new DateTime(2026, 11, 24),
            ShiftType = "Morning",
            ExpectedClockIn = new TimeSpan(6, 0, 0),
            ExpectedClockOut = new TimeSpan(14, 0, 0),
            ActualClockIn = new TimeSpan(5, 50, 0),
            ActualClockOut = null, // Missing clock-out
            Status = "Partial",
            Source = "GIS_iWork"
        };

        _context.GISAttendanceRecords.Add(record);
        await _context.SaveChangesAsync();

        // Act
        var exceptions = await gisService.DetectMissingTransactionsAsync(new DateTime(2026, 11, 24));

        // Assert
        var missingCO = exceptions.FirstOrDefault(e => e.ExceptionType == "MissingClockOut");
        missingCO.Should().NotBeNull();
        missingCO!.EmployeeId.Should().Be("GIS-TEST-001");
    }

    [Fact]
    public async Task DetectUnplannedAbsences_ShouldFindAbsentScheduledEmployees()
    {
        // Arrange — Employee scheduled but no attendance record and no approved leave
        var scope = _factory.Services.CreateScope();
        var gisService = scope.ServiceProvider.GetRequiredService<GISIntegrationService>();

        var schedule = new GISShiftSchedule
        {
            EmployeeId = "GIS-ABSENT-001",
            ScheduleDate = new DateTime(2026, 11, 25),
            ShiftType = "Morning",
            ShiftCode = "ES",
            ExpectedClockIn = new TimeSpan(6, 0, 0),
            ExpectedClockOut = new TimeSpan(14, 0, 0),
            IsWorkingDay = true
        };

        _context.GISShiftSchedules.Add(schedule);
        await _context.SaveChangesAsync();

        // Act
        var exceptions = await gisService.DetectUnplannedAbsencesAsync(new DateTime(2026, 11, 25));

        // Assert
        var absent = exceptions.FirstOrDefault(e => e.ExceptionType == "UnplannedAbsence");
        absent.Should().NotBeNull();
        absent!.EmployeeId.Should().Be("GIS-ABSENT-001");
    }

    [Fact]
    public async Task DetectExceptions_Comprehensive_ShouldDetectMultipleTypes()
    {
        // Arrange
        var scope = _factory.Services.CreateScope();
        var gisService = scope.ServiceProvider.GetRequiredService<GISIntegrationService>();

        // Late arrival record
        _context.GISAttendanceRecords.Add(new GISAttendanceRecord
        {
            EmployeeId = "GIS-TEST-001",
            AttendanceDate = new DateTime(2026, 11, 26),
            ShiftType = "Morning",
            ExpectedClockIn = new TimeSpan(6, 0, 0),
            ExpectedClockOut = new TimeSpan(14, 0, 0),
            ActualClockIn = new TimeSpan(6, 40, 0),
            Status = "Late",
            Source = "GIS_iWork"
        });
        await _context.SaveChangesAsync();

        // Act
        var allExceptions = await gisService.DetectExceptionsAsync(new DateTime(2026, 11, 26));

        // Assert
        allExceptions.Should().NotBeEmpty();
        allExceptions.Any(e => e.ExceptionType == "LateArrival").Should().BeTrue();
    }

    #endregion

    #region Clock Transaction Processing

    [Fact]
    public async Task SyncClockTransaction_ShouldPersistAndProcess()
    {
        // Arrange
        var scope = _factory.Services.CreateScope();
        var gisService = scope.ServiceProvider.GetRequiredService<GISIntegrationService>();

        var transaction = new GISClockTransactionDTO
        {
            EmployeeId = "GIS-TEST-001",
            TransactionDateTime = new DateTime(2026, 11, 27, 5, 50, 0),
            TransactionType = "ClockIn",
            TerminalId = "TERM-001",
            TerminalLocation = "Main Entrance"
        };

        // Act
        var result = await gisService.SyncClockTransactionRealTimeAsync(transaction);

        // Assert
        result.Success.Should().BeTrue();
        result.RecordsProcessed.Should().Be(1);

        var dbTxn = await _context.GISClockTransactions
            .FirstOrDefaultAsync(t => t.EmployeeId == "GIS-TEST-001" &&
                                      t.TransactionDateTime.Date == new DateTime(2026, 11, 27).Date);
        dbTxn.Should().NotBeNull();
        dbTxn!.TransactionType.Should().Be("ClockIn");
    }

    [Fact]
    public async Task ClockTransaction_ClockOut_ShouldMarkCorrectly()
    {
        // Arrange
        var scope = _factory.Services.CreateScope();
        var gisService = scope.ServiceProvider.GetRequiredService<GISIntegrationService>();

        var transaction = new GISClockTransactionDTO
        {
            EmployeeId = "GIS-TEST-001",
            TransactionDateTime = new DateTime(2026, 11, 27, 14, 10, 0),
            TransactionType = "ClockOut",
            TerminalId = "TERM-001",
            TerminalLocation = "Main Entrance"
        };

        // Act
        var result = await gisService.SyncClockTransactionRealTimeAsync(transaction);

        // Assert
        result.Success.Should().BeTrue();

        var dbTxn = await _context.GISClockTransactions
            .FirstOrDefaultAsync(t => t.TransactionType == "ClockOut" &&
                                      t.EmployeeId == "GIS-TEST-001");
        dbTxn.Should().NotBeNull();
    }

    #endregion

    #region Query Operations

    [Fact]
    public async Task GetEmployee_ShouldReturnExistingGISData()
    {
        // Arrange
        var scope = _factory.Services.CreateScope();
        var gisService = scope.ServiceProvider.GetRequiredService<GISIntegrationService>();

        // Act
        var employee = await gisService.GetEmployeeAsync("GIS-TEST-001");

        // Assert
        employee.Should().NotBeNull();
        employee!.EmployeeId.Should().Be("GIS-TEST-001");
    }

    [Fact]
    public async Task GetEmployee_NotFound_ShouldReturnNull()
    {
        // Arrange
        var scope = _factory.Services.CreateScope();
        var gisService = scope.ServiceProvider.GetRequiredService<GISIntegrationService>();

        // Act
        var employee = await gisService.GetEmployeeAsync("NONEXISTENT");

        // Assert
        employee.Should().BeNull();
    }

    [Fact]
    public async Task GetAttendanceRecords_DateRange_ShouldReturnFiltered()
    {
        // Arrange
        var scope = _factory.Services.CreateScope();
        var gisService = scope.ServiceProvider.GetRequiredService<GISIntegrationService>();

        // Act
        var records = await gisService.GetAttendanceRecordsAsync(
            "GIS-TEST-001",
            new DateTime(2026, 11, 1),
            new DateTime(2026, 11, 30));

        // Assert
        records.Should().NotBeEmpty();
        foreach (var r in records)
        {
            r.EmployeeId.Should().Be("GIS-TEST-001");
            r.AttendanceDate.Should().BeInRange(
                new DateTime(2026, 11, 1), new DateTime(2026, 11, 30));
        }
    }

    #endregion
}
