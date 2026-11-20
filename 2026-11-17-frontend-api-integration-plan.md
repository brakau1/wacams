# Frontend API Integration Plan

**Date**: 2026-11-17  
**Status**: Ready for Implementation  
**SubAgent**: SubAgent B - Frontend API Integration  
**Priority**: P0 (CRITICAL)

---

## Executive Summary

This document provides the complete execution plan for replacing all mock data with live API integrations across 13 frontend feature modules. The plan includes detailed specifications for each module, API contracts, React Query hooks, and migration strategies.

**Current State**: Mixed - 6 modules use mock data, 7 modules already integrate with APIs  
**Target State**: 100% API integration with proper error handling and loading states  
**Estimated Effort**: 10-15 days

---

## Module Inventory & Status

### Modules Requiring Migration (Mock Data → API)

| # | Module | Current State | Migration Priority | Effort |
|---|--------|---------------|-------------------|--------|
| 1 | attendance | ❌ Mock data | HIGH | 2 days |
| 2 | dashboard | ⚠️ Partial (has fallback) | HIGH | 1 day |
| 3 | exceptions | ❌ Mock data | HIGH | 2 days |
| 4 | warnings | ❌ Mock data | HIGH | 1 day |
| 5 | employees | ⚠️ Partial (has fallback) | MEDIUM | 1 day |
| 6 | manager-dashboard | ⚠️ Partial (has fallback) | MEDIUM | 1 day |

### Modules Already Integrated (Require Enhancement)

| # | Module | Current State | Enhancement Needed |
|---|--------|---------------|-------------------|
| 7 | escalations | ✅ API integrated | Add error handling, loading states |
| 8 | audit | ✅ API integrated | Add error handling, loading states |
| 9 | reports | ✅ API integrated | Add error handling, loading states |
| 10 | executive-dashboard | ✅ API integrated | Add error handling, loading states |
| 11 | hr-command-centre | ✅ API integrated | Add error handling, loading states |
| 12 | admin | ✅ API integrated | Add error handling, loading states |
| 13 | auth | ✅ API integrated | Add error handling, loading states |

---

## Module 1: Attendance

### Current State
- **File**: `Frontend/src/features/attendance/AttendancePage.tsx`
- **Mock Data**: `MOCK_RECORDS` (20 hardcoded records)
- **Components**: CalendarView, DailyAttendanceTable, LostTimeSummary, EmployeeFilters

### Target API Endpoints

#### GET /api/attendance
**Query Parameters**:
- `departmentId` (optional): Filter by department
- `startDate` (required): ISO date string
- `endDate` (required): ISO date string
- `status` (optional): Filter by status (Present, Late, EarlyDeparture, Absent, AWOL)

**Response DTO**:
```typescript
interface AttendanceRecord {
  id: string;
  employeeId: string;
  employeeName: string;
  displayName: string;
  departmentId: string;
  departmentName: string;
  date: string; // ISO date
  scheduledShift: string;
  shiftStartTime: string;
  shiftEndTime: string;
  clockInTime: string | null;
  clockOutTime: string | null;
  status: 'Present' | 'Late' | 'EarlyDeparture' | 'Absent' | 'AWOL';
  lateMinutes: number;
  earlyMinutes: number;
  totalHours: number;
  notes: string | null;
  correctedBy: string | null;
  correctionReason: string | null;
}

interface AttendanceResponse {
  items: AttendanceRecord[];
  totalItems: number;
  page: number;
  pageSize: number;
}
```

#### GET /api/attendance/summary
**Query Parameters**:
- `departmentId` (optional)
- `startDate` (required)
- `endDate` (required)

**Response DTO**:
```typescript
interface AttendanceSummary {
  totalLateMinutes: number;
  totalEarlyMinutes: number;
  totalLostMinutes: number;
  departmentBreakdown: Array<{
    name: string;
    minutes: number;
  }>;
}
```

### React Query Hook

