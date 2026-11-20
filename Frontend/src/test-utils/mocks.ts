import type { User, Role, AttendanceRecord, Escalation, WarningLetter, AttendanceException, DepartmentKPI, TrendPoint, TopLateEntry } from '@/core/api/types'
import type { Escalation as EscalationType, EscalationStatus, EscalationLevel, EscalationDashboardStats } from '@/core/api/types/escalation'

// ─── Mock Users for Each Role ──────────────────────────────────────────────

export const mockSuperAdmin: User = {
  id: 'admin-1',
  email: 'admin@wacams.com',
  firstName: 'System',
  lastName: 'Administrator',
  displayName: 'System Administrator',
  employeeId: '900001',
  roleId: '1',
  roleName: 'SuperAdmin',
  departmentId: '1',
  departmentName: 'IT',
  managerId: null,
  isActive: true,
  lastLoginAt: '2026-11-13T08:00:00Z',
  createdAt: '2025-01-01T00:00:00Z',
  updatedAt: '2026-11-13T08:00:00Z',
}

export const mockSeniorManager: User = {
  id: 'mgr-1',
  email: 'george@wacams.com',
  firstName: 'George',
  lastName: 'Ntseke',
  displayName: 'George Ntseke',
  employeeId: '707995',
  roleId: '3',
  roleName: 'Manager',
  departmentId: '1',
  departmentName: 'Production',
  managerId: 'admin-1',
  isActive: true,
  lastLoginAt: '2026-11-13T07:30:00Z',
  createdAt: '2025-03-15T00:00:00Z',
  updatedAt: '2026-11-13T07:30:00Z',
}

export const mockTeamLeader: User = {
  id: 'tl-1',
  email: 'botlhale@wacams.com',
  firstName: 'Botlhale',
  lastName: 'Rakau',
  displayName: 'Botlhale Rakau',
  employeeId: '708102',
  roleId: '4',
  roleName: 'TeamLeader',
  departmentId: '1',
  departmentName: 'Production',
  managerId: 'mgr-1',
  isActive: true,
  lastLoginAt: '2026-11-13T06:00:00Z',
  createdAt: '2025-06-01T00:00:00Z',
  updatedAt: '2026-11-13T06:00:00Z',
}

export const mockOperator: User = {
  id: 'op-1',
  email: 'lesedi@wacams.com',
  firstName: 'Lesedi',
  lastName: 'Mashile',
  displayName: 'Lesedi Mashile',
  employeeId: '708513',
  roleId: '5',
  roleName: 'Operator',
  departmentId: '1',
  departmentName: 'Production',
  managerId: 'mgr-1',
  isActive: true,
  lastLoginAt: '2026-11-13T05:45:00Z',
  createdAt: '2025-09-01T00:00:00Z',
  updatedAt: '2026-11-13T05:45:00Z',
}

export const mockQualityTechnician: User = {
  id: 'qt-1',
  email: 'quality@wacams.com',
  firstName: 'Quality',
  lastName: 'Technician',
  displayName: 'Quality Technician',
  employeeId: '708200',
  roleId: '2',
  roleName: 'Operator',
  departmentId: '2',
  departmentName: 'Quality',
  managerId: 'mgr-2',
  isActive: true,
  lastLoginAt: '2026-11-13T07:00:00Z',
  createdAt: '2025-04-01T00:00:00Z',
  updatedAt: '2026-11-13T07:00:00Z',
}

// ─── Mock Attendance Records ───────────────────────────────────────────────

export function createMockAttendanceRecord(overrides?: Partial<AttendanceRecord>): AttendanceRecord {
  return {
    id: `rec-${Date.now()}`,
    employeeId: '708513',
    employeeName: 'Lesedi Mashile',
    displayName: 'Lesedi Mashile',
    departmentId: '1',
    departmentName: 'Production',
    date: new Date().toISOString().split('T')[0],
    scheduledShift: 'ES',
    shiftStartTime: '06:00',
    shiftEndTime: '14:00',
    clockInTime: '05:55',
    clockOutTime: '14:10',
    status: 'Present',
    lateMinutes: 0,
    earlyMinutes: 0,
    totalHours: 8.25,
    notes: null,
    correctedBy: null,
    correctionReason: null,
    ...overrides,
  }
}

