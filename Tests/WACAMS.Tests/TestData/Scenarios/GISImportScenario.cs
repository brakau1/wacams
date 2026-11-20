using WACAMS.Core.Entities;

namespace WACAMS.Tests.TestData.Scenarios;

/// <summary>
/// Test scenarios for GIS/iWork attendance data import.
/// Covers successful imports, error handling, duplicate detection, and edge cases.
/// </summary>
public static class GISImportScenario
{
    /// <summary>
    /// Scenario: Bulk import of 100 employees with varying success/failure states.
    /// 
    /// Expected results:
    /// - 100 employees imported successfully
    /// - 10 employees with errors (invalid dates, missing IDs)
    /// - 5 duplicate records detected
    /// - Import summary with success/error/duplicate counts
    /// </summary>
    public static ImportResult CreateBulkImportScenario()
    {
        var successRecords = new List<ImportRecord>();
        var errorRecords = new List<ImportRecord>();
        var duplicateRecords = new List<ImportRecord>();

        // 100 successful imports
        for (int i = 1; i <= 100; i++)
        {
            successRecords.Add(new ImportRecord
            {
                EmployeeId = $"EMP{i:D3}",
                DisplayName = $"{EmployeeFixtures.FirstNames[(i - 1) % EmployeeFixtures.FirstNames.Length]} {EmployeeFixtures.LastNames[(i - 1) % EmployeeFixtures.LastNames.Length]}",
                Status = "Success",
                ShiftCode = new[] { "ES", "MS", "NS" }[i % 3],
                DepartmentName = new[] { "Production", "Quality", "Logistics", "Maintenance", "Admin" }[i % 5],
                ImportedAt = DateTime.UtcNow,
            });
        }

        // 10 error records — various error types
        var errorTypes = new[]
        {
            "Invalid date format '31/12/2026' in row 101",
            "Missing employee ID in row 102",
            "Invalid shift code 'XYZ' in row 103",
            "Department 'UnknownDept' not found in system — row 104",
            "Date out of range: 2025-01-01 is before system start date — row 105",
            "Duplicate employee ID 'EMP001' already exists — row 106",
            "Malformed clock-in time 'abc:def' in row 107",
            "Negative minutes late value '-30' in row 108",
            "Employee email format invalid 'john@' in row 109",
            "Shift start time exceeds 23:59 in row 110",
        };

        for (int i = 0; i < 10; i++)
        {
            errorRecords.Add(new ImportRecord
            {
                EmployeeId = i == 1 ? "" : $"EMP{100 + i:D3}",
                DisplayName = $"Error Record {i + 1}",
                Status = "Error",
                Error = errorTypes[i],
                RowNumber = 100 + i + 1,
                ImportedAt = DateTime.UtcNow,
            });
        }

        // 5 duplicate records
        for (int i = 1; i <= 5; i++)
        {
            duplicateRecords.Add(new ImportRecord
            {
                EmployeeId = $"EMP{i:D3}",
                DisplayName = EmployeeFixtures.CreateEmployee(i).DisplayName,
                Status = "Duplicate",
                Error = $"Employee ID 'EMP{i:D3}' already exists in system — skipped import.",
                RowNumber = 110 + i,
                ImportedAt = DateTime.UtcNow,
            });
        }

        return new ImportResult
        {
            TotalRows = 115,
            SuccessCount = 100,
            FailureCount = 10,
            DuplicateCount = 5,
            Records = successRecords.Concat(errorRecords).Concat(duplicateRecords).ToList(),
            Summary = "Import completed: 100 successes, 10 errors, 5 duplicates out of 115 total rows.",
        };
    }

    /// <summary>
    /// Scenario: Shift code mapping from GIS format to internal format.
    /// 
    /// GIS codes → Internal mappings:
    /// ES → Morning (06:00-14:00)
    /// MS → Afternoon (14:00-22:00)
    /// NS → Night (22:00-06:00)
    /// LDS → Long Day (06:00-18:00)
    /// DS → Day (08:00-17:00)
    /// </summary>
    public static Dictionary<string, ShiftMapping> CreateShiftCodeMappings()
    {
        return new Dictionary<string, ShiftMapping>
        {
            { "ES", new ShiftMapping { GisCode = "ES", MappedTo = "Morning", StartTime = TimeSpan.FromHours(6), EndTime = TimeSpan.FromHours(14) } },
            { "MS", new ShiftMapping { GisCode = "MS", MappedTo = "Afternoon", StartTime = TimeSpan.FromHours(14), EndTime = TimeSpan.FromHours(22) } },
            { "NS", new ShiftMapping { GisCode = "NS", MappedTo = "Night", StartTime = TimeSpan.FromHours(22), EndTime = TimeSpan.FromHours(6) } },
            { "LDS", new ShiftMapping { GisCode = "LDS", MappedTo = "LongDay", StartTime = TimeSpan.FromHours(6), EndTime = TimeSpan.FromHours(18) } },
            { "DS", new ShiftMapping { GisCode = "DS", MappedTo = "Day", StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(17) } },
            { "FDS", new ShiftMapping { GisCode = "FDS", MappedTo = "FullDay", StartTime = TimeSpan.FromHours(7), EndTime = TimeSpan.FromHours(19) } },
            { "zdy", new ShiftMapping { GisCode = "zdy", MappedTo = "Custom", StartTime = null, EndTime = null, IsCustom = true } },
        };
    }

