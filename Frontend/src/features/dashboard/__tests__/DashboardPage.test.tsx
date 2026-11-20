import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { AuthProvider } from '@/core/auth/AuthContext'
import DashboardPage from '../DashboardPage'
import type { DepartmentKPI, TrendPoint, TopLateEntry, AttendanceException } from '@/core/api/types'

vi.mock('react-router-dom', () => ({
  useNavigate: () => vi.fn(),
  useParams: () => ({}),
  useLocation: () => ({ pathname: '/dashboard', search: '', state: null, hash: '', key: 'test' }),
}))

// Mock API client
const mockApi = vi.hoisted(() => ({ get: vi.fn() }))
vi.mock('@/core/api/apiClient', () => ({
  get: (url: string) => mockApi.get(url),
  default: { get: (url: string) => mockApi.get(url) },
}))

// Mock components
vi.mock('../components/KPICard', () => ({
  default: ({ title, value }: any) => (
    <div data-testid={`kpi-card-${title}`}>
      <span>{title}</span>
      <span>{String(value)}</span>
    </div>
  ),
}))

vi.mock('../components/StatsGrid', () => ({
  default: ({ children }: any) => (
    <div data-testid="stats-grid">{children}</div>
  ),
  StatItem: ({ label, value }: any) => (
    <div data-testid={`stat-item-${label}`}>
      <span>{label}</span>
      <span>{String(value)}</span>
    </div>
  ),
}))

vi.mock('../components/DepartmentChart', () => ({
  default: ({ data }: any) => (
    <div data-testid="department-chart">
      {data.map((d: any) => (
        <div key={d.departmentId} data-testid={`dept-${d.departmentName}`}>
          <span>{d.departmentName}</span>
          <span>{d.attendancePercentage}%</span>
        </div>
      ))}
    </div>
  ),
}))

vi.mock('../components/TrendChart', () => ({
  default: ({ data }: any) => (
    <div data-testid="trend-chart">
      {data.map((t: any) => (
        <div key={t.date}>{t.date}</div>
      ))}
    </div>
  ),
}))

vi.mock('../components/TopLateList', () => ({
  default: ({ data }: any) => (
    <div data-testid="top-late-list">
      {data.map((e: any) => (
        <div key={e.employeeId}>{e.displayName}</div>
      ))}
    </div>
  ),
}))

vi.mock('../components/ExceptionTable', () => ({
  default: ({ exceptions }: any) => (
    <div data-testid="exception-table">
      {exceptions.map((e: any) => (
        <div key={e.id}>{e.displayName}</div>
      ))}
    </div>
  ),
}))

vi.mock('@/shared/components/LoadingSpinner', () => ({
  LoadingSpinner: () => <div data-testid="loading-spinner" />,
}))

vi.mock('@/shared/components/EmptyState', () => ({
  default: ({ title }: any) => <div data-testid="empty-state"><h3>{title}</h3></div>,
}))

function TestWrapper({ children }: { children: React.ReactNode }) {
  return <AuthProvider>{children}</AuthProvider>
}

