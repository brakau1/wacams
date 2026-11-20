# Disaster Recovery Plan

## Overview
This document outlines the disaster recovery procedures for the WACAMS (Workforce Attendance & Corrective Action Management System) platform. It provides step-by-step instructions for recovering from various disaster scenarios.

**Last Updated:** 2026-11-16  
**Version:** 1.0  
**Maintained By:** SuperAdmin Team  
**RTO (Recovery Time Objective):** 4 hours  
**RPO (Recovery Point Objective):** 1 hour

---

## Table of Contents
1. [Disaster Recovery Principles](#disaster-recovery-principles)
2. [Backup Strategy](#backup-strategy)
3. [Disaster Scenarios](#disaster-scenarios)
4. [Recovery Procedures](#recovery-procedures)
5. [Communication Plan](#communication-plan)
6. [Testing and Validation](#testing-and-validation)
7. [Post-Recovery Actions](#post-recovery-actions)

---

## Disaster Recovery Principles

### 1. Prevention First
- Regular backups (daily, weekly, monthly)
- Redundant systems where possible
- Proactive monitoring and alerting

### 2. Rapid Response
- Clear escalation procedures
- Pre-defined recovery steps
- Automated recovery where possible

### 3. Data Integrity
- Verify data consistency after recovery
- Maintain audit trails
- Validate business logic

---

## Backup Strategy

### Database Backups

#### Daily Backups (Automated)
```bash
# Backup schedule: Every day at 2:00 AM
BACKUP DATABASE WACAMS 
TO DISK = '/backups/daily/wacams_$(date +%Y%m%d).bak'
WITH COMPRESSION, STATS = 10;
```

#### Weekly Full Backups
```bash
# Backup schedule: Every Sunday at 1:00 AM
BACKUP DATABASE WACAMS 
TO DISK = '/backups/weekly/wacams_full_$(date +%Y%m%d).bak'
WITH COMPRESSION, STATS = 10;
```

#### Monthly Archive Backups
```bash
# Backup schedule: First day of month at 12:00 AM
BACKUP DATABASE WACAMS 
TO DISK = '/backups/monthly/wacams_archive_$(date +%Y%m).bak'
WITH COMPRESSION, STATS = 10;
```

### Transaction Log Backups
```bash
# Backup schedule: Every 15 minutes
BACKUP LOG WACAMS 
TO DISK = '/backups/logs/wacams_log_$(date +%Y%m%d_%H%M).trn'
WITH COMPRESSION, STATS = 10;
```

### Configuration Backups

#### System Configuration
```bash
# Export system settings
curl -X GET http://localhost:5000/api/admin/configuration/export \
  -H "Authorization: Bearer $TOKEN" \
  -o /backups/config/system_config_$(date +%Y%m%d).json
```

#### Permission Templates
```bash
# Export permission templates
curl -X GET http://localhost:5000/api/admin/permission-templates/export \
  -H "Authorization: Bearer $TOKEN" \
  -o /backups/config/permission_templates_$(date +%Y%m%d).json
```

### File Storage Backups

#### Document Storage
```bash
# Backup uploaded documents
rsync -avz /app/storage/documents/ /backups/files/documents_$(date +%Y%m%d)/

# Backup generated reports
rsync -avz /app/storage/reports/ /backups/files/reports_$(date +%Y%m%d)/
```

---

## Disaster Scenarios

### Scenario 1: Database Server Failure

**Impact:** Complete system unavailability  
**Probability:** Low  
**Recovery Time:** 2-4 hours

#### Symptoms
- Database connection errors
- Application crashes
- Unable to access any data

#### Recovery Steps
1. **Assess the situation**
   ```bash
   # Check database status
   systemctl status postgresql
   
   # Check error logs
   tail -f /var/log/postgresql/postgresql.log
   ```

2. **Failover to standby server** (if available)
   ```bash
   # Promote standby to primary
   su - postgres -c "pg_ctl promote -D /var/lib/postgresql/data"
   ```

3. **Restore from backup** (if no standby)
   ```bash
   # Stop application
   systemctl stop wacams-api
   
   # Restore database
   RESTORE DATABASE WACAMS 
   FROM DISK = '/backups/daily/wacams_$(date +%Y%m%d).bak'
   WITH RECOVERY;
   
   # Apply transaction logs
   RESTORE LOG WACAMS 
   FROM DISK = '/backups/logs/wacams_log_*.trn'
   WITH RECOVERY;
   ```

4. **Verify recovery**
   ```bash
   # Check database integrity
   psql -d WACAMS -c "SELECT COUNT(*) FROM Users;"
   
   # Restart application
   systemctl start wacams-api
   ```

### Scenario 2: Application Server Failure

**Impact:** System unavailability  
**Probability:** Medium  
**Recovery Time:** 1-2 hours

#### Symptoms
- Application not responding
- HTTP 503 errors
- Service crashes

#### Recovery Steps
1. **Check application logs**
   ```bash
   journalctl -u wacams-api -n 100
   ```

2. **Restart application service**
   ```bash
   systemctl restart wacams-api
   ```

3. **If restart fails, redeploy**
   ```bash
   # Pull latest stable version
   cd /app/wacams
   git checkout tags/v1.0.0
   
   # Rebuild and restart
   dotnet build
   systemctl restart wacams-api
   ```

4. **Verify recovery**
   ```bash
   curl http://localhost:5000/health
   ```

### Scenario 3: Data Corruption

**Impact:** Partial or complete data loss  
**Probability:** Low  
**Recovery Time:** 3-5 hours

#### Symptoms
- Inconsistent data
- Calculation errors
- Missing records

#### Recovery Steps
1. **Stop write operations**
   ```bash
   # Set database to read-only
   psql -d WACAMS -c "ALTER DATABASE WACAMS SET READ_ONLY;"
   ```

2. **Identify corruption point**
   ```sql
   -- Check last known good state
   SELECT MAX(Timestamp) FROM AuditLogs 
   WHERE Action = 'SystemHealthCheck' AND Status = 'OK';
   ```

3. **Restore to point before corruption**
   ```bash
   # Restore database
   RESTORE DATABASE WACAMS 
   FROM DISK = '/backups/daily/wacams_$(date -d 'yesterday' +%Y%m%d).bak'
   WITH NORECOVERY;
   
   # Apply logs up to corruption point
   RESTORE LOG WACAMS 
   FROM DISK = '/backups/logs/wacams_log_*.trn'
   WITH STOPAT = '2026-11-15 23:00:00', RECOVERY;
   ```

4. **Verify data integrity**
   ```bash
   # Run integrity checks
   dotnet ef database verify-integrity
   ```

### Scenario 4: Security Breach

**Impact:** Data compromise, unauthorized access  
**Probability:** Medium  
**Recovery Time:** 4-8 hours

#### Symptoms
- Unauthorized access detected
- Data exfiltration alerts
- Unusual system behavior

#### Recovery Steps
1. **Immediate lockdown**
   ```bash
   # Revoke all sessions
   curl -X POST http://localhost:5000/api/admin/security/revoke-all-sessions
   
   # Enable maintenance mode
   psql -d WACAMS -c "UPDATE SystemSettings SET Value = 'true' WHERE Key = 'MaintenanceMode';"
   ```

2. **Reset all credentials**
   ```bash
   # Force password reset for all users
   curl -X POST http://localhost:5000/api/admin/security/force-password-reset-all
   
   # Regenerate API keys
   curl -X POST http://localhost:5000/api/admin/security/regenerate-api-keys
   ```

3. **Audit all changes**
   ```sql
   -- Find all changes during breach window
   SELECT * FROM AuditLogs 
   WHERE Timestamp >= '2026-11-16 00:00:00'
   ORDER BY Timestamp;
   ```

4. **Restore from clean backup**
   ```bash
   # Restore to point before breach
   RESTORE DATABASE WACAMS 
   FROM DISK = '/backups/daily/wacams_$(date -d '2 days ago' +%Y%m%d).bak'
   WITH RECOVERY;
   ```

5. **Enhance security**
   ```bash
   # Update firewall rules
   ufw enable
   ufw allow 22/tcp
   ufw allow 80/tcp
   ufw allow 443/tcp
   
   # Enable additional logging
   systemctl enable auditd
   systemctl start auditd
   ```

### Scenario 5: Network Failure

**Impact:** System unavailability  
**Probability:** Medium  
**Recovery Time:** 1-3 hours

#### Symptoms
- Network connectivity issues
- Unable to reach servers
- DNS resolution failures

#### Recovery Steps
1. **Check network connectivity**
   ```bash
   # Test basic connectivity
   ping 8.8.8.8
   
   # Check DNS
   nslookup wacams.company.com
   
   # Check firewall
   ufw status
   ```

2. **Restore network configuration**
   ```bash
   # Restore network settings
   cp /backups/config/network_$(date +%Y%m%d).conf /etc/network/interfaces
   systemctl restart networking
   ```

3. **Verify connectivity**
   ```bash
   # Test application access
   curl http://wacams.company.com/health
   ```

---

## Recovery Procedures

### Database Recovery

#### Step-by-Step Database Restore
```bash
#!/bin/bash
# database-restore.sh

BACKUP_FILE=$1
RESTORE_POINT=$2

echo "Starting database restore from: $BACKUP_FILE"

# Stop application
systemctl stop wacams-api

# Restore database
psql -U postgres -d WACAMS -c "
  ALTER DATABASE WACAMS SET READ_ONLY;
"

pg_restore -U postgres -d WACAMS $BACKUP_FILE

# Apply transaction logs if restore point specified
if [ ! -z "$RESTORE_POINT" ]; then
  for log in /backups/logs/wacams_log_*.trn; do
    psql -U postgres -d WACAMS -c "
      RESTORE LOG WACAMS FROM DISK = '$log' 
      WITH STOPAT = '$RESTORE_POINT', NORECOVERY;
    "
  done
fi

# Set database back to read-write
psql -U postgres -d WACAMS -c "
  ALTER DATABASE WACAMS SET READ_WRITE;
"

# Verify restore
psql -U postgres -d WACAMS -c "SELECT COUNT(*) FROM Users;"

# Restart application
systemctl start wacams-api

echo "Database restore completed"
```

### Application Recovery

#### Step-by-Step Application Restore
```bash
#!/bin/bash
# application-restore.sh

VERSION=$1

echo "Restoring application to version: $VERSION"

# Stop application
systemctl stop wacams-api

# Backup current version
cp -r /app/wacams /backups/apps/wacams_$(date +%Y%m%d_%H%M)

# Restore version
cd /app/wacams
git fetch --tags
git checkout tags/v$VERSION

# Rebuild
dotnet restore
dotnet build --configuration Release

# Restart
systemctl start wacams-api

# Verify
sleep 10
curl -f http://localhost:5000/health || exit 1

echo "Application restore completed"
```

### Configuration Recovery

#### Step-by-Step Configuration Restore
```bash
#!/bin/bash
# configuration-restore.sh

SNAPSHOT_FILE=$1

echo "Restoring configuration from: $SNAPSHOT_FILE"

# Import system settings
curl -X POST http://localhost:5000/api/admin/configuration/import \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d @$SNAPSHOT_FILE

# Verify import
curl -X GET http://localhost:5000/api/admin/configuration \
  -H "Authorization: Bearer $TOKEN"

echo "Configuration restore completed"
```

---

## Communication Plan

### Internal Communication

#### Incident Declaration (0-15 minutes)
```
Subject: [CRITICAL] WACAMS System Outage - Incident Declared

Team,

We are experiencing a critical system outage affecting WACAMS.

Impact: [Description of impact]
Severity: Critical
Incident Commander: [Name]
Next Update: 15 minutes

Please stand by for updates.
```

#### Status Updates (Every 30 minutes)
```
Subject: [UPDATE] WACAMS System Outage - Status Update #X

Team,

Current Status: [In Progress / Resolved]
Progress: [Description of progress]
ETA: [Estimated time to resolution]
Next Update: 30 minutes

Contact: [Incident Commander]
```

#### Resolution Notification
```
Subject: [RESOLVED] WACAMS System Outage - Incident Resolved

Team,

The WACAMS system outage has been resolved.

Duration: [Start time] to [End time]
Root Cause: [Brief description]
Impact: [Summary of impact]
Next Steps: [Post-incident actions]

Thank you for your patience.
```

### External Communication

#### Customer Notification (Initial)
```
Subject: WACAMS System Maintenance Notice

Dear Customer,

We are currently experiencing technical difficulties with the WACAMS system. Our team is working to resolve this issue as quickly as possible.

Expected Resolution: [Time estimate]
Impact: [Description of impact]

We apologize for any inconvenience and will provide updates shortly.

Best regards,
WACAMS Support Team
```

#### Customer Notification (Resolution)
```
Subject: WACAMS System Restored

Dear Customer,

The WACAMS system has been restored and is now fully operational.

Duration of Outage: [Start time] to [End time]
Impact: [Summary of impact]

We apologize for any disruption to your operations. If you experience any issues, please contact our support team.

Best regards,
WACAMS Support Team
```

---

## Testing and Validation

### Disaster Recovery Testing Schedule

#### Monthly Tests
- **Database Restore Test:** Restore latest backup to test environment
- **Application Failover Test:** Switch to standby server
- **Configuration Restore Test:** Import configuration snapshot

#### Quarterly Tests
- **Full Disaster Recovery Drill:** Simulate complete system failure
- **Security Breach Simulation:** Test breach response procedures
- **Network Failover Test:** Test network redundancy

### Test Procedures

#### Database Restore Test
```bash
#!/bin/bash
# dr-test-database.sh

echo "Starting disaster recovery test: Database Restore"

# Create test database
psql -U postgres -c "CREATE DATABASE WACAMS_Test;"

# Restore backup
pg_restore -U postgres -d WACAMS_Test /backups/daily/wacams_$(date +%Y%m%d).bak

# Verify data
psql -U postgres -d WACAMS_Test -c "SELECT COUNT(*) FROM Users;"

# Cleanup
psql -U postgres -c "DROP DATABASE WACAMS_Test;"

echo "Test completed successfully"
```

#### Application Failover Test
```bash
#!/bin/bash
# dr-test-failover.sh

echo "Starting disaster recovery test: Application Failover"

# Switch to standby
ssh standby-server "systemctl start wacams-api"

# Verify standby is active
curl -f http://standby-server:5000/health || exit 1

# Switch back to primary
ssh standby-server "systemctl stop wacams-api"
systemctl start wacams-api

echo "Test completed successfully"
```

---

## Post-Recovery Actions

### Immediate Actions (0-1 hour)
- [ ] Verify system functionality
- [ ] Check data integrity
- [ ] Review audit logs
- [ ] Notify stakeholders
- [ ] Monitor system performance

### Short-Term Actions (1-24 hours)
- [ ] Conduct post-incident review
- [ ] Document lessons learned
- [ ] Update recovery procedures
- [ ] Review monitoring alerts
- [ ] Validate backup integrity

### Long-Term Actions (1-7 days)
- [ ] Implement preventive measures
- [ ] Update disaster recovery plan
- [ ] Conduct training sessions
- [ ] Review and update runbooks
- [ ] Schedule follow-up tests

---

## Support Contacts

### Emergency Contacts
- **Incident Commander:** +27 XX XXX XXXX
- **Database Administrator:** +27 XX XXX XXXX
- **System Administrator:** +27 XX XXX XXXX
- **Network Administrator:** +27 XX XXX XXXX
- **Security Team:** +27 XX XXX XXXX

### Escalation Matrix

| Severity | Response Time | Escalation Path |
|----------|---------------|-----------------|
| Critical | 15 minutes | IC → CTO → CEO |
| High | 30 minutes | IC → CTO |
| Medium | 1 hour | IC → Tech Lead |
| Low | 4 hours | Tech Lead |

---

## Appendix

### A. Backup Locations

| Backup Type | Location | Retention |
|-------------|----------|-----------|
| Daily Database | /backups/daily/ | 30 days |
| Weekly Database | /backups/weekly/ | 12 weeks |
| Monthly Database | /backups/monthly/ | 12 months |
| Transaction Logs | /backups/logs/ | 7 days |
| Configuration | /backups/config/ | 90 days |
| File Storage | /backups/files/ | 30 days |

### B. Recovery Time Objectives

| Component | RTO | RPO |
|-----------|-----|-----|
| Database | 2 hours | 1 hour |
| Application | 1 hour | 0 (stateless) |
| Configuration | 30 minutes | 24 hours |
| File Storage | 1 hour | 24 hours |

### C. Critical System Components

```
┌─────────────────────────────────────────────┐
│           WACAMS Architecture               │
├─────────────────────────────────────────────┤
│  Load Balancer (HAProxy)                    │
│    ├── App Server 1 (Primary)               │
│    ├── App Server 2 (Standby)               │
│    └── App Server 3 (Standby)               │
│                                             │
│  Database Cluster (PostgreSQL)              │
│    ├── Primary DB                           │
│    ├── Standby DB (Sync)                    │
│    └── Standby DB (Async)                   │
│                                             │
│  Storage                                    │
│    ├── Document Storage (MinIO)             │
│    └── Backup Storage (NFS)                 │
└─────────────────────────────────────────────┘
```

### D. Recovery Scripts Reference

| Script | Purpose | Location |
|--------|---------|----------|
| database-restore.sh | Restore database | /scripts/dr/ |
| application-restore.sh | Restore application | /scripts/dr/ |
| configuration-restore.sh | Restore configuration | /scripts/dr/ |
| dr-test-database.sh | Test database restore | /scripts/dr/tests/ |
| dr-test-failover.sh | Test application failover | /scripts/dr/tests/ |

---

**Document Control**
- **Owner:** SuperAdmin Team
- **Review Cycle:** Quarterly
- **Next Review:** 2027-02-16
- **Approved By:** Botlhale Rakau (803341)
- **Last Test:** 2026-11-16
- **Next Test:** 2026-12-16
