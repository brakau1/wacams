# Escalation Workflow System

## Overview

The Escalation Workflow System provides automated multi-level escalation for attendance exceptions that require managerial action. When an exception is not resolved within the configured timeframe, it automatically escalates to the next management level.

## Escalation Hierarchy

The system implements a 7-level escalation hierarchy:

| Level | Role | Escalation Time |
|-------|------|-----------------|
| 1 | Manager Level 1 | 24 hours |
| 2 | Manager Level 2 | 48 hours |
| 3 | Manager Level 3 | 72 hours |
| 4 | Manager Level 4 | 96 hours |
| 5 | Manager Level 5 | 120 hours |
| 6 | HR | 144 hours |
| 7 | Admin | Final level |

## Key Features

### 1. Automatic Escalation
- Background service runs every 15 minutes
- Checks for escalations due for escalation
- Automatically moves to next level when deadline passes
- Stops at Admin level (final escalation)

### 2. Document Upload Auto-Close
- When a manager uploads the corrective action document
- Escalation automatically closes
- Prevents further escalation

### 3. Status Tracking
- **Pending**: Initial state, awaiting manager action
- **Acknowledged**: Manager has acknowledged the escalation
- **ActionTaken**: Manager has taken action
- **Escalated**: Moved to next level
- **AutoClosed**: Document uploaded, auto-closed
- **Resolved**: Manually resolved

### 4. Audit Trail
- Complete history of all escalation actions
- Tracks who performed each action
- Timestamps for all state changes
- Comments and notes captured

## Architecture

### Core Components

```
WACAMS.Core/
└── Escalations/
    ├── Models/
    │   ├── Escalation.cs              # Main entity
    │   ├── EscalationLevel.cs         # Enum: ManagerLevel1-7
    │   ├── EscalationStatus.cs        # Enum: Pending, Acknowledged, etc.
    │   └── EscalationHistory.cs       # History tracking
    ├── Rules/
    │   └── EscalationRules.cs         # Escalation configuration
    └── Interfaces/
        ├── IEscalationRepository.cs   # Data access
        └── IEscalationService.cs      # Business logic

WACAMS.Infrastructure/
├── Repositories/
│   └── EscalationRepository.cs        # EF Core implementation
├── Services/
│   └── EscalationService.cs           # Business logic implementation
├── BackgroundServices/
│   └── EscalationBackgroundService.cs # Auto-escalation processor
├── Notifications/
│   ├── IEscalationNotificationService.cs
│   └── EscalationNotificationService.cs
└── Audit/
    ├── IAuditService.cs
    ├── AuditLog.cs
    └── AuditService.cs

WACAMS.Api/
└── Controllers/
    └── EscalationsController.cs       # REST API endpoints
```

## Database Schema

### escalations Table
```sql
CREATE TABLE escalations (
    id UUID PRIMARY KEY,
    exception_id UUID NOT NULL,
    employee_id VARCHAR(50),
    current_level INTEGER NOT NULL,
    assigned_to VARCHAR(50),
    status VARCHAR(50) NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL,
    acknowledged_at TIMESTAMP WITH TIME ZONE,
    action_taken_at TIMESTAMP WITH TIME ZONE,
    escalated_at TIMESTAMP WITH TIME ZONE,
    resolved_at TIMESTAMP WITH TIME ZONE,
    auto_closed_at TIMESTAMP WITH TIME ZONE,
    next_escalation_time TIMESTAMP WITH TIME ZONE NOT NULL,
    document_uploaded BOOLEAN NOT NULL,
    document_url VARCHAR(500),
    document_uploaded_at TIMESTAMP WITH TIME ZONE,
    notes TEXT,
    action_taken_notes TEXT,
    created_by VARCHAR(50) NOT NULL,
    updated_by VARCHAR(50),
    updated_at TIMESTAMP WITH TIME ZONE
);
```

### escalation_history Table
```sql
CREATE TABLE escalation_history (
    id UUID PRIMARY KEY,
    escalation_id UUID NOT NULL,
    from_level INTEGER,
    to_level INTEGER,
    action VARCHAR(100) NOT NULL,
    performed_by VARCHAR(50),
    comments TEXT,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL
);
```

## API Endpoints

### GET /api/escalations/{id}
Get escalation by ID

### GET /api/escalations/exception/{exceptionId}
Get all escalations for an exception

### POST /api/escalations
Create a new escalation
```json
{
  "exceptionId": "guid",
  "createdBy": "userId"
}
```

