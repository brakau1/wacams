namespace WACAMS.Api.DTOs;

public class BulkOperationResultDto
{
    public int TotalRequested { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = new();
}
