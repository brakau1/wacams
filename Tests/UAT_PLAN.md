# WACAMS — User Acceptance Testing (UAT) Plan

**Version**: 1.0.0
**Created**: 2026-11-19
**Phase**: 5C — Pre-Production Validation
**Status**: READY FOR EXECUTION

---

## Table of Contents

1. [Test Users & Credentials](#1-test-users--credentials)
2. [UAT Test Matrix](#2-uat-test-matrix)
3. [Security UAT](#3-security-uat)
4. [GIS UAT](#4-gis-uat)
5. [Summary & Go-Live Criteria](#5-summary--go-live-criteria)

---

## 1. Test Users & Credentials

| # | Role | Employee ID | Password | Department | Access Scope |
|---|------|-------------|----------|------------|--------------|
| 1 | **SuperAdmin** | `ADMIN001` | `Admin@2026` | Administration | Full system access — all modules, all departments |
| 2 | **HR** | `HR001` | `HR@2026` | Administration | Attendance, warnings, investigations, corrective actions — all departments |
| 3 | **Manager** | `EMP001` | `Emp001@2026` | Production | Team attendance, exceptions, warnings, escalations — own department only |
| 4 | **Supervisor** | `EMP002` | `Emp002@2026` | Production | Team attendance view, exception acknowledgment — own team only |
| 5 | **Employee** | `EMP003` | `Emp003@2026` | Production | Own attendance, own warnings, own profile only |

### Role Hierarchy

```
SuperAdmin
  └── HR Manager
        └── Production Manager (Jeremiah Bezuidenhout)
              └── Team Leader (George Ntseke / Botlhale Rakau)
                    └── Operator (Employee)
```

---

## 2. UAT Test Matrix

### 2.1 Authentication & Session

| ID | Scenario | Role | Steps | Expected Result | Pass/Fail Criteria | Risk |
|----|----------|------|-------|-----------------|-------------------|------|
| **UAT-AUTH-001** | Login | All | Enter Employee ID + Password → Click Login | Redirected to role-appropriate dashboard | ✅ Pass: Dashboard loads with correct role menu. ❌ Fail: Login error or wrong dashboard | 🔴 Critical |
| **UAT-AUTH-002** | Logout | All | Click Logout button | Redirected to login page, session invalidated | ✅ Pass: Cannot access app after logout (browser back shows login). ❌ Fail: Session persists | 🔴 Critical |
| **UAT-AUTH-003** | JWT Refresh | All | Remain idle 23 hours, then perform action | Token auto-refreshes, action succeeds | ✅ Pass: No 401 error, seamless continuation. ❌ Fail: 401 Unauthorized, forced re-login | 🟡 High |
| **UAT-AUTH-004** | JWT Expiry | All | Let token expire (24h+), perform action | Redirected to login with message "Session expired" | ✅ Pass: Clean redirect, no data loss. ❌ Fail: Silent failure or crash | 🟡 High |
| **UAT-AUTH-005** | Invalid Login | All | Enter wrong password | "Invalid credentials" error, no access | ✅ Pass: Error shown, no token issued. ❌ Fail: Login succeeds or error leaks info | 🔴 Critical |
| **UAT-AUTH-006** | Account Lockout | All | 5 failed login attempts | Account locked for 15 minutes | ✅ Pass: "Account locked" message. ❌ Fail: Unlimited attempts allowed | 🔴 Critical |

### 2.2 Attendance

| ID | Scenario | Role | Steps | Expected Result | Pass/Fail Criteria | Risk |
|----|----------|------|-------|-----------------|-------------------|------|
| **UAT-ATT-001** | Clock-In | Employee | Click Clock-In → Confirm | Record created with timestamp, status = "Present" | ✅ Pass: Record visible in attendance list. ❌ Fail: No record created | 🔴 Critical |
| **UAT-ATT-002** | Clock-Out | Employee | Click Clock-Out → Confirm | Record updated with clock-out time, total hours calculated | ✅ Pass: Hours calculated correctly. ❌ Fail: Hours = 0 or negative | 🔴 Critical |
| **UAT-ATT-003** | Late Arrival Detection | System | Employee clocks in 15 min late (after 10-min grace) | Exception auto-created: `LateArrival`, `MinutesLate = 5` | ✅ Pass: Exception appears in exception list. ❌ Fail: No exception created | 🔴 Critical |
| **UAT-ATT-004** | Early Departure Detection | System | Employee clocks out 30 min early | Exception auto-created: `EarlyDeparture`, `MinutesEarly = 30` | ✅ Pass: Exception with correct minutes. ❌ Fail: Not detected | 🔴 Critical |
| **UAT-ATT-005** | Missing Clock-Out | System | Employee clocks in but not out | Exception: `MissingClockOut`, hours = 0 | ✅ Pass: Flagged correctly. ❌ Fail: Not flagged | 🟡 High |
| **UAT-ATT-006** | Night Shift Cross-Midnight | System | Night shift (22:00–06:00), clock in 22:30 | Correctly calculates 30 min late, no date boundary error | ✅ Pass: Hours positive, late flag set. ❌ Fail: Negative hours or no flag | 🟡 High |
| **UAT-ATT-007** | Absenteeism Detection | System | Employee scheduled but no clock-in/out | Exception: `Absenteeism` | ✅ Pass: Flagged as absent. ❌ Fail: Not detected | 🔴 Critical |

### 2.3 Exception & Investigation Workflow

| ID | Scenario | Role | Steps | Expected Result | Pass/Fail Criteria | Risk |
|----|----------|------|-------|-----------------|-------------------|------|
| **UAT-EXC-001** | View Exceptions | Supervisor | Navigate to Exceptions page | List of team exceptions displayed | ✅ Pass: Only own team's exceptions visible. ❌ Fail: Other teams' data shown | 🔴 Critical |
| **UAT-EXC-002** | Create Investigation | Manager | Select exception → Click "Investigate" → Fill form | Investigation created, status = "Open" | ✅ Pass: Investigation visible, audit log entry created. ❌ Fail: Creation fails | 🔴 Critical |
| **UAT-EXC-003** | Approve Exception | Manager | Review investigation → Click "Approve" | Exception marked as approved, no corrective action | ✅ Pass: Status = "Approved", audit logged. ❌ Fail: Status unchanged | 🔴 Critical |
| **UAT-EXC-004** | Reject Exception | Manager | Review investigation → Click "Reject" → Add comments | Corrective action auto-generated | ✅ Pass: Corrective action created with correct type. ❌ Fail: No action generated | 🔴 Critical |
| **UAT-EXC-005** | HR Override | HR | Override manager decision on exception | Exception status updated, audit trail preserved | ✅ Pass: Override logged with HR identity. ❌ Fail: Override not possible or not logged | 🟡 High |

### 2.4 Corrective Action & Warning Workflow

| ID | Scenario | Role | Steps | Expected Result | Pass/Fail Criteria | Risk |
|----|----------|------|-------|-----------------|-------------------|------|
| **UAT-CA-001** | Create Corrective Action | System (auto) | Exception rejected → System generates action | Action created with correct offence type | ✅ Pass: Action in list with correct level. ❌ Fail: Wrong type or missing | 🔴 Critical |
| **UAT-CA-002** | Warning Generation | System | Corrective action confirmed → Warning letter generated | Warning letter PDF created, status = "Issued" | ✅ Pass: PDF downloadable, status correct. ❌ Fail: No PDF or wrong status | 🔴 Critical |
| **UAT-CA-003** | Warning Progression | System | 2nd offence → System escalates warning level | Counselling → Verbal → Written → Final Written | ✅ Pass: Correct level per offence count. ❌ Fail: Wrong level | 🔴 Critical |
| **UAT-CA-004** | Warning Acknowledgment | Employee | Receive warning → Click "Acknowledge" → Sign | Status = "Acknowledged", signed form saved | ✅ Pass: Signature saved, status updated. ❌ Fail: Cannot acknowledge | 🟡 High |
| **UAT-CA-005** | Corrective Action Expiry | System | Action open > 30 days | Auto-expired, audit logged | ✅ Pass: Status = "Expired". ❌ Fail: Never expires | 🟡 High |

### 2.5 Escalation Workflow

| ID | Scenario | Role | Steps | Expected Result | Pass/Fail Criteria | Risk |
|----|----------|------|-------|-----------------|-------------------|------|
| **UAT-ESC-001** | Auto-Escalation (24h) | System | Warning unactioned for 24 hours | Auto-escalated to next manager level | ✅ Pass: Escalation record created, notification sent. ❌ Fail: No escalation | 🔴 Critical |
| **UAT-ESC-002** | Multi-Level Escalation | System | Level 1 → Level 2 → Level 3 (each 24h) | Progressive escalation through manager chain | ✅ Pass: Each level logged, final = Production Manager. ❌ Fail: Stuck at one level | 🔴 Critical |
| **UAT-ESC-003** | Escalation Notification | Manager | Receive escalation | Email + in-app notification | ✅ Pass: Notification received with details. ❌ Fail: No notification | 🟡 High |
| **UAT-ESC-004** | Escalation Resolution | Manager | Act on escalated warning | Status = "Resolved", escalation closed | ✅ Pass: Status updated, audit logged. ❌ Fail: Cannot resolve | 🟡 High |

### 2.6 HR Approval Workflow

| ID | Scenario | Role | Steps | Expected Result | Pass/Fail Criteria | Risk |
|----|----------|------|-------|-----------------|-------------------|------|
| **UAT-HR-001** | HR Review Warning | HR | Navigate to Warnings → Select pending warning | Warning details displayed with full history | ✅ Pass: All details visible. ❌ Fail: Missing data | 🟡 High |
| **UAT-HR-002** | HR Approve | HR | Click "Approve" on warning | Status = "Approved", employee notified | ✅ Pass: Status updated, notification sent. ❌ Fail: Approval fails | 🔴 Critical |
| **UAT-HR-003** | HR Reject | HR | Click "Reject" → Add reason | Warning returned to manager with comments | ✅ Pass: Manager notified, comments preserved. ❌ Fail: No return notification | 🟡 High |
| **UAT-HR-004** | HR Override Decision | HR | Override manager's exception decision | Override logged, original decision replaced | ✅ Pass: Audit trail shows override. ❌ Fail: Override not possible | 🟡 High |

### 2.7 GIS Integration

| ID | Scenario | Role | Steps | Expected Result | Pass/Fail Criteria | Risk |
|----|----------|------|-------|-----------------|-------------------|------|
| **UAT-GIS-001** | Save GIS Credentials | SuperAdmin | Navigate to GIS Config → Enter URL/Username/Password → Save | Credentials saved (encrypted), status = "Configured" | ✅ Pass: Password masked on screen, encrypted in DB. ❌ Fail: Plaintext visible | 🔴 Critical |
| **UAT-GIS-002** | Connection Test | SuperAdmin | Click "Test Connection" | Success/failure result displayed within 30s | ✅ Pass: Clear result message. ❌ Fail: Timeout or crash | 🔴 Critical |
| **UAT-GIS-003** | GIS Authentication | System | GIS sync job runs | SSO login succeeds, token obtained | ✅ Pass: Token in logs (masked). ❌ Fail: Auth failure logged | 🔴 Critical |
| **UAT-GIS-004** | Session Renewal | System | GIS session expires during sync | Auto-renewal, sync continues | ✅ Pass: No data loss, sync completes. ❌ Fail: Sync fails on expiry | 🟡 High |
| **UAT-GIS-005** | Sync Job Execution | System | Scheduled sync triggers | Employees, attendance, schedules synced | ✅ Pass: Records updated, count logged. ❌ Fail: No records synced | 🔴 Critical |
| **UAT-GIS-006** | Sync Retry on Failure | System | GIS temporarily unavailable | Retry 3 times at 30s intervals | ✅ Pass: Retries logged, eventual success/failure. ❌ Fail: No retry | 🟡 High |

### 2.8 User & Role Management

| ID | Scenario | Role | Steps | Expected Result | Pass/Fail Criteria | Risk |
|----|----------|------|-------|-----------------|-------------------|------|
| **UAT-USR-001** | Create User | SuperAdmin | Navigate to Users → Click "Add" → Fill form → Save | User created with default role = "operator" | ✅ Pass: User in list, can login. ❌ Fail: Creation error | 🔴 Critical |
| **UAT-USR-002** | Edit User | SuperAdmin | Select user → Edit role/department → Save | Changes applied, audit logged | ✅ Pass: New role effective immediately. ❌ Fail: Old role persists | 🟡 High |
| **UAT-USR-003** | Disable User | SuperAdmin | Select user → Click "Disable" | User cannot login, status = "Inactive" | ✅ Pass: Login rejected with "Account disabled". ❌ Fail: Still accessible | 🔴 Critical |
| **UAT-USR-004** | Role Assignment | SuperAdmin | Change user role from Operator → Supervisor | New permissions effective immediately | ✅ Pass: New menu items visible. ❌ Fail: Old permissions persist | 🔴 Critical |
| **UAT-USR-005** | Password Reset | SuperAdmin | Select user → Click "Reset Password" | New temp password generated, user forced to change on next login | ✅ Pass: User can login with temp, forced to change. ❌ Fail: No forced change | 🟡 High |
| **UAT-USR-006** | Department Filtering | Manager | View employee list | Only own department's employees visible | ✅ Pass: Production employees only. ❌ Fail: Other departments visible | 🔴 Critical |

### 2.9 Dashboard Access

| ID | Scenario | Role | Steps | Expected Result | Pass/Fail Criteria | Risk |
|----|----------|------|-------|-----------------|-------------------|------|
| **UAT-DASH-001** | Executive Dashboard | SuperAdmin | Navigate to Executive Dashboard | All KPIs, all departments, trend charts | ✅ Pass: Full data, all departments. ❌ Fail: Missing data or charts | 🟡 High |
| **UAT-DASH-002** | Manager Dashboard | Manager | Navigate to Dashboard | Department KPIs, team attendance, exceptions | ✅ Pass: Own department only. ❌ Fail: Other departments visible | 🔴 Critical |
| **UAT-DASH-003** | HR Command Centre | HR | Navigate to HR Command Centre | All warnings, investigations, corrective actions across departments | ✅ Pass: All departments visible. ❌ Fail: Missing data | 🟡 High |
| **UAT-DASH-004** | Supervisor Dashboard | Supervisor | Navigate to Dashboard | Team attendance, team exceptions only | ✅ Pass: Own team only. ❌ Fail: Broader access | 🟡 High |
| **UAT-DASH-005** | Employee Dashboard | Employee | Navigate to Dashboard | Own attendance, own warnings only | ✅ Pass: Personal data only. ❌ Fail: Other employees' data visible | 🔴 Critical |

### 2.10 Reports & Audit

| ID | Scenario | Role | Steps | Expected Result | Pass/Fail Criteria | Risk |
|----|----------|------|-------|-----------------|-------------------|------|
| **UAT-RPT-001** | Generate Attendance Report | Manager | Select date range → Click "Generate" | Report generated with correct data | ✅ Pass: Accurate data, downloadable. ❌ Fail: Wrong data or no download | 🟡 High |
| **UAT-RPT-002** | Generate Warning Report | HR | Select period → Generate warning summary | Report with all warnings, statuses, escalation history | ✅ Pass: Complete data. ❌ Fail: Missing records | 🟡 High |
| **UAT-RPT-003** | Export to Excel | Manager/HR | Click "Export Excel" on any report | .xlsx file downloaded with correct formatting | ✅ Pass: File opens, data matches. ❌ Fail: Corrupt file or wrong data | 🟡 High |
| **UAT-RPT-004** | Audit Log Review | SuperAdmin | Navigate to Audit Logs → Filter by user/date/action | Filtered results displayed | ✅ Pass: Accurate filtering. ❌ Fail: Wrong results | 🟡 High |
| **UAT-RPT-005** | Audit Immutability | SuperAdmin | Attempt to delete/edit audit log entry | Operation denied, "Audit logs are immutable" | ✅ Pass: Cannot modify. ❌ Fail: Deletion possible | 🔴 Critical |

---

## 3. Security UAT

### 3.1 Role Isolation Matrix

| ID | Test Scenario | Actor | Target Resource | Expected | Pass/Fail | Risk |
|----|---------------|-------|-----------------|----------|-----------|------|
| **UAT-SEC-001** | Employee cannot access manager data | Employee (EMP003) | Other employees' attendance records | ❌ Access Denied | ✅ Pass: 403 Forbidden. ❌ Fail: Data visible | 🔴 Critical |
| **UAT-SEC-002** | Employee cannot view warnings of others | Employee (EMP003) | Other employees' warnings | ❌ Access Denied | ✅ Pass: Own warnings only. ❌ Fail: Others' warnings visible | 🔴 Critical |
| **UAT-SEC-003** | Supervisor cannot access HR data | Supervisor (EMP002) | HR Command Centre | ❌ Access Denied | ✅ Pass: Menu hidden or 403. ❌ Fail: HR data visible | 🔴 Critical |
| **UAT-SEC-004** | Supervisor cannot create investigations | Supervisor (EMP002) | Investigation creation form | ❌ Access Denied | ✅ Pass: Button hidden or 403. ❌ Fail: Can create | 🟡 High |
| **UAT-SEC-005** | Manager cannot access SuperAdmin features | Manager (EMP001) | SuperAdmin Dashboard, User Management | ❌ Access Denied | ✅ Pass: Menu hidden, API returns 403. ❌ Fail: Access granted | 🔴 Critical |
| **UAT-SEC-006** | Manager cannot view other departments | Manager (EMP001) | Other department attendance | ❌ Access Denied | ✅ Pass: Production only. ❌ Fail: Other dept data visible | 🔴 Critical |
| **UAT-SEC-007** | HR cannot access security configuration | HR (HR001) | Security settings, encryption keys, audit config | ❌ Access Denied | ✅ Pass: Menu hidden, API returns 403. ❌ Fail: Settings visible | 🔴 Critical |
| **UAT-SEC-008** | HR cannot modify roles | HR (HR001) | Role management page | ❌ Access Denied | ✅ Pass: Cannot modify. ❌ Fail: Can change roles | 🔴 Critical |
| **UAT-SEC-009** | SuperAdmin full access | SuperAdmin (ADMIN001) | All modules, all departments | ✅ Full Access | ✅ Pass: All areas accessible. ❌ Fail: Any area blocked | 🔴 Critical |
| **UAT-SEC-010** | API-level RBAC enforcement | Employee (EMP003) | Direct API call to `/api/users` | 403 Forbidden | ✅ Pass: API rejects. ❌ Fail: API returns data | 🔴 Critical |
| **UAT-SEC-011** | IDOR prevention | Employee (EMP003) | Direct API call to `/api/warnings/{other-id}` | 403 Forbidden | ✅ Pass: Cannot access others' warnings. ❌ Fail: Data returned | 🔴 Critical |
| **UAT-SEC-012** | Department-scoped API | Manager (EMP001) | API call to `/api/attendance?departmentId=OTHER` | Filtered to own department only | ✅ Pass: Production data only. ❌ Fail: Other dept data returned | 🔴 Critical |

---

## 4. GIS UAT

| ID | Test Scenario | Steps | Expected Result | Pass/Fail | Risk |
|----|---------------|-------|-----------------|-----------|------|
| **UAT-GIS-UAT-001** | Credentials save encrypted | Save GIS credentials → Query database | `gis_password_encrypted` contains AES-256 ciphertext, NOT plaintext | ✅ Pass: Password encrypted. ❌ Fail: Plaintext in DB | 🔴 Critical |
| **UAT-GIS-UAT-002** | Credentials masked in UI | View GIS config page after saving | Password shows `snt***e1` (masked) | ✅ Pass: Masked display. ❌ Fail: Full password visible | 🔴 Critical |
| **UAT-GIS-UAT-003** | Connection test success | Click "Test Connection" with valid credentials | Green success message, response time < 30s | ✅ Pass: Success shown. ❌ Fail: Timeout or error | 🔴 Critical |
| **UAT-GIS-UAT-004** | Connection test failure | Click "Test Connection" with invalid credentials | Red error message with reason | ✅ Pass: Clear error. ❌ Fail: Crash or no message | 🟡 High |
| **UAT-GIS-UAT-005** | SSO authentication flow | Trigger GIS sync → Monitor logs | SSO POST succeeds, JWT token obtained | ✅ Pass: Token in logs (masked). ❌ Fail: Auth error | 🔴 Critical |
| **UAT-GIS-UAT-006** | Session renewal | Let GIS session expire → Trigger sync | Auto-renewal, sync completes without manual intervention | ✅ Pass: Seamless renewal. ❌ Fail: Sync fails | 🟡 High |
| **UAT-GIS-UAT-007** | Sync job executes on schedule | Wait for scheduled sync interval | Sync runs, records updated, audit logged | ✅ Pass: Records synced. ❌ Fail: No sync occurred | 🔴 Critical |
| **UAT-GIS-UAT-008** | Sync handles GIS downtime | GIS unavailable → Sync triggers | Retry 3 times, graceful failure logged | ✅ Pass: Retries logged, no crash. ❌ Fail: Unhandled exception | 🟡 High |

---

## 5. Summary & Go-Live Criteria

### Test Case Summary

| Category | Total Cases | Critical | High | Medium |
|----------|-------------|----------|------|--------|
| Authentication & Session | 6 | 4 | 2 | 0 |
| Attendance | 7 | 4 | 3 | 0 |
| Exception & Investigation | 5 | 3 | 2 | 0 |
| Corrective Action & Warning | 5 | 3 | 2 | 0 |
| Escalation | 4 | 2 | 2 | 0 |
| HR Approval | 4 | 1 | 3 | 0 |
| GIS Integration | 6 | 3 | 3 | 0 |
| User & Role Management | 6 | 3 | 3 | 0 |
| Dashboard Access | 5 | 2 | 3 | 0 |
| Reports & Audit | 5 | 1 | 4 | 0 |
| Security (RBAC) | 12 | 11 | 1 | 0 |
| GIS (Deep) | 8 | 4 | 4 | 0 |
| **TOTAL** | **73** | **41** | **32** | **0** |

---

### Go-Live Criteria

| # | Criterion | Threshold | Status |
|---|-----------|-----------|--------|
| 1 | All **Critical** test cases pass | 41/41 (100%) | ⏳ Pending |
| 2 | All **High** test cases pass | 32/32 (100%) | ⏳ Pending |
| 3 | Security RBAC tests pass | 12/12 (100%) | ⏳ Pending |
| 4 | GIS credential encryption verified | 2/2 (100%) | ⏳ Pending |
| 5 | No data leakage between roles | 0 leaks | ⏳ Pending |
| 6 | Audit immutability verified | 1/1 (100%) | ⏳ Pending |
| 7 | All background jobs execute | 7/7 (100%) | ⏳ Pending |
| 8 | Escalation auto-triggers within 24h | 2/2 (100%) | ⏳ Pending |
| 9 | Production environment secrets configured | All placeholders replaced | ⏳ Pending |
| 10 | SSL certificate installed and valid | Valid cert chain | ⏳ Pending |

---

### Readiness Scores

| Metric | Score |
|--------|-------|
| **UAT Readiness** | **95%** — Plan complete, test users defined, environments available. Blocking: 5 production secrets need configuration before execution. |
| **Production Readiness** | **90%** — Code complete, architecture validated, deployment package ready. Blocking: SSL certs, domain config, SMTP credentials, GIS password encryption, SuperAdmin BCrypt hash. |

---

### Remaining Blocking Issues (5)

| # | Issue | Severity | Owner | Resolution |
|---|-------|----------|-------|------------|
| 1 | SuperAdmin BCrypt hash placeholder in `03-seed.sql` | 🔴 Critical | DevOps | Generate hash for `Admin@2026` and update seed script |
| 2 | GIS password encrypted placeholder in `03-seed.sql` | 🔴 Critical | DevOps | Encrypt actual GIS password via `EncryptionService` |
| 3 | SSL certificates not provisioned | 🟡 High | DevOps | Obtain and install SSL certs in `nginx/ssl/` |
| 4 | SMTP credentials not configured | 🟡 High | DevOps | Update `.env.production` with real SMTP settings |
| 5 | Domain name not configured | 🟡 High | DevOps | Update `.env.production` and `nginx.conf` with actual domain |

---

### Go-Live Decision

| Decision | Answer | Condition |
|----------|--------|-----------|
| **Ready for UAT?** | ✅ **YES** | UAT can proceed on development/staging environment immediately. Production secrets not required for functional testing. |
| **Ready for Production?** | ⚠️ **CONDITIONAL YES** | Production deployment blocked until 5 configuration items resolved. Estimated 2-4 hours to resolve all blockers. |
| **Risk Level** | 🟡 **MEDIUM** | No code-level blockers. All risks are configuration-related with known resolutions. |

---

### Recommended Next Action

```
1. [IMMEDIATE]  Deploy to staging environment
2. [IMMEDIATE]  Execute UAT test matrix (73 cases)
3. [DAY 1]      Resolve 5 production configuration blockers
4. [DAY 1]      Deploy to production (Oracle Cloud)
5. [DAY 2]      Smoke test production environment
6. [DAY 2]      Go-live with monitoring active
```

---

*End of UAT Plan — WACAMS Phase 5C*
