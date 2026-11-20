# WACAMS Dashboard — Full-Stack Integration Gap Analysis

**Date**: 2026-11-17 ~15:00  
**Scope**: All 4 dashboard modules + backend KPI architecture  
**Method**: End-to-end trace from frontend page → React Query / raw fetch → API Controller → Service → Database

---

## TL;DR

| Module | Integration % | Status |
|--------|--------------|--------|
| **Executive Dashboard** | **85%** | ⚠️ Frontend connected to real API, but KPI Services not registered in DI |
| **Manager Dashboard** | **0%** | 🚫 Mock data only — no backend controller exists |
| **General Dashboard** | **0%** | 🚫 Mock data only — no backend controller exists |
| **HR Command Centre** | **0%** | 🚫 Mock data only — no backend controller exists |
| **Overall Completion** | **~21%** | Critical gaps block all non-executive dashboards |

---

## 1. Backend Component Audit

### Controllers Present
| Controller | Route Prefix | Status |
|------------|-------------|--------|
| `ExecutiveDashboardController` | `/api/executive-dashboard` | ✅ EXISTS — 3 endpoints: summary, heatmap, leaderboard |
| `ReportsController` | `/api/reports` | ⚠️ EXISTS — but missing `dashboard-kpi`, `manager-dashboard`, `hr-command-centre` |
| ~~`DashboardController`~~ | N/A | ❌ DOES NOT EXIST |

### KPI Services — CRITICAL GAP
| Service | Registered in DI? | Used by Controller? |
|---------|------------------|---------------------|
| `AttendanceKpiService` | ❌ **NOT REGISTERED** | ❌ Not used |
| `DepartmentKpiService` | ❌ **NOT REGISTERED** | ❌ Not used |
| `ExecutiveKpiService` | ❌ **NOT REGISTERED** | ❌ Not used |
| `ExecutiveAnalyticsService` | ❌ **NOT REGISTERED** | ❌ Not used |
| `DashboardAggregationService` | ❌ **NOT REGISTERED** | ❌ Not used |

**Root Cause**: `ApiServiceCollectionExtensions.cs` registers only basic services (`AuthService`, `EmployeeService`, `AttendanceService`, etc.). None of the KPI/Dashboard aggregation services are wired into DI.

### Services Present But Unused
| Service | Role | Connected? |
|---------|------|-----------|
| `DashboardService.cs` | Aggregates executive/manager/supervisor/employee views | ❌ No controller consumes it |

---

## 2. Frontend Page-by-Page Trace

### 2A. Executive Dashboard (`/executive-dashboard`)

| Field | Value |
|-------|-------|
| **Page** | `features/executive-dashboard/ExecutiveDashboardPage.tsx` |
| **Route** | `/executive-dashboard` |
| **Data Source** | Real API calls via `get()` |
| **Uses Mock Data?** | ❌ No — initial state is `null`/`[]` |
| **API Endpoints** | `GET /api/executive-dashboard/summary`<br>`GET /api/executive-dashboard/heatmap`<br>`GET /api/executive-dashboard/leaderboard` |
| **DTO Contract** | `ExecutiveSummary`, `DepartmentHeatmap[]`, `DepartmentLeaderboard[]` |
| **React Query Hook** | ❌ None — uses raw `useEffect` + `setInterval(30s)` |
| **Authorization** | `[Authorize]` on controller (role TBD) |
| **Department Scope** | Global (all departments) |

**Flow**:
```
ExecutiveDashboardPage.tsx
  → get(ENDPOINTS.executiveDashboard.summary)
  → GET /api/executive-dashboard/summary
  → ExecutiveDashboardController.GetSummary()
  → Direct EF Core queries on _db.AttendanceRecords, _db.Escalations, _db.WarningLetters
  → PostgreSQL tables
```

**✅ VERDICT**: Frontend ↔ Backend connected. BUT:
- `DashboardService` (which wraps `AttendanceKpiService`) is NOT used by this controller
- Controller does direct DB queries instead of using KPI services
- KPI services never registered in DI

---

### 2B. Manager Dashboard (`/manager-dashboard`)

| Field | Value |
|-------|-------|
| **Page** | `features/manager-dashboard/ManagerDashboardPage.tsx` |
| **Route** | `/manager-dashboard` |
| **Data Source** | **MOCK DATA ONLY** |
| **Uses Mock Data?** | ✅ YES — `MOCK_SUMMARY`, `MOCK_TEAM_SUMMARY`, `MOCK_DEPARTMENTS`, etc. |
| **Uses Real API?** | ⚠️ Attempts one call but silently falls back to mock |
| **API Endpoint Attempted** | `GET /api/reports/manager-dashboard` |
| **Backend Endpoint Exists?** | ❌ **NO** — ReportsController has no such method |
| **React Query Hook** | ❌ None — raw `useState` + `useEffect` |
| **Authorization** | None defined |
| **Department Scope** | Hardcoded mock department "Production" |

