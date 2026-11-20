using WACAMS.Core.Entities;

namespace WACAMS.Tests.TestData;

/// <summary>
/// Test data factory for Employee entities — 50 employees across 5 departments.
/// Covers all roles, manager hierarchy chains (3 levels deep), active/inactive states,
/// and employees with/without warning letters.
/// </summary>
public static class EmployeeFixtures
{
    private static readonly string[] FirstNames =
    {
        "Thabo", "Nomsa", "Sipho", "Zanele", "Mandla", "Pulane", "Kagiso", "Lerato",
        "Bongani", "Themba", "Refilwe", "Tshepo", "Mpho", "Karabo", "Lebo", "Dineo",
        "Andile", "Nomvula", "Sbu", "Busisiwe", "Jabulani", "Katlego", "Mokgadi",
        "Rethabile", "Vusi", "Ayanda", "Sinovuyo", "Ntokozo", "Zolisa", "Nonhlanhla",
        "Siyabonga", "Ntombi", "Musa", "Palesa", "Siphosiba", "Thandi", "Nkosingesihle",
        "Gugu", "Mandla", "Buhle", "Sibusiso", "Nomzamo", "Lucky", "Mahlangu",
        "Sindiso", "Noxolo", "Phumlani", "Mamello"
    };

    private static readonly string[] LastNames =
    {
        "Dlamini", "Ndlovu", "Mokoena", "Nkosi", "Mthembu", "Zulu", "Shabalala",
        "Motaung", "Pilane", "Molefe", "Khumalo", "Baloyi", "Masuku", "Manganyi",
        "Tau", "Mdluli", "Van Wyk", "Botha", "Fourie", "de Villiers", "van der Merwe",
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Miller", "Davis",
        "Wilson", "Moore", "Taylor", "Anderson", "Thomas", "Jackson", "White",
        "Harris", "Martin", "Thompson", "Robinson", "Clark", "Lewis", "Lee",
        "Walker", "Hall", "Allen", "Young", "King", "Wright", "Scott"
    };

