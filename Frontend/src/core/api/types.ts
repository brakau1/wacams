// ============================================================================
// WACAMS — Shared TypeScript Types matching backend DTOs
// ============================================================================

// ─── Auth Types ────────────────────────────────────────────────────────────

export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  accessToken: string
  refreshToken: string
  expiresIn: number
  user: User
}

export interface RefreshTokenRequest {
  refreshToken: string
}

export interface RegisterRequest {
  email: string
  password: string
  firstName: string
  lastName: string
  employeeId: string
  roleId: string
  departmentId: string
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

// ─── User / RBAC Types ─────────────────────────────────────────────────────

export type Role = 'SuperAdmin' | 'SeniorManager' | 'Manager' | 'Coordinator' | 'TeamLeader' | 'Operator'

export interface User {
  id: string
  email: string
  firstName: string
  lastName: string
  displayName: string
  employeeId: string
  roleId: string
  roleName: Role
  departmentId: string
  departmentName: string
  managerId: string | null
  isActive: boolean
  lastLoginAt: string | null
  createdAt: string
  updatedAt: string
}

export interface CreateUserDto {
  email: string
  password: string
  firstName: string
  lastName: string
  employeeId: string
  roleId: string
  departmentId: string
  managerId?: string | null
}

export interface UpdateUserDto {
  firstName?: string
  lastName?: string
  roleId?: string
  departmentId?: string
  managerId?: string | null
  isActive?: boolean
}

// ─── Department Types ──────────────────────────────────────────────────────

export interface Department {
  id: string
  name: string
  code: string
  parentId: string | null
  hasChildren: boolean
  attendanceTargetPercentage: number
  lateThresholdMinutes: number
  earlyDepartureThresholdMinutes: number
  gracePeriodMinutes: number
}

export interface CreateDepartmentDto {
  name: string
  code: string
  parentId?: string | null
  attendanceTargetPercentage?: number
  lateThresholdMinutes?: number
  earlyDepartureThresholdMinutes?: number
  gracePeriodMinutes?: number
}

export interface UpdateDepartmentDto {
  name?: string
  attendanceTargetPercentage?: number
  lateThresholdMinutes?: number
  earlyDepartureThresholdMinutes?: number
  gracePeriodMinutes?: number
}

// ─── Employee Types ────────────────────────────────────────────────────────

export type EmployeeStatus = 'Active' | 'Inactive' | 'OnLeave' | 'Terminated'

export interface Employee {
  id: string
  employeeId: string
  firstName: string
  lastName: string
  displayName: string
  email: string
  departmentId: string
  departmentName: string
  managerId: string | null
  managerName: string | null
  jobTitle: string | null
  status: EmployeeStatus
  hireDate: string
  shiftCode: string
  isClockIn: boolean
  clockInTime: string | null
  clockOutTime: string | null
  createdAt: string
  updatedAt: string
}

export interface BulkUploadResult {
  totalRows: number
  successCount: number
  failureCount: number
  errors: string[]
}

// ─── Attendance Types ──────────────────────────────────────────────────────

export type AttendanceStatus = 'Present' | 'Late' | 'EarlyDeparture' | 'Absent' | 'AWOL' | 'SickLeave' | 'FamilyResponsibilityLeave' | 'AnnualLeave' | 'Rest' | 'UnscheduledClockIn' | 'MissingClockIn' | 'MissingClockOut'

export interface AttendanceRecord {
  id: string
  employeeId: string
  employeeName: string
  displayName: string
  departmentId: string
  departmentName: string
  date: string
  scheduledShift: string
  shiftStartTime: string
  shiftEndTime: string
  clockInTime: string | null
  clockOutTime: string | null
  status: AttendanceStatus
  lateMinutes: number
  earlyMinutes: number
  totalHours: number
  notes: string | null
  correctedBy: string | null
  correctionReason: string | null
}

export interface ClockInRequest {
  employeeId: string
  timestamp?: string
  location?: { latitude: number; longitude: number }
}

export interface ClockOutRequest {
  employeeId: string
  timestamp?: string
}

// ─── Exception Types ───────────────────────────────────────────────────────

export type ExceptionType =
  | 'LateArrival'
  | 'EarlyDeparture'
  | 'AttendanceException'
  | 'UnplannedAbsenteeism'
  | 'AWOL'
  | 'SickLeave'
  | 'FamilyResponsibilityLeave'
  | 'ShiftCancellation'
  | 'WorkshopAttendance'
  | 'Training'
  | 'MissingClockIn'
  | 'MissingClockOut'
  | 'UnscheduledClockIn'
  | 'UnscheduledClockOut'

export type ExceptionStatus = 'Open' | 'UnderReview' | 'Investigated' | 'Closed' | 'Escalated'

export interface AttendanceException {
  id: string
  employeeId: string
  employeeName: string
  displayName: string
  departmentId: string
  departmentName: string
  exceptionType: ExceptionType
  date: string
  description: string
  lostProductionMinutes: number
  status: ExceptionStatus
  assignedTo: string | null
  assignedToName: string | null
  investigatorNotes: string | null
  investigatedAt: string | null
  investigatedBy: string | null
  createdAt: string
  updatedAt: string
}

export interface InvestigateExceptionDto {
  notes: string
  actionTaken: string
  resolveType: string
}

// ─── Warning Types ─────────────────────────────────────────────────────────

export type WarningType = 'Counselling' | 'VerbalWarning' | 'WrittenWarning' | 'FinalWrittenWarning'

export interface WarningLetter {
  id: string
  employeeId: string
  employeeName: string
  displayName: string
  departmentId: string
  departmentName: string
  warningType: WarningType
  relatedExceptionId: string | null
  title: string
  description: string
  severity: number
  signedFormUrl: string | null
  signedBy: string | null
  signedAt: string | null
  issuedBy: string | null
  issuedByName: string | null
  status: 'Draft' | 'Issued' | 'Acknowledged' | 'Archived'
  escalationLevel: number
  createdAt: string
  updatedAt: string
}

export interface GenerateWarningDto {
  employeeId: string
  exceptionIds: string[]
  warningType: WarningType
  title: string
  description: string
}

// ─── Escalation Types ──────────────────────────────────────────────────────

export type EscalationStatus = 'Pending' | 'InProgress' | 'Completed' | 'Expired'

export interface Escalation {
  id: string
  relatedExceptionId: string | null
  relatedWarningId: string | null
  fromEmployeeId: string | null
  fromEmployeeName: string | null
  toEmployeeId: string
  toEmployeeName: string
  toRole: string
  escalationLevel: number
  status: EscalationStatus
  message: string
  respondedAt: string | null
  respondedBy: string | null
  responseNotes: string | null
  deadline: string
  completedAt: string | null
  createdAt: string
}

// ─── Report Types ──────────────────────────────────────────────────────────

export interface AttendanceSummary {
  departmentId: string
  departmentName: string
  totalEmployees: number
  presentCount: number
  lateCount: number
  absentCount: number
  awolCount: number
  sickLeaveCount: number
  familyResponsibilityCount: number
  annualLeaveCount: number
  attendancePercentage: number
  totalLateMinutes: number
  totalEarlyMinutes: number
  totalLostMinutes: number
}

export interface ReportFilter {
  startDate: string
  endDate: string
  departmentIds?: string[]
  employeeIds?: string[]
  status?: AttendanceStatus[]
  exceptionTypes?: ExceptionType[]
}

// ─── Audit Log Types ───────────────────────────────────────────────────────

export type AuditAction = 'CREATE' | 'UPDATE' | 'DELETE' | 'LOGIN' | 'LOGOUT' | 'CLOCK_IN' | 'CLOCK_OUT' | 'INVESTIGATE' | 'GENERATE_WARNING' | 'ESCALATE' | 'UPLOAD_FORM'

export interface AuditLog {
  id: string
  entity_type: string
  entity_id: string
  action: AuditAction
  summary: string
  oldValue: Record<string, unknown> | null
  newValue: Record<string, unknown> | null
  performed_by: string
  performed_by_name: string
  ip_address: string | null
  user_agent: string | null
  created_at: string
}

// ─── System Settings Types ─────────────────────────────────────────────────

export interface SystemSettings {
  id: string
  key: string
  value: string
  category: string
  description: string
  updatedAt: string
  updatedBy: string
}

// ─── Dashboard KPI Types ───────────────────────────────────────────────────

export interface DashboardKPI {
  totalEmployees: number
  onTimePercentage: number
  averageLateMinutes: number
  activeExceptions: number
  pendingWarnings: number
  departmentStats: DepartmentKPI[]
  trendData: TrendPoint[]
  topLateArrivals: TopLateEntry[]
  recentExceptions: AttendanceException[]
}

export interface DepartmentKPI {
  departmentId: string
  departmentName: string
  attendancePercentage: number
  totalEmployees: number
  presentCount: number
  lateCount: number
  absentCount: number
}

export interface TrendPoint {
  date: string
  onTimePercentage: number
  lateCount: number
  exceptionCount: number
}

export interface TopLateEntry {
  employeeId: string
  displayName: string
  departmentName: string
  lateCount: number
  totalLateMinutes: number
}

// ─── Pagination / Response Wrappers ────────────────────────────────────────

export interface PaginatedResponse<T> {
  items: T[]
  totalItems: number
  currentPage: number
  pageSize: number
  totalPages: number
}

export interface ApiResponse<T> {
  success: boolean
  data: T
  message?: string
  error?: string
}

export interface ApiError {
  statusCode: number
  message: string
  errors?: string[]
}

// ─── HR Command Centre Types ───────────────────────────────────────────────

export interface HRCommandCentreKPIs {
  plantAttendancePercentage: number
  lostProductionHours: number
  openWarnings: number
  finalWarnings: number
  awolCases: number
  escalations: number
  departmentsAtRisk: number
}

export interface DepartmentRanking {
  departmentId: string
  departmentName: string
  attendancePercentage: number
  totalEmployees: number
  presentCount: number
  lateCount: number
  absentCount: number
  exceptionCount: number
  riskLevel: 'low' | 'medium' | 'high' | 'critical'
}

export interface ExceptionAnalysis {
  exceptionType: string
  count: number
  percentage: number
  lostMinutes: number
}

export interface HRCommandCentreData {
  kpis: HRCommandCentreKPIs
  attendanceTrend: TrendPoint[]
  departmentRanking: DepartmentRanking[]
  topOffenders: TopLateEntry[]
  exceptionAnalysis: ExceptionAnalysis[]
}

// ─── Executive Dashboard Types ─────────────────────────────────────────────

export interface ExecutiveSummary {
  employeesPresent: number
  currentAbsenteeism: number
  attendancePercentage: number
  lostMinutes: number
  lostHours: number
  openEscalations: number
  pendingInvestigations: number
  correctiveActions: number
  totalActiveEmployees: number
  asOf: string
}

export interface DepartmentHeatmap {
  departmentId: string
  departmentName: string
  departmentCode: string
  totalEmployees: number
  presentCount: number
  lateCount: number
  absentCount: number
  lostMinutes: number
  attendancePercentage: number
  targetPercentage: number
  gapToTarget: number
  heatLevel: 'green' | 'yellow' | 'orange' | 'red'
}

export interface DepartmentLeaderboard {
  departmentId: string
  departmentName: string
  departmentCode: string
  totalEmployees: number
  attendancePercentage: number
  targetPercentage: number
  totalLostHours: number
  exceptionsCount: number
  trend: 'up' | 'down' | 'stable'
  rank: number
}
