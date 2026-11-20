import { useQuery } from '@tanstack/react-query'
import { get } from '@/core/api/apiClient'
import { ENDPOINTS } from '@/core/api/endpoints'
import type {
  DepartmentKPI,
  TrendPoint,
  TopLateEntry,
  AttendanceException,
  AttendanceSummary,
  HRCommandCentreData,
  ExecutiveSummary,
  DepartmentHeatmap,
  DepartmentLeaderboard,
  AttendanceRecord,
  WarningLetter,
  Employee
} from '@/core/api/types'

// ─── Query Keys ──────────────────────────────────────────────────────────────

const dashboardBase = ['dashboard'] as const
const executiveBase = [...dashboardBase, 'executive'] as const

export const dashboardKeys = {
  all: dashboardBase,
  kpi: () => [...dashboardBase, 'kpi'] as const,
  manager: () => [...dashboardBase, 'manager'] as const,
  hrCommandCentre: () => [...dashboardBase, 'hr-command-centre'] as const,
  executive: {
    all: executiveBase,
    summary: () => [...executiveBase, 'summary'] as const,
    heatmap: () => [...executiveBase, 'heatmap'] as const,
    leaderboard: () => [...executiveBase, 'leaderboard'] as const,
  },
}

// ─── Dashboard KPI Hook ──────────────────────────────────────────────────────

export interface DashboardKPIData {
  kpi: DepartmentKPI[]
  trends: TrendPoint[]
  topLate: TopLateEntry[]
  exceptions: AttendanceException[]
}

export function useDashboardKPI() {
  return useQuery<DashboardKPIData>({
    queryKey: dashboardKeys.kpi(),
    queryFn: async () => {
      const response = await get<DashboardKPIData>(ENDPOINTS.reports.dashboardKPI)
      return response.data ?? response as unknown as DashboardKPIData
    },
    staleTime: 60_000, // 1 minute
    refetchInterval: 60_000, // Auto-refresh every 60s
  })
}

// ─── Manager Dashboard Hook ──────────────────────────────────────────────────

export interface ManagerDashboardData {
  summary: AttendanceSummary
  teamSummary: AttendanceSummary
  departments: DepartmentKPI[]
  trends: TrendPoint[]
  topOffenders: TopLateEntry[]
  exceptions: AttendanceException[]
}

export function useManagerDashboard() {
  return useQuery<ManagerDashboardData>({
    queryKey: dashboardKeys.manager(),
    queryFn: async () => {
      const response = await get<ManagerDashboardData>(ENDPOINTS.reports.managerDashboard)
      return response.data ?? response as unknown as ManagerDashboardData
    },
    staleTime: 60_000,
    refetchInterval: 60_000,
  })
}

// ─── HR Command Centre Hook ──────────────────────────────────────────────────

export function useHRCommandCentre() {
  return useQuery<HRCommandCentreData>({
    queryKey: dashboardKeys.hrCommandCentre(),
    queryFn: async () => {
      const response = await get<HRCommandCentreData>(ENDPOINTS.reports.hrCommandCentre)
      return response.data ?? response as unknown as HRCommandCentreData
    },
    staleTime: 60_000,
    refetchInterval: 60_000,
  })
}

// ─── Executive Dashboard Hooks ───────────────────────────────────────────────

export function useExecutiveSummary() {
  return useQuery<ExecutiveSummary>({
    queryKey: dashboardKeys.executive.summary(),
    queryFn: async () => {
      const response = await get<ExecutiveSummary>(ENDPOINTS.executiveDashboard.summary)
      return response.data ?? response as unknown as ExecutiveSummary
    },
    staleTime: 30_000, // 30s — more frequent for exec view
    refetchInterval: 30_000,
  })
}

export function useExecutiveHeatmap() {
  return useQuery<DepartmentHeatmap[]>({
    queryKey: dashboardKeys.executive.heatmap(),
    queryFn: async () => {
      const response = await get<DepartmentHeatmap[]>(ENDPOINTS.executiveDashboard.heatmap)
      return response.data ?? response as unknown as DepartmentHeatmap[]
    },
    staleTime: 30_000,
    refetchInterval: 30_000,
  })
}

export function useExecutiveLeaderboard() {
  return useQuery<DepartmentLeaderboard[]>({
    queryKey: dashboardKeys.executive.leaderboard(),
    queryFn: async () => {
      const response = await get<DepartmentLeaderboard[]>(ENDPOINTS.executiveDashboard.leaderboard)
      return response.data ?? response as unknown as DepartmentLeaderboard[]
    },
    staleTime: 30_000,
    refetchInterval: 30_000,
  })
}

// ─── Attendance Hooks ────────────────────────────────────────────────────────

const attendanceBase = ['attendance'] as const

