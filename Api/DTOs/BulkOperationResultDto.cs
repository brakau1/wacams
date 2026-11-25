namespace WACAMS.Api.DTOs;

public class BulkOperationResultDto
{
    public string OperationType { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int TotalRequested { get; set; }
    public int SuccessCount { get; set; }
    public int Succeeded { get; set; }
    public int ErrorCount { get; set; }
    public int Failed { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public string InitiatedBy { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<string> Errors { get; set; } = new();
}
