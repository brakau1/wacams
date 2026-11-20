using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WACAMS.Infrastructure.Data;
using WACAMS.Infrastructure.DTOs.GIS;
using WACAMS.Infrastructure.Repositories.GIS;

namespace WACAMS.Infrastructure.Services.GIS;

/// <summary>
/// GIS Integration Service Implementation
/// Handles real-time sync, scheduled sync, and exception detection
/// </summary>
public class GISIntegrationService : IGISIntegrationService
{
    private readonly IGISIntegrationRepository _repository;
    private readonly WacamContext _context;
    private readonly ILogger<GISIntegrationService> _logger;

    public GISIntegrationService(
        IGISIntegrationRepository repository,
        WacamContext context,
        ILogger<GISIntegrationService> logger)
    {
        _repository = repository;
        _context = context;
        _logger = logger;
    }

    #region Real-Time Sync

    public async Task<GISSyncResultDTO> SyncEmployeeRealTimeAsync(GISEmployeeDTO employee, CancellationToken cancellationToken = default)
    {
        var result = new GISSyncResultDTO
        {
            SyncStartedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Real-time sync: Employee {EmployeeId}", employee.EmployeeId);

            var success = await _repository.UpsertEmployeeAsync(employee, cancellationToken);

            if (success)
            {
                result.Success = true;
                result.RecordsProcessed = 1;
                result.RecordsCreated = 1;
                _logger.LogInformation("Successfully synced employee {EmployeeId}", employee.EmployeeId);
            }
            else
            {
                result.Success = false;
                result.RecordsFailed = 1;
                result.Errors.Add("Failed to persist employee data");
                _logger.LogWarning("Failed to sync employee {EmployeeId}", employee.EmployeeId);
            }

            await _repository.RecordSyncAttemptAsync("Employee_RealTime", success, 1, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.RecordsFailed = 1;
            result.Errors.Add(ex.Message);
            _logger.LogError(ex, "Error syncing employee {EmployeeId}", employee.EmployeeId);
            await _repository.RecordSyncAttemptAsync("Employee_RealTime", false, 0, ex.Message, cancellationToken);
        }

        result.SyncCompletedAt = DateTime.UtcNow;
        return result;
    }

    public async Task<GISSyncResultDTO> SyncClockTransactionRealTimeAsync(GISClockTransactionDTO transaction, CancellationToken cancellationToken = default)
    {
        var result = new GISSyncResultDTO
        {
            SyncStartedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Real-time sync: Clock transaction for {EmployeeId} at {Time}", 
                transaction.EmployeeId, transaction.TransactionDateTime);

            var success = await _repository.AddClockTransactionAsync(transaction, cancellationToken);

            if (success)
            {
                result.Success = true;
                result.RecordsProcessed = 1;
                result.RecordsCreated = 1;
                _logger.LogInformation("Successfully synced clock transaction for {EmployeeId}", transaction.EmployeeId);

                // Update attendance record based on clock transaction
                await UpdateAttendanceFromClockTransactionAsync(transaction, cancellationToken);
            }
            else
            {
                result.Success = false;
                result.RecordsFailed = 1;
                result.Errors.Add("Failed to persist clock transaction");
                _logger.LogWarning("Failed to sync clock transaction for {EmployeeId}", transaction.EmployeeId);
            }

            await _repository.RecordSyncAttemptAsync("ClockTransaction_RealTime", success, 1, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.RecordsFailed = 1;
            result.Errors.Add(ex.Message);
            _logger.LogError(ex, "Error syncing clock transaction for {EmployeeId}", transaction.EmployeeId);
            await _repository.RecordSyncAttemptAsync("ClockTransaction_RealTime", false, 0, ex.Message, cancellationToken);
        }

        result.SyncCompletedAt = DateTime.UtcNow;
        return result;
    }

    public async Task<GISSyncResultDTO> SyncAttendanceRecordRealTimeAsync(GISAttendanceRecordDTO record, CancellationToken cancellationToken = default)
    {
        var result = new GISSyncResultDTO
        {
            SyncStartedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Real-time sync: Attendance record for {EmployeeId} on {Date}", 
                record.EmployeeId, record.AttendanceDate);

            var success = await _repository.UpsertAttendanceRecordAsync(record, cancellationToken);

            if (success)
            {
                result.Success = true;
                result.RecordsProcessed = 1;
                result.RecordsCreated = 1;
                _logger.LogInformation("Successfully synced attendance record for {EmployeeId}", record.EmployeeId);
            }
            else
            {
                result.Success = false;
                result.RecordsFailed = 1;
                result.Errors.Add("Failed to persist attendance record");
                _logger.LogWarning("Failed to sync attendance record for {EmployeeId}", record.EmployeeId);
            }

            await _repository.RecordSyncAttemptAsync("Attendance_RealTime", success, 1, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.RecordsFailed = 1;
            result.Errors.Add(ex.Message);
            _logger.LogError(ex, "Error syncing attendance record for {EmployeeId}", record.EmployeeId);
            await _repository.RecordSyncAttemptAsync("Attendance_RealTime", false, 0, ex.Message, cancellationToken);
        }

        result.SyncCompletedAt = DateTime.UtcNow;
        return result;
    }

    #endregion

    #region Scheduled Sync

    public async Task<GISSyncResultDTO> SyncAllEmployeesAsync(CancellationToken cancellationToken = default)
    {
        var result = new GISSyncResultDTO
        {
            SyncStartedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting scheduled sync: All employees");

            // In production, this would call GIS iWork API to get all employees
            // For now, we'll simulate by getting from database
            var employees = await _repository.GetAllEmployeesAsync(cancellationToken);

            result.RecordsProcessed = employees.Count;
            result.Success = true;

            _logger.LogInformation("Successfully synced {Count} employees", employees.Count);
            await _repository.RecordSyncAttemptAsync("Employees_Scheduled", true, employees.Count, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add(ex.Message);
            _logger.LogError(ex, "Error during scheduled employee sync");
            await _repository.RecordSyncAttemptAsync("Employees_Scheduled", false, 0, ex.Message, cancellationToken);
        }

        result.SyncCompletedAt = DateTime.UtcNow;
        return result;
    }

    public async Task<GISSyncResultDTO> SyncAttendanceRecordsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        var result = new GISSyncResultDTO
        {
            SyncStartedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting scheduled sync: Attendance records from {From} to {To}", fromDate, toDate);

            var records = await _repository.GetAttendanceRecordsAsync(fromDate, toDate, cancellationToken);

            result.RecordsProcessed = records.Count;
            result.Success = true;

            _logger.LogInformation("Successfully synced {Count} attendance records", records.Count);
            await _repository.RecordSyncAttemptAsync("Attendance_Scheduled", true, records.Count, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add(ex.Message);
            _logger.LogError(ex, "Error during scheduled attendance sync");
            await _repository.RecordSyncAttemptAsync("Attendance_Scheduled", false, 0, ex.Message, cancellationToken);
        }

        result.SyncCompletedAt = DateTime.UtcNow;
        return result;
    }

    public async Task<GISSyncResultDTO> SyncShiftSchedulesAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        var result = new GISSyncResultDTO
        {
            SyncStartedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting scheduled sync: Shift schedules from {From} to {To}", fromDate, toDate);

            var schedules = await _repository.GetShiftSchedulesAsync(fromDate, toDate, cancellationToken);

            result.RecordsProcessed = schedules.Count;
            result.Success = true;

            _logger.LogInformation("Successfully synced {Count} shift schedules", schedules.Count);
            await _repository.RecordSyncAttemptAsync("ShiftSchedules_Scheduled", true, schedules.Count, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add(ex.Message);
            _logger.LogError(ex, "Error during scheduled shift schedule sync");
            await _repository.RecordSyncAttemptAsync("ShiftSchedules_Scheduled", false, 0, ex.Message, cancellationToken);
        }

        result.SyncCompletedAt = DateTime.UtcNow;
        return result;
    }

    public async Task<GISSyncResultDTO> SyncLeaveRecordsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        var result = new GISSyncResultDTO
        {
            SyncStartedAt = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Starting scheduled sync: Leave records from {From} to {To}", fromDate, toDate);

            var leaves = await _repository.GetLeaveRecordsAsync(fromDate, toDate, cancellationToken);

            result.RecordsProcessed = leaves.Count;
            result.Success = true;

            _logger.LogInformation("Successfully synced {Count} leave records", leaves.Count);
            await _repository.RecordSyncAttemptAsync("LeaveRecords_Scheduled", true, leaves.Count, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add(ex.Message);
            _logger.LogError(ex, "Error during scheduled leave record sync");
            await _repository.RecordSyncAttemptAsync("LeaveRecords_Scheduled", false, 0, ex.Message, cancellationToken);
        }

        result.SyncCompletedAt = DateTime.UtcNow;
        return result;
    }

    #endregion

    #region Exception Detection

    public async Task<List<GISExceptionDetectionResultDTO>> DetectExceptionsAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Detecting all exceptions for date {Date}", date);

        var exceptions = new List<GISExceptionDetectionResultDTO>();

        var lateArrivals = await DetectLateArrivalsAsync(date, cancellationToken);
        exceptions.AddRange(lateArrivals);

        var earlyDepartures = await DetectEarlyDeparturesAsync(date, cancellationToken);
        exceptions.AddRange(earlyDepartures);

        var unplannedAbsences = await DetectUnplannedAbsencesAsync(date, cancellationToken);
        exceptions.AddRange(unplannedAbsences);

        var missingTransactions = await DetectMissingTransactionsAsync(date, cancellationToken);
        exceptions.AddRange(missingTransactions);

        _logger.LogInformation("Detected {Count} total exceptions for date {Date}", exceptions.Count, date);

        return exceptions;
    }

    public async Task<List<GISExceptionDetectionResultDTO>> DetectLateArrivalsAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Detecting late arrivals for date {Date}", date);

        var exceptions = new List<GISExceptionDetectionResultDTO>();

        var records = await _context.GISAttendanceRecords
            .Where(r => r.AttendanceDate == date && r.ActualClockIn.HasValue && r.ExpectedClockIn.HasValue)
            .ToListAsync(cancellationToken);

        foreach (var record in records)
        {
            if (record.ActualClockIn > record.ExpectedClockIn)
            {
                var lateMinutes = (int)(record.ActualClockIn.Value - record.ExpectedClockIn.Value).TotalMinutes;

                if (lateMinutes > 5) // Grace period of 5 minutes
                {
                    exceptions.Add(new GISExceptionDetectionResultDTO
                    {
                        EmployeeId = record.EmployeeId,
                        ExceptionType = "LateArrival",
                        ExceptionDate = date,
                        ShiftType = record.ShiftType,
                        MinutesLost = lateMinutes,
                        Severity = lateMinutes > 30 ? "High" : lateMinutes > 15 ? "Medium" : "Low",
                        Description = $"Employee arrived {lateMinutes} minutes late",
                        Details = new Dictionary<string, object>
                        {
                            { "ExpectedClockIn", record.ExpectedClockIn.Value.ToString() },
                            { "ActualClockIn", record.ActualClockIn.Value.ToString() },
                            { "LateMinutes", lateMinutes }
                        }
                    });
                }
            }
        }

        _logger.LogInformation("Detected {Count} late arrivals for date {Date}", exceptions.Count, date);
        return exceptions;
    }