```typescript
// src/core/api/hooks/useAttendance.ts
import { useQuery } from '@tanstack/react-query';
import { get } from '../apiClient';
import { ENDPOINTS } from '../endpoints';

export function useAttendance(filters: {
  departmentId?: string;
  startDate: string;
  endDate: string;
  status?: string;
}) {
  return useQuery({
    queryKey: ['attendance', filters],
    queryFn: async () => {
      const params = new URLSearchParams();
      params.set('startDate', filters.startDate);
      params.set('endDate', filters.endDate);
      if (filters.departmentId) params.set('departmentId', filters.departmentId);
      if (filters.status) params.set('status', filters.status);
      
      const response = await get<AttendanceResponse>(
        `${ENDPOINTS.attendance.list}?${params}`
      );
      return response.data ?? response;
    },
    staleTime: 30000, // 30 seconds
  });
}

export function useAttendanceSummary(filters: {
  departmentId?: string;
  startDate: string;
  endDate: string;
}) {
  return useQuery({
    queryKey: ['attendance-summary', filters],
    queryFn: async () => {
      const params = new URLSearchParams();
      params.set('startDate', filters.startDate);
      params.set('endDate', filters.endDate);
      if (filters.departmentId) params.set('departmentId', filters.departmentId);
      
      const response = await get<AttendanceSummary>(
        `${ENDPOINTS.attendance.summary}?${params}`
      );
      return response.data ?? response;
    },
  });
}
```

### Permission Requirements
- **Read**: All authenticated users
- **Department Scope**: Users can only see attendance for departments they have access to
- **Role Requirements**: None (all roles can view)

### Loading States
- Initial load: Full-page spinner
- Filter change: Inline spinner on table
- Pagination: Skeleton loader

### Error States
- API error: Toast notification + retry button
- Empty state: "No attendance records found for the selected period"
- Permission denied: Redirect to unauthorized page

### Migration Effort: 2 days
**Tasks**:
1. Create React Query hooks (2 hours)
2. Replace mock data in AttendancePage.tsx (4 hours)
3. Update CalendarView component (2 hours)
4. Update DailyAttendanceTable component (2 hours)
5. Update LostTimeSummary component (2 hours)
6. Add error handling and loading states (2 hours)
7. Testing and validation (2 hours)

### Dependencies
- Backend: AttendanceController (already exists)
- Backend: AttendanceService (already exists)
- Frontend: React Query (already installed)

---

## Module 2: Dashboard

### Current State
- **File**: `Frontend/src/features/dashboard/DashboardPage.tsx`
- **Mock Data**: `MOCK_DEPARTMENTS`, `MOCK_TRENDS`, `MOCK_TOP_LATE`, `MOCK_EXCEPTIONS`
- **API Integration**: Already has fallback to API, but needs enhancement

### Target API Endpoints

#### GET /api/reports/dashboard-kpi
**Response DTO**:
```typescript
interface DashboardKPIResponse {
  kpi: DepartmentKPI[];
  trends: TrendPoint[];
  topLate: TopLateEntry[];
  exceptions: AttendanceException[];
}

interface DepartmentKPI {
  departmentId: string;
  departmentName: string;
  attendancePercentage: number;
  totalEmployees: number;
  presentCount: number;
  lateCount: number;
  absentCount: number;
}

interface TrendPoint {
  date: string;
  onTimePercentage: number;
  lateCount: number;
  exceptionCount: number;
}

interface TopLateEntry {
  employeeId: string;
  displayName: string;
  departmentName: string;
  lateCount: number;
  totalLateMinutes: number;
}

interface AttendanceException {
  id: string;
  employeeId: string;
  employeeName: string;
  displayName: string;
  departmentId: string;
  departmentName: string;
  exceptionType: string;
  date: string;
  description: string;
  lostProductionMinutes: number;
  status: string;
  assignedTo: string | null;
  assignedToName: string | null;
  investigatorNotes: string | null;
  investigatedAt: string | null;
  investigatedBy: string | null;
  createdAt: string;
  updatedAt: string;
}
```

### React Query Hook

```typescript
// src/core/api/hooks/useDashboard.ts
export function useDashboardKPI() {
  return useQuery({
    queryKey: ['dashboard-kpi'],
    queryFn: async () => {
      const response = await get<DashboardKPIResponse>(
        ENDPOINTS.reports.dashboardKPI
      );
      return response.data ?? response;
    },
    staleTime: 60000, // 1 minute
    refetchInterval: 300000, // 5 minutes
  });
}
```

### Permission Requirements
- **Read**: All authenticated users
- **Department Scope**: Filtered by user's department access