**Flow**:
```
ManagerDashboardPage.tsx
  → useState(MOCK_SUMMARY), useState(MOCK_DEPARTMENTS), etc.
  → useEffect(() => { try { get('/api/reports/manager-dashboard') } catch { /* stays on mock */ }})
  → Backend: ENDPOINT DOES NOT EXIST
  → Result: Always shows mock data
```

**🚫 VERDICT**: COMPLETELY DISCONNECTED. Needs:
1. New controller endpoint or reuse `DashboardService.GetManagerDashboardAsync(Guid)`
2. DTO contract definition
3. DI registration for `DashboardService` + `AttendanceKpiService`

---

### 2C. General Dashboard (`/dashboard`)

| Field | Value |
|-------|-------|
| **Page** | `features/dashboard/DashboardPage.tsx` |
| **Route** | `/dashboard` |
| **Data Source** | **MOCK DATA ONLY** |
| **Uses Mock Data?** | ✅ YES — `MOCK_DEPARTMENTS`, `MOCK_TRENDS`, `MOCK_TOP_LATE`, `MOCK_EXCEPTIONS` |
| **Uses Real API?** | ⚠️ Attempts one call but silently falls back to mock |
| **API Endpoint Attempted** | `GET /api/reports/dashboard-kpi` |
| **Backend Endpoint Exists?** | ❌ **NO** — ReportsController has no such method |
| **React Query Hook** | ❌ None — raw `useState` + `useEffect` |
| **Authorization** | None defined |
| **Department Scope** | Hardcoded mock departments (Production, Quality, Engineering, Logistics, Office) |

**Flow**:
```
DashboardPage.tsx
  → useState(MOCK_DEPARTMENTS), useState(MOCK_TRENDS), etc.
  → useEffect(() => { try { get('/api/reports/dashboard-kpi') } catch { /* stays on mock */ }})
  → Backend: ENDPOINT DOES NOT EXIST
  → Result: Always shows mock data
```

**🚫 VERDICT**: COMPLETELY DISCONNECTED. Same pattern as Manager Dashboard.

---

### 2D. HR Command Centre (`/hr-command-centre`)

| Field | Value |
|-------|-------|
| **Page** | `features/hr-command-centre/HRCommandCentrePage.tsx` |
| **Route** | `/hr-command-centre` |
| **Data Source** | **MOCK DATA ONLY** |
| **Uses Mock Data?** | ✅ YES — `MOCK_KPIS`, `MOCK_TREND`, `MOCK_DEPT_RANKING`, `MOCK_TOP_OFFENDERS`, `MOCK_EXCEPTION_ANALYSIS` |
| **Uses Real API?** | ⚠️ Attempts one call but silently falls back to mock |
| **API Endpoint Attempted** | `GET /api/reports/hr-command-centre` |
| **Backend Endpoint Exists?** | ❌ **NO** — ReportsController has no such method |
| **React Query Hook** | ❌ None — raw `useState` + `useEffect` |
| **Authorization** | None defined |
| **Department Scope** | Plant-wide (all departments) |

**Flow**:
```
HRCommandCentrePage.tsx
  → useState(MOCK_KPIS), useState(MOCK_TREND), etc.
  → useEffect(() => { try { get('/api/reports/hr-command-centre') } catch { /* stays on mock */ }})
  → Backend: ENDPOINT DOES NOT EXIST
  → Result: Always shows mock data
```

**🚫 VERDICT**: COMPLETELY DISCONNECTED. Same pattern.

---

## 3. KPI Card Trace Matrix

For each required KPI card, the following trace was performed:

| KPI Card | Executive Dash | Manager Dash | General Dash | HR Cmd Centre | Backend Coverage |
|----------|---------------|--------------|--------------|---------------|-----------------|
| Present Employees | ✅ Exec API | ❌ Mock | ❌ Mock | ❌ Mock | ✅ Exec controller |
| Absent Employees | ✅ Exec API | ❌ Mock | ❌ Mock | ❌ Mock | ✅ Exec controller |
| Attendance % | ✅ Exec API | ❌ Mock | ❌ Mock | ❌ Mock | ✅ Exec controller |
| Open Exceptions | ✅ Exec API (via pending investigations) | ❌ Mock | ❌ Mock | ❌ Mock | ✅ Exec controller |
| Open Investigations | ✅ Exec API | ❌ Mock | ❌ Mock | ❌ Mock | ✅ Exec controller |
| Corrective Actions | ✅ Exec API | ❌ Mock | ❌ Mock | ❌ Mock | ✅ Exec controller |
| Escalations | ✅ Exec API | ❌ Mock | ❌ Mock | ❌ Mock | ✅ Exec controller |
| Lost Minutes | ✅ Exec API | ❌ Mock | ❌ Mock | ❌ Mock | ✅ Exec controller |
| Lost Hours | ✅ Exec API | ❌ Mock | ❌ Mock | ❌ Mock | ✅ Exec controller |
| Department Rankings | ✅ Exec API (leaderboard) | ❌ Mock | ❌ Mock | ❌ Mock | ✅ Exec controller |

