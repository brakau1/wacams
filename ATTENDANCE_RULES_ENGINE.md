# Attendance Rules Engine

## Overview

The Attendance Rules Engine is a comprehensive system for calculating attendance metrics, detecting exceptions, and managing corrective actions. It implements a complete workflow from attendance event detection through investigation to corrective action generation.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Attendance Rules Engine                    │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────┐      ┌──────────────┐      ┌──────────┐  │
│  │   Rules      │      │   Services   │      │Validation│  │
│  │              │      │              │      │          │  │
│  │ • LateArrival│      │ • RulesEngine│      │• Attend  │  │
│  │ • EarlyDept  │      │ • Exception  │      │• Except  │  │
│  │ • LostProd   │      │ • Corrective │      │• Correct │  │
│  │ • Absenteeism│      │              │      │          │  │
│  └──────────────┘      └──────────────┘      └──────────┘  │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

## Components

### 1. Calculation Rules

#### LateArrivalCalculationRule
- **Purpose**: Calculates late arrival time and determines if employee is late
- **Input**: ScheduledStart, ActualStart, LateGraceMinutes
- **Output**: LateMinutes, IsLate flag, Exception details
- **Logic**: 
  ```
  LateMinutes = ActualStart - ScheduledStart
  IsLate = LateMinutes > LateGraceMinutes
  ```

#### EarlyDepartureCalculationRule
- **Purpose**: Calculates early departure time and determines if employee left early
- **Input**: ScheduledEnd, ActualEnd, EarlyGraceMinutes
- **Output**: EarlyMinutes, IsEarlyDeparture flag, Exception details
- **Logic**:
  ```
  EarlyMinutes = ScheduledEnd - ActualEnd
  IsEarlyDeparture = EarlyMinutes > EarlyGraceMinutes
  ```

#### LostProductionCalculationRule
- **Purpose**: Calculates total lost production minutes and hours
- **Input**: All time data (Scheduled/Actual Start/End)
- **Output**: LostMinutes, LostHours, Exception details
- **Logic**:
  ```
  LostMinutes = LateMinutes + EarlyMinutes
  LostHours = LostMinutes / 60
  ```

#### UnplannedAbsenteeismCalculationRule
- **Purpose**: Detects unplanned absences and calculates lost time
- **Input**: Scheduled times, Actual times, Leave status, Cancellation status
- **Output**: IsAbsent flag, LostMinutes (full shift), Exception details
- **Logic**:
  ```
  IsAbsent = !IsOnLeave && !IsShiftCancelled && !ActualStart && !ActualEnd
  LostMinutes = Full shift duration if absent
  ```

### 2. Business Rules

#### ExceptionSeverityRule
- **Purpose**: Determines exception severity based on impact
- **Logic**:
  - **High**: LostMinutes > 120 OR IsAbsent
  - **Medium**: LostMinutes >= 30
  - **Low**: LostMinutes < 30

#### CorrectiveActionDeterminationRule
- **Purpose**: Determines appropriate corrective action based on exception type and history
- **Progressive Discipline**:
  - **Late/Early**: VerbalWarning → WrittenWarning → FinalWrittenWarning → Dismissal
  - **Absence**: WrittenWarning → FinalWrittenWarning → Dismissal
  - **Missing Clock**: VerbalWarning → WrittenWarning → FinalWrittenWarning → Dismissal

### 3. Services

#### AttendanceRulesEngine
- **CalculateAttendanceAsync**: Applies all calculation rules to determine attendance status
- **DetectExceptionsAsync**: Finds all exceptions for a given date/department
- **ProcessExceptionAsync**: Handles investigation workflow (approve/reject)
- **GenerateCorrectiveActionAsync**: Creates corrective action for approved exceptions
- **CalculateDepartmentAttendanceAsync**: Calculates department-wide attendance percentage
- **CalculateLostProductionAsync**: Calculates total lost production for a department

### 4. Validation

#### AttendanceValidator
- **Validate**: Validates attendance calculation context
  - Required fields: EmployeeId, AttendanceDate, ShiftType
  - Time ranges: ScheduledStart < ScheduledEnd
  - Reasonable bounds: Actual times within 12 hours of scheduled
  - Non-negative grace periods

- **ValidateExceptionProcessing**: Validates exception investigation
  - Required: ExceptionId, InvestigatorId, Decision
  - Valid decisions: "approve", "reject"

- **ValidateCorrectiveActionGeneration**: Validates corrective action creation
  - Required: ExceptionId, IssuedBy

## Workflow

### 1. Attendance Event Detection
```
Attendance Record Created
    ↓
CalculateAttendanceAsync()
    ↓
┌─────────────────────────────────┐
│ Apply Rules:                    │
│ 1. UnplannedAbsenceRule         │
│ 2. LateArrivalRule              │
│ 3. EarlyDepartureRule           │
│ 4. LostProductionRule           │
└─────────────────────────────────┘
    ↓
AttendanceCalculationResult
    ↓
Has Exception? ──No──→ Done
    ↓ Yes
Create AttendanceException
```

### 2. Exception Investigation
```
Exception Created (Status: Pending)
    ↓
Investigator Reviews
    ↓
ProcessExceptionAsync()
    ↓
┌─────────────────────────────────┐
│ Decision:                       │
│ • Approve → Status: Approved    │
│ • Reject  → Status: Rejected    │
└─────────────────────────────────┘
    ↓
Exception Updated
    ↓
Approved? ──No──→ Done
    ↓ Yes
Generate Corrective Action
```