### Migration Effort: 1 day
**Tasks**:
1. Create React Query hook (1 hour)
2. Remove mock data constants (1 hour)
3. Update component to use hook (2 hours)
4. Add proper error handling (2 hours)
5. Testing and validation (2 hours)

---

## Module 3: Exceptions

### Current State
- **File**: `Frontend/src/features/exceptions/ExceptionsPage.tsx`
- **Mock Data**: `MOCK_EXCEPTIONS` (5 hardcoded records)
- **Components**: ExceptionCard, StatusTimeline, InvestigationForm, BulkInvestigateModal

### Target API Endpoints

#### GET /api/exceptions
**Query Parameters**:
- `status` (optional): Open, UnderReview, Investigated, Closed, Escalated
- `departmentId` (optional)
- `exceptionType` (optional)
- `startDate` (optional)
- `endDate` (optional)
- `page` (optional, default: 1)
- `pageSize` (optional, default: 50)

**Response DTO**:
```typescript
interface ExceptionsResponse {
  items: AttendanceException[];
  totalItems: number;
  page: number;
  pageSize: number;
}
```

#### PUT /api/exceptions/:id/investigate
**Request Body**:
```typescript
interface InvestigateExceptionRequest {
  notes: string;
  decision: 'Approve' | 'Reject';
}
```

#### PUT /api/exceptions/bulk-investigate
**Request Body**:
```typescript
interface BulkInvestigateRequest {
  exceptionIds: string[];
  notes: string;
  decision: 'Approve' | 'Reject';
}
```

### React Query Hooks

```typescript
// src/core/api/hooks/useExceptions.ts
export function useExceptions(filters: {
  status?: string;
  departmentId?: string;
  exceptionType?: string;
  startDate?: string;
  endDate?: string;
}) {
  return useQuery({
    queryKey: ['exceptions', filters],
    queryFn: async () => {
      const params = new URLSearchParams();
      if (filters.status) params.set('status', filters.status);
      if (filters.departmentId) params.set('departmentId', filters.departmentId);
      if (filters.exceptionType) params.set('exceptionType', filters.exceptionType);
      if (filters.startDate) params.set('startDate', filters.startDate);
      if (filters.endDate) params.set('endDate', filters.endDate);
      
      const response = await get<ExceptionsResponse>(
        `${ENDPOINTS.exceptions.list}?${params}`
      );
      return response.data ?? response;
    },
  });
}

export function useInvestigateException() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: async ({ id, data }: { id: string; data: InvestigateExceptionRequest }) => {
      await put(`${ENDPOINTS.exceptions.list}/${id}/investigate`, data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['exceptions'] });
    },
  });
}

export function useBulkInvestigateExceptions() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: async (data: BulkInvestigateRequest) => {
      await put(`${ENDPOINTS.exceptions.list}/bulk-investigate`, data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['exceptions'] });
    },
  });
}
```

### Permission Requirements
- **Read**: All authenticated users
- **Investigate**: Supervisor, Manager, HR, Admin, SuperAdmin only
- **Department Scope**: Users can only investigate exceptions in their departments

### Migration Effort: 2 days
**Tasks**:
1. Create React Query hooks (3 hours)
2. Replace mock data in ExceptionsPage.tsx (4 hours)
3. Update InvestigationForm component (2 hours)
4. Update BulkInvestigateModal component (2 hours)
5. Add error handling and loading states (2 hours)
6. Testing and validation (3 hours)

---

## Module 4: Warnings

### Current State
- **File**: `Frontend/src/features/warnings/WarningsPage.tsx`
- **Mock Data**: `MOCK_WARNINGS` (3 hardcoded records)
- **Components**: WarningCard, ThresholdProgress, WarningTimeline, WarningGenerationWizard

### Target API Endpoints

#### GET /api/warnings
**Query Parameters**:
- `status` (optional): Draft, Issued, Acknowledged
- `employeeId` (optional)
- `departmentId` (optional)

**Response DTO**:
```typescript
interface WarningsResponse {
  items: WarningLetter[];
  totalItems: number;
}

interface WarningLetter {
  id: string;
  employeeId: string;
  employeeName: string;
  displayName: string;
  departmentId: string;
  departmentName: string;
  warningType: string;
  relatedExceptionId: string;
  title: string;
  description: string;
  severity: number;
  signedFormUrl: string | null;
  signedBy: string | null;
  signedAt: string | null;
  issuedBy: string;
  issuedByName: string;
  status: string;
  escalationLevel: number;
  createdAt: string;
  updatedAt: string;
}
```