---

## 4. Gap Inventory

### P0 — Critical Blockers
| # | Gap | Impact | Effort |
|---|-----|-------|--------|
| **G-001** | KPI Services not registered in DI (`AttendanceKpiService`, `DepartmentKpiService`, `ExecutiveKpiService`, `ExecutiveAnalyticsService`, `DashboardAggregationService`) | **All** dashboard services cannot be instantiated even if controllers tried to use them | ~2h |
| **G-002** | `DashboardService` never consumed by any controller | `GetManagerDashboardAsync()`, `GetSupervisorDashboardAsync()`, `GetEmployeeDashboardAsync()` dead code | ~2h (wire up endpoints) |
| **G-003** | Missing controller endpoints: `GET /api/reports/dashboard-kpi`, `GET /api/reports/manager-dashboard`, `GET /api/reports/hr-command-centre` | 3 frontend pages stuck on mock data forever | ~4h |

### P1 — Architecture Improvements
| # | Gap | Impact | Effort |
|---|-----|-------|--------|
| **G-004** | No React Query hooks — all 4 pages use raw `useEffect` + `setState` | No caching, no background refetch, no optimistic updates | ~6h (create 4 hook files) |
| **G-005** | Executive controller uses direct `_db.*` queries instead of `DashboardService` / `AttendanceKpiService` | Code duplication, no shift-aware filtering, no centralized KPI logic | ~3h |
| **G-006** | No DTO types defined for manager/hr command centre responses | TypeScript compilation will fail once real endpoints return data | ~2h |
| **G-007** | No authorization policies on dashboard routes (`/manager-dashboard`, `/dashboard`, `/hr-command-centre`) | Any authenticated user can access all dashboards regardless of role | ~2h |
| **G-008** | No department-scoped filtering in any dashboard query | Managers see other departments' data, supervisors see everything | ~4h |

### P2 — UX & Performance
| # | Gap | Impact | Effort |
|---|-----|-------|--------|
| **G-009** | Executive dashboard refreshes every 30s with full re-fetch | No incremental updates, potential server load | ~1h |
| **G-010** | No loading/error states for failed API calls (catch blocks silently stay on mock) | Users won't know when API is down vs. when data genuinely matches mock | ~2h |
| **G-011** | No pagination or virtualization for exception tables | Will break at scale (>500 exceptions) | ~3h |

---

## 5. Remaining Integration Tasks

### Immediate (P0)
1. **Register KPI Services in DI** — Add to `ApiServiceCollectionExtensions.cs`:
   ```csharp
   services.AddScoped<AttendanceKpiService>();
   services.AddScoped<DepartmentKpiService>();
   services.AddScoped<ExecutiveKpiService>();
   services.AddScoped<ExecutiveAnalyticsService>();
   services.AddScoped<DashboardAggregationService>();
   ```

2. **Add missing controller endpoints** in `ReportsController.cs`:
   - `GET /api/reports/dashboard-kpi` → returns `DashboardService.GetExecutiveDashboardAsync()`
   - `GET /api/reports/manager-dashboard/{departmentId}` → returns `DashboardService.GetManagerDashboardAsync(departmentId)`
   - `GET /api/reports/hr-command-centre` → aggregates plant-wide stats

3. **Wire `DashboardService`** into existing flow — refactor `ExecutiveDashboardController` to use `DashboardService` instead of direct `_db.*` queries.

### Short-term (P1)
4. Create React Query hooks: `useExecutiveDashboard()`, `useManagerDashboard()`, `useDashboardKPIs()`, `useHRCommandCentre()`
5. Define TypeScript DTO contracts in `core/api/types.ts` for all new endpoints
6. Add RBAC route guards in `App.tsx` for `/manager-dashboard`, `/hr-command-centre`
7. Implement department-scoped filtering in service layer

### Medium-term (P2)
8. Replace polling with Server-Sent Events or WebSockets for real-time dashboard updates
9. Add proper error boundaries and retry logic
10. Pagination for large datasets

---

## 6. Estimated Completion Effort

| Phase | Tasks | Person-Hours |
|-------|-------|-------------|
| **P0 (Critical)** | G-001, G-002, G-003 | ~8 hours |
| **P1 (Architecture)** | G-004 through G-008 | ~13 hours |
| **P2 (UX/Performance)** | G-009 through G-011 | ~6 hours |
| **Total** | All gaps | **~27 hours** (~3.5 working days) |

---

## 7. Success Criteria for Live Dashboard

When all P0 tasks are complete:
- [ ] All 4 KPI services instantiable via DI
- [ ] `DashboardService` accessible from controllers
- [ ] All 3 missing endpoints functional (`dashboard-kpi`, `manager-dashboard`, `hr-command-centre`)
- [ ] Frontend pages receive live data from database (no mock fallback active)
- [ ] Executive dashboard continues working (existing connection preserved)

---

*created_at: 2026-11-17*
*updated_at: 2026-11-17*
*memory_strength: 0.95*
*half_life_days: 365*
*scope: workspace*
