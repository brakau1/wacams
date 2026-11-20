using WACAMS.Core.RulesEngine.Rules;

namespace WACAMS.Core.Services;

/// <summary>
/// Interface for attendance data validation
/// </summary>
public interface IAttendanceValidator
{
    /// <summary>
    /// Validate attendance calculation context
    /// </summary>
    ValidationResult Validate(AttendanceCalculationContext context);

    /// <summary>
    /// Validate exception processing
    /// </summary>
    ValidationResult ValidateExceptionProcessing(Guid exceptionId, string investigatorId, string decision);

    /// <summary>
    /// Validate corrective action generation
    /// </summary>
    ValidationResult ValidateCorrectiveActionGeneration(Guid exceptionId, string issuedBy);
}

/// <summary>
/// Validation result
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = new();

    public void AddError(string error)
    {
        IsValid = false;
        Errors.Add(error);
    }
}
