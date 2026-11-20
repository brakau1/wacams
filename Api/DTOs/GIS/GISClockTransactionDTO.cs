namespace WACAMS.Api.Integration.GIS.DTOs;

/// <summary>
/// GIS iWork Clock Transaction DTO
/// </summary>
public class GISClockTransactionDTO
{
    public string EmployeeId { get; set; } = string.Empty;
    public DateTime TransactionDateTime { get; set; }
    public string TransactionType { get; set; } = string.Empty; // "ClockIn" or "ClockOut"
    public string? TerminalId { get; set; }
    public string? TerminalLocation { get; set; }
    public string? BiometricData { get; set; }
    public bool IsValid { get; set; } = true;
    public string? ValidationMessage { get; set; }
}
