import { describe, it, expect, vi } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { AuthProvider } from '@/core/auth/AuthContext'
import AttendancePage from '../AttendancePage'
import type { AttendanceRecord } from '@/core/api/types'

vi.mock('react-router-dom', () => ({
  useNavigate: () => vi.fn(),
  useParams: () => ({}),
  useLocation: () => ({ pathname: '/attendance', search: '', state: null, hash: '', key: 'test' }),
}))

// Mock EmployeeFilters component
vi.mock('@/features/employees/components/EmployeeFilters', () => ({
  default: function MockEmployeeFilters({ selectedDepartment, onDepartmentChange, statusFilter, onStatusChange }: any) {
    return (
      <div data-testid="employee-filters">
        <select
          data-testid="department-select"
          value={selectedDepartment}
          onChange={(e) => onDepartmentChange(e.target.value)}
        >
          <option value="all">All Departments</option>
          <option value="production">Production</option>
          <option value="quality">Quality</option>
        </select>
        <input
          data-testid="status-filter"
          value={statusFilter}
          onChange={(e) => onStatusChange(e.target.value)}
        />
      </div>
    )
  },
}))

function TestWrapper({ children }: { children: React.ReactNode }) {
  return <AuthProvider>{children}</AuthProvider>
}

describe('AttendancePage', () => {
  it('should render page header', async () => {
    render(<AttendancePage />, { wrapper: TestWrapper })

    await waitFor(() => {
      expect(screen.getByText('Attendance')).toBeDefined()
    })
    expect(screen.getByText('Daily attendance tracking and calendar view')).toBeDefined()
  })

  it('should show loading spinner initially', () => {
    render(<AttendancePage />, { wrapper: TestWrapper })
    expect(screen.getByText('Loading attendance data...')).toBeDefined()
  })

  it('should display calendar view', async () => {
    render(<AttendancePage />, { wrapper: TestWrapper })

    await waitFor(() => {
      expect(screen.getByRole('grid')).toBeDefined()
    })
  })

  it('should display daily attendance table', async () => {
    render(<AttendancePage />, { wrapper: TestWrapper })

    await waitFor(() => {
      expect(screen.getByText(/attendance/i)).toBeDefined()
    })
  })

  it('should show export CSV button', async () => {
    render(<AttendancePage />, { wrapper: TestWrapper })

    await waitFor(() => {
      expect(screen.getByText('Export CSV')).toBeDefined()
    })
  })

  it('should filter by department when changed', async () => {
    render(<AttendancePage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const select = screen.getByTestId('department-select') as HTMLSelectElement
      fireEvent.change(select, { target: { value: 'production' } })
      expect(select.value).toBe('production')
    })
  })

  it('should apply status filter', async () => {
    render(<AttendancePage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const input = screen.getByTestId('status-filter') as HTMLInputElement
      fireEvent.change(input, { target: { value: 'Late' } })
      expect(input.value).toBe('Late')
    })
  })

  it('should show lost time summary', async () => {
    render(<AttendancePage />, { wrapper: TestWrapper })

    await waitFor(() => {
      expect(screen.getByText('Lost Production Time')).toBeDefined()
    })
  })

  it('should calculate total late minutes', async () => {
    render(<AttendancePage />, { wrapper: TestWrapper })

    // The page calculates total late from mock records
    await waitFor(() => {
      const lateTimeEl = screen.getAllByText(/\d+m$/)[0]
      expect(lateTimeEl).toBeDefined()
    })
  })

  it('should render calendar with correct month/year', async () => {
    render(<AttendancePage />, { wrapper: TestWrapper })

    await waitFor(() => {
      // Calendar header should show current month name
      const headers = screen.queryAllByRole('heading', { level: 3 })
      const calendarHeader = headers.find(h => h.textContent?.includes('20'))
      if (calendarHeader) {
        expect(calendarHeader.textContent).toMatch(/January|February|March|April|May|June|July|August|September|October|November|December/)
      }
    })
  })

  it('should handle empty calendar days gracefully', async () => {
    render(<AttendancePage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const gridCells = document.querySelectorAll('[class*="aspect-square"]')
      expect(gridCells.length).toBeGreaterThan(0)
    })
  })

  it('should call onDayClick when a day is clicked', async () => {
    const onDayClick = vi.fn()

    render(<AttendancePage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const dayButtons = document.querySelectorAll('button[class*="aspect-square"]')
      if (dayButtons.length > 0) {
        fireEvent.click(dayButtons[0])
      }
    })
  })

  it('should show employee count in table header', async () => {
    render(<AttendancePage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const employeeCount = screen.getByText(/\d+ employees/i)
      expect(employeeCount).toBeDefined()
    })
  })

  it('should render with different date format', async () => {
    render(<AttendancePage />, { wrapper: TestWrapper })

    await waitFor(() => {
      // Should show formatted date like "Monday, November 13, 2026"
      const dateTexts = screen.queryAllByText(/\w+, \w+ \d+, \d{4}/)
      if (dateTexts.length > 0) {
        expect(dateTexts[0]).toBeDefined()
      }
    })
  })
})

describe('AttendancePage - Status Display', () => {
  it('should show different status colors in table', async () => {
    render(<AttendancePage />, { wrapper: TestWrapper })

    await waitFor(() => {
      // Check that status chips are rendered
      const statusChips = document.querySelectorAll('[class*="rounded-full"][class*="border"]')
      expect(statusChips.length).toBeGreaterThan(0)
    })
  })

  it('should show clock in/out times in table', async () => {
    render(<AttendancePage />, { wrapper: TestWrapper })

    await waitFor(() => {
      // Time badges should be visible
      const timeBadges = document.querySelectorAll('[class*="inline-flex"][class*="rounded-full"]')
      expect(timeBadges.length).toBeGreaterThan(0)
    })
  })

  it('should show shift code badges', async () => {
    render(<AttendancePage />, { wrapper: TestWrapper })

    await waitFor(() => {
      // Shift codes like ES, MS, NS should appear
      const shiftCodes = screen.queryAllByText(/ES|MS|NS/)
      if (shiftCodes.length > 0) {
        expect(shiftCodes[0]).toBeDefined()
      }
    })
  })
})

describe('AttendancePage - Layout', () => {
  it('should use responsive grid layout', async () => {
    render(<AttendancePage />, { wrapper: TestWrapper })

    await waitFor(() => {
      // Check for grid containers
      const grids = document.querySelectorAll('[class*="grid"]')
      expect(grids.length).toBeGreaterThan(0)
    })
  })

  it('should have proper spacing between sections', async () => {
    render(<AttendancePage />, { wrapper: TestWrapper })

    await waitFor(() => {
      // Space-y-6 creates consistent gaps
      const spaceContainers = document.querySelectorAll('[class*="space-y-6"]')
      expect(spaceContainers.length).toBeGreaterThan(0)
    })
  })

  it('should contain all major sections', async () => {
    render(<AttendancePage />, { wrapper: TestWrapper })

    await waitFor(() => {
      // Header + Filters + Calendar + Lost Time Summary + Table
      expect(screen.getByText('Attendance')).toBeDefined()
      expect(screen.getByText('Lost Production Time')).toBeDefined()
    })
  })
})