#### POST /api/warnings/generate
**Request Body**:
```typescript
interface GenerateWarningRequest {
  employeeId: string;
  exceptionId: string;
  warningType: string;
  title: string;
  description: string;
}
```

### React Query Hooks

```typescript
// src/core/api/hooks/useWarnings.ts
export function useWarnings(filters: {
  status?: string;
  employeeId?: string;
  departmentId?: string;
}) {
  return useQuery({
    queryKey: ['warnings', filters],
    queryFn: async () => {
      const params = new URLSearchParams();
      if (filters.status) params.set('status', filters.status);
      if (filters.employeeId) params.set('employeeId', filters.employeeId);
      if (filters.departmentId) params.set('departmentId', filters.departmentId);
      
      const response = await get<WarningsResponse>(
        `${ENDPOINTS.warnings.list}?${params}`
      );
      return response.data ?? response;
    },
  });
}

export function useGenerateWarning() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: async (data: GenerateWarningRequest) => {
      await post(ENDPOINTS.warnings.generate, data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['warnings'] });
    },
  });
}
```

### Permission Requirements
- **Read**: All authenticated users
- **Generate**: Supervisor, Manager, HR, Admin, SuperAdmin only
- **Department Scope**: Users can only generate warnings for their departments

### Migration Effort: 1 day
**Tasks**:
1. Create React Query hooks (2 hours)
2. Replace mock data in WarningsPage.tsx (3 hours)
3. Update WarningGenerationWizard component (2 hours)
4. Add error handling and loading states (1 hour)

---

## Module 5: Employees

### Current State
- **File**: `Frontend/src/features/employees/EmployeeListPage.tsx`
- **Mock Data**: `MOCK_EMPLOYEES` (5 hardcoded records)
- **API Integration**: Already has fallback to API, but needs enhancement

### Target API Endpoints

#### GET /api/employees
**Query Parameters**:
- `departmentId` (optional)
- `status` (optional): Active, OnLeave, Inactive
- `search` (optional)
- `page` (optional, default: 1)
- `pageSize` (optional, default: 50)

**Response DTO**:
```typescript
interface EmployeesResponse {
  items: Employee[];
  totalItems: number;
  page: number;
  pageSize: number;
}

interface Employee {
  id: string;
  employeeId: string;
  firstName: string;
  lastName: string;
  displayName: string;
  email: string;
  departmentId: string;
  departmentName: string;
  managerId: string;
  managerName: string;
  jobTitle: string;
  status: string;
  hireDate: string;
  shiftCode: string;
  isClockIn: boolean;
  clockInTime: string | null;
  clockOutTime: string | null;
  createdAt: string;
  updatedAt: string;
}
```

### React Query Hook

```typescript
// src/core/api/hooks/useEmployees.ts
export function useEmployees(filters: {
  departmentId?: string;
  status?: string;
  search?: string;
}) {
  return useQuery({
    queryKey: ['employees', filters],
    queryFn: async () => {
      const params = new URLSearchParams();
      if (filters.departmentId) params.set('departmentId', filters.departmentId);
      if (filters.status) params.set('status', filters.status);
      if (filters.search) params.set('search', filters.search);
      
      const response = await get<EmployeesResponse>(
        `${ENDPOINTS.employees.list}?${params}`
      );
      return response.data ?? response;
    },
  });
}
```

### Migration Effort: 1 day

---

## Module 6: Manager Dashboard

### Current State
- **File**: `Frontend/src/features/manager-dashboard/ManagerDashboardPage.tsx`
- **Mock Data**: Multiple mock constants
- **API Integration**: Already has fallback to API, but needs enhancement

### Target API Endpoints

#### GET /api/reports/manager-dashboard
**Response DTO**:
```typescript
interface ManagerDashboardResponse {
  summary: AttendanceSummary;
  teamSummary: AttendanceSummary;
  departments: DepartmentKPI[];
  trends: TrendPoint[];
  topOffenders: TopLateEntry[];
  exceptions: AttendanceException[];
}
```

### React Query Hook