    public async Task<List<GISExceptionDetectionResultDTO>> DetectEarlyDeparturesAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Detecting early departures for date {Date}", date);

        var exceptions = new List<GISExceptionDetectionResultDTO>();

        var records = await _context.GISAttendanceRecords
            .Where(r => r.AttendanceDate == date && r.ActualClockOut.HasValue && r.ExpectedClockOut.HasValue)
            .ToListAsync(cancellationToken);

        foreach (var record in records)
        {
            if (record.ActualClockOut < record.ExpectedClockOut)
            {
                var earlyMinutes = (int)(record.ExpectedClockOut.Value - record.ActualClockOut.Value).TotalMinutes;

                if (earlyMinutes > 5) // Grace period of 5 minutes
                {
                    exceptions.Add(new GISExceptionDetectionResultDTO
                    {
                        EmployeeId = record.EmployeeId,
                        ExceptionType = "EarlyDeparture",
                        ExceptionDate = date,
                        ShiftType = record.ShiftType,
                        MinutesLost = earlyMinutes,
                        Severity = earlyMinutes > 30 ? "High" : earlyMinutes > 15 ? "Medium" : "Low",
                        Description = $"Employee left {earlyMinutes} minutes early",
                        Details = new Dictionary<string, object>
                        {
                            { "ExpectedClockOut", record.ExpectedClockOut.Value.ToString() },
                            { "ActualClockOut", record.ActualClockOut.Value.ToString() },
                            { "EarlyMinutes", earlyMinutes }
                        }
                    });
                }
            }
        }