### POST /api/escalations/{id}/acknowledge
Acknowledge an escalation
```json
{
  "acknowledgedBy": "userId"
}
```

### POST /api/escalations/{id}/action-taken
Mark action taken
```json
{
  "actionNotes": "Corrective action implemented",
  "updatedBy": "userId"
}
```

### POST /api/escalations/{id}/escalate
Manually escalate to next level
```json
{
  "escalatedBy": "userId"
}
```

### POST /api/escalations/{id}/upload-document
Upload corrective action document
```json
{
  "documentUrl": "https://storage.example.com/doc.pdf",
  "uploadedBy": "userId"
}
```

### POST /api/escalations/{id}/resolve
Resolve an escalation
```json
{
  "resolvedBy": "userId"
}
```

## Service Implementation

### EscalationService

```csharp
public class EscalationService : IEscalationService
{
    private readonly IEscalationRepository _repository;
    private readonly ILogger<EscalationService> _logger;
    private readonly EscalationRules _rules;

    public async Task<Escalation> CreateEscalationAsync(Guid exceptionId, string createdBy)
    {
        var escalation = new Escalation
        {
            ExceptionId = exceptionId,
            CurrentLevel = EscalationLevel.ManagerLevel1,
            AssignedTo = "ManagerLevel1",
            Status = EscalationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            NextEscalationTime = _rules.CalculateNextEscalationTime(
                EscalationLevel.ManagerLevel1, 
                DateTime.UtcNow),
            DocumentUploaded = false,
            CreatedBy = createdBy
        };

        return await _repository.CreateAsync(escalation);
    }

    public async Task<Escalation> EscalateAsync(Guid id, string escalatedBy)
    {
        var escalation = await _repository.GetByIdAsync(id);
        var nextLevel = _rules.GetNextLevel(escalation.CurrentLevel);

        if (nextLevel == null)
        {
            // Final level reached
            escalation.Status = EscalationStatus.AutoClosed;
            escalation.AutoClosedAt = DateTime.UtcNow;
        }
        else
        {
            escalation.CurrentLevel = nextLevel.Value;
            escalation.AssignedTo = nextLevel.Value.ToString();
            escalation.Status = EscalationStatus.Escalated;
            escalation.EscalatedAt = DateTime.UtcNow;
            escalation.NextEscalationTime = _rules.CalculateNextEscalationTime(
                nextLevel.Value, 
                DateTime.UtcNow);
        }

        return await _repository.UpdateAsync(escalation);
    }

    public async Task<Escalation> UploadDocumentAsync(Guid id, string documentUrl, string uploadedBy)
    {
        var escalation = await _repository.GetByIdAsync(id);
        
        escalation.DocumentUploaded = true;
        escalation.DocumentUrl = documentUrl;
        escalation.DocumentUploadedAt = DateTime.UtcNow;

        // Auto-close when document is uploaded
        if (escalation.Status != EscalationStatus.Resolved)
        {
            escalation.Status = EscalationStatus.AutoClosed;
            escalation.AutoClosedAt = DateTime.UtcNow;
        }

        return await _repository.UpdateAsync(escalation);
    }
}
```

### Background Service

```csharp
public class EscalationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EscalationBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var escalationService = scope.ServiceProvider
                    .GetRequiredService<IEscalationService>();

                await escalationService.ProcessEscalationsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing escalations");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }
}
```

## Escalation Rules Configuration

```csharp
public class EscalationRules
{
    public Dictionary<EscalationLevel, int> EscalationHours { get; set; } = new()
    {
        { EscalationLevel.ManagerLevel1, 24 },
        { EscalationLevel.ManagerLevel2, 48 },
        { EscalationLevel.ManagerLevel3, 72 },
        { EscalationLevel.ManagerLevel4, 96 },
        { EscalationLevel.ManagerLevel5, 120 },
        { EscalationLevel.HR, 144 }
    };

    public EscalationLevel? GetNextLevel(EscalationLevel currentLevel)
    {
        return currentLevel switch
        {
            EscalationLevel.ManagerLevel1 => EscalationLevel.ManagerLevel2,
            EscalationLevel.ManagerLevel2 => EscalationLevel.ManagerLevel3,
            EscalationLevel.ManagerLevel3 => EscalationLevel.ManagerLevel4,
            EscalationLevel.ManagerLevel4 => EscalationLevel.ManagerLevel5,
            EscalationLevel.ManagerLevel5 => EscalationLevel.HR,
            EscalationLevel.HR => EscalationLevel.Admin,
            EscalationLevel.Admin => null // Final level
        };
    }

    public DateTime CalculateNextEscalationTime(EscalationLevel currentLevel, DateTime fromTime)
    {
        var hours = GetHoursBeforeEscalation(currentLevel);
        return fromTime.AddHours(hours);
    }
}
```