```typescript
// src/core/api/hooks/useManagerDashboard.ts
export function useManagerDashboard() {
  return useQuery({
    queryKey: ['manager-dashboard'],
    queryFn: async () => {
      const response = await get<ManagerDashboardResponse>(
        ENDPOINTS.reports.managerDashboard
      );
      return response.data ?? response;
    },
    staleTime: 60000,
    refetchInterval: 300000,
  });
}
```

### Migration Effort: 1 day

---

## Modules 7-13: Enhancement Required

These modules already integrate with APIs but need error handling and loading state improvements.

### Module 7: Escalations
- **File**: `Frontend/src/features/escalations/EscalationsPage.tsx`
- **Status**: ✅ API integrated
- **Enhancement**: Add error boundaries, retry logic, better loading states
- **Effort**: 0.5 days

### Module 8: Audit
- **File**: `Frontend/src/features/audit/AuditLogPage.tsx`
- **Status**: ✅ API integrated
- **Enhancement**: Add error boundaries, retry logic
- **Effort**: 0.5 days

### Module 9: Reports
- **File**: `Frontend/src/features/reports/ReportsPage.tsx`
- **Status**: ✅ API integrated
- **Enhancement**: Add error boundaries, retry logic
- **Effort**: 0.5 days

### Module 10: Executive Dashboard
- **File**: `Frontend/src/features/executive-dashboard/ExecutiveDashboardPage.tsx`
- **Status**: ✅ API integrated
- **Enhancement**: Add error boundaries, retry logic
- **Effort**: 0.5 days

### Module 11: HR Command Centre
- **File**: `Frontend/src/features/hr-command-centre/HRCommandCentrePage.tsx`
- **Status**: ✅ API integrated
- **Enhancement**: Add error boundaries, retry logic
- **Effort**: 0.5 days

### Module 12: Admin
- **Files**: `Frontend/src/features/admin/UserManagementPage.tsx`, `DepartmentSettingsPage.tsx`, `SystemSettingsPage.tsx`
- **Status**: ✅ API integrated
- **Enhancement**: Add error boundaries, retry logic
- **Effort**: 0.5 days

### Module 13: Auth
- **File**: `Frontend/src/features/auth/LoginPage.tsx`
- **Status**: ✅ API integrated
- **Enhancement**: Add error handling for login failures
- **Effort**: 0.5 days

---

## Implementation Sequence

### Phase 1: Core Infrastructure (Day 1)
1. Create API hooks directory structure
2. Implement shared error handling utilities
3. Create common loading state components
4. Set up React Query client configuration

### Phase 2: Critical Modules (Days 2-5)
1. Module 1: Attendance (2 days)
2. Module 3: Exceptions (2 days)
3. Module 4: Warnings (1 day)

### Phase 3: Dashboard Modules (Days 6-8)
1. Module 2: Dashboard (1 day)
2. Module 5: Employees (1 day)
3. Module 6: Manager Dashboard (1 day)

### Phase 4: Enhancement (Days 9-12)
1. Enhance modules 7-13 (3 days)
2. Add error boundaries across all modules
3. Implement retry logic
4. Add loading state improvements

### Phase 5: Testing & Validation (Days 13-15)
1. Integration testing
2. Error scenario testing
3. Performance testing
4. User acceptance testing

---

## Success Criteria

✅ All 13 modules use live API data  
✅ Zero mock data remaining  
✅ Proper error handling on all pages  
✅ Loading states implemented  
✅ Permission guards working  
✅ Department scoping enforced  
✅ React Query hooks for all data fetching  
✅ Error boundaries in place  
✅ Retry logic implemented  
✅ Performance optimized (caching, stale time)  

---

## Dependencies

### Backend (Must Be Complete)
- All API controllers implemented
- All services implemented
- Database migrations applied
- Authentication working
- Authorization enforced

### Frontend (Already Available)
- React Query installed
- API client configured
- Type definitions created
- Shared components available

---

## Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| Backend API not ready | Use mock data as fallback during development |
| API contract mismatch | Define TypeScript interfaces matching backend DTOs |
| Performance issues | Implement proper caching with React Query |
| Error handling gaps | Create shared error boundary components |
| Permission issues | Test with different user roles |

---

**Document Status**: Ready for Implementation  
**Next Step**: Exit plan-file-mode and execute migration