export const attendanceKeys = {
  all: attendanceBase,
  list: (date?: string, departmentId?: string) => [...attendanceBase, 'list', { date, departmentId }] as const,
  byId: (id: string) => [...attendanceBase, id] as const,
  summary: (date?: string) => [...attendanceBase, 'summary', { date }] as const,
  employeeHistory: (employeeId: string) => [...attendanceBase, 'employee', employeeId, 'history'] as const,
}

export function useAttendanceList(date?: string, departmentId?: string) {
  return useQuery<AttendanceRecord[]>({
    queryKey: attendanceKeys.list(date, departmentId),
    queryFn: async () => {
      const url = ENDPOINTS.attendance.getByDate(date || new Date().toISOString().split('T')[0], departmentId)
      const response = await get<AttendanceRecord[]>(url)
      return response.data ?? response as unknown as AttendanceRecord[]
    },
    staleTime: 30_000,
    refetchInterval: 60_000,
  })
}

// ─── Exceptions Hooks ────────────────────────────────────────────────────────

const exceptionsBase = ['exceptions'] as const

export const exceptionKeys = {
  all: exceptionsBase,
  list: (status?: string) => [...exceptionsBase, 'list', { status }] as const,
  byId: (id: string) => [...exceptionsBase, id] as const,
  open: () => [...exceptionsBase, 'open'] as const,
}

export function useExceptions(status?: string) {
  return useQuery<AttendanceException[]>({
    queryKey: exceptionKeys.list(status),
    queryFn: async () => {
      const url = status && status !== 'all' 
        ? `${ENDPOINTS.exceptions.list}?status=${status}`
        : ENDPOINTS.exceptions.list
      const response = await get<AttendanceException[]>(url)
      return response.data ?? response as unknown as AttendanceException[]
    },
    staleTime: 30_000,
    refetchInterval: 60_000,
  })
}

// ─── Warnings Hooks ──────────────────────────────────────────────────────────

const warningsBase = ['warnings'] as const

export const warningKeys = {
  all: warningsBase,
  list: (status?: string) => [...warningsBase, 'list', { status }] as const,
  byId: (id: string) => [...warningsBase, id] as const,
  byEmployee: (employeeId: string) => [...warningsBase, 'employee', employeeId] as const,
}

export function useWarnings(status?: string) {
  return useQuery<WarningLetter[]>({
    queryKey: warningKeys.list(status),
    queryFn: async () => {
      const url = status && status !== 'all'
        ? `${ENDPOINTS.warnings.list}?status=${status}`
        : ENDPOINTS.warnings.list
      const response = await get<WarningLetter[]>(url)
      return response.data ?? response as unknown as WarningLetter[]
    },
    staleTime: 30_000,
  })
}

// ─── Employees Hooks ─────────────────────────────────────────────────────────

const employeesBase = ['employees'] as const

export const employeeKeys = {
  all: employeesBase,
  list: () => [...employeesBase, 'list'] as const,
  byId: (id: string) => [...employeesBase, id] as const,
  byEmployeeId: (employeeId: string) => [...employeesBase, 'employeeId', employeeId] as const,
}

export function useEmployees() {
  return useQuery<Employee[]>({
    queryKey: employeeKeys.list(),
    queryFn: async () => {
      const response = await get<{ items: Employee[] }>(ENDPOINTS.employees.list)
      const data = response.data ?? response
      return (data as any).items ?? data ?? []
    },
    staleTime: 60_000,
  })
}

export function useEmployee(id: string) {
  return useQuery<Employee>({
    queryKey: employeeKeys.byId(id),
    queryFn: async () => {
      const response = await get<Employee>(ENDPOINTS.employees.getById(id))
      return response.data ?? response as unknown as Employee
    },
    enabled: !!id,
    staleTime: 60_000,
  })
}

export function useEmployeeExceptions(employeeId: string) {
  return useQuery<AttendanceException[]>({
    queryKey: [...exceptionKeys.all, 'employee', employeeId],
    queryFn: async () => {
      const response = await get<AttendanceException[]>(ENDPOINTS.exceptions.getByEmployee(employeeId))
      return response.data ?? response as unknown as AttendanceException[]
    },
    enabled: !!employeeId,
    staleTime: 30_000,
  })
}

export function useEmployeeWarnings(employeeId: string) {
  return useQuery<WarningLetter[]>({
    queryKey: [...warningKeys.all, 'employee', employeeId],
    queryFn: async () => {
      const response = await get<WarningLetter[]>(ENDPOINTS.warnings.getByEmployee(employeeId))
      return response.data ?? response as unknown as WarningLetter[]
    },
    enabled: !!employeeId,
    staleTime: 30_000,
  })
}

export function useWarning(id: string) {
  return useQuery<WarningLetter>({
    queryKey: warningKeys.byId(id),
    queryFn: async () => {
      const response = await get<WarningLetter>(ENDPOINTS.warnings.getById(id))
      return response.data ?? response as unknown as WarningLetter
    },
    enabled: !!id,
    staleTime: 30_000,
  })
}
