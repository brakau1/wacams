using WACAMS.Infrastructure.DTOs.GIS;

namespace WACAMS.Infrastructure.Services.GIS;

/// <summary>
/// GIS Integration Service Interface
/// Handles all GIS iWork integration operations
/// </summary>
public interface IGISIntegrationService
{
    // Real-Time Sync
    Task<GISSyncResultDTO> SyncEmployeeRealTimeAsync(GISEmployeeDTO employee, CancellationToken cancellationToken = default);
    Task<GISSyncResultDTO> SyncClockTransactionRealTimeAsync(GISClockTransactionDTO transaction, CancellationToken cancellationToken = default);
    Task<GISSyncResultDTO> SyncAttendanceRecordRealTimeAsync(GISAttendanceRecordDTO record, CancellationToken cancellationToken = default);

    // Scheduled Sync
    Task<GISSyncResultDTO> SyncAllEmployeesAsync(CancellationToken cancellationToken = default);
    Task<GISSyncResultDTO> SyncAttendanceRecordsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<GISSyncResultDTO> SyncShiftSchedulesAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<GISSyncResultDTO> SyncLeaveRecordsAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);

    // Exception Detection
    Task<List<GISExceptionDetectionResultDTO>> DetectExceptionsAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<List<GISExceptionDetectionResultDTO>> DetectLateArrivalsAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<List<GISExceptionDetectionResultDTO>> DetectEarlyDeparturesAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<List<GISExceptionDetectionResultDTO>> DetectUnplannedAbsencesAsync(DateTime date, CancellationToken cancellationToken = default);
    Task<List<GISExceptionDetectionResultDTO>> DetectMissingTransactionsAsync(DateTime date, CancellationToken cancellationToken = default);

    // Query Operations
    Task<GISEmployeeDTO?> GetEmployeeAsync(string employeeId, CancellationToken cancellationToken = default);
    Task<List<GISAttendanceRecordDTO>> GetAttendanceRecordsAsync(string employeeId, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
    Task<DateTime?> GetLastSyncTimeAsync(string syncType, CancellationToken cancellationToken = default);
}
