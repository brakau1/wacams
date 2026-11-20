using Microsoft.EntityFrameworkCore;
using WACAMS.Api.Data;
using WACAMS.Api.Integration.GIS.DTOs;
using WACAMS.Api.Integration.GIS.Models;

namespace WACAMS.Api.Integration.GIS.Repositories;

/// <summary>
/// GIS Integration Repository Implementation
/// </summary>
public class GISIntegrationRepository : IGISIntegrationRepository
{
    private readonly WacamDbContext _context;

    public GISIntegrationRepository(WacamDbContext context)
    {
        _context = context;
    }

    #region Employee Sync

    public async Task<bool> UpsertEmployeeAsync(GISEmployeeDTO employee, CancellationToken cancellationToken = default)
    {
        var existing = await _context.GISEmployees
            .FirstOrDefaultAsync(e => e.EmployeeId == employee.EmployeeId, cancellationToken);

        if (existing == null)
        {
            var newEmployee = new GISEmployee
            {
                EmployeeId = employee.EmployeeId,
                DisplayName = employee.DisplayName,
                Email = employee.Email,
                Gender = employee.Gender,
                Nationality = employee.Nationality,
                DepartmentCode = employee.DepartmentCode,
                DepartmentName = employee.DepartmentName,
                EmploymentStatus = employee.EmploymentStatus,
                HireDate = employee.HireDate,
                Phone = employee.Phone,
                ManagerId = employee.ManagerId,
                LastSyncedAt = DateTime.UtcNow
            };
            _context.GISEmployees.Add(newEmployee);
        }
        else
        {
            existing.DisplayName = employee.DisplayName;
            existing.Email = employee.Email;
            existing.Gender = employee.Gender;
            existing.Nationality = employee.Nationality;
            existing.DepartmentCode = employee.DepartmentCode;
            existing.DepartmentName = employee.DepartmentName;
            existing.EmploymentStatus = employee.EmploymentStatus;
            existing.HireDate = employee.HireDate;
            existing.Phone = employee.Phone;
            existing.ManagerId = employee.ManagerId;
            existing.LastSyncedAt = DateTime.UtcNow;
        }

        return await _context.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<List<GISEmployeeDTO>> GetAllEmployeesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.GISEmployees
            .Select(e => new GISEmployeeDTO
            {
                EmployeeId = e.EmployeeId,
                DisplayName = e.DisplayName,
                Email = e.Email,
                Gender = e.Gender,
                Nationality = e.Nationality,
                DepartmentCode = e.DepartmentCode,
                DepartmentName = e.DepartmentName,
                EmploymentStatus = e.EmploymentStatus,
                HireDate = e.HireDate,
                Phone = e.Phone,
                ManagerId = e.ManagerId
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<GISEmployeeDTO?> GetEmployeeByIdAsync(string employeeId, CancellationToken cancellationToken = default)
    {
        var employee = await _context.GISEmployees
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId, cancellationToken);

        if (employee == null) return null;

        return new GISEmployeeDTO
        {
            EmployeeId = employee.EmployeeId,
            DisplayName = employee.DisplayName,
            Email = employee.Email,
            Gender = employee.Gender,
            Nationality = employee.Nationality,
            DepartmentCode = employee.DepartmentCode,
            DepartmentName = employee.DepartmentName,
            EmploymentStatus = employee.EmploymentStatus,
            HireDate = employee.HireDate,
            Phone = employee.Phone,
            ManagerId = employee.ManagerId
        };
    }

    #endregion

    #region Attendance Records

    public async Task<bool> UpsertAttendanceRecordAsync(GISAttendanceRecordDTO record, CancellationToken cancellationToken = default)
    {
        var existing = await _context.GISAttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == record.EmployeeId 
                && r.AttendanceDate == record.AttendanceDate 
                && r.ShiftType == record.ShiftType, cancellationToken);

        if (existing == null)
        {
            var newRecord = new GISAttendanceRecord
            {
                EmployeeId = record.EmployeeId,
                AttendanceDate = record.AttendanceDate,
                ShiftType = record.ShiftType,
                ExpectedClockIn = record.ExpectedClockIn,
                ExpectedClockOut = record.ExpectedClockOut,
                ActualClockIn = record.ActualClockIn,
                ActualClockOut = record.ActualClockOut,
                Status = record.Status,
                Source = record.Source,
                RawData = record.RawData != null ? System.Text.Json.JsonSerializer.Serialize(record.RawData) : null,
                SyncedAt = DateTime.UtcNow
            };
            _context.GISAttendanceRecords.Add(newRecord);
        }
        else
        {
            existing.ExpectedClockIn = record.ExpectedClockIn;
            existing.ExpectedClockOut = record.ExpectedClockOut;
            existing.ActualClockIn = record.ActualClockIn;
            existing.ActualClockOut = record.ActualClockOut;
            existing.Status = record.Status;
            existing.Source = record.Source;
            existing.RawData = record.RawData != null ? System.Text.Json.JsonSerializer.Serialize(record.RawData) : null;
            existing.SyncedAt = DateTime.UtcNow;
        }

        return await _context.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<List<GISAttendanceRecordDTO>> GetAttendanceRecordsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return await _context.GISAttendanceRecords
            .Where(r => r.AttendanceDate >= fromDate && r.AttendanceDate <= toDate)
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

    public async Task<GISAttendanceRecordDTO?> GetAttendanceRecordAsync(string employeeId, DateTime date, string shiftType, CancellationToken cancellationToken = default)
    {
        var record = await _context.GISAttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == employeeId 
                && r.AttendanceDate == date 
                && r.ShiftType == shiftType, cancellationToken);

        if (record == null) return null;

        return new GISAttendanceRecordDTO
        {
            EmployeeId = record.EmployeeId,
            AttendanceDate = record.AttendanceDate,
            ShiftType = record.ShiftType,
            ExpectedClockIn = record.ExpectedClockIn,
            ExpectedClockOut = record.ExpectedClockOut,
            ActualClockIn = record.ActualClockIn,
            ActualClockOut = record.ActualClockOut,
            Status = record.Status,
            Source = record.Source
        };
    }

    #endregion

    #region Clock Transactions

    public async Task<bool> AddClockTransactionAsync(GISClockTransactionDTO transaction, CancellationToken cancellationToken = default)
    {
        var entity = new GISClockTransaction
        {
            EmployeeId = transaction.EmployeeId,
            TransactionDateTime = transaction.TransactionDateTime,
            TransactionType = transaction.TransactionType,
            TerminalId = transaction.TerminalId,
            TerminalLocation = transaction.TerminalLocation,
            BiometricData = transaction.BiometricData,
            IsValid = transaction.IsValid,
            ValidationMessage = transaction.ValidationMessage,
            SyncedAt = DateTime.UtcNow
        };

        _context.GISClockTransactions.Add(entity);
        return await _context.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<List<GISClockTransactionDTO>> GetClockTransactionsAsync(string employeeId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return await _context.GISClockTransactions
            .Where(t => t.EmployeeId == employeeId 
                && t.TransactionDateTime >= fromDate 
                && t.TransactionDateTime <= toDate)
            .Select(t => new GISClockTransactionDTO
            {
                EmployeeId = t.EmployeeId,
                TransactionDateTime = t.TransactionDateTime,
                TransactionType = t.TransactionType,
                TerminalId = t.TerminalId,
                TerminalLocation = t.TerminalLocation,
                BiometricData = t.BiometricData,
                IsValid = t.IsValid,
                ValidationMessage = t.ValidationMessage
            })
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Shift Schedules

    public async Task<bool> UpsertShiftScheduleAsync(GISShiftScheduleDTO schedule, CancellationToken cancellationToken = default)
    {
        var existing = await _context.GISShiftSchedules
            .FirstOrDefaultAsync(s => s.EmployeeId == schedule.EmployeeId 
                && s.ScheduleDate == schedule.ScheduleDate, cancellationToken);

        if (existing == null)
        {
            var newSchedule = new GISShiftSchedule
            {
                EmployeeId = schedule.EmployeeId,
                ScheduleDate = schedule.ScheduleDate,
                ShiftType = schedule.ShiftType,
                ExpectedClockIn = schedule.ExpectedClockIn,
                ExpectedClockOut = schedule.ExpectedClockOut,
                IsWorkingDay = schedule.IsWorkingDay,
                ShiftCode = schedule.ShiftCode,
                SyncedAt = DateTime.UtcNow
            };
            _context.GISShiftSchedules.Add(newSchedule);
        }
        else
        {
            existing.ShiftType = schedule.ShiftType;
            existing.ExpectedClockIn = schedule.ExpectedClockIn;
            existing.ExpectedClockOut = schedule.ExpectedClockOut;
            existing.IsWorkingDay = schedule.IsWorkingDay;
            existing.ShiftCode = schedule.ShiftCode;
            existing.SyncedAt = DateTime.UtcNow;
        }

        return await _context.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<GISShiftScheduleDTO?> GetShiftScheduleAsync(string employeeId, DateTime date, CancellationToken cancellationToken = default)
    {
        var schedule = await _context.GISShiftSchedules
            .FirstOrDefaultAsync(s => s.EmployeeId == employeeId && s.ScheduleDate == date, cancellationToken);

        if (schedule == null) return null;

        return new GISShiftScheduleDTO
        {
            EmployeeId = schedule.EmployeeId,
            ScheduleDate = schedule.ScheduleDate,
            ShiftType = schedule.ShiftType,
            ExpectedClockIn = schedule.ExpectedClockIn,
            ExpectedClockOut = schedule.ExpectedClockOut,
            IsWorkingDay = schedule.IsWorkingDay,
            ShiftCode = schedule.ShiftCode
        };
    }

    public async Task<List<GISShiftScheduleDTO>> GetShiftSchedulesAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return await _context.GISShiftSchedules
            .Where(s => s.ScheduleDate >= fromDate && s.ScheduleDate <= toDate)
            .Select(s => new GISShiftScheduleDTO
            {
                EmployeeId = s.EmployeeId,
                ScheduleDate = s.ScheduleDate,
                ShiftType = s.ShiftType,
                ExpectedClockIn = s.ExpectedClockIn,
                ExpectedClockOut = s.ExpectedClockOut,
                IsWorkingDay = s.IsWorkingDay,
                ShiftCode = s.ShiftCode
            })
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Shift Cancellations

    public async Task<bool> AddShiftCancellationAsync(GISShiftCancellationDTO cancellation, CancellationToken cancellationToken = default)
    {
        var entity = new GISShiftCancellation
        {
            EmployeeId = cancellation.EmployeeId,
            CancelledDate = cancellation.CancelledDate,
            OriginalShiftType = cancellation.OriginalShiftType,
            CancellationReason = cancellation.CancellationReason,
            CancelledAt = cancellation.CancelledAt,
            CancelledBy = cancellation.CancelledBy,
            IsApproved = cancellation.IsApproved,
            SyncedAt = DateTime.UtcNow
        };

        _context.GISShiftCancellations.Add(entity);
        return await _context.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<List<GISShiftCancellationDTO>> GetShiftCancellationsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return await _context.GISShiftCancellations
            .Where(c => c.CancelledDate >= fromDate && c.CancelledDate <= toDate)
            .Select(c => new GISShiftCancellationDTO
            {
                EmployeeId = c.EmployeeId,
                CancelledDate = c.CancelledDate,
                OriginalShiftType = c.OriginalShiftType,
                CancellationReason = c.CancellationReason,
                CancelledAt = c.CancelledAt,
                CancelledBy = c.CancelledBy,
                IsApproved = c.IsApproved
            })
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Leave Records

    public async Task<bool> UpsertLeaveRecordAsync(GISLeaveRecordDTO leave, CancellationToken cancellationToken = default)
    {
        var existing = await _context.GISLeaveRecords
            .FirstOrDefaultAsync(l => l.EmployeeId == leave.EmployeeId && l.LeaveDate == leave.LeaveDate, cancellationToken);

        if (existing == null)
        {
            var newLeave = new GISLeaveRecord
            {
                EmployeeId = leave.EmployeeId,
                LeaveDate = leave.LeaveDate,
                LeaveType = leave.LeaveType,
                LeaveReason = leave.LeaveReason,
                IsApproved = leave.IsApproved,
                ApprovedAt = leave.ApprovedAt,
                ApprovedBy = leave.ApprovedBy,
                SupportingDocument = leave.SupportingDocument,
                SyncedAt = DateTime.UtcNow
            };
            _context.GISLeaveRecords.Add(newLeave);
        }
        else
        {
            existing.LeaveType = leave.LeaveType;
            existing.LeaveReason = leave.LeaveReason;
            existing.IsApproved = leave.IsApproved;
            existing.ApprovedAt = leave.ApprovedAt;
            existing.ApprovedBy = leave.ApprovedBy;
            existing.SupportingDocument = leave.SupportingDocument;
            existing.SyncedAt = DateTime.UtcNow;
        }

        return await _context.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<List<GISLeaveRecordDTO>> GetLeaveRecordsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default)
    {
        return await _context.GISLeaveRecords
            .Where(l => l.LeaveDate >= fromDate && l.LeaveDate <= toDate)
            .Select(l => new GISLeaveRecordDTO
            {
                EmployeeId = l.EmployeeId,
                LeaveDate = l.LeaveDate,
                LeaveType = l.LeaveType,
                LeaveReason = l.LeaveReason,
                IsApproved = l.IsApproved,
                ApprovedAt = l.ApprovedAt,
                ApprovedBy = l.ApprovedBy,
                SupportingDocument = l.SupportingDocument
            })
            .ToListAsync(cancellationToken);
    }

    #endregion

    #region Sync Tracking

    public async Task<bool> RecordSyncAttemptAsync(string syncType, bool success, int recordsProcessed, string? errorMessage = null, CancellationToken cancellationToken = default)
    {
        var log = new GISSyncLog
        {
            SyncType = syncType,
            Success = success,
            RecordsProcessed = recordsProcessed,
            ErrorMessage = errorMessage,
            SyncedAt = DateTime.UtcNow
        };

        _context.GISSyncLogs.Add(log);
        return await _context.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<DateTime?> GetLastSuccessfulSyncAsync(string syncType, CancellationToken cancellationToken = default)
    {
        var lastSync = await _context.GISSyncLogs
            .Where(l => l.SyncType == syncType && l.Success)
            .OrderByDescending(l => l.SyncedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return lastSync?.SyncedAt;
    }

    #endregion
}
