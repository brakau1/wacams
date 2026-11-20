# Progressive Discipline System

## Overview

The Progressive Discipline System implements a comprehensive corrective action management framework with automatic offence tracking, validity periods, and auto-expiry functionality.

## Progressive Discipline Levels

### Late Arrival & Early Departure

| Offence # | Disciplinary Level | Validity Period |
|-----------|-------------------|-----------------|
| 1st | Counselling | 90 days |
| 2nd | Verbal Warning | 120 days |
| 3rd | Written Warning | 180 days |
| 4th | Final Written Warning | 12 months |
| 5th+ | Dismissal Hearing | Permanent |

### Key Principle

**No automatic warning until manager investigation completed.** All corrective actions require:
1. Manager investigation of the attendance exception
2. Approval of the investigation outcome
3. Manual issuance of the corrective action

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│              Progressive Discipline System                    │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────┐      ┌──────────────┐      ┌──────────┐  │
│  │   Models     │      │   Services   │      │   Rules  │  │
│  │              │      │              │      │          │  │
│  │ • Corrective │      │ • Engine     │      │ • Progres│  │
│  │ • Offence    │      │ • AutoExpiry │      │          │  │
│  │ • Validity   │      │              │      │          │  │
│  └──────────────┘      └──────────────┘      └──────────┘  │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

## Components

### Models

#### CorrectiveAction
Represents a disciplinary action issued to an employee.

**Key Properties:**
- `Id`: Unique identifier
- `EmployeeId`: Employee receiving the action
- `ExceptionId`: Related attendance exception
- `OffenceType`: Type of offence (LateArrival, EarlyDeparture, etc.)
- `OffenceNumber`: Sequential offence count (1st, 2nd, 3rd, etc.)
- `Level`: Disciplinary level (Counselling, VerbalWarning, etc.)
- `IssuedBy`: Manager who issued the action
- `Status`: Current status (Issued, Acknowledged, Disputed, etc.)
- `ValidFrom`: Action start date
- `ValidUntil`: Action expiry date
- `IsExpired`: Whether the action has expired
- `RequiresHearing`: Whether a dismissal hearing is required

#### OffenceCounter
Tracks the number of offences for an employee by offence type.

**Key Properties:**
- `EmployeeId`: Employee identifier
- `OffenceType`: Type of offence
- `TotalOffences`: Total lifetime offences
- `ActiveOffences`: Non-expired offences (used for determining next level)
- `LastOffenceDate`: Date of most recent offence

#### ValidityPeriod
Configuration for how long each disciplinary level remains active.

**Default Configuration:**
- Counselling: 90 days
- VerbalWarning: 120 days
- WrittenWarning: 180 days
- FinalWrittenWarning: 365 days
- DismissalHearing: Permanent

### Services

#### ICorrectiveActionEngine
Main service for managing corrective actions.

**Key Methods:**

```csharp
Task<DisciplinaryActionResult> DetermineActionAsync(
    string employeeId,
    Guid exceptionId,
    OffenceType offenceType,
    string issuedBy,
    string reason)
```
Determines and creates a corrective action based on offence history.

```csharp
Task<int> GetOffenceCountAsync(string employeeId, OffenceType offenceType)
```
Gets the current active offence count for an employee.

```csharp
Task<bool> AcknowledgeActionAsync(Guid actionId, string acknowledgedBy)
```
Records employee acknowledgment of the corrective action.

```csharp
Task<bool> DisputeActionAsync(Guid actionId, string disputedBy, string reason)
```
Records a dispute of the corrective action.

```csharp
Task<bool> ScheduleHearingAsync(Guid actionId, DateTime hearingDate, string scheduledBy)
```
Schedules a dismissal hearing (for 5th+ offences).

```csharp
Task<bool> RecordHearingOutcomeAsync(Guid actionId, string outcome, string recordedBy)
```
Records the outcome of a dismissal hearing.

#### IAutoExpiryService
Service for automatically expiring corrective actions.

**Key Methods:**

```csharp
Task<int> ProcessExpiredActionsAsync()
```
Processes all expired actions and updates counters.

```csharp
Task<bool> IsExpiredAsync(Guid actionId)
```
Checks if a specific action has expired.

### Rules

#### ProgressiveDisciplineRule
Business logic for determining disciplinary levels.

**Progression for Late Arrival / Early Departure:**
```csharp
1st offence → Counselling
2nd offence → VerbalWarning
3rd offence → WrittenWarning
4th offence → FinalWrittenWarning
5th+ offence → DismissalHearing
```

## Workflow

