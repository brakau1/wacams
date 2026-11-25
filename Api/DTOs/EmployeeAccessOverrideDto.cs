namespace WACAMS.Api.DTOs;

public class EmployeeAccessOverrideDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime OverrideDate { get; set; }
    public string? OverriddenBy { get; set; }
}
