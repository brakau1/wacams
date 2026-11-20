/**
 * WACAMS — API endpoint constants
 */
export const API_BASE = '/api'

export const ENDPOINTS = {
  // ─── Auth ──────────────────────────────────────────────────────────────
  auth: {
    login: `${API_BASE}/auth/login`,
    logout: `${API_BASE}/auth/logout`,
    refreshToken: `${API_BASE}/auth/refresh`,
    register: `${API_BASE}/auth/register`,
    changePassword: `${API_BASE}/auth/change-password`,
    me: `${API_BASE}/auth/me`
  },

  // ─── Users ─────────────────────────────────────────────────────────────
  users: {
    list: `${API_BASE}/users`,
    getById: (id: string) => `${API_BASE}/users/${id}`,
    create: `${API_BASE}/users`,
    update: (id: string) => `${API_BASE}/users/${id}`,
    disable: (id: string) => `${API_BASE}/users/${id}/disable`,
    resetPassword: (id: string) => `${API_BASE}/users/${id}/reset-password`,
    search: `${API_BASE}/users/search`
  },

  // ─── Departments ───────────────────────────────────────────────────────
  departments: {
    list: `${API_BASE}/departments`,
    getById: (id: string) => `${API_BASE}/departments/${id}`,
    create: `${API_BASE}/departments`,
    update: (id: string) => `${API_BASE}/departments/${id}`,
    delete: (id: string) => `${API_BASE}/departments/${id}`
  },

  // ─── Employees ─────────────────────────────────────────────────────────
  employees: {
    list: `${API_BASE}/employees`,
    getById: (id: string) => `${API_BASE}/employees/${id}`,
    getByEmployeeId: (employeeId: string) => `${API_BASE}/employees/by-id/${employeeId}`,
    bulkUpload: `${API_BASE}/employees/bulk-upload`,
    getHierarchy: (departmentId?: string) => `${API_BASE}/employees/hierarchy${departmentId ? `?departmentId=${departmentId}` : ''}`
  },

  // ─── Attendance ────────────────────────────────────────────────────────
  attendance: {
    list: `${API_BASE}/attendance`,
    getByDate: (date: string, departmentId?: string) =>
      `${API_BASE}/attendance?date=${date}${departmentId ? `&departmentId=${departmentId}` : ''}`,
    clockIn: `${API_BASE}/attendance/clock-in`,
    clockOut: `${API_BASE}/attendance/clock-out`,
    correction: `${API_BASE}/attendance/correction`,
    summary: `${API_BASE}/attendance/summary`,
    employeeHistory: (employeeId: string) => `${API_BASE}/attendance/${employeeId}/history`,
    monthlyCalendar: (employeeId: string, year: number, month: number) =>
      `${API_BASE}/attendance/${employeeId}/calendar?year=${year}&month=${month}`
  },

  // ─── Exceptions ────────────────────────────────────────────────────────
  exceptions: {
    list: `${API_BASE}/exceptions`,
    getById: (id: string) => `${API_BASE}/exceptions/${id}`,
    getByEmployee: (employeeId: string) => `${API_BASE}/exceptions/employee/${employeeId}`,
    investigate: (id: string) => `${API_BASE}/exceptions/${id}/investigate`,
    bulkInvestigate: `${API_BASE}/exceptions/bulk-investigate`,
    getOpen: `${API_BASE}/exceptions/open`,
    escalate: (id: string) => `${API_BASE}/exceptions/${id}/escalate`
  },

  // ─── Warnings ──────────────────────────────────────────────────────────
  warnings: {
    list: `${API_BASE}/warnings`,
    getById: (id: string) => `${API_BASE}/warnings/${id}`,
    getByEmployee: (employeeId: string) => `${API_BASE}/warnings/employee/${employeeId}`,
    generate: `${API_BASE}/warnings/generate`,
    uploadForm: (id: string) => `${API_BASE}/warnings/${id}/upload-form`,
    acknowledge: (id: string) => `${API_BASE}/warnings/${id}/acknowledge`,
    thresholds: (employeeId: string) => `${API_BASE}/warnings/employee/${employeeId}/thresholds`
  },

  // ─── Escalations ───────────────────────────────────────────────────────
  escalations: {
    list: `${API_BASE}/escalations`,
    getById: (id: string) => `${API_BASE}/escalations/${id}`,
    getByEmployee: (employeeId: string) => `${API_BASE}/escalations/employee/${employeeId}`,
    respond: (id: string) => `${API_BASE}/escalations/${id}/respond`
  },

  // ─── Executive Dashboard ─────────────────────────────────────────────────
  executiveDashboard: {
    summary: `${API_BASE}/executive-dashboard/summary`,
    heatmap: `${API_BASE}/executive-dashboard/heatmap`,
    leaderboard: `${API_BASE}/executive-dashboard/leaderboard`
  },

  // ─── Reports ───────────────────────────────────────────────────────────
  reports: {
    attendance: `${API_BASE}/reports/attendance`,
    exception: `${API_BASE}/reports/exceptions`,
    warning: `${API_BASE}/reports/warnings`,
    dashboardKPI: `${API_BASE}/reports/dashboard-kpi`,
    managerDashboard: `${API_BASE}/reports/manager-dashboard`,
    hrCommandCentre: `${API_BASE}/reports/hr-command-centre`,
    exportCSV: `${API_BASE}/reports/export-csv`
  },

  // ─── Audit Log ─────────────────────────────────────────────────────────
  audit: {
    list: `${API_BASE}/audit-logs`,
    getById: (id: string) => `${API_BASE}/audit-logs/${id}`,
    filter: `${API_BASE}/audit-logs/filter`
  },

  // ─── System Settings ───────────────────────────────────────────────────
  settings: {
    list: `${API_BASE}/settings`,
    getByKey: (key: string) => `${API_BASE}/settings/key/${key}`,
    update: (key: string) => `${API_BASE}/settings/key/${key}`
  }
} as const

// Helper to convert endpoint path to query params for filtering
export function buildQueryParams(params: Record<string, string | number | boolean>): string {
  return Object.entries(params)
    .filter(([, v]) => v !== undefined && v !== null && v !== '')
    .map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(String(v))}`)
    .join('&')
}
