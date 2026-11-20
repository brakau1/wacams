using WACAMS.Api.Integration.GIS.DTOs;

namespace WACAMS.Api.Integration.GIS.Repositories;

/// <summary>
/// GIS Integration Repository Interface
/// Handles data persistence for GIS iWork integration
/// </summary>
public interface IGISIntegrationRepository
{
    // Employee Sync
    Task<bool> UpsertEmployeeAsync(GISEmployeeDTO employee, CancellationToken cancellationToken = default);
    Task<List<GISEmployeeDTO>> GetAllEmployeesAsync(CancellationToken cancellationToken = default);
    Task<GISEmployeeDTO?> GetEmployeeByIdAsync(string employeeId, CancellationToken cancellationToken = default);

    // Attendance Records
    Task<bool> UpsertAttendanceRecordAsync(GISAttendanceRecordDTO record, CancellationToken cancellationToken = default);
    Task<List<GISAttendanceRecordDTO>> GetAttendanceRecordsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<GISAttendanceRecordDTO?> GetAttendanceRecordAsync(string employeeId, DateTime date, string shiftType, CancellationToken cancellationToken = default);

    // Clock Transactions
    Task<bool> AddClockTransactionAsync(GISClockTransactionDTO transaction, CancellationToken cancellationToken = default);
    Task<List<GISClockTransactionDTO>> GetClockTransactionsAsync(string employeeId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    // Shift Schedules
    Task<bool> UpsertShiftScheduleAsync(GISShiftScheduleDTO schedule, CancellationToken cancellationToken = default);
    Task<GISShiftScheduleDTO?> GetShiftScheduleAsync(string employeeId, DateTime date, CancellationToken cancellationToken = default);
    Task<List<GISShiftScheduleDTO>> GetShiftSchedulesAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    // Shift Cancellations
    Task<bool> AddShiftCancellationAsync(GISShiftCancellationDTO cancellation, CancellationToken cancellationToken = default);
    Task<List<GISShiftCancellationDTO>> GetShiftCancellationsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    // Leave Records
    Task<bool> UpsertLeaveRecordAsync(GISLeaveRecordDTO leave, CancellationToken cancellationToken = default);
    Task<List<GISLeaveRecordDTO>> GetLeaveRecordsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    // Sync Tracking
    Task<bool> RecordSyncAttemptAsync(string syncType, bool success, int recordsProcessed, string? errorMessage = null, CancellationToken cancellationToken = default);
    Task<DateTime?> GetLastSuccessfulSyncAsync(string syncType, CancellationToken cancellationToken = default);
}
