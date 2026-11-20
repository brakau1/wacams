# WACAMS Disaster Recovery Plan

## Overview

This document outlines the disaster recovery (DR) strategy for WACAMS deployed on Oracle Cloud Infrastructure (OCI).

## Recovery Objectives

### Recovery Time Objective (RTO)
- **Critical Services**: 30 minutes
- **Full System Recovery**: 2 hours
- **Data Recovery**: 1 hour

### Recovery Point Objective (RPO)
- **Database**: 1 hour (hourly backups)
- **Application Data**: 24 hours (daily backups)
- **Configuration**: Real-time (Git version control)

## Backup Strategy

### Database Backups

#### Automated Backups
```bash
# Daily full backup (2:00 AM)
0 2 * * * /opt/wacams/backup/backup.sh full

# Hourly incremental backup
0 * * * * /opt/wacams/backup/backup.sh incremental

# Weekly full backup (Sunday 3:00 AM)
0 3 * * 0 /opt/wacams/backup/backup.sh full --weekly

# Monthly full backup (1st of month 4:00 AM)
0 4 1 * * /opt/wacams/backup/backup.sh full --monthly
```

#### Backup Retention
- **Hourly**: Keep last 24 hours
- **Daily**: Keep last 7 days
- **Weekly**: Keep last 4 weeks
- **Monthly**: Keep last 12 months

#### Backup Storage
- **Primary**: Local disk (/backup/postgresql)
- **Secondary**: Oracle Cloud Object Storage
- **Tertiary**: Cross-region replication

### Application Backups

#### Configuration Files
- Stored in Git repository
- Automatic backup on every commit
- Environment-specific .env files (encrypted)

#### Uploaded Files
- Daily sync to Object Storage
- Cross-region replication enabled
- Versioning enabled for accidental deletion

### System Backups

#### Boot Volume Backups
- **Policy**: Bronze (weekly, 30 days retention)
- **Custom Policy**: Daily at 2:00 AM, 14 days retention

#### Block Volume Backups
- **Policy**: Gold (daily, 30 days retention)
- **Incremental**: Enabled after first full backup

## Disaster Scenarios

### Scenario 1: Database Corruption

**Detection:**
- Application errors related to database
- PostgreSQL logs showing corruption
- Failed queries

**Recovery Steps:**
1. Stop application services
   ```bash
   docker-compose stop backend
   ```

2. Assess corruption
   ```bash
   docker-compose exec postgres psql -U wacams -c "CHECKPOINT;"
   ```

3. Restore from latest backup
   ```bash
   /opt/wacams/backup/restore.sh latest
   ```

4. Apply WAL logs (if available)
   ```bash
   /opt/wacams/backup/restore-pitr.sh --target-time "2026-11-17 14:00:00"
   ```

5. Verify data integrity
   ```bash
   docker-compose exec postgres psql -U wacams -c "SELECT COUNT(*) FROM employees;"
   ```

6. Restart services
   ```bash
   docker-compose start backend
   ```

**RTO**: 30 minutes  
**RPO**: 1 hour

### Scenario 2: Complete Instance Failure

**Detection:**
- Instance unreachable
- OCI console showing instance terminated
- Health checks failing

**Recovery Steps:**
1. Launch new compute instance from boot volume backup
   ```bash
   oci compute instance launch \
     --availability-domain <AD> \
     --compartment-id <compartment-ocid> \
     --shape VM.Standard3.Flex \
     --image-id <image-ocid> \
     --boot-volume-id <boot-volume-backup-ocid>
   ```

2. Attach data volumes
   ```bash
   oci compute volume-attachment attach \
     --instance-id <new-instance-ocid> \
     --volume-id <data-volume-ocid> \
     --type paravirtualized
   ```

3. Mount volumes and verify data
   ```bash
   sudo mount /dev/sdb /data
   sudo ls -la /data/wacams
   ```