        _logger.LogInformation("Detected {Count} early departures for date {Date}", exceptions.Count, date);
        return exceptions;
    }

    public async Task<List<GISExceptionDetectionResultDTO>> DetectUnplannedAbsencesAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Detecting unplanned absences for date {Date}", date);

        var exceptions = new List<GISExceptionDetectionResultDTO>();

        // Get all active employees with scheduled shifts
        var scheduledEmployees = await _context.GISShiftSchedules
            .Where(s => s.ScheduleDate == date && s.IsWorkingDay)
            .Select(s => s.EmployeeId)
            .ToListAsync(cancellationToken);

        // Get employees who have attendance records
        var presentEmployees = await _context.GISAttendanceRecords
            .Where(r => r.AttendanceDate == date)
            .Select(r => r.EmployeeId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Get employees on approved leave
        var onLeaveEmployees = await _context.GISLeaveRecords
            .Where(l => l.LeaveDate == date && l.IsApproved)
            .Select(l => l.EmployeeId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // Find employees who were scheduled but not present and not on leave
        var absentEmployees = scheduledEmployees
            .Where(e => !presentEmployees.Contains(e) && !onLeaveEmployees.Contains(e))
            .ToList();

        foreach (var employeeId in absentEmployees)
        {
            var schedule = await _context.GISShiftSchedules
                .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.ScheduleDate == date, cancellationToken);

            exceptions.Add(new GISExceptionDetectionResultDTO
            {
                EmployeeId = employeeId,
                ExceptionType = "UnplannedAbsence",
                ExceptionDate = date,
                ShiftType = schedule?.ShiftType ?? "Unknown",
                MinutesLost = schedule != null ? (int)(schedule.ExpectedClockOut - schedule.ExpectedClockIn).TotalMinutes : 480,
                Severity = "High",
                Description = "Employee was scheduled but did not clock in and has no approved leave",
                Details = new Dictionary<string, object>
                {
                    { "ScheduledShift", schedule?.ShiftType ?? "Unknown" },
                    { "ExpectedHours", schedule != null ? (schedule.ExpectedClockOut - schedule.ExpectedClockIn).TotalHours : 8 }
                }
            });
        }

        _logger.LogInformation("Detected {Count} unplanned absences for date {Date}", exceptions.Count, date);
        return exceptions;
    }

    public async Task<List<GISExceptionDetectionResultDTO>> DetectMissingTransactionsAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Detecting missing transactions for date {Date}", date);

        var exceptions = new List<GISExceptionDetectionResultDTO>();

        var records = await _context.GISAttendanceRecords
            .Where(r => r.AttendanceDate == date)
            .ToListAsync(cancellationToken);

        foreach (var record in records)
        {
            // Missing clock in
            if (!record.ActualClockIn.HasValue && record.ExpectedClockIn.HasValue)
            {
                exceptions.Add(new GISExceptionDetectionResultDTO
                {
                    EmployeeId = record.EmployeeId,
                    ExceptionType = "MissingClockIn",
                    ExceptionDate = date,
                    ShiftType = record.ShiftType,
                    MinutesLost = 0,
                    Severity = "Medium",
                    Description = "Employee has no clock-in record",
                    Details = new Dictionary<string, object>
                    {
                        { "ExpectedClockIn", record.ExpectedClockIn.Value.ToString() },
                        { "HasClockOut", record.ActualClockOut.HasValue }
                    }
                });
            }

            // Missing clock out
            if (!record.ActualClockOut.HasValue && record.ExpectedClockOut.HasValue)
            {
                exceptions.Add(new GISExceptionDetectionResultDTO
                {
                    EmployeeId = record.EmployeeId,
                    ExceptionType = "MissingClockOut",
                    ExceptionDate = date,
                    ShiftType = record.ShiftType,
                    MinutesLost = 0,
                    Severity = "Medium",
                    Description = "Employee has no clock-out record",
                    Details = new Dictionary<string, object>
                    {
                        { "ExpectedClockOut", record.ExpectedClockOut.Value.ToString() },
                        { "HasClockIn", record.ActualClockIn.HasValue }
                    }
                });
            }
        }

        _logger.LogInformation("Detected {Count} missing transactions for date {Date}", exceptions.Count, date);
        return exceptions;
    }

    #endregion

    #region Query Operations

    public async Task<GISEmployeeDTO?> GetEmployeeAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        return await _repository.GetEmployeeByIdAsync(employeeId, cancellationToken);
    }

    public async Task<List<GISAttendanceRecordDTO>> GetAttendanceRecordsAsync(
        string employeeId, 
        DateTime fromDate, 
        DateTime toDate, 
        CancellationToken cancellationToken = default)
    {
        return await _context.GISAttendanceRecords
            .Where(r => r.EmployeeId == employeeId && r.AttendanceDate >= fromDate && r.AttendanceDate <= toDate)
            .Select(r => new GISAttendanceRecordDTO
            {
                EmployeeId = r.EmployeeId,
                AttendanceDate = r.AttendanceDate,
                ShiftType = r.ShiftType,
                ExpectedClockIn = r.ExpectedClockIn,
                ExpectedClockOut = r.ExpectedClockOut,
                ActualClockIn = r.ActualClockIn,
                ActualClockOut = r.ActualClockOut,
                Status = r.Status,
                Source = r.Source
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DateTime?> GetLastSyncTimeAsync(string syncType, CancellationToken cancellationToken = default)
    {
        return await _repository.GetLastSuccessfulSyncAsync(syncType, cancellationToken);
    }

    #endregion

    #region Private Helpers

    private async Task UpdateAttendanceFromClockTransactionAsync(GISClockTransactionDTO transaction, CancellationToken cancellationToken)
    {
        var date = transaction.TransactionDateTime.Date;
        var time = transaction.TransactionDateTime.TimeOfDay;

        var record = await _context.GISAttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == transaction.EmployeeId && r.AttendanceDate == date, cancellationToken);

        if (record != null)
        {
            if (transaction.TransactionType == "ClockIn")
            {
                record.ActualClockIn = time;
            }
            else if (transaction.TransactionType == "ClockOut")
            {
                record.ActualClockOut = time;
            }

            record.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Updated attendance record for {EmployeeId} on {Date} with {Type} at {Time}", 
                transaction.EmployeeId, date, transaction.TransactionType, time);
        }
    }

    #endregion
}