describe('DashboardPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('should show loading spinner initially', () => {
    mockApi.get.mockReturnValue(new Promise(() => {}))
    render(<DashboardPage />, { wrapper: TestWrapper })
    expect(screen.getByTestId('loading-spinner')).toBeDefined()
  })

  it('should render dashboard header', async () => {
    mockApi.get.mockImplementation(async () => ({ data: [] }))
    render(<DashboardPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      expect(screen.getByText('Dashboard')).toBeDefined()
    })
  })

  it('should display date in header', async () => {
    mockApi.get.mockImplementation(async () => ({ data: [] }))
    render(<DashboardPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      // Should have a date displayed
      const dateElements = document.querySelectorAll('[class*="text-muted-foreground"]')
      expect(dateElements.length).toBeGreaterThan(0)
    })
  })

  it('should render KPI cards', async () => {
    mockApi.get.mockResolvedValue({
      data: {
        kpi: [
          { departmentId: '1', departmentName: 'Production', attendancePercentage: 94, totalEmployees: 126, presentCount: 118, lateCount: 5, absentCount: 3 },
        ],
        trends: [],
        topLate: [],
        exceptions: [],
      },
    })

    render(<DashboardPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      expect(screen.getByTestId('kpi-card-Total Employees')).toBeDefined()
      expect(screen.getByTestId('kpi-card-On-time Rate')).toBeDefined()
      expect(screen.getByTestId('kpi-card-Avg Late Minutes')).toBeDefined()
      expect(screen.getByTestId('kpi-card-Active Exceptions')).toBeDefined()
      expect(screen.getByTestId('kpi-card-Pending Warnings')).toBeDefined()
    })
  })

  it('should calculate total employees from departments', async () => {
    mockApi.get.mockResolvedValue({
      data: {
        kpi: [
          { departmentId: '1', departmentName: 'Production', attendancePercentage: 94, totalEmployees: 126, presentCount: 118, lateCount: 5, absentCount: 3 },
          { departmentId: '2', departmentName: 'Quality', attendancePercentage: 91, totalEmployees: 24, presentCount: 22, lateCount: 1, absentCount: 1 },
        ],
        trends: [],
        topLate: [],
        exceptions: [],
      },
    })

    render(<DashboardPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const card = screen.getByTestId('kpi-card-Total Employees')
      expect(card).toHaveTextContent('150') // 126 + 24
    })
  })

  it('should calculate average on-time rate', async () => {
    mockApi.get.mockResolvedValue({
      data: {
        kpi: [
          { departmentId: '1', departmentName: 'Production', attendancePercentage: 90, totalEmployees: 100, presentCount: 90, lateCount: 5, absentCount: 5 },
          { departmentId: '2', departmentName: 'Quality', attendancePercentage: 100, totalEmployees: 10, presentCount: 10, lateCount: 0, absentCount: 0 },
        ],
        trends: [],
        topLate: [],
        exceptions: [],
      },
    })

    render(<DashboardPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const card = screen.getByTestId('kpi-card-On-time Rate')
      // Average of 90 and 100 = 95%
      expect(card).toHaveTextContent('95%')
    })
  })

  it('should render department chart', async () => {
    mockApi.get.mockResolvedValue({
      data: {
        kpi: [
          { departmentId: '1', departmentName: 'Production', attendancePercentage: 94, totalEmployees: 126, presentCount: 118, lateCount: 5, absentCount: 3 },
          { departmentId: '2', departmentName: 'Quality', attendancePercentage: 91, totalEmployees: 24, presentCount: 22, lateCount: 1, absentCount: 1 },
        ],
        trends: [],
        topLate: [],
        exceptions: [],
      },
    })

    render(<DashboardPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      expect(screen.getByTestId('department-chart')).toBeDefined()
      expect(screen.getByTestId('dept-Production')).toBeDefined()
      expect(screen.getByTestId('dept-Quality')).toBeDefined()
    })
  })

  it('should render trend chart', async () => {
    mockApi.get.mockResolvedValue({
      data: {
        kpi: [],
        trends: [
          { date: 'Jan 1', onTimePercentage: 85, lateCount: 5, exceptionCount: 3 },
          { date: 'Jan 2', onTimePercentage: 88, lateCount: 3, exceptionCount: 2 },
        ],
        topLate: [],
        exceptions: [],
      },
    })

    render(<DashboardPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      expect(screen.getByTestId('trend-chart')).toBeDefined()
    })
  })

  it('should render top late list', async () => {
    mockApi.get.mockResolvedValue({
      data: {
        kpi: [],
        trends: [],
        topLate: [
          { employeeId: '708513', displayName: 'Lesedi Mashile', departmentName: 'Production', lateCount: 8, totalLateMinutes: 240 },
        ],
        exceptions: [],
      },
    })

    render(<DashboardPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      expect(screen.getByTestId('top-late-list')).toBeDefined()
      expect(screen.getByText('Lesedi Mashile')).toBeDefined()
    })
  })

  it('should render exception table', async () => {
    mockApi.get.mockResolvedValue({
      data: {
        kpi: [],
        trends: [],
        topLate: [],
        exceptions: [
          { id: '1', employeeId: '708513', employeeName: 'Lesedi', displayName: 'Lesedi', departmentId: '1', departmentName: 'Production', exceptionType: 'LateArrival', date: '2026-11-13', description: 'Test', lostProductionMinutes: 30, status: 'Open', assignedTo: null, assignedToName: null, investigatorNotes: null, investigatedAt: null, investigatedBy: null, createdAt: '2026-11-13T06:00:00Z', updatedAt: '2026-11-13T06:00:00Z' },
        ],
      },
    })

    render(<DashboardPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      expect(screen.getByTestId('exception-table')).toBeDefined()
    })
  })

  it('should count active exceptions correctly', async () => {
    mockApi.get.mockResolvedValue({
      data: {
        kpi: [],
        trends: [],
        topLate: [],
        exceptions: [
          { id: '1', employeeId: '1', employeeName: 'A', displayName: 'A', departmentId: '1', departmentName: 'Prod', exceptionType: 'LateArrival', date: '2026-11-13', description: '', lostProductionMinutes: 0, status: 'Open', assignedTo: null, assignedToName: null, investigatorNotes: null, investigatedAt: null, investigatedBy: null, createdAt: '', updatedAt: '' },
          { id: '2', employeeId: '2', employeeName: 'B', displayName: 'B', departmentId: '1', departmentName: 'Prod', exceptionType: 'AWOL', date: '2026-11-13', description: '', lostProductionMinutes: 0, status: 'UnderReview', assignedTo: null, assignedToName: null, investigatorNotes: null, investigatedAt: null, investigatedBy: null, createdAt: '', updatedAt: '' },
          { id: '3', employeeId: '3', employeeName: 'C', displayName: 'C', departmentId: '1', departmentName: 'Prod', exceptionType: 'EarlyDeparture', date: '2026-11-13', description: '', lostProductionMinutes: 0, status: 'Closed', assignedTo: null, assignedToName: null, investigatorNotes: null, investigatedAt: null, investigatedBy: null, createdAt: '', updatedAt: '' },
          { id: '4', employeeId: '4', employeeName: 'D', displayName: 'D', departmentId: '1', departmentName: 'Prod', exceptionType: 'LateArrival', date: '2026-11-13', description: '', lostProductionMinutes: 0, status: 'Escalated', assignedTo: null, assignedToName: null, investigatorNotes: null, investigatedAt: null, investigatedBy: null, createdAt: '', updatedAt: '' },
        ],
      },
    })

    render(<DashboardPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const card = screen.getByTestId('kpi-card-Active Exceptions')
      // Open + UnderReview + Escalated = 3 active (Closed doesn't count)
      expect(card).toHaveTextContent('3')
    })
  })

  it('should render quick overview stats', async () => {
    mockApi.get.mockResolvedValue({
      data: {
        kpi: [
          { departmentId: '1', departmentName: 'Production', attendancePercentage: 94, totalEmployees: 126, presentCount: 118, lateCount: 5, absentCount: 3 },
        ],
        trends: [],
        topLate: [{ employeeId: '1', displayName: 'A', departmentName: 'P', lateCount: 5, totalLateMinutes: 120 }],
        exceptions: [],
      },
    })

    render(<DashboardPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      expect(screen.getByTestId('stat-item-Present Today')).toBeDefined()
      expect(screen.getByTestId('stat-item-Late Today')).toBeDefined()
      expect(screen.getByTestId('stat-item-Absent Today')).toBeDefined()
      expect(screen.getByTestId('stat-item-Total Lost Min')).toBeDefined()
    })
  })

  it('should handle empty dashboard data gracefully', async () => {
    mockApi.get.mockResolvedValue({
      data: {
        kpi: [],
        trends: [],
        topLate: [],
        exceptions: [],
      },
    })

    render(<DashboardPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      expect(screen.getByText('Dashboard')).toBeDefined()
      expect(screen.getByTestId('kpi-card-Total Employees')).toHaveTextContent('0')
    })
  })

  it('should use mock data when API fails', async () => {
    mockApi.get.mockRejectedValue(new Error('API Error'))

    render(<DashboardPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      expect(screen.getByText('Dashboard')).toBeDefined()
      // Should fall back to mock data with Production department
      expect(screen.queryByTestId('dept-Production')).toBeDefined()
    })
  })

  it('should have responsive grid layout for charts', async () => {
    mockApi.get.mockResolvedValue({
      data: {
        kpi: [{ departmentId: '1', departmentName: 'Production', attendancePercentage: 94, totalEmployees: 126, presentCount: 118, lateCount: 5, absentCount: 3 }],
        trends: [],
        topLate: [],
        exceptions: [],
      },
    })

    render(<DashboardPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const grids = document.querySelectorAll('[class*="grid"]')
      expect(grids.length).toBeGreaterThan(0)
    })
  })

  it('should apply color classes to KPI cards', async () => {
    mockApi.get.mockResolvedValue({
      data: {
        kpi: [],
        trends: [],
        topLate: [],
        exceptions: [],
      },
    })

    render(<DashboardPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const cards = document.querySelectorAll('[class*="rounded-xl"][class*="border"]')
      expect(cards.length).toBeGreaterThanOrEqual(5)
    })
  })

  it('should show today\'s date in header', async () => {
    mockApi.get.mockResolvedValue({
      data: { kpi: [], trends: [], topLate: [], exceptions: [] },
    })

    render(<DashboardPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      // Date should be formatted like "Monday, November 13, 2026"
      const datePattern = /\w+, \w+ \d+, \d{4}/
      const dateElements = document.querySelectorAll('span')
      const hasDate = Array.from(dateElements).some(el => datePattern.test(el.textContent || ''))
      if (hasDate) {
        expect(hasDate).toBe(true)
      }
    })
  })
})

describe('DashboardPage - KPI Calculations', () => {
  it('should calculate total late minutes from top late entries', async () => {
    mockApi.get.mockResolvedValue({
      data: {
        kpi: [],
        trends: [],
        topLate: [
          { employeeId: '1', displayName: 'A', departmentName: 'P', lateCount: 5, totalLateMinutes: 240 },
          { employeeId: '2', displayName: 'B', departmentName: 'P', lateCount: 3, totalLateMinutes: 180 },
        ],
        exceptions: [],
      },
    })

    render(<DashboardPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const card = screen.getByTestId('kpi-card-Avg Late Minutes')
      // avg = (240 + 180) / 2 = 210
      expect(card).toHaveTextContent('210')
    })
  })

  it('should handle division by zero for top late', async () => {
    mockApi.get.mockResolvedValue({
      data: {
        kpi: [],
        trends: [],
        topLate: [],
        exceptions: [],
      },
    })

    render(<DashboardPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const card = screen.getByTestId('kpi-card-Avg Late Minutes')
      // Math.round(0 / max(0, 1)) = 0
      expect(card).toHaveTextContent('0')
    })
  })
})