4. Update DNS records
   ```bash
   oci dns record instance update \
     --zone-name-or-id wacams.yourcompany.com \
     --items '[{"domain":"wacams.yourcompany.com","rtype":"A","rdata":"<new-ip>","ttl":60}]'
   ```

5. Start services
   ```bash
   cd /opt/wacams/oracle-cloud-deployment
   docker-compose up -d
   ```

6. Verify all services
   ```bash
   docker-compose ps
   curl -k https://wacams.yourcompany.com/health
   ```

**RTO**: 2 hours  
**RPO**: 24 hours

### Scenario 3: Regional Outage

**Detection:**
- OCI region status page showing outage
- Multiple services unavailable
- Network connectivity issues

**Recovery Steps:**
1. Activate DR region
   ```bash
   # Switch to DR region
   export OCI_REGION=eu-frankfurt-1
   ```

2. Restore database from cross-region backup
   ```bash
   oci os object get \
     --bucket-name wacams-backups \
     --name latest-backup.sql.gz \
     --file /tmp/latest-backup.sql.gz
   
   /opt/wacams/backup/restore.sh /tmp/latest-backup.sql.gz
   ```

3. Deploy application from Git
   ```bash
   git clone https://github.com/your-org/wacams.git
   cd wacams/oracle-cloud-deployment
   cp .env.dr .env
   docker-compose up -d
   ```

4. Update DNS to DR region
   ```bash
   oci dns record instance update \
     --zone-name-or-id wacams.yourcompany.com \
     --items '[{"domain":"wacams.yourcompany.com","rtype":"A","rdata":"<dr-lb-ip>","ttl":60}]'
   ```

5. Verify services
   ```bash
   curl -k https://wacams.yourcompany.com/health
   ```

**RTO**: 4 hours  
**RPO**: 24 hours

### Scenario 4: Data Deletion (Accidental/Malicious)

**Detection:**
- User reports missing data
- Audit logs showing mass deletion
- Sudden drop in record counts

**Recovery Steps:**
1. Stop application to prevent further damage
   ```bash
   docker-compose stop backend
   ```

2. Identify point in time before deletion
   ```bash
   # Check audit logs
   docker-compose exec postgres psql -U wacams -c "
     SELECT * FROM audit_logs 
     WHERE action = 'DELETE' 
     ORDER BY timestamp DESC 
     LIMIT 10;
   "
   ```

3. Restore to point in time
   ```bash
   /opt/wacams/backup/restore-pitr.sh --target-time "2026-11-17 13:00:00"
   ```

4. Verify data integrity
   ```bash
   docker-compose exec postgres psql -U wacams -c "
     SELECT COUNT(*) FROM employees;
     SELECT COUNT(*) FROM attendance_records;
   "
   ```

5. Restart services
   ```bash
   docker-compose start backend
   ```

6. Review security logs
   ```bash
   docker-compose logs --tail=1000 backend | grep -i "delete"
   ```

**RTO**: 1 hour  
**RPO**: 1 hour

### Scenario 5: Ransomware Attack

**Detection:**
- Files encrypted
- Ransom note discovered
- Unusual file modifications

**Recovery Steps:**
1. Isolate affected systems
   ```bash
   # Stop all services
   docker-compose down
   
   # Disconnect from network (if physical access)
   # Or terminate instance and create new one
   ```

2. Assess damage
   ```bash
   # Check for encrypted files
   find /data/wacams -name "*.encrypted" -o -name "*.locked"
   ```

3. Wipe and rebuild
   ```bash
   # Terminate compromised instance
   oci compute instance terminate --instance-id <compromised-instance-ocid>
   
   # Launch new instance
   oci compute instance launch ...
   ```

4. Restore from clean backup (before infection)
   ```bash
   # Find backup before infection date
   oci os object list --bucket-name wacams-backups | grep "2026-11-16"
   
   # Restore
   /opt/wacams/backup/restore.sh <clean-backup-file>
   ```

5. Rotate all credentials
   ```bash
   # Database password
   docker-compose exec postgres psql -U wacams -c "ALTER USER wacams WITH PASSWORD 'NEW_PASSWORD';"
   
   # Update .env file
   nano .env
   
   # Restart services
   docker-compose up -d
   ```