## Workflow Example

### Scenario: Late Attendance Exception

1. **Exception Created** (Day 0, 08:00)
   - Employee clocks in 30 minutes late
   - Exception created with category "LateArrival"

2. **Escalation Created** (Day 0, 08:05)
   - Escalation created at ManagerLevel1
   - Next escalation time: Day 1, 08:05 (24 hours)
   - Status: Pending

3. **Manager Acknowledges** (Day 0, 10:00)
   - ManagerLevel1 acknowledges the escalation
   - Status: Acknowledged

4. **No Action Taken** (Day 1, 08:05)
   - Background service detects overdue escalation
   - Automatically escalates to ManagerLevel2
   - Next escalation time: Day 3, 08:05 (48 hours)
   - Status: Escalated

5. **Document Uploaded** (Day 2, 14:00)
   - ManagerLevel2 uploads corrective action document
   - Escalation auto-closes
   - Status: AutoClosed

## Monitoring and Reporting

### Active Escalations Query
```sql
SELECT 
    e.id,
    e.exception_id,
    e.employee_id,
    e.current_level,
    e.assigned_to,
    e.status,
    e.next_escalation_time,
    EXTRACT(EPOCH FROM (e.next_escalation_time - NOW())) / 3600 AS hours_remaining
FROM escalations e
WHERE e.status IN ('Pending', 'Acknowledged', 'ActionTaken', 'Escalated')
ORDER BY e.next_escalation_time ASC;
```

### Escalation Statistics
```sql
SELECT 
    current_level,
    COUNT(*) as total_escalations,
    AVG(EXTRACT(EPOCH FROM (resolved_at - created_at)) / 3600) as avg_resolution_hours
FROM escalations
WHERE created_at >= NOW() - INTERVAL '30 days'
GROUP BY current_level
ORDER BY current_level;
```

## Integration Points

### 1. Exception Service Integration
When an exception is created that requires escalation:
```csharp
var escalation = await _escalationService.CreateEscalationAsync(
    exception.Id,
    "System");
```

### 2. Document Upload Integration
When a corrective action document is uploaded:
```csharp
await _escalationService.UploadDocumentAsync(
    escalationId,
    documentUrl,
    userId);
```

### 3. Notification Integration
Send notifications when escalation occurs:
```csharp
await _notificationService.SendEscalationCreatedNotificationAsync(escalation);
await _notificationService.SendEscalationEscalatedNotificationAsync(escalation);
```

## Configuration

### appsettings.json
```json
{
  "Escalation": {
    "BackgroundService": {
      "Enabled": true,
      "CheckIntervalMinutes": 15
    },
    "Levels": {
      "ManagerLevel1": 24,
      "ManagerLevel2": 48,
      "ManagerLevel3": 72,
      "ManagerLevel4": 96,
      "ManagerLevel5": 120,
      "HR": 144
    }
  }
}
```

## Best Practices

1. **Monitor Active Escalations**: Set up alerts for escalations approaching their deadline
2. **Review Escalation Trends**: Analyze which exceptions are frequently escalating
3. **Manager Training**: Ensure managers understand their responsibilities
4. **Document Upload**: Encourage prompt document uploads to auto-close escalations
5. **Regular Audits**: Review escalation history for process improvements

## Future Enhancements

- [ ] Email/SMS notifications at each escalation level
- [ ] Dashboard for monitoring active escalations
- [ ] Escalation analytics and reporting
- [ ] Custom escalation rules per exception type
- [ ] Escalation SLA tracking
- [ ] Integration with HR systems for manager mapping
- [ ] Mobile app for managers to handle escalations
- [ ] Escalation approval workflows

## Related Documentation

- [Attendance Exception Management](./ATTENDANCE_EXCEPTIONS.md)
- [Progressive Discipline System](./PROGRESSIVE_DISCIPLINE.md)
- [Attendance Rules Engine](./ATTENDANCE_RULES_ENGINE.md)
- [GIS Integration](./GIS_INTEGRATION.md)