    /// <summary>
    /// Creates 50 employees distributed across 5 departments with realistic South African names.
    /// </summary>
    public static List<Employee> CreateAllEmployees()
    {
        var employees = new List<Employee>();

        // Department assignments: Production(1-12), Quality(13-22), Logistics(23-32), Maintenance(33-42), Admin(43-50)
        var departmentIds = new Dictionary<int, int>
        {
            { 1, 1 }, { 2, 1 }, { 3, 1 }, { 4, 1 }, { 5, 1 }, { 6, 1 },
            { 7, 1 }, { 8, 1 }, { 9, 1 }, { 10, 1 }, { 11, 1 }, { 12, 1 },
            { 13, 2 }, { 14, 2 }, { 15, 2 }, { 16, 2 }, { 17, 2 }, { 18, 2 },
            { 19, 2 }, { 20, 2 }, { 21, 2 }, { 22, 2 },
            { 23, 3 }, { 24, 3 }, { 25, 3 }, { 26, 3 }, { 27, 3 }, { 28, 3 },
            { 29, 3 }, { 30, 3 }, { 31, 3 }, { 32, 3 },
            { 33, 4 }, { 34, 4 }, { 35, 4 }, { 36, 4 }, { 37, 4 }, { 38, 4 },
            { 39, 4 }, { 40, 4 }, { 41, 4 }, { 42, 4 },
            { 43, 5 }, { 44, 5 }, { 45, 5 }, { 46, 5 }, { 47, 5 }, { 48, 5 },
            { 49, 5 }, { 50, 5 }
        };

        // Role assignment pattern
        var roleMap = new Dictionary<int, string>
        {
            { 1, "SuperAdmin" },   // Overall admin
            { 2, "Supervisor" },   // Production supervisor
            { 3, "TeamLeader" },   // Production team leader
            { 13, "Supervisor" },  // Quality supervisor
            { 23, "TeamLeader" },  // Logistics team leader
            { 33, "Supervisor" },  // Maintenance supervisor
            { 43, "HR" },          // HR manager
            { 44, "Admin" },       // Admin staff
        };

        // Manager hierarchy: SuperAdmin → Supervisors → TeamLeaders → Operators
        // Level 1: SuperAdmin (ID: EMP001) manages no one directly except supervisors
        // Level 2: Supervisors report to SuperAdmin
        // Level 3: TeamLeaders report to their respective supervisors
        // Level 4: Operators report to team leaders or supervisors

        var managerAssignments = new Dictionary<int, string?>
        {
            { 1, null },           // SuperAdmin - top of hierarchy
            { 2, "EMP001" },       // Production Supervisor → SuperAdmin
            { 3, "EMP002" },       // Production TL → Prod Supervisor
            { 4, "EMP002" },       // Production TL → Prod Supervisor
            { 5, "EMP003" },       // Operator → Production TL
            { 6, "EMP003" },       // Operator → Production TL
            { 7, "EMP003" },       // Operator → Production TL
            { 8, "EMP004" },       // Operator → Production TL
            { 9, "EMP004" },       // Operator → Production TL
            { 10, "EMP004" },      // Operator → Production TL
            { 11, "EMP002" },      // Operator → Production Supervisor
            { 12, "EMP002" },      // Operator → Production Supervisor
            { 13, null },          // Quality Supervisor (reports to SuperAdmin for escalation purposes)
            { 14, "EMP013" },      // Quality TL → Quality Supervisor
            { 15, "EMP013" },      // Quality TL → Quality Supervisor
            { 16, "EMP014" },      // Operator → Quality TL
            { 17, "EMP014" },      // Operator → Quality TL
            { 18, "EMP014" },      // Operator → Quality TL
            { 19, "EMP015" },      // Operator → Quality TL
            { 20, "EMP015" },      // Operator → Quality TL
            { 21, "EMP013" },      // Operator → Quality Supervisor
            { 22, "EMP013" },      // Operator → Quality Supervisor
            { 23, null },          // Logistics Supervisor
            { 24, "EMP023" },      // Logistics TL → Logistics Supervisor
            { 25, "EMP023" },      // Logistics TL → Logistics Supervisor
            { 26, "EMP024" },      // Operator → Logistics TL
            { 27, "EMP024" },      // Operator → Logistics TL
            { 28, "EMP024" },      // Operator → Logistics TL
            { 29, "EMP025" },      // Operator → Logistics TL
            { 30, "EMP025" },      // Operator → Logistics TL
            { 31, "EMP023" },      // Operator → Logistics Supervisor
            { 32, "EMP023" },      // Operator → Logistics Supervisor
            { 33, null },          // Maintenance Supervisor
            { 34, "EMP033" },      // Maintenance TL → Maintenance Supervisor
            { 35, "EMP033" },      // Maintenance TL → Maintenance Supervisor
            { 36, "EMP034" },      // Operator → Maintenance TL
            { 37, "EMP034" },      // Operator → Maintenance TL
            { 38, "EMP034" },      // Operator → Maintenance TL
            { 39, "EMP035" },      // Operator → Maintenance TL
            { 40, "EMP035" },      // Operator → Maintenance TL
            { 41, "EMP033" },      // Operator → Maintenance Supervisor
            { 42, "EMP033" },      // Operator → Maintenance Supervisor
            { 43, null },          // HR Manager (top level)
            { 44, "EMP043" },      // Admin Staff → HR Manager
            { 45, "EMP043" },      // Admin Staff → HR Manager
            { 46, "EMP043" },      // Admin Staff → HR Manager
            { 47, "EMP043" },      // Admin Staff → HR Manager
            { 48, "EMP043" },      // Admin Staff → HR Manager
            { 49, "EMP043" },      // Admin Staff → HR Manager
            { 50, "EMP043" },      // Admin Staff → HR Manager
        };

        // Shift code assignment
        var shiftCodes = new string[] { "ES", "MS", "NS", "LDS", "DS" };

        // Inactive employees: indices 25, 40, 48
        var inactiveIndices = new HashSet<int> { 25, 40, 48 };

        for (int i = 1; i <= 50; i++)
        {
            var empId = $"EMP{i:D3}";
            var firstName = FirstNames[(i - 1) % FirstNames.Length];
            var lastName = LastNames[(i - 1) % LastNames.Length];
            var displayName = $"{firstName} {lastName}";

            employees.Add(new Employee
            {
                Id = i,
                EmployeeId = empId,
                DisplayName = displayName,
                Email = $"{empId.ToLower()}@yfpoza.com",
                PhoneNumber = $"+27{Random.Shared.Next(700000000, 999999999)}",
                DepartmentId = departmentIds[i],
                Role = roleMap.TryGetValue(i, out var role) ? role : "Operator",
                ManagerId = managerAssignments[i],
                IsActive = !inactiveIndices.Contains(i),
                PasswordHash = "h1234567890",
                CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(30, 365)),
            });
        }

        return employees;
    }

    /// <summary>
    /// Creates a specific employee by index for targeted testing.
    /// </summary>
    public static Employee CreateEmployee(int index)
    {
        var all = CreateAllEmployees();
        if (index < 1 || index > 50)
            throw new ArgumentOutOfRangeException(nameof(index), "Index must be between 1 and 50.");
        return all[index - 1];
    }

    /// <summary>
    /// Gets the SuperAdmin employee (EMP001).
    /// </summary>
    public static Employee GetSuperAdmin() => CreateEmployee(1);

    /// <summary>
    /// Gets the Production Supervisor (EMP002).
    /// </summary>
    public static Employee GetProductionSupervisor() => CreateEmployee(2);

    /// <summary>
    /// Gets a typical operator with warnings (EMP005 — has 3 late arrivals).
    /// </summary>
    public static Employee GetOperatorWithWarnings() => CreateEmployee(5);

    /// <summary>
    /// Gets an inactive employee (EMP025).
    /// </summary>
    public static Employee GetInactiveEmployee() => CreateEmployee(25);

    /// <summary>
    /// Gets a clean operator with no prior warnings (EMP006).
    /// </summary>
    public static Employee GetCleanOperator() => CreateEmployee(6);
}