### 3. Corrective Action Generation
```
Exception Approved
    ↓
GetPriorOccurrences()
    ↓
DetermineAction()
    ↓
┌─────────────────────────────────┐
│ Progressive Discipline:         │
│ 1st: VerbalWarning              │
│ 2nd: WrittenWarning             │
│ 3rd: FinalWrittenWarning        │
│ 4th+: Dismissal                 │
└─────────────────────────────────┘
    ↓
Create CorrectiveAction
    ↓
Notify Employee & Manager
```

## Configuration

### Default Settings
```json
{
  "AttendanceRules": {
    "LateGraceMinutes": 5,
    "EarlyGraceMinutes": 5,
    "LateThresholdMinutes": 30,
    "HalfDayThresholdMinutes": 120
  }
}
```

### Severity Thresholds
- **Low**: < 30 minutes lost
- **Medium**: 30-120 minutes lost
- **High**: > 120 minutes lost OR absent

## Unit Tests

### Test Coverage

#### LateArrivalCalculationRuleTests
- ✅ Employee is late (15 minutes)
- ✅ Employee is late within grace period (3 minutes)
- ✅ Employee is on time
- ✅ Employee is early
- ✅ Missing data handling

#### EarlyDepartureCalculationRuleTests
- ✅ Employee leaves early (60 minutes)
- ✅ Employee leaves early within grace period (3 minutes)
- ✅ Employee stays until scheduled end
- ✅ Employee stays late
- ✅ Missing data handling

#### LostProductionCalculationRuleTests
- ✅ Late arrival calculates lost minutes
- ✅ Early departure calculates lost minutes
- ✅ Late + early calculates total lost minutes
- ✅ Missing clock-in calculates full shift lost minutes
- ✅ Missing clock-out calculates partial shift lost minutes
- ✅ No exceptions returns zero lost minutes
- ✅ LostHours conversion from minutes

#### UnplannedAbsenteeismCalculationRuleTests
- ✅ Absent employee returns full shift lost minutes
- ✅ Employee on leave returns not absent
- ✅ Shift cancelled returns not absent
- ✅ Present employee returns not absent

## Usage Examples

### Calculate Attendance
```csharp
var context = new AttendanceCalculationContext
{
    EmployeeId = "EMP001",
    AttendanceDate = DateTime.Today,
    ShiftType = "Morning",
    ScheduledStart = TimeSpan.FromHours(8),
    ActualStart = TimeSpan.FromHours(8).Add(TimeSpan.FromMinutes(15)),
    ScheduledEnd = TimeSpan.FromHours(17),
    ActualEnd = TimeSpan.FromHours(17),
    LateGraceMinutes = 5,
    EarlyGraceMinutes = 5
};

var result = await rulesEngine.CalculateAttendanceAsync(context);

// result.IsLate = true
// result.LateMinutes = 15
// result.LostMinutes = 15
// result.HasException = true
```

### Detect Exceptions
```csharp
var exceptions = await rulesEngine.DetectExceptionsAsync(
    DateTime.Today, 
    departmentId: "PROD");

foreach (var ex in exceptions)
{
    Console.WriteLine($"{ex.EmployeeId}: {ex.ExceptionType} - {ex.MinutesLost} minutes");
}
```

### Process Exception
```csharp
var updatedException = await rulesEngine.ProcessExceptionAsync(
    exceptionId: Guid.NewGuid(),
    investigatorId: "MGR001",
    decision: "approve",
    notes: "No valid reason provided");

// updatedException.Status = ExceptionStatus.Approved
// updatedException.IsApproved = true
```

### Generate Corrective Action
```csharp
var action = await rulesEngine.GenerateCorrectiveActionAsync(
    exceptionId: Guid.NewGuid(),
    issuedBy: "MGR001");

// action.ActionType = "WrittenWarning"
// action.Description = "WrittenWarning - Occurrence #2"
```

## Integration Points

### Database
- `AttendanceException` table: Stores all detected exceptions
- `CorrectiveAction` table: Stores all corrective actions
- `AttendanceRecord` table: Source of attendance data

### Repositories
- `IAttendanceExceptionRepository`: CRUD operations for exceptions
- `ICorrectiveActionRepository`: CRUD operations for corrective actions

### Services
- `IAttendanceRulesEngine`: Main service interface
- `AttendanceRulesEngine`: Implementation with all business logic

## Future Enhancements

1. **Configurable Rules**: Allow dynamic rule configuration via UI
2. **Rule Chaining**: Support for complex rule combinations
3. **Exception Exemptions**: Handle special cases (e.g., approved late arrivals)
4. **Automated Notifications**: Email/SMS alerts for exceptions
5. **Trend Analysis**: Identify patterns in attendance exceptions
6. **Integration with HR Systems**: Sync with leave management, payroll

## Testing

Run all tests:
```bash
dotnet test WACAMS.Core.RulesEngine.Tests
```

Run specific test class:
```bash
dotnet test --filter "FullyQualifiedName~LateArrivalCalculationRuleTests"
```

## Summary

The Attendance Rules Engine provides a robust, testable, and maintainable system for:
- ✅ Calculating late arrivals, early departures, and lost production
- ✅ Detecting unplanned absenteeism
- ✅ Managing exception investigation workflow
- ✅ Generating progressive disciplinary actions
- ✅ Comprehensive validation and error handling
- ✅ Full unit test coverage

All components follow SOLID principles and are designed for extensibility and maintainability.
