namespace WACAMS.Core.Enums;

/// <summary>
/// Represents the standard department codes used across the organization.
/// Used as a lookup key for department identification in reports and analytics.
/// </summary>
public enum DepartmentCode
{
    /// <summary>
    /// Production Department — manufacturing, assembly, painting line operations.
    /// </summary>
    Production = 1,

    /// <summary>
    /// Quality Department — quality control, inspection, testing.
    /// </summary>
    Quality = 2,

    /// <summary>
    /// Engineering Department — process engineering, continuous improvement.
    /// </summary>
    Engineering = 3,

    /// <summary>
    /// Maintenance Department — equipment maintenance, repairs, preventive servicing.
    /// </summary>
    Maintenance = 4,

    /// <summary>
    /// Logistics Department — warehousing, material handling, distribution.
    /// </summary>
    Logistics = 5,

    /// <summary>
    /// Office Department — administrative, finance, procurement, general office.
    /// </summary>
    Office = 6,

    /// <summary>
    /// Human Resources Department — recruitment, payroll, employee relations.
    /// </summary>
    HR = 7
}
