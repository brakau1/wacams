namespace WACAMS.Api.Integration.GIS.DTOs;

/// <summary>
/// GIS Sync Result DTO
/// </summary>
public class GISSyncResultDTO
{
    public bool Success { get; set; }
    public int RecordsProcessed { get; set; }
    public int RecordsCreated { get; set; }
    public int RecordsUpdated { get; set; }
    public int RecordsFailed { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public DateTime SyncStartedAt { get; set; }
    public DateTime SyncCompletedAt { get; set; }
    public TimeSpan Duration => SyncCompletedAt - SyncStartedAt;
}