    /// <summary>
    /// Scenario: Invalid data handling — specific edge cases that should be rejected.
    /// </summary>
    public static List<ImportValidationError> CreateInvalidDataCases()
    {
        return new List<ImportValidationError>
        {
            new()
            {
                RowNumber = 1,
                Field = "date_str",
                Value = "31/12/2026",
                Error = "Invalid date format. Expected YYYY-MM-DD.",
                Severity = "Error",
                Action = "Reject record",
            },
            new()
            {
                RowNumber = 2,
                Field = "employee_code",
                Value = "",
                Error = "Employee code is required and cannot be empty.",
                Severity = "Error",
                Action = "Reject record",
            },
            new()
            {
                RowNumber = 3,
                Field = "clock_in_time",
                Value = "25:00",
                Error = "Clock-in time '25:00' exceeds valid range (00:00-23:59).",
                Severity = "Error",
                Action = "Reject record",
            },
            new()
            {
                RowNumber = 4,
                Field = "shift_code",
                Value = "INVALID",
                Error = "Shift code 'INVALID' does not match any known GIS shift code.",
                Severity = "Warning",
                Action = "Flag for manual review",
            },
            new()
            {
                RowNumber = 5,
                Field = "department",
                Value = "NonExistentDept",
                Error = "Department 'NonExistentDept' not found in department registry.",
                Severity = "Warning",
                Action = "Flag for manual review",
            },
        };
    }
}

/// <summary>
/// Represents a single record in an import result.
/// </summary>
public record ImportRecord
{
    public string EmployeeId { get; init; } = null!;
    public string DisplayName { get; init; } = null!;
    public string Status { get; init; } = null!;
    public string? ShiftCode { get; init; }
    public string? DepartmentName { get; init; }
    public string? Error { get; init; }
    public int? RowNumber { get; init; }
    public DateTime ImportedAt { get; init; }
}

/// <summary>
/// Summary of an import operation.
/// </summary>
public record ImportResult
{
    public int TotalRows { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public int DuplicateCount { get; init; }
    public List<ImportRecord> Records { get; init; } = new();
    public string Summary { get; init; } = null!;
}

/// <summary>
/// Maps a GIS shift code to its internal representation.
/// </summary>
public record ShiftMapping
{
    public string GisCode { get; init; } = null!;
    public string MappedTo { get; init; } = null!;
    public TimeSpan? StartTime { get; init; }
    public TimeSpan? EndTime { get; init; }
    public bool IsCustom { get; init; }
}

/// <summary>
/// Represents a validation error found during import.
/// </summary>
public record ImportValidationError
{
    public int RowNumber { get; init; }
    public string Field { get; init; } = null!;
    public string Value { get; init; } = null!;
    public string Error { get; init; } = null!;
    public string Severity { get; init; } = null!; // "Error" or "Warning"
    public string Action { get; init; } = null!;
}

// Add extension method for FirstNames/LastNames used in fixture
public static class EmployeeFixturesExtensions
{
    public static readonly string[] FirstNames = new[]
    {
        "Thabo", "Nomsa", "Sipho", "Zanele", "Mandla", "Pulane", "Kagiso", "Lerato",
        "Bongani", "Themba", "Refilwe", "Tshepo", "Mpho", "Karabo", "Lebo", "Dineo",
        "Andile", "Nomvula", "Sbu", "Busisiwe", "Jabulani", "Katlego", "Mokgadi",
        "Rethabile", "Vusi", "Ayanda", "Sinovuyo", "Ntokozo", "Zolisa", "Nonhlanhla",
        "Siyabonga", "Ntombi", "Musa", "Palesa", "Siphosiba", "Thandi", "Nkosingesihle",
        "Gugu", "Mandla", "Buhle", "Sibusiso", "Nomzamo", "Lucky", "Mahlangu",
        "Sindiso", "Noxolo", "Phumlani", "Mamello"
    };

    public static readonly string[] LastNames = new[]
    {
        "Dlamini", "Ndlovu", "Mokoena", "Nkosi", "Mthembu", "Zulu", "Shabalala",
        "Motaung", "Pilane", "Molefe", "Khumalo", "Baloyi", "Masuku", "Manganyi",
        "Tau", "Mdluli", "Van Wyk", "Botha", "Fourie", "de Villiers", "van der Merwe",
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Miller", "Davis",
        "Wilson", "Moore", "Taylor", "Anderson", "Thomas", "Jackson", "White",
        "Harris", "Martin", "Thompson", "Robinson", "Clark", "Lewis", "Lee",
        "Walker", "Hall", "Allen", "Young", "King", "Wright", "Scott"
    };
}