6. Review and enhance security
   - Enable additional firewall rules
   - Review access logs
   - Implement additional monitoring
   - Consider security audit

**RTO**: 4 hours  
**RPO**: 24 hours

## Cross-Region Replication

### Database Replication

#### Setup
```bash
# Primary region (af-johannesburg-1)
# Configure WAL archiving
ALTER SYSTEM SET wal_level = 'replica';
ALTER SYSTEM SET max_wal_senders = 5;
ALTER SYSTEM SET wal_keep_segments = 64;

# Create replication slot
SELECT pg_create_physical_replication_slot('dr_replica');
```

#### Replication Script
```bash
#!/bin/bash
# sync-to-dr.sh

# Sync latest backup to DR region
oci os object put \
  --bucket-name wacams-dr-backups \
  --file /backup/postgresql/latest.sql.gz \
  --region eu-frankfurt-1

# Sync WAL logs
rsync -avz /backup/postgresql/wal/ dr-server:/backup/wal/
```

### Object Storage Replication

#### Configuration
```bash
# Enable cross-region replication
oci os replication create \
  --bucket-name wacams-storage \
  --replication-policy-name "dr-replication" \
  --destination-bucket-name wacams-storage-dr \
  --destination-region eu-frankfurt-1
```

## Testing and Validation

### Quarterly DR Tests

#### Test Plan
1. **Tabletop Exercise** (1 hour)
   - Review DR plan
   - Walk through scenarios
   - Identify gaps

2. **Technical Test** (4 hours)
   - Restore database from backup
   - Deploy application to DR region
   - Verify all services
   - Test failover DNS

3. **Full Simulation** (8 hours)
   - Simulate complete outage
   - Execute full DR plan
   - Measure actual RTO/RPO
   - Document lessons learned

#### Test Checklist
- [ ] Backup restoration successful
- [ ] Application deployed and running
- [ ] Database integrity verified
- [ ] DNS failover working
- [ ] SSL certificates valid
- [ ] Email notifications working
- [ ] Monitoring alerts triggered
- [ ] All services accessible
- [ ] Performance acceptable
- [ ] Documentation updated

### Backup Verification

#### Daily Checks
```bash
# Verify backup exists
ls -lh /backup/postgresql/latest.sql.gz

# Verify backup integrity
pg_restore --list /backup/postgresql/latest.sql.gz > /dev/null && echo "OK" || echo "FAILED"

# Verify backup size (should be > 10MB)
BACKUP_SIZE=$(stat -f%z /backup/postgresql/latest.sql.gz)
if [ $BACKUP_SIZE -lt 10000000 ]; then
  echo "WARNING: Backup size suspiciously small"
fi
```

#### Monthly Test Restore
```bash
# Restore to test database
createdb -U wacams wacams_test
pg_restore -U wacams -d wacams_test /backup/postgresql/latest.sql.gz

# Verify data
psql -U wacams -d wacams_test -c "SELECT COUNT(*) FROM employees;"

# Cleanup
dropdb -U wacams wacams_test
```

## Communication Plan

### Internal Notification

#### Incident Response Team
- **Primary**: IT Manager (phone, email, SMS)
- **Secondary**: Senior Developer (phone, email)
- **Tertiary**: System Administrator (phone, email)

#### Notification Template
```
Subject: [URGENT] WACAMS System Outage - Incident #<number>

Time: <timestamp>
Status: <investigating/identified/resolving/resolved>
Impact: <description of impact>
Expected Resolution: <ETA>

Details:
<brief description of issue>

Next Update: <time of next update>

Contact: <incident commander phone>
```

### External Communication

#### User Notification
```
Subject: WACAMS System Maintenance

Dear Users,

We are currently experiencing technical difficulties with the WACAMS system. 
Our team is working to resolve the issue as quickly as possible.

Expected Resolution: <ETA>

We apologize for any inconvenience and will provide updates as they become available.

Thank you for your patience.

IT Support Team
```

