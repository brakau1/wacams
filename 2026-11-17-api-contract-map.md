# API Contract Map

**Date**: 2026-11-17  
**Purpose**: Complete mapping of all frontend API contracts  
**Status**: Ready for Implementation

---

## Overview

This document provides a comprehensive map of all API contracts between the frontend and backend, organized by module. Each contract includes the endpoint, HTTP method, request/response types, and validation rules.

---

## Authentication & Authorization

### POST /api/auth/login
**Purpose**: Authenticate user and return JWT token  
**Request**:
```typescript
interface LoginRequest {
  employeeId: string;
  password: string;
}
```
**Response**:
```typescript
interface LoginResponse {
  token: string;
  user: {
    id: string;
    employeeId: string;
    displayName: string;
    role: string;
    departmentId: string;
  };
  expiresAt: string;
}
```
**Validation**:
- `employeeId`: Required, non-empty
- `password`: Required, non-empty

**Error Codes**:
- 401: Invalid credentials
- 403: Account disabled
- 429: Too many login attempts

---

## Attendance Module

### GET /api/attendance
**Purpose**: Retrieve attendance records with filtering  
**Query Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| departmentId | string | No | Filter by department |
| startDate | string | Yes | ISO date (YYYY-MM-DD) |
| endDate | string | Yes | ISO date (YYYY-MM-DD) |
| status | string | No | Present, Late, EarlyDeparture, Absent, AWOL |
| page | number | No | Page number (default: 1) |
| pageSize | number | No | Items per page (default: 50) |

**Response**:
```typescript
interface AttendanceResponse {
  items: AttendanceRecord[];
  totalItems: number;
  page: number;
  pageSize: number;
}

interface AttendanceRecord {
  id: string;
  employeeId: string;
  employeeName: string;
  displayName: string;
  departmentId: string;
  departmentName: string;
  date: string;
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
```

**Authorization**: All authenticated users  
**Department Scope**: Filtered by user's department access

---

### GET /api/attendance/summary
**Purpose**: Get attendance summary statistics  
**Query Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| departmentId | string | No | Filter by department |
| startDate | string | Yes | ISO date |
| endDate | string | Yes | ISO date |

**Response**:
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

---

### POST /api/attendance/clock-in
**Purpose**: Record employee clock-in  
**Request**:
```typescript
interface ClockInRequest {
  employeeId: string;
  clockInTime: string; // ISO datetime
  location?: {
    latitude: number;
    longitude: number;
  };
}
```
**Response**:
```typescript
interface ClockInResponse {
  success: boolean;
  record: AttendanceRecord;
}
```

**Authorization**: Employee can only clock in themselves, supervisors can clock in their team

---

### POST /api/attendance/clock-out
**Purpose**: Record employee clock-out  
**Request**:
```typescript
interface ClockOutRequest {
  employeeId: string;
  clockOutTime: string; // ISO datetime
}
```
**Response**:
```typescript
interface ClockOutResponse {
  success: boolean;
  record: AttendanceRecord;
}
```

---

## Exceptions Module

### GET /api/exceptions
**Purpose**: Retrieve attendance exceptions  
**Query Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| status | string | No | Open, UnderReview, Investigated, Closed, Escalated |
| departmentId | string | No | Filter by department |
| exceptionType | string | No | LateArrival, EarlyDeparture, etc. |
| startDate | string | No | ISO date |
| endDate | string | No | ISO date |
| page | number | No | Page number |
| pageSize | number | No | Items per page |

**Response**:
```typescript
interface ExceptionsResponse {
  items: AttendanceException[];
  totalItems: number;
  page: number;
  pageSize: number;
}

interface AttendanceException {
  id: string;
  employeeId: string;
  employeeName: string;
  displayName: string;
  departmentId: string;
  departmentName: string;
  exceptionType: 'LateArrival' | 'EarlyDeparture' | 'UnplannedAbsenteeism' | 'AWOL' | 'MissingClockIn' | 'MissingClockOut' | 'UnscheduledClockIn' | 'UnscheduledClockOut';
  date: string;
  description: string;
  lostProductionMinutes: number;
  status: 'Open' | 'UnderReview' | 'Investigated' | 'Closed' | 'Escalated';
  assignedTo: string | null;
  assignedToName: string | null;
  investigatorNotes: string | null;
  investigatedAt: string | null;
  investigatedBy: string | null;
  createdAt: string;
  updatedAt: string;
}
```

**Authorization**: All authenticated users can view, only supervisors+ can investigate

---

### PUT /api/exceptions/:id/investigate
**Purpose**: Investigate and resolve an exception  
**Path Parameters**:
- `id`: Exception ID (required)

**Request**:
```typescript
interface InvestigateExceptionRequest {
  notes: string;
  decision: 'Approve' | 'Reject';
}
```
**Response**:
```typescript
interface InvestigateExceptionResponse {
  success: boolean;
  exception: AttendanceException;
}
```

