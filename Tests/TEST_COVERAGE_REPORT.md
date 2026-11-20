# WACAMS — Test Coverage Report Template

**Generated**: 2026-11-13  
**Project**: Workforce Attendance & Corrective Action Management System (WACAMS)  
**Version**: 1.0.0  
**Status**: TEMPLATE — To be populated after test execution

---

## Table of Contents

1. [Executive Summary](#executive-summary)
2. [Unit Test Coverage by Module](#unit-test-coverage-by-module)
3. [Integration Test Coverage by Workflow](#integration-test-coverage-by-workflow)
4. [Security Test Coverage by Category](#security-test-coverage-by-category)
5. [Load Test Results Summary](#load-test-results-summary)
6. [Test Execution Time Metrics](#test-execution-time-metrics)
7. [Untested Critical Paths](#untested-critical-paths)
8. [Coverage Trends](#coverage-trends)

---

## Executive Summary

| Metric | Target | Actual | Status |
|--------|--------|--------|--------|
| **Overall Line Coverage** | ≥ 80% | _TBD_ | ⏳ Pending |
| **Overall Branch Coverage** | ≥ 70% | _TBD_ | ⏳ Pending |
| **Overall Function Coverage** | ≥ 75% | _TBD_ | ⏳ Pending |
| **Critical Path Coverage** | 100% | _TBD_ | ⏳ Pending |
| **Total Tests Run** | — | _TBD_ | ⏳ Pending |
| **Tests Passed** | — | _TBD_ | ⏳ Pending |
| **Tests Failed** | 0 | _TBD_ | ⏳ Pending |
| **Tests Skipped** | — | _TBD_ | ⏳ Pending |

### Release Gate Assessment

| Gate | Criteria | Result |
|------|----------|--------|
| **Line Coverage ≥ 80%** | All modules meet minimum | ⏳ Pending |
| **Branch Coverage ≥ 70%** | All modules meet minimum | ⏳ Pending |
| **Zero Critical Bugs** | No P0 issues open | ⏳ Pending |
| **All Acceptance Criteria Tested** | AC-ATT, AC-ESC, AC-DIS, AC-SEC, AC-GIS, AC-DASH | ⏳ Pending |
| **Performance Thresholds Met** | API < 200ms, Dashboard < 500ms | ⏳ Pending |

---

## Unit Test Coverage by Module

### Backend (.NET / C#)

#### Core Domain Layer

| Module | Lines | Functions | Branches | Classes | Status |
|--------|-------|-----------|----------|---------|--------|
| `Core.Entities.Employee` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `Core.Entities.AttendanceRecord` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `Core.Entities.WarningLetter` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `Core.Entities.Escalation` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `Core.Entities.Department` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `Core.Entities.AttendanceException` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `Core.Enums.*` | _TBD_% | _TBD_% | _TBD_% | — | ✅ N/A |
| `Core.Constants.*` | _TBD_% | _TBD_% | _TBD_% | — | ✅ N/A |

#### Business Services

| Module | Lines | Functions | Branches | Classes | Status |
|--------|-------|-----------|----------|---------|--------|
| `Api.Services.AttendanceService` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `Api.Services.WarningService` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `Api.Services.EscalationService` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `Api.Services.ExceptionService` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `Api.Services.AuthService` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `Api.Services.EmployeeService` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `Api.Services.PermissionService` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `Api.Services.NotificationService` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `Api.Services.ReportService` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `Api.Services.AuditService` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |

#### Infrastructure Layer

| Module | Lines | Functions | Branches | Classes | Status |
|--------|-------|-----------|----------|---------|--------|
| `Infrastructure.Repositories.AttendanceRepository` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `Infrastructure.Repositories.WarningRepository` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `Infrastructure.Repositories.EmployeeRepository` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `Infrastructure.Services.AttendanceCalculationService` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `Infrastructure.Services.EscalationService` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `Infrastructure.Services.WarningGenerationService` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `Infrastructure.Services.NotificationService` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `Infrastructure.BackgroundServices.EscalationBackgroundService` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `Infrastructure.Persistence.Configuration.*` | _TBD_% | _TBD_% | _TBD_% | — | ✅ N/A |

#### GIS Integration

| Module | Lines | Functions | Branches | Classes | Status |
|--------|-------|-----------|----------|---------|--------|
| `src/backend/WACAMS.Api.Integration.GIS.GISIntegrationService` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `src/backend/WACAMS.Api.Integration.GIS.GISScheduledSyncService` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `src/backend/WACAMS.Api.Integration.GIS.Repositories.*` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |

#### Disciplinary Engine

| Module | Lines | Functions | Branches | Classes | Status |
|--------|-------|-----------|----------|---------|--------|
| `src/backend/WACAMS.Core.Disciplinary.Rules.ProgressiveDisciplineRule` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `src/backend/WACAMS.Core.Disciplinary.Services.CorrectiveActionEngine` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `src/backend/WACAMS.Core.Disciplinary.Services.AutoExpiryService` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `src/backend/WACAMS.Core.Disciplinary.Services.CorrectiveActionFormService` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `src/backend/WACAMS.Core.Escalations.Rules.EscalationRules` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |

#### Rules Engine

| Module | Lines | Functions | Branches | Classes | Status |
|--------|-------|-----------|----------|---------|--------|
| `src/backend/WACAMS.Core.RulesEngine.Services.AttendanceRulesEngine` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `src/backend/WACAMS.Core.RulesEngine.Validation.AttendanceValidator` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `src/backend/WACAMS.Core.RulesEngine.Rules.LateArrivalCalculationRule` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `src/backend/WACAMS.Core.RulesEngine.Rules.EarlyDepartureCalculationRule` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `src/backend/WACAMS.Core.RulesEngine.Rules.LostProductionCalculationRule` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `src/backend/WACAMS.Core.RulesEngine.Rules.UnplannedAbsenteeismCalculationRule` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `src/backend/WACAMS.Core.RulesEngine.Rules.CorrectiveActionDeterminationRule` | _TBD_% | _TBD_% | _TBD_% | _TBD_% | ⏳ |

### Frontend (TypeScript / React)

| Module | Lines | Functions | Components | Status |
|--------|-------|-----------|------------|--------|
| `src/core/api/types.ts` | _TBD_% | _TBD_% | — | ✅ N/A |
| `src/core/utils/` | _TBD_% | _TBD_% | — | ⏳ |
| `src/core/api/` | _TBD_% | _TBD_% | — | ⏳ |
| `src/features/auth/` | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `src/features/attendance/` | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `src/features/warnings/` | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `src/features/escalations/` | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `src/features/dashboard/` | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `src/features/hr-command-centre/` | _TBD_% | _TBD_% | _TBD_% | ⏳ |
| `src/test-utils/fixtures/` | _TBD_% | _TBD_% | — | ✅ N/A |

---

## Integration Test Coverage by Workflow

| Workflow | Test Count | Passed | Failed | Coverage | Status |
|----------|-----------|--------|--------|----------|--------|
| Login + RBAC enforcement | _TBD_ | _TBD_ | _TBD_ | _TBD_% | ⏳ |
| Clock-in / Clock-out flow | _TBD_ | _TBD_ | _TBD_ | _TBD_% | ⏳ |
| Late arrival detection → Exception → Warning | _TBD_ | _TBD_ | _TBD_ | _TBD_% | ⏳ |
| Progressive discipline chain (Counselling → Final) | _TBD_ | _TBD_ | _TBD_ | _TBD_% | ⏳ |
| Investigation workflow (Pending → Investigated → Warning) | _TBD_ | _TBD_ | _TBD_ | _TBD_% | ⏳ |
| Escalation auto-trigger on FinalWarning | _TBD_ | _TBD_ | _TBD_ | _TBD_% | ⏳ |
| 24-hour escalation timeout → Level 2 | _TBD_ | _TBD_ | _TBD_ | _TBD_% | ⏳ |
| Max level reached → Auto-resolve | _TBD_ | _TBD_ | _TBD_ | _TBD_% | ⏳ |
| GIS data import (success path) | _TBD_ | _TBD_ | _TBD_ | _TBD_% | ⏳ |
| GIS data import (error handling) | _TBD_ | _TBD_ | _TBD_ | _TBD_% | ⏳ |
| GIS duplicate detection | _TBD_ | _TBD_ | _TBD_ | _TBD_% | ⏳ |
| Department-scoped attendance queries | _TBD_ | _TBD_ | _TBD_ | _TBD_% | ⏳ |
| Dashboard real-time KPI updates | _TBD_ | _TBD_ | _TBD_ | _TBD_% | ⏳ |
| Notification delivery (Email/Teams/SMS) | _TBD_ | _TBD_ | _TBD_ | _TBD_% | ⏳ |
| PDF warning form generation | _TBD_ | _TBD_ | _TBD_ | _TBD_% | ⏳ |

---

## Security Test Coverage by Category

| Category | Test Count | Passed | Failed | Vulnerabilities Found | Status |
|----------|-----------|--------|--------|----------------------|--------|
| **Authentication** | _TBD_ | _TBD_ | _TBD_ | _TBD_ | ⏳ |
| └─ JWT token validation | _TBD_ | _TBD_ | _TBD_ | — | ⏳ |
| └─ Refresh token rotation | _TBD_ | _TBD_ | _TBD_ | — | ⏳ |
| └─ Session fixation | _TBD_ | _TBD_ | _TBD_ | — | ⏳ |
| **Authorization / RBAC** | _TBD_ | _TBD_ | _TBD_ | _TBD_ | ⏳ |
| └─ Role-based endpoint access | _TBD_ | _TBD_ | _TBD_ | — | ⏳ |
| └─ Department scope enforcement | _TBD_ | _TBD_ | _TBD_ | — | ⏳ |
| └─ IDOR prevention | _TBD_ | _TBD_ | _TBD_ | _TBD_ | ⏳ |
| **Input Validation** | _TBD_ | _TBD_ | _TBD_ | _TBD_ | ⏳ |
| └─ SQL injection attempts | _TBD_ | _TBD_ | _TBD_ | — | ⏳ |
| └─ XSS in free-text fields | _TBD_ | _TBD_ | _TBD_ | — | ⏳ |
| └─ Mass assignment protection | _TBD_ | _TBD_ | _TBD_ | — | ⏳ |
| **Data Protection** | _TBD_ | _TBD_ | _TBD_ | _TBD_ | ⏳ |
| └─ Password hashing verification | _TBD_ | _TBD_ | _TBD_ | — | ⏳ |
| └─ Sensitive data in logs | _TBD_ | _TBD_ | _TBD_ | — | ⏳ |
| └─ Encryption at rest | _TBD_ | _TBD_ | _TBD_ | — | ⏳ |
| **API Security** | _TBD_ | _TBD_ | _TBD_ | _TBD_ | ⏳ |
| └─ Rate limiting | _TBD_ | _TBD_ | _TBD_ | — | ⏳ |
| └─ CORS policy | _TBD_ | _TBD_ | _TBD_ | — | ⏳ |
| └─ Request size limits | _TBD_ | _TBD_ | _TBD_ | — | ⏳ |
| **Business Logic Security** | _TBD_ | _TBD_ | _TBD_ | _TBD_ | ⏳ |
| └─ Warning bypass attempts | _TBD_ | _TBD_ | _TBD_ | — | ⏳ |
| └─ Escalation manipulation | _TBD_ | _TBD_ | _TBD_ | — | ⏳ |
| └─ Data tampering in attendance | _TBD_ | _TBD_ | _TBD_ | — | ⏳ |

---

## Load Test Results Summary

| Scenario | Concurrent Users | Duration | Avg Response | p95 Response | p99 Response | Error Rate | Status |
|----------|-----------------|----------|-------------|-------------|-------------|------------|--------|
| Dashboard load (all KPIs) | 10 | 5 min | _TBD_ ms | _TBD_ ms | _TBD_ ms | _TBD_% | ⏳ |
| Dashboard load (all KPIs) | 50 | 5 min | _TBD_ ms | _TBD_ ms | _TBD_ ms | _TBD_% | ⏳ |
| Dashboard load (all KPIs) | 100 | 5 min | _TBD_ ms | _TBD_ ms | _TBD_ ms | _TBD_% | ⏳ |
| Attendance query (single employee) | 10 | 5 min | _TBD_ ms | _TBD_ ms | _TBD_ ms | _TBD_% | ⏳ |
| Attendance query (department-wide) | 10 | 5 min | _TBD_ ms | _TBD_ ms | _TBD_ ms | _TBD_% | ⏳ |
| GIS import (100 records) | 1 | 1 run | _TBD_ ms | _TBD_ ms | _TBD_ ms | _TBD_% | ⏳ |
| GIS import (500 records) | 1 | 1 run | _TBD_ ms | _TBD_ ms | _TBD_ ms | _TBD_% | ⏳ |
| Warning generation | 5 | 5 min | _TBD_ ms | _TBD_ ms | _TBD_ ms | _TBD_% | ⏳ |
| Notification dispatch (bulk) | 1 | 1 run | _TBD_ ms | _TBD_ ms | _TBD_ ms | _TBD_% | ⏳ |

### Performance Thresholds

| Metric | Target | Actual | Status |
|--------|--------|--------|--------|
| Dashboard load time | < 500ms | _TBD_ ms | ⏳ |
| API endpoint avg response | < 200ms | _TBD_ ms | ⏳ |
| p95 response time | < 500ms | _TBD_ ms | ⏳ |
| Database query time | < 100ms | _TBD_ ms | ⏳ |
| GIS import throughput | > 50 rec/sec | _TBD_ rec/sec | ⏳ |
| Memory usage (steady state) | < 512 MB | _TBD_ MB | ⏳ |
| CPU usage (steady state) | < 60% | _TBD_% | ⏳ |

---

## Test Execution Time Metrics

### Unit Tests

| Category | Test Count | Execution Time | Per-Test Avg | Status |
|----------|-----------|---------------|-------------|--------|
| Entity tests | _TBD_ | _TBD_ s | _TBD_ ms | ⏳ |
| Service tests | _TBD_ | _TBD_ s | _TBD_ ms | ⏳ |
| Repository tests | _TBD_ | _TBD_ s | _TBD_ ms | ⏳ |
| Rules engine tests | _TBD_ | _TBD_ s | _TBD_ ms | ⏳ |
| Disciplinary engine tests | _TBD_ | _TBD_ s | _TBD_ ms | ⏳ |
| Utility/helper tests | _TBD_ | _TBD_ s | _TBD_ ms | ⏳ |
| **Subtotal** | _TBD_ | _TBD_ s | _TBD_ ms | ⏳ |

### Integration Tests

| Category | Test Count | Execution Time | Per-Test Avg | Status |
|----------|-----------|---------------|-------------|--------|
| Auth workflows | _TBD_ | _TBD_ s | _TBD_ ms | ⏳ |
| Attendance workflows | _TBD_ | _TBD_ s | _TBD_ ms | ⏳ |
| Warning workflows | _TBD_ | _TBD_ s | _TBD_ ms | ⏳ |
| Escalation workflows | _TBD_ | _TBD_ s | _TBD_ ms | ⏳ |
| GIS integration | _TBD_ | _TBD_ s | _TBD_ ms | ⏳ |
| Dashboard workflows | _TBD_ | _TBD_ s | _TBD_ ms | ⏳ |
| **Subtotal** | _TBD_ | _TBD_ s | _TBD_ ms | ⏳ |

### Frontend Tests

| Category | Test Count | Execution Time | Per-Test Avg | Status |
|----------|-----------|---------------|-------------|--------|
| Component tests | _TBD_ | _TBD_ s | _TBD_ ms | ⏳ |
| Page tests | _TBD_ | _TBD_ s | _TBD_ ms | ⏳ |
| Hook/utility tests | _TBD_ | _TBD_ s | _TBD_ ms | ⏳ |
| Fixture validation | _TBD_ | _TBD_ s | _TBD_ ms | ⏳ |
| **Subtotal** | _TBD_ | _TBD_ s | _TBD_ ms | ⏳ |

### Total Execution Summary

| Phase | Target Time | Actual Time | Status |
|-------|------------|-------------|--------|
| Unit Tests | < 5 minutes | _TBD_ | ⏳ |
| Integration Tests | < 30 minutes | _TBD_ | ⏳ |
| Frontend Tests | < 10 minutes | _TBD_ | ⏳ |
| Load Tests | < 15 minutes | _TBD_ | ⏳ |
| **Grand Total** | < 60 minutes | _TBD_ | ⏳ |

---

## Untested Critical Paths

The following critical paths have **insufficient or zero test coverage** and must be addressed before release:

| # | Code Path | Risk Level | Current Coverage | Recommendation | Est. Effort |
|---|-----------|-----------|-----------------|----------------|-------------|
| 1 | `src/backend/WACAMS.Infrastructure.BackgroundServices.EscalationBackgroundService` | 🔴 HIGH | _TBD_% | Add integration tests for 24h timeout logic and auto-escalation chain | 4h |
| 2 | `Api.Services.EscalationService` auto-escalation trigger | 🔴 HIGH | _TBD_% | Add unit tests for manager hierarchy traversal and escalation creation | 3h |
| 3 | `src/backend/WACAMS.Core.Disciplinary.Services.AutoExpiryService` | 🟡 MEDIUM | _TBD_% | Add tests for warning expiry calculation and archival triggers | 2h |
| 4 | `Infrastructure.Repositories.AttendanceRepository` batch operations | 🟡 MEDIUM | _TBD_% | Add tests for bulk attendance insert and update scenarios | 2h |
| 5 | `src/backend/WACAMS.Api.Integration.GIS.GISScheduledSyncService` | 🟡 MEDIUM | _TBD_% | Add integration tests for scheduled sync and error recovery | 4h |
| 6 | `src/backend/WACAMS.Core.RulesEngine.Validation.AttendanceValidator` | 🟢 LOW | _TBD_% | Add edge case tests for boundary conditions | 1h |
| 7 | `Infrastructure.Services.NotificationChannels.*` (all 4 channels) | 🟢 LOW | _TBD_% | Add mock-based tests for each notification channel | 2h |

---

## Coverage Trends

### Historical Coverage (if available)

| Date | Lines % | Functions % | Branches % | Test Count |
|------|---------|-------------|------------|------------|
| _TBD_ | _TBD_% | _TBD_% | _TBD_% | _TBD_ |
| _TBD_ | _TBD_% | _TBD_% | _TBD_% | _TBD_ |
| _TBD_ | _TBD_% | _TBD_% | _TBD_% | _TBD_ |

### Coverage by File Type

| File Type | Files Analyzed | Lines Covered | Functions Covered | Branches Covered |
|-----------|---------------|--------------|-------------------|-----------------|
| `.cs` (backend) | _TBD_ | _TBD_% | _TBD_% | _TBD_% |
| `.ts` (frontend) | _TBD_ | _TBD_% | _TBD_% | _TBD_% |
| `.tsx` (components) | _TBD_ | _TBD_% | _TBD_% | _TBD_% |
| `_Test.cs` (test files) | _TBD_ | N/A | N/A | N/A |
| `*.test.ts` (test files) | _TBD_ | N/A | N/A | N/A |

---

## Notes

- This report is a **template** and will be populated after test execution.
- Coverage targets follow the test pyramid: **Unit > Widget > Integration**.
- All **P0 acceptance criteria** must have corresponding automated tests with ≥ 90% branch coverage.
- Security-related code (auth, authorization, input validation) requires **≥ 95% line coverage**.
- Use `dotnet test --collect:"XPlat Code Coverage"` for backend coverage.
- Use `vitest run --coverage` for frontend coverage.

---

*Last Updated: 2026-11-13*  
*Owner: Testing Agent (testing-agent)*  
*Next Review: After test execution completion*