export const mockAttendanceRecords: AttendanceRecord[] = [
  createMockAttendanceRecord({
    id: 'rec-1', employeeId: '708513', displayName: 'Lesedi Mashile', status: 'Present',
    clockInTime: '05:55', clockOutTime: '14:10', lateMinutes: 0, earlyMinutes: 0,
  }),
  createMockAttendanceRecord({
    id: 'rec-2', employeeId: '707995', displayName: 'Thabo Mokoena', status: 'Late',
    clockInTime: '06:35', clockOutTime: '14:05', lateMinutes: 35, earlyMinutes: 0,
  }),
  createMockAttendanceRecord({
    id: 'rec-3', employeeId: '708102', displayName: 'Nomsa Dlamini', status: 'EarlyDeparture',
    clockInTime: '05:50', clockOutTime: '13:20', lateMinutes: 0, earlyMinutes: 40,
  }),
  createMockAttendanceRecord({
    id: 'rec-4', employeeId: '707888', displayName: 'Sipho Ndlovu', status: 'Absent',
    clockInTime: null, clockOutTime: null, lateMinutes: 0, earlyMinutes: 0,
  }),
  createMockAttendanceRecord({
    id: 'rec-5', employeeId: '708234', displayName: 'Zanele Khumalo', status: 'AWOL',
    clockInTime: null, clockOutTime: null, lateMinutes: 0, earlyMinutes: 0,
  }),
]

// ─── Mock Exceptions ───────────────────────────────────────────────────────

export function createMockException(overrides?: Partial<AttendanceException>): AttendanceException {
  return {
    id: `exc-${Date.now()}`,
    employeeId: '708513',
    employeeName: 'Lesedi Mashile',
    displayName: 'Lesedi Mashile',
    departmentId: '1',
    departmentName: 'Production',
    exceptionType: 'LateArrival',
    date: new Date().toISOString().split('T')[0],
    description: 'Test exception',
    lostProductionMinutes: 15,
    status: 'Open',
    assignedTo: null,
    assignedToName: null,
    investigatorNotes: null,
    investigatedAt: null,
    investigatedBy: null,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    ...overrides,
  }
}

export const mockExceptions: AttendanceException[] = [
  createMockException({
    id: 'exc-1', employeeId: '708513', displayName: 'Lesedi Mashile',
    exceptionType: 'LateArrival', status: 'Open', lostProductionMinutes: 30,
  }),
  createMockException({
    id: 'exc-2', employeeId: '707995', displayName: 'Thabo Mokoena',
    exceptionType: 'MissingClockIn', status: 'UnderReview', lostProductionMinutes: 0,
    assignedTo: 'mgr-1', assignedToName: 'George Ntseke',
  }),
  createMockException({
    id: 'exc-3', employeeId: '708102', displayName: 'Nomsa Dlamini',
    exceptionType: 'EarlyDeparture', status: 'Investigated', lostProductionMinutes: 45,
    investigatorNotes: 'Verified with supervisor',
  }),
  createMockException({
    id: 'exc-4', employeeId: '707888', displayName: 'Sipho Ndlovu',
    exceptionType: 'AWOL', status: 'Escalated', lostProductionMinutes: 480,
  }),
]

// ─── Mock Warnings ─────────────────────────────────────────────────────────

export function createMockWarning(overrides?: Partial<WarningLetter>): WarningLetter {
  return {
    id: `warn-${Date.now()}`,
    employeeId: '708513',
    employeeName: 'Lesedi Mashile',
    displayName: 'Lesedi Mashile',
    departmentId: '1',
    departmentName: 'Production',
    warningType: 'VerbalWarning',
    relatedExceptionId: 'exc-1',
    title: 'Repeated Late Arrivals',
    description: 'Employee has been late multiple times.',
    severity: 2,
    signedFormUrl: null,
    signedBy: null,
    signedAt: null,
    issuedBy: 'mgr-1',
    issuedByName: 'George Ntseke',
    status: 'Issued',
    escalationLevel: 1,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    ...overrides,
  }
}

export const mockWarnings: WarningLetter[] = [
  createMockWarning({
    id: 'warn-1', employeeId: '708513', displayName: 'Lesedi Mashile',
    warningType: 'VerbalWarning', status: 'Issued', severity: 2,
  }),
  createMockWarning({
    id: 'warn-2', employeeId: '707995', displayName: 'Thabo Mokoena',
    warningType: 'WrittenWarning', status: 'Acknowledged', severity: 3,
    signedFormUrl: '/forms/warn-2.pdf', signedBy: 'mgr-1', signedAt: '2026-10-20T14:00:00Z',
  }),
  createMockWarning({
    id: 'warn-3', employeeId: '707888', displayName: 'Sipho Ndlovu',
    warningType: 'FinalWrittenWarning', status: 'Draft', severity: 4,
  }),
]