**Authorization**: Supervisor, Manager, HR, Admin, SuperAdmin only  
**Department Scope**: Can only investigate exceptions in user's departments

---

### PUT /api/exceptions/bulk-investigate
**Purpose**: Investigate multiple exceptions at once  
**Request**:
```typescript
interface BulkInvestigateRequest {
  exceptionIds: string[];
  notes: string;
  decision: 'Approve' | 'Reject';
}
```
**Response**:
```typescript
interface BulkInvestigateResponse {
  success: boolean;
  processedCount: number;
  failedCount: number;
}
```

---

## Warnings Module

### GET /api/warnings
**Purpose**: Retrieve warning letters  
**Query Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| status | string | No | Draft, Issued, Acknowledged |
| employeeId | string | No | Filter by employee |
| departmentId | string | No | Filter by department |

**Response**:
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
  warningType: 'Counselling' | 'VerbalWarning' | 'WrittenWarning' | 'FinalWrittenWarning';
  relatedExceptionId: string;
  title: string;
  description: string;
  severity: number;
  signedFormUrl: string | null;
  signedBy: string | null;
  signedAt: string | null;
  issuedBy: string;
  issuedByName: string;
  status: 'Draft' | 'Issued' | 'Acknowledged';
  escalationLevel: number;
  createdAt: string;
  updatedAt: string;
}
```

---

### POST /api/warnings/generate
**Purpose**: Generate a new warning letter  
**Request**:
```typescript
interface GenerateWarningRequest {
  employeeId: string;
  exceptionId: string;
  warningType: 'Counselling' | 'VerbalWarning' | 'WrittenWarning' | 'FinalWrittenWarning';
  title: string;
  description: string;
}
```
**Response**:
```typescript
interface GenerateWarningResponse {
  success: boolean;
  warning: WarningLetter;
}
```

**Authorization**: Supervisor, Manager, HR, Admin, SuperAdmin only

---

### POST /api/warnings/:id/sign
**Purpose**: Record warning signature  
**Path Parameters**:
- `id`: Warning ID (required)

**Request**:
```typescript
interface SignWarningRequest {
  signedBy: string;
  signatureData: string; // Base64 encoded signature
}
```
**Response**:
```typescript
interface SignWarningResponse {
  success: boolean;
  warning: WarningLetter;
}
```

---

## Employees Module

### GET /api/employees
**Purpose**: Retrieve employee list  
**Query Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| departmentId | string | No | Filter by department |
| status | string | No | Active, OnLeave, Inactive |
| search | string | No | Search by name or employee ID |
| page | number | No | Page number |
| pageSize | number | No | Items per page |

**Response**:
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
  status: 'Active' | 'OnLeave' | 'Inactive';
  hireDate: string;
  shiftCode: string;
  isClockIn: boolean;
  clockInTime: string | null;
  clockOutTime: string | null;
  createdAt: string;
  updatedAt: string;
}
```

---

### GET /api/employees/:id
**Purpose**: Get employee details  
**Path Parameters**:
- `id`: Employee ID (required)

**Response**:
```typescript
interface EmployeeDetailResponse {
  employee: Employee;
  attendanceSummary: {
    totalDays: number;
    presentDays: number;
    lateDays: number;
    absentDays: number;
    attendancePercentage: number;
  };
  warningHistory: WarningLetter[];
  exceptionHistory: AttendanceException[];
}
```

---

## Dashboard Module

### GET /api/reports/dashboard-kpi
**Purpose**: Get dashboard KPI data  
**Response**:
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
```

---

### GET /api/reports/manager-dashboard
**Purpose**: Get manager-specific dashboard data  
**Response**:
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

---

### GET /api/executive-dashboard/summary
**Purpose**: Get executive dashboard summary  
**Response**:
```typescript
interface ExecutiveSummary {
  employeesPresent: number;
  totalActiveEmployees: number;
  currentAbsenteeism: number;
  attendancePercentage: number;
  lostHours: number;
  lostMinutes: number;
  openEscalations: number;
  pendingInvestigations: number;
  correctiveActions: number;
}
```

---

### GET /api/executive-dashboard/heatmap
**Purpose**: Get department heatmap data  
**Response**:
```typescript
interface DepartmentHeatmap[] {
  departmentId: string;
  departmentName: string;
  departmentCode: string;
  attendancePercentage: number;
  presentCount: number;
  totalEmployees: number;
  lateCount: number;
  absentCount: number;
  lostMinutes: number;
  heatLevel: 'green' | 'yellow' | 'orange' | 'red';
  targetPercentage: number;
  gapToTarget: number;
}
```

---

### GET /api/executive-dashboard/leaderboard
**Purpose**: Get department leaderboard  
**Response**:
```typescript
interface DepartmentLeaderboard[] {
  departmentId: string;
  departmentName: string;
  departmentCode: string;
  rank: number;
  attendancePercentage: number;
  totalEmployees: number;
  exceptionsCount: number;
  totalLostHours: number;
  trend: 'up' | 'down' | 'stable';
  targetPercentage: number;
}
```

---

### GET /api/reports/hr-command-centre
**Purpose**: Get HR command centre data  
**Response**:
```typescript
interface HRCommandCentreData {
  kpis: HRCommandCentreKPIs;
  attendanceTrend: TrendPoint[];
  departmentRanking: DepartmentRanking[];
  topOffenders: TopLateEntry[];
  exceptionAnalysis: ExceptionAnalysis[];
}