### 1. Manager Investigation
```
Attendance Exception Detected
    ↓
Manager Reviews Exception
    ↓
Investigation Completed? ──No──→ Pending
    ↓ Yes
Approve/Reject
    ↓ Approved
Generate Corrective Action
```

### 2. Corrective Action Generation
```
Manager Issues Action
    ↓
DetermineActionAsync()
    ↓
┌─────────────────────────────────┐
│ 1. Get current offence count    │
│ 2. Increment count              │
│ 3. Determine disciplinary level │
│ 4. Get validity period          │
│ 5. Create CorrectiveAction      │
│ 6. Update OffenceCounter        │
└─────────────────────────────────┘
    ↓
Action Created
    ↓
Notify Employee
```

### 3. Employee Acknowledgment
```
Employee Receives Action
    ↓
Reviews Action
    ↓
┌─────────────────────────────────┐
│ Acknowledge → Status: Acknowledged │
│ Dispute   → Status: Disputed       │
└─────────────────────────────────┘
```

### 4. Dismissal Hearing (5th+ Offence)
```
5th Offence Detected
    ↓
DismissalHearing Required
    ↓
ScheduleHearingAsync()
    ↓
Hearing Conducted
    ↓
┌─────────────────────────────────┐
│ Outcome:                        │
│ • Upheld → Status: Upheld       │
│ • Overturned → Status: Overturned│
│   (Decrement counter)           │
└─────────────────────────────────┘
```

### 5. Auto-Expiry
```
Background Service Runs (Daily)
    ↓
ProcessExpiredActionsAsync()
    ↓
┌─────────────────────────────────┐
│ For each expired action:        │
│ 1. Mark as expired              │
│ 2. Update status                │
│ 3. Decrement active counter     │
└─────────────────────────────────┘
    ↓
Counters Updated
```

## Usage Examples

### Determine Corrective Action
```csharp
var result = await correctiveActionEngine.DetermineActionAsync(
    employeeId: "EMP001",
    exceptionId: exception.Id,
    offenceType: OffenceType.LateArrival,
    issuedBy: "MGR001",
    reason: "Employee arrived 45 minutes late without valid reason");

if (result.Success)
{
    Console.WriteLine($"Issued: {result.Description}");
    Console.WriteLine($"Level: {result.DeterminedLevel}");
    Console.WriteLine($"Offence #: {result.OffenceNumber}");
    Console.WriteLine($"Valid Until: {result.ValidUntil}");
    
    if (result.RequiresHearing)
    {
        Console.WriteLine("Dismissal hearing required");
    }
}
```

### Acknowledge Action
```csharp
var success = await correctiveActionEngine.AcknowledgeActionAsync(
    actionId: action.Id,
    acknowledgedBy: "EMP001");

if (success)
{
    Console.WriteLine("Action acknowledged");
}
```

### Dispute Action
```csharp
var success = await correctiveActionEngine.DisputeActionAsync(
    actionId: action.Id,
    disputedBy: "EMP001",
    reason: "I was delayed due to traffic accident");

if (success)
{
    Console.WriteLine("Action disputed");
}
```

### Schedule Hearing
```csharp
var success = await correctiveActionEngine.ScheduleHearingAsync(
    actionId: action.Id,
    hearingDate: DateTime.UtcNow.AddDays(7),
    scheduledBy: "MGR001");

if (success)
{
    Console.WriteLine("Hearing scheduled");
}
```

### Record Hearing Outcome
```csharp
var success = await correctiveActionEngine.RecordHearingOutcomeAsync(
    actionId: action.Id,
    outcome: "Dismissal upheld",
    recordedBy: "MGR001");

if (success)
{
    Console.WriteLine("Hearing outcome recorded");
}
```

### Process Expired Actions
```csharp
var processedCount = await autoExpiryService.ProcessExpiredActionsAsync();
Console.WriteLine($"Processed {processedCount} expired actions");
```

## Database Schema