// ─── Mock Escalations (from types/escalation.ts) ───────────────────────────

export function createMockEscalation(overrides?: Partial<EscalationType>): EscalationType {
  return {
    id: `esc-${Date.now()}`,
    exceptionId: 'exc-1',
    employeeId: '708513',
    currentLevel: 'ManagerLevel1',
    assignedTo: 'George Ntseke',
    status: 'Pending',
    createdAt: new Date().toISOString(),
    nextEscalationTime: new Date(Date.now() + 86400000).toISOString(), // tomorrow
    documentUploaded: false,
    createdBy: 'system',
    ...overrides,
  }
}

export const mockEscalations: EscalationType[] = [
  createMockEscalation({
    id: 'esc-1', employeeId: '708513', currentLevel: 'ManagerLevel1',
    assignedTo: 'George Ntseke', status: 'Pending',
    nextEscalationTime: new Date(Date.now() + 86400000).toISOString(),
  }),
  createMockEscalation({
    id: 'esc-2', employeeId: '707995', currentLevel: 'ManagerLevel2',
    assignedTo: 'Jeremiah Bezuidenhout', status: 'Acknowledged',
    acknowledgedAt: new Date().toISOString(),
    nextEscalationTime: new Date(Date.now() + 172800000).toISOString(),
  }),
  createMockEscalation({
    id: 'esc-3', employeeId: '707888', currentLevel: 'ManagerLevel1',
    assignedTo: 'George Ntseke', status: 'Resolved',
    resolvedAt: new Date().toISOString(),
    nextEscalationTime: new Date().toISOString(),
  }),
]

export const mockEscalationStats: EscalationDashboardStats = {
  totalActive: 3,
  pendingCount: 1,
  acknowledgedCount: 1,
  overdueCount: 1,
  resolvedToday: 2,
  avgResolutionHours: 18.5,
  byLevel: {
    ManagerLevel1: 1,
    ManagerLevel2: 1,
    ManagerLevel3: 0,
    ManagerLevel4: 0,
    ManagerLevel5: 0,
    HR: 0,
    Admin: 0,
  },
}

// ─── Mock Dashboard Data ───────────────────────────────────────────────────

export const mockDepartments: DepartmentKPI[] = [
  { departmentId: '1', departmentName: 'Production', attendancePercentage: 94, totalEmployees: 126, presentCount: 118, lateCount: 5, absentCount: 3 },
  { departmentId: '2', departmentName: 'Quality', attendancePercentage: 91, totalEmployees: 24, presentCount: 22, lateCount: 1, absentCount: 1 },
  { departmentId: '3', departmentName: 'Engineering', attendancePercentage: 88, totalEmployees: 18, presentCount: 16, lateCount: 1, absentCount: 1 },
  { departmentId: '4', departmentName: 'Logistics', attendancePercentage: 86, totalEmployees: 30, presentCount: 26, lateCount: 3, absentCount: 1 },
  { departmentId: '5', departmentName: 'Office', attendancePercentage: 97, totalEmployees: 12, presentCount: 12, lateCount: 0, absentCount: 0 },
]

export const mockTrends: TrendPoint[] = Array.from({ length: 14 }, (_, i) => ({
  date: new Date(Date.now() - (13 - i) * 86400000).toLocaleDateString('en-US', { month: 'short', day: 'numeric' }),
  onTimePercentage: 85 + Math.random() * 12,
  lateCount: Math.floor(Math.random() * 15),
  exceptionCount: Math.floor(Math.random() * 8),
}))

export const mockTopLate: TopLateEntry[] = [
  { employeeId: '708513', displayName: 'Lesedi Mashile', departmentName: 'Production', lateCount: 8, totalLateMinutes: 240 },
  { employeeId: '707995', displayName: 'Thabo Mokoena', departmentName: 'Production', lateCount: 6, totalLateMinutes: 180 },
  { employeeId: '708102', displayName: 'Nomsa Dlamini', departmentName: 'Quality', lateCount: 5, totalLateMinutes: 120 },
]

// ─── API Response Wrappers ─────────────────────────────────────────────────

export function mockApiResponse<T>(data: T): { success: boolean; data: T } {
  return { success: true, data }
}

// ─── MSW Handlers Setup ────────────────────────────────────────────────────

export async function setupMockApi(handlers: any[]) {
  // This is a placeholder — MSW browser workers need additional setup
  // For unit tests we mock fetch/apiClient directly
}