interface HRCommandCentreKPIs {
  plantAttendancePercentage: number;
  lostProductionHours: number;
  openWarnings: number;
  finalWarnings: number;
  awolCases: number;
  escalations: number;
  departmentsAtRisk: number;
}

interface DepartmentRanking {
  departmentId: string;
  departmentName: string;
  attendancePercentage: number;
  totalEmployees: number;
  presentCount: number;
  lateCount: number;
  absentCount: number;
  exceptionCount: number;
  riskLevel: 'low' | 'medium' | 'high' | 'critical';
}

interface ExceptionAnalysis {
  exceptionType: string;
  count: number;
  percentage: number;
  lostMinutes: number;
}
```

---

## Escalations Module

### GET /api/escalations
**Purpose**: Retrieve escalations  
**Query Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| status | string | No | Pending, Acknowledged, ActionTaken, Escalated, AutoClosed, Resolved |
| level | string | No | ManagerLevel1-5, HR, Admin |

**Response**:
```typescript
interface EscalationsResponse {
  items: Escalation[];
  totalItems: number;
}

interface Escalation {
  id: string;
  exceptionId: string;
  employeeId: string;
  employeeName: string;
  currentLevel: string;
  assignedTo: string;
  assignedToName: string;
  status: string;
  createdAt: string;
  dueAt: string;
  resolvedAt: string | null;
}
```

---

### GET /api/escalations/stats
**Purpose**: Get escalation statistics  
**Response**:
```typescript
interface EscalationStats {
  totalActive: number;
  byLevel: Record<string, number>;
  byStatus: Record<string, number>;
  overdueCount: number;
}
```

---

## Audit Module

### GET /api/audit
**Purpose**: Retrieve audit log entries  
**Query Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| entityType | string | No | Employee, AttendanceRecord, etc. |
| action | string | No | Create, Update, Delete |
| search | string | No | Search term |
| page | number | No | Page number |
| pageSize | number | No | Items per page |

**Response**:
```typescript
interface AuditResponse {
  items: AuditEntry[];
  totalItems: number;
}

interface AuditEntry {
  id: string;
  entityType: string;
  entityId: string;
  action: string;
  oldValue: string;
  newValue: string;
  performedBy: string;
  performedAt: string;
  ipAddress?: string;
}
```

---

## Reports Module

### GET /api/reports/attendance
**Purpose**: Generate attendance report  
**Query Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| startDate | string | Yes | ISO date |
| endDate | string | Yes | ISO date |
| reportType | string | Yes | individual, department |
| departmentIds | string[] | No | Department IDs |

**Response**:
```typescript
interface AttendanceReportResponse {
  items: ReportResult[];
}

interface ReportResult {
  employeeId: string;
  employeeName: string;
  department: string;
  totalDays: number;
  presentDays: number;
  lateDays: number;
  absentDays: number;
  exceptionsCount: number;
  lostMinutes: number;
  attendancePercentage: number;
}
```

---

### GET /api/reports/export-csv
**Purpose**: Export report as CSV  
**Query Parameters**: Same as /api/reports/attendance  
**Response**: CSV file download

---

## Admin Module

### GET /api/users
**Purpose**: Retrieve user list  
**Query Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| search | string | No | Search term |
| role | string | No | Filter by role |

**Response**:
```typescript
interface UsersResponse {
  items: UserItem[];
}

interface UserItem {
  id: string;
  employeeId: string;
  username: string;
  displayName: string;
  role: string;
  isActive: boolean;
  lastLoginAt?: string;
}
```

---

### PUT /api/users/:id/toggle
**Purpose**: Enable/disable user  
**Path Parameters**:
- `id`: User ID (required)

**Request**:
```typescript
interface ToggleUserRequest {
  isActive: boolean;
}
```

---

### POST /api/users/register
**Purpose**: Register new user  
**Request**:
```typescript
interface RegisterUserRequest {
  employeeId: string;
  username: string;
  password: string;
  role: string;
}
```

---

## Contract Validation Checklist

Before implementation, verify:
- [ ] All TypeScript interfaces match backend DTOs
- [ ] All required fields are marked as required
- [ ] All optional fields are marked as optional
- [ ] All enum values match backend enums
- [ ] All date formats are ISO 8601
- [ ] All pagination follows consistent pattern
- [ ] All error responses follow consistent format
- [ ] All authorization requirements are documented
- [ ] All department scope requirements are documented

---

**Document Status**: Complete  
**Next Step**: Use this map to implement React Query hooks and API client calls
