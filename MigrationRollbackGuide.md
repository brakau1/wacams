# Migration Rollback Guide

## Overview
This document provides comprehensive guidance for rolling back database migrations, configuration changes, and system state in the WACAMS (Workforce Attendance & Corrective Action Management System) platform.

**Last Updated:** 2026-11-16  
**Version:** 1.0  
**Maintained By:** SuperAdmin Team

---

## Table of Contents
1. [Rollback Principles](#rollback-principles)
2. [Database Migration Rollback](#database-migration-rollback)
3. [Configuration Rollback](#configuration-rollback)
4. [Permission Rollback](#permission-rollback)
5. [Role Assignment Rollback](#role-assignment-rollback)
6. [Emergency Rollback Procedures](#emergency-rollback-procedures)
7. [Rollback Validation](#rollback-validation)
8. [Post-Rollback Checklist](#post-rollback-checklist)

---

## Rollback Principles

### 1. Audit Trail Preservation
- **All rollback operations are logged** in the audit trail
- Original state is preserved in snapshot tables
- Rollback actions are tagged with `ActionType: "Rollback"`

### 2. Reversibility
- Every migration includes an `Up()` and `Down()` method
- Configuration snapshots are taken before major changes
- Permission templates support versioning

### 3. Safety First
- **Never rollback in production without approval**
- Always test rollback in staging first
- Maintain backup snapshots for 90 days

---

## Database Migration Rollback

### EF Core Migrations

#### List Available Migrations
```bash
dotnet ef migrations list
```

#### Rollback to Specific Migration
```bash
# Rollback to previous migration
dotnet ef database update <PreviousMigrationName>

# Rollback to specific migration
dotnet ef database update <TargetMigrationName>

# Rollback all migrations (WARNING: Deletes all data)
dotnet ef database update 0
```

#### Generate Rollback Script
```bash
# Generate SQL script for rollback
dotnet ef migrations script <FromMigration> <ToMigration> --idempotent -o rollback.sql
```

### Manual Rollback Steps

#### 1. Identify Current Migration State
```sql
-- Check current migration version
SELECT * FROM __EFMigrationsHistory ORDER BY MigrationId DESC;
```

#### 2. Backup Current State
```sql
-- Create backup of critical tables
CREATE TABLE Users_Backup_20261116 AS SELECT * FROM Users;
CREATE TABLE Roles_Backup_20261116 AS SELECT * FROM Roles;
CREATE TABLE Permissions_Backup_20261116 AS SELECT * FROM Permissions;
```

#### 3. Execute Rollback
```bash
# Apply rollback migration
dotnet ef database update <TargetMigration>

# Verify rollback
dotnet ef database verify
```

---

## Configuration Rollback

### System Configuration Snapshots

#### View Available Snapshots
```bash
# Via API
GET /api/admin/configuration/snapshots

# Via SuperAdmin Portal
Navigate to: SuperAdmin Portal → Recovery → Configuration Snapshots
```

#### Restore Configuration Snapshot
```bash
# Via API
POST /api/admin/configuration/restore
{
  "snapshotId": "guid-here",
  "reason": "Rolling back due to performance issues"
}
```

#### Manual Configuration Restore
```sql
-- Restore system settings from snapshot
UPDATE SystemSettings ss
SET Value = snap.Value
FROM ConfigurationSnapshots snap
WHERE ss.Key = snap.Key
  AND snap.SnapshotId = '<snapshot-id>';
```

### Feature Flag Rollback
```sql
-- Disable feature flags
UPDATE FeatureFlags
SET IsEnabled = 0
WHERE FeatureName IN ('NewAttendanceRules', 'EnhancedReporting');
```

---

## Permission Rollback

### Permission Template Rollback

#### View Template Versions
```bash
GET /api/admin/permission-templates/{templateId}/versions
```

#### Restore Previous Template Version
```bash
POST /api/admin/permission-templates/{templateId}/restore-version
{
  "versionNumber": 3,
  "reason": "Rolling back due to access issues"
}
```

#### Manual Permission Restore
```sql
-- Restore permissions from audit log
INSERT INTO RolePermissions (RoleId, PermissionId, CreatedAt, CreatedBy)
SELECT 
  aud.OldRoleId,
  aud.OldPermissionId,
  NOW(),
  'ROLLBACK'
FROM AuditLogs aud
WHERE aud.Action = 'PermissionChanged'
  AND aud.Timestamp >= '2026-11-15'
  AND aud.Timestamp <= '2026-11-16';
```

### Bulk Permission Rollback
```bash
# Restore all permissions to previous state
POST /api/admin/permissions/bulk-restore
{
  "snapshotId": "guid-here",
  "reason": "Emergency rollback",
  "affectedRoles": ["Supervisor", "Manager"]
}
```

---

## Role Assignment Rollback

### Individual Role Rollback
```bash
# View role change history
GET /api/admin/users/{employeeId}/role-history

# Restore previous role
POST /api/admin/users/{employeeId}/restore-role
{
  "targetDate": "2026-11-15T10:00:00Z",
  "reason": "Incorrect role assignment"
}
```

### Bulk Role Rollback
```bash
# Restore multiple users to previous roles
POST /api/admin/users/bulk-restore-roles
{
  "employeeIds": ["123456", "789012"],
  "targetDate": "2026-11-15T10:00:00Z",
  "reason": "Mass role assignment error"
}
```

### SQL-Based Role Rollback
```sql
-- Restore roles from audit log
UPDATE Users u
SET RoleId = aud.OldRoleId,
    UpdatedAt = NOW(),
    UpdatedBy = 'ROLLBACK'
FROM AuditLogs aud
WHERE u.EmployeeId = aud.EntityId
  AND aud.Action = 'RoleChanged'
  AND aud.Timestamp >= '2026-11-15';
```

---

## Emergency Rollback Procedures

### Scenario 1: System-Wide Failure

#### Immediate Actions
1. **Stop all background services**
   ```bash
   systemctl stop wacams-scheduler
   systemctl stop wacams-notifications
   ```

2. **Enable maintenance mode**
   ```bash
   # Set maintenance flag in database
   UPDATE SystemSettings 
   SET Value = 'true' 
   WHERE Key = 'MaintenanceMode';
   ```

3. **Rollback to last known good state**
   ```bash
   # Identify last good migration
   dotnet ef migrations list
   
   # Rollback
   dotnet ef database update <LastGoodMigration>
   ```

4. **Restore configuration**
   ```bash
   POST /api/admin/configuration/restore
   {
     "snapshotId": "<last-good-snapshot>",
     "reason": "Emergency rollback - system failure"
   }
   ```

5. **Restart services**
   ```bash
   systemctl start wacams-scheduler
   systemctl start wacams-notifications
   ```

### Scenario 2: Data Corruption

#### Recovery Steps
1. **Stop write operations**
   ```sql
   -- Set database to read-only
   ALTER DATABASE WACAMS SET READ_ONLY;
   ```

2. **Restore from backup**
   ```bash
   # Restore database backup
   RESTORE DATABASE WACAMS 
   FROM DISK = '/backups/wacams_20261115.bak'
   WITH RECOVERY;
   ```

3. **Replay transactions**
   ```sql
   -- Apply transaction logs up to corruption point
   RESTORE LOG WACAMS 
   FROM DISK = '/backups/wacams_log_20261116.trn'
   WITH STOPAT = '2026-11-16 10:00:00';
   ```

4. **Verify data integrity**
   ```bash
   dotnet ef database verify-integrity
   ```

### Scenario 3: Security Breach

#### Immediate Lockdown
1. **Revoke all active sessions**
   ```bash
   POST /api/admin/security/revoke-all-sessions
   ```

2. **Reset all passwords**
   ```bash
   POST /api/admin/security/force-password-reset-all
   ```

3. **Rollback permission changes**
   ```bash
   POST /api/admin/permissions/rollback-since
   {
     "since": "2026-11-16T00:00:00Z",
     "reason": "Security breach investigation"
   }
   ```

4. **Audit all changes**
   ```sql
   -- Find all changes during breach window
   SELECT * FROM AuditLogs 
   WHERE Timestamp >= '2026-11-16 00:00:00'
     AND Timestamp <= '2026-11-16 10:00:00'
   ORDER BY Timestamp;
   ```

---

## Rollback Validation

### Pre-Rollback Checks
```bash
# Verify backup exists
ls -lh /backups/wacams_$(date +%Y%m%d).bak

# Check current migration state
dotnet ef migrations list --no-connect

# Verify database connectivity
dotnet ef database verify
```

### Post-Rollback Validation

#### 1. Database Integrity
```sql
-- Check for orphaned records
SELECT COUNT(*) FROM UserRoles ur
WHERE NOT EXISTS (
  SELECT 1 FROM Users u WHERE u.Id = ur.UserId
);

-- Check for invalid references
SELECT COUNT(*) FROM RolePermissions rp
WHERE NOT EXISTS (
  SELECT 1 FROM Roles r WHERE r.Id = rp.RoleId
);
```

#### 2. Application Health
```bash
# Run health checks
curl http://localhost:5000/health

# Verify API endpoints
curl http://localhost:5000/api/health/detailed
```

#### 3. Data Consistency
```sql
-- Verify user counts
SELECT Role, COUNT(*) as UserCount
FROM Users
WHERE IsActive = 1
GROUP BY Role;

-- Verify permission counts
SELECT r.Name, COUNT(rp.PermissionId) as PermissionCount
FROM Roles r
LEFT JOIN RolePermissions rp ON r.Id = rp.RoleId
GROUP BY r.Name;
```

#### 4. Audit Trail Verification
```sql
-- Verify rollback is logged
SELECT * FROM AuditLogs
WHERE Action LIKE '%Rollback%'
  AND Timestamp >= DATEADD(hour, -1, GETDATE())
ORDER BY Timestamp DESC;
```

---

## Post-Rollback Checklist

### Immediate Actions (0-15 minutes)
- [ ] Verify database integrity
- [ ] Check application health endpoints
- [ ] Review audit logs for rollback entry
- [ ] Notify stakeholders of rollback completion
- [ ] Monitor error logs for anomalies

### Short-Term Actions (15-60 minutes)
- [ ] Test critical user workflows
- [ ] Verify attendance calculations
- [ ] Check notification delivery
- [ ] Validate report generation
- [ ] Review dashboard metrics

### Long-Term Actions (1-24 hours)
- [ ] Conduct post-mortem analysis
- [ ] Update incident report
- [ ] Review monitoring alerts
- [ ] Update rollback documentation
- [ ] Schedule follow-up review

---

## Rollback Automation

### Automated Rollback Script
```bash
#!/bin/bash
# automated-rollback.sh

MIGRATION_NAME=$1
REASON=$2

echo "Starting rollback to migration: $MIGRATION_NAME"
echo "Reason: $REASON"

# Create snapshot
SNAPSHOT_ID=$(curl -s -X POST http://localhost:5000/api/admin/configuration/snapshot \
  -H "Authorization: Bearer $TOKEN" \
  -d "{\"description\":\"Pre-rollback snapshot\",\"reason\":\"$REASON\"}" | jq -r '.id')

echo "Created snapshot: $SNAPSHOT_ID"

# Execute rollback
dotnet ef database update $MIGRATION_NAME

# Verify rollback
if [ $? -eq 0 ]; then
  echo "Rollback successful"
  
  # Log to audit
  curl -X POST http://localhost:5000/api/admin/audit/log \
    -H "Authorization: Bearer $TOKEN" \
    -d "{
      \"action\":\"AutomatedRollback\",
      \"details\":\"Rolled back to $MIGRATION_NAME. Reason: $REASON. Snapshot: $SNAPSHOT_ID\"
    }"
else
  echo "Rollback failed!"
  exit 1
fi
```

### Usage
```bash
chmod +x automated-rollback.sh
./automated-rollback.sh "Migration_20261115" "Performance degradation"
```

---

## Support Contacts

### Emergency Contacts
- **Database Administrator:** dba@company.com
- **System Architect:** architect@company.com
- **Security Team:** security@company.com
- **SuperAdmin:** 803341 (Botlhale Rakau)

### Escalation Path
1. On-call Developer
2. Technical Lead
3. System Architect
4. CTO

---

## Appendix

### A. Common Rollback Scenarios

| Scenario | Rollback Type | Estimated Time | Risk Level |
|----------|--------------|----------------|------------|
| Failed migration | Database | 5-15 min | Low |
| Permission error | Configuration | 2-5 min | Low |
| Role misassignment | Role Assignment | 5-10 min | Medium |
| Data corruption | Full Restore | 30-60 min | High |
| Security breach | Emergency Lockdown | 15-30 min | Critical |

### B. Rollback Command Reference

```bash
# Database
dotnet ef database update <migration>
dotnet ef migrations script <from> <to>

# Configuration
POST /api/admin/configuration/restore
POST /api/admin/configuration/snapshot

# Permissions
POST /api/admin/permissions/bulk-restore
POST /api/admin/permission-templates/{id}/restore-version

# Roles
POST /api/admin/users/{id}/restore-role
POST /api/admin/users/bulk-restore-roles

# Security
POST /api/admin/security/revoke-all-sessions
POST /api/admin/security/force-password-reset-all
```

### C. SQL Queries for Rollback

```sql
-- Find recent migrations
SELECT * FROM __EFMigrationsHistory 
ORDER BY MigrationId DESC 
LIMIT 10;

-- Check audit logs
SELECT * FROM AuditLogs 
WHERE Timestamp >= DATEADD(hour, -24, GETDATE())
ORDER BY Timestamp DESC;

-- Verify user roles
SELECT u.EmployeeId, u.DisplayName, r.Name as RoleName
FROM Users u
JOIN Roles r ON u.RoleId = r.Id
WHERE u.IsActive = 1;
```

---

**Document Control**
- **Owner:** SuperAdmin Team
- **Review Cycle:** Quarterly
- **Next Review:** 2027-02-16
- **Approved By:** Botlhale Rakau (803341)