### corrective_actions Table
```sql
CREATE TABLE corrective_actions (
    id UUID PRIMARY KEY,
    employee_id VARCHAR(20) NOT NULL,
    exception_id UUID NOT NULL,
    offence_type VARCHAR(50) NOT NULL,
    offence_number INTEGER NOT NULL,
    level VARCHAR(50) NOT NULL,
    description TEXT NOT NULL,
    reason TEXT NOT NULL,
    issued_by VARCHAR(20) NOT NULL,
    issued_at TIMESTAMPTZ NOT NULL,
    due_date TIMESTAMPTZ,
    is_acknowledged BOOLEAN DEFAULT FALSE,
    acknowledged_at TIMESTAMPTZ,
    acknowledged_by VARCHAR(20),
    status VARCHAR(50) NOT NULL,
    resolution_notes TEXT,
    resolved_at TIMESTAMPTZ,
    resolved_by VARCHAR(20),
    valid_from TIMESTAMPTZ NOT NULL,
    valid_until TIMESTAMPTZ NOT NULL,
    is_expired BOOLEAN DEFAULT FALSE,
    expired_at TIMESTAMPTZ,
    requires_hearing BOOLEAN DEFAULT FALSE,
    hearing_scheduled_at TIMESTAMPTZ,
    hearing_outcome TEXT,
    hearing_conducted_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL,
    created_by VARCHAR(50) NOT NULL,
    updated_by VARCHAR(50) NOT NULL
);

CREATE INDEX idx_corrective_actions_employee ON corrective_actions(employee_id);
CREATE INDEX idx_corrective_actions_exception ON corrective_actions(exception_id);
CREATE INDEX idx_corrective_actions_status ON corrective_actions(status);
CREATE INDEX idx_corrective_actions_valid_until ON corrective_actions(valid_until);
CREATE INDEX idx_corrective_actions_expired ON corrective_actions(is_expired);
```

### offence_counters Table
```sql
CREATE TABLE offence_counters (
    id UUID PRIMARY KEY,
    employee_id VARCHAR(20) NOT NULL,
    offence_type VARCHAR(50) NOT NULL,
    total_offences INTEGER NOT NULL DEFAULT 0,
    active_offences INTEGER NOT NULL DEFAULT 0,
    last_offence_date TIMESTAMPTZ NOT NULL,
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ NOT NULL,
    UNIQUE(employee_id, offence_type)
);

CREATE INDEX idx_offence_counters_employee ON offence_counters(employee_id);
CREATE INDEX idx_offence_counters_type ON offence_counters(offence_type);
```

### validity_periods Table
```sql
CREATE TABLE validity_periods (
    id UUID PRIMARY KEY,
    level VARCHAR(50) NOT NULL UNIQUE,
    validity_days INTEGER NOT NULL,
    description TEXT NOT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ NOT NULL
);

-- Seed default validity periods
INSERT INTO validity_periods (level, validity_days, description) VALUES
('Counselling', 90, 'Counselling valid for 90 days'),
('VerbalWarning', 120, 'Verbal Warning valid for 120 days'),
('WrittenWarning', 180, 'Written Warning valid for 180 days'),
('FinalWrittenWarning', 365, 'Final Written Warning valid for 12 months'),
('DismissalHearing', 9999, 'Dismissal Hearing is permanent');
```

## Unit Tests

### ProgressiveDisciplineRuleTests
- ✅ LateArrival progression (1st → Counselling, 2nd → VerbalWarning, etc.)
- ✅ EarlyDeparture progression (same as LateArrival)
- ✅ Invalid count returns None
- ✅ RequiresHearing returns correct values
- ✅ GetDescription returns correct descriptions
- ✅ GetOrdinal returns correct ordinals (1st, 2nd, 3rd, 4th, 11th, etc.)

### CorrectiveActionEngineTests
- ✅ First offence returns Counselling
- ✅ Fifth offence returns DismissalHearing
- ✅ AcknowledgeAction updates status
- ✅ DisputeAction updates status and notes
- ✅ ScheduleHearing updates status and date
- ✅ RecordHearingOutcome (Upheld) sets status
- ✅ RecordHearingOutcome (Overturned) decrements counter

## Running Tests

```bash
dotnet test WACAMS.Core.Disciplinary.Tests
```

## Background Service Configuration

Add to `Program.cs`:
```csharp
builder.Services.AddHostedService<AutoExpiryBackgroundService>();
```

Create background service:
```csharp
public class AutoExpiryBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutoExpiryBackgroundService> _logger;

    public AutoExpiryBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AutoExpiryBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var expiryService = scope.ServiceProvider.GetRequiredService<IAutoExpiryService>();
                
                var processedCount = await expiryService.ProcessExpiredActionsAsync();
                _logger.LogInformation("Processed {Count} expired corrective actions", processedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired corrective actions");
            }

            // Run daily
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}
```

## Summary

The Progressive Discipline System provides:
- ✅ Complete progressive discipline workflow
- ✅ Automatic offence counting
- ✅ Validity period tracking
- ✅ Auto-expiry functionality
- ✅ Dismissal hearing management
- ✅ Comprehensive audit trail
- ✅ Full unit test coverage

All actions require manager investigation before issuance - no automatic warnings.