## Post-Incident Review

### Documentation Requirements

After every DR event, document:
1. **Timeline**
   - Detection time
   - Response time
   - Recovery time
   - Resolution time

2. **Root Cause Analysis**
   - What happened
   - Why it happened
   - Contributing factors

3. **Impact Assessment**
   - Duration of outage
   - Data loss (if any)
   - Users affected
   - Business impact

4. **Lessons Learned**
   - What worked well
   - What didn't work
   - Improvements needed

5. **Action Items**
   - Immediate fixes
   - Long-term improvements
   - Process changes
   - Training needs

### Continuous Improvement

#### Quarterly Review
- Review all incidents from quarter
- Update DR plan based on lessons learned
- Test new recovery procedures
- Update contact lists
- Review and update backups

#### Annual Review
- Full DR plan review
- Technology stack review
- Compliance audit
- Cost optimization review
- Security assessment

## Cost Considerations

### DR Infrastructure Costs

#### Minimal Setup (Shared Infrastructure)
- Compute: $0 (use existing)
- Storage: $25/month (Object Storage)
- Data Transfer: $10/month
- **Total**: $35/month

#### Active-Passive Setup
- Compute: $165/month (standby instance)
- Storage: $50/month (replicated volumes)
- Database: $100/month (read replica)
- Data Transfer: $50/month
- **Total**: $365/month

#### Active-Active Setup
- Compute: $330/month (2 instances)
- Storage: $100/month (replicated volumes)
- Database: $300/month (multi-region)
- Load Balancer: $30/month
- Data Transfer: $100/month
- **Total**: $860/month

### Cost Optimization
- Use Object Storage for backups (cheaper than Block Volume)
- Enable lifecycle policies to move old backups to Archive tier
- Use cross-region replication only for critical data
- Consider Oracle Cloud Free Tier for DR region

## Compliance and Audit

### Regulatory Requirements
- **POPIA**: Data protection and breach notification
- **ISO 27001**: Information security management
- **COBIT**: IT governance and management

### Audit Trail
- All backup operations logged
- Restore operations logged
- DR tests documented
- Incident reports archived

### Retention Policy
- Incident reports: 7 years
- Backup logs: 3 years
- DR test results: 5 years
- Audit trails: 7 years

## Appendix

### Contact List

| Role | Name | Phone | Email |
|------|------|-------|-------|
| IT Manager | John Doe | +27 82 123 4567 | john.doe@company.com |
| Senior Developer | Jane Smith | +27 82 234 5678 | jane.smith@company.com |
| System Admin | Bob Johnson | +27 82 345 6789 | bob.johnson@company.com |
| Oracle Support | - | +1 800 Oracle Help | support@oracle.com |

### Oracle Cloud Resources

| Resource | OCID | Region |
|----------|------|--------|
| Compartment | ocid1.compartment.oc1..aaaaaaa... | af-johannesburg-1 |
| VCN | ocid1.vcn.oc1..aaaaaaa... | af-johannesburg-1 |
| Compute Instance | ocid1.instance.oc1..aaaaaaa... | af-johannesburg-1 |
| Boot Volume | ocid1.bootvolume.oc1..aaaaaaa... | af-johannesburg-1 |
| Block Volume | ocid1.volume.oc1..aaaaaaa... | af-johannesburg-1 |
| Object Storage Bucket | ocid1.bucket.oc1..aaaaaaa... | af-johannesburg-1 |
| Load Balancer | ocid1.loadbalancer.oc1..aaaaaaa... | af-johannesburg-1 |

### Recovery Scripts Location

```
/opt/wacams/scripts/
├── backup.sh
├── restore.sh
├── restore-pitr.sh
├── failover.sh
├── failback.sh
├── verify-backup.sh
└── test-dr.sh
```

---

**Document Version**: 1.0  
**Last Updated**: 2026-11-17  
**Next Review**: 2027-02-17  
**Approved By**: IT Manager
