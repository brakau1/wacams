// Mock API handler for development mode
import type { AxiosRequestConfig, AxiosResponse } from 'axios'
import {
  mockDepartments,
  mockEmployees,
  mockAttendanceRecords,
  mockExceptions,
  mockWarnings,
  mockEscalations,
  mockAuditLogs,
  mockDashboardKPI,
  mockAttendanceSummary,
  mockHRCommandCentreData,
  mockExecutiveSummary,
  mockDepartmentHeatmap,
  mockDepartmentLeaderboard,
  mockSystemSettings,
  mockGISEmployees,
  mockGISTransactions,
  mockGISSyncResults,
  mockGISExceptionResults
} from './mockData'

const delay = (ms: number) => new Promise(resolve => setTimeout(resolve, ms))

interface MockResponse {
  success: boolean
  data: any
  message?: string
}

export async function handleMockRequest(config: AxiosRequestConfig): Promise<MockResponse> {
  await delay(100 + Math.random() * 200) // Simulate network latency
  
  const url = config.url || ''
  const method = (config.method || 'get').toLowerCase()
  
  // Auth endpoints
  if (url.includes('/auth/login')) {
    return {
      success: true,
      data: {
        accessToken: 'mock-jwt-token-' + Date.now(),
        refreshToken: 'mock-refresh-token-' + Date.now(),
        user: {
          id: '1',
          email: 'dev@wacams.local',
          firstName: 'Development',
          lastName: 'User',
          displayName: 'Development User',
          employeeId: 'DEV001',
          roleId: '1',
          roleName: 'SuperAdmin',
          departmentId: '5',
          departmentName: 'Administration',
          managerId: null,
          isActive: true,
          lastLoginAt: new Date().toISOString(),
          createdAt: '2024-01-01T00:00:00Z',
          updatedAt: new Date().toISOString()
        }
      }
    }
  }
  
  if (url.includes('/auth/me')) {
    return {
      success: true,
      data: {
        id: '1',
        email: 'dev@wacams.local',
        firstName: 'Development',
        lastName: 'User',
        displayName: 'Development User',
        employeeId: 'DEV001',
        roleId: '1',
        roleName: 'SuperAdmin',
        departmentId: '5',
        departmentName: 'Administration',
        managerId: null,
        isActive: true,
        lastLoginAt: new Date().toISOString(),
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: new Date().toISOString()
      }
    }
  }
  
  // Departments
  if (url.includes('/departments') && method === 'get') {
    return { success: true, data: mockDepartments }
  }
  
  // Employees
  if (url.includes('/employees') && method === 'get') {
    if (url.includes('/hierarchy')) {
      const supervisors = mockEmployees.filter(e => e.managerId === null)
      const subordinates = mockEmployees.filter(e => e.managerId !== null)
      return { success: true, data: { supervisors, subordinates } }
    }
    return { success: true, data: { items: mockEmployees, totalItems: mockEmployees.length } }
  }
  
  // Attendance
  if (url.includes('/attendance')) {
    if (url.includes('/summary')) {
      return { success: true, data: mockAttendanceSummary }
    }
    if (url.includes('/history')) {
      return { success: true, data: mockAttendanceRecords.slice(0, 50) }
    }
    return { success: true, data: mockAttendanceRecords }
  }
  
  // Exceptions
  if (url.includes('/exceptions')) {
    if (url.includes('/open')) {
      return { success: true, data: mockExceptions.filter(e => e.status === 'Open') }
    }
    return { success: true, data: mockExceptions }
  }
  
  // Warnings
  if (url.includes('/warnings')) {
    if (url.includes('/thresholds')) {
      return {
        success: true,
        data: {
          counselling: 3,
          verbalWarning: 5,
          writtenWarning: 8,
          finalWrittenWarning: 12
        }
      }
    }
    return { success: true, data: mockWarnings }
  }
  
  // Escalations
  if (url.includes('/escalations')) {
    return { success: true, data: mockEscalations }
  }
  
  // Audit logs
  if (url.includes('/audit-logs')) {
    return { success: true, data: { items: mockAuditLogs, totalItems: mockAuditLogs.length } }
  }
  
  // Reports
  if (url.includes('/reports/dashboard-kpi')) {
    return {
      success: true,
      data: {
        kpi: mockDashboardKPI.departmentStats,
        trends: mockDashboardKPI.trendData,
        topLate: mockDashboardKPI.topLateArrivals,
        exceptions: mockDashboardKPI.recentExceptions
      }
    }
  }
  
  if (url.includes('/reports/manager-dashboard')) {
    return {
      success: true,
      data: {
        summary: mockAttendanceSummary,
        teamSummary: mockAttendanceSummary,
        departments: mockDashboardKPI.departmentStats,
        trends: mockDashboardKPI.trendData,
        topOffenders: mockDashboardKPI.topLateArrivals,
        exceptions: mockExceptions
      }
    }
  }
  
  if (url.includes('/reports/hr-command-centre')) {
    return { success: true, data: mockHRCommandCentreData }
  }
  
  // Executive dashboard
  if (url.includes('/executive-dashboard/summary')) {
    return { success: true, data: mockExecutiveSummary }
  }
  
  if (url.includes('/executive-dashboard/heatmap')) {
    return { success: true, data: mockDepartmentHeatmap }
  }
  
  if (url.includes('/executive-dashboard/leaderboard')) {
    return { success: true, data: mockDepartmentLeaderboard }
  }
  
  // System settings
  if (url.includes('/settings')) {
    return { success: true, data: mockSystemSettings }
  }
  
  // GIS integration
  if (url.includes('/gis/employees')) {
    return { success: true, data: mockGISEmployees }
  }
  
  if (url.includes('/gis/transactions')) {
    return { success: true, data: mockGISTransactions }
  }
  
  if (url.includes('/gis/sync-results')) {
    return { success: true, data: mockGISSyncResults }
  }
  
  if (url.includes('/gis/exception-results')) {
    return { success: true, data: mockGISExceptionResults }
  }
  
  // Default response
  return { success: true, data: [] }
}

export function isMockMode(): boolean {
  return import.meta.env.VITE_MOCK_MODE === 'true'
}
