namespace WACAMS.Api.Integration.GIS.DTOs;

/// <summary>
/// GIS iWork Employee DTO
/// </summary>
public class GISEmployeeDTO
{
    public string EmployeeId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string Nationality { get; set; } = string.Empty;
    public string DepartmentCode { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string EmploymentStatus { get; set; } = string.Empty;
    public DateTime? HireDate { get; set; }
    public string? Phone { get; set; }
    public string? ManagerId { get; set; }
}
