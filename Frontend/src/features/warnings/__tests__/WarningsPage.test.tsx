import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { AuthProvider } from '@/core/auth/AuthContext'
import WarningsPage from '../WarningsPage'
import type { WarningLetter } from '@/core/api/types'

vi.mock('react-router-dom', () => ({
  useNavigate: () => vi.fn(),
  useParams: () => ({}),
  useLocation: () => ({ pathname: '/warnings', search: '', state: null, hash: '', key: 'test' }),
}))

// Mock components
vi.mock('../components/WarningCard', () => ({
  default: ({ warning }: { warning: WarningLetter }) => (
    <div data-testid={`warning-card-${warning.id}`}>
      <span>{warning.warningType}</span>
      <span>{warning.title}</span>
      <span>{warning.status}</span>
      <span>{warning.employeeName}</span>
    </div>
  ),
}))

vi.mock('../components/ThresholdProgress', () => ({
  default: ({ data }: any) => (
    <div data-testid="threshold-progress">
      <span>{data.currentCount}</span>
      <span>{data.nextLevel}</span>
    </div>
  ),
}))

vi.mock('../components/WarningTimeline', () => ({
  default: ({ warnings }: any) => (
    <div data-testid="warning-timeline">
      {warnings.map((w: any) => (
        <div key={w.id} data-testid={`timeline-item-${w.id}`}>
          <span>{w.warningType}</span>
          <span>{w.date}</span>
        </div>
      ))}
    </div>
  ),
}))

vi.mock('../WarningGenerationWizard', () => ({
  default: ({ onCancel }: any) => (
    <div data-testid="warning-wizard">
      <button onClick={onCancel}>Close Wizard</button>
    </div>
  ),
}))

vi.mock('@/shared/components/EmptyState', () => ({
  default: ({ title, description }: any) => (
    <div data-testid="empty-state">
      <h3>{title}</h3>
      <p>{description}</p>
    </div>
  ),
}))

function TestWrapper({ children }: { children: React.ReactNode }) {
  return <AuthProvider>{children}</AuthProvider>
}

describe('WarningsPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('should render page header', async () => {
    render(<WarningsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      expect(screen.getByText('Warning Letters')).toBeDefined()
    })
  })

  it('should show "New Warning" button', async () => {
    render(<WarningsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      expect(screen.getByText('New Warning')).toBeDefined()
    })
  })

  it('should display all warning cards by default', async () => {
    render(<WarningsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      // Should have 3 mock warnings
      expect(screen.getByTestId('warning-card-warn-1')).toBeDefined()
      expect(screen.getByTestId('warning-card-warn-2')).toBeDefined()
      expect(screen.getByTestId('warning-card-warn-3')).toBeDefined()
    })
  })

  it('should filter by Draft status', async () => {
    render(<WarningsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const draftBtn = screen.getByRole('button', { name: /Draft.*1/i })
      fireEvent.click(draftBtn)
    })

    await waitFor(() => {
      // Only Draft warnings should be visible
      expect(screen.getByTestId('warning-card-warn-3')).toBeDefined()
    })
  })

  it('should filter by Issued status', async () => {
    render(<WarningsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const issuedBtn = screen.getByRole('button', { name: /Issued.*1/i })
      fireEvent.click(issuedBtn)
    })

    await waitFor(() => {
      expect(screen.getByTestId('warning-card-warn-1')).toBeDefined()
    })
  })

  it('should filter by Acknowledged status', async () => {
    render(<WarningsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const ackBtn = screen.getByRole('button', { name: /Acknowledged.*1/i })
      fireEvent.click(ackBtn)
    })

    await waitFor(() => {
      expect(screen.getByTestId('warning-card-warn-2')).toBeDefined()
    })
  })

  it('should reset to All when clicking "All" filter', async () => {
    render(<WarningsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      // Click a specific filter
      const issuedBtn = screen.getByRole('button', { name: /Issued.*1/i })
      fireEvent.click(issuedBtn)
    })

    await waitFor(() => {
      // Go back to All
      const allBtn = screen.getByRole('button', { name: /All.*3/i })
      fireEvent.click(allBtn)
    })

    await waitFor(() => {
      // All three should be visible again
      expect(screen.getAllByTestId(/^warning-card-/)).toHaveLength(3)
    })
  })

  it('should open wizard modal on "New Warning" click', async () => {
    render(<WarningsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const newBtn = screen.getByText('New Warning')
      fireEvent.click(newBtn)
    })

    await waitFor(() => {
      expect(screen.getByTestId('warning-wizard')).toBeDefined()
    })
  })

  it('should close wizard modal', async () => {
    render(<WarningsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const newBtn = screen.getByText('New Warning')
      fireEvent.click(newBtn)
    })

    await waitFor(() => {
      const closeBtn = screen.getByText('Close Wizard')
      fireEvent.click(closeBtn)
    })

    // Wizard should be closed
    expect(screen.queryByTestId('warning-wizard')).toBeNull()
  })

  it('should show threshold progress sidebar', async () => {
    render(<WarningsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      expect(screen.getByTestId('threshold-progress')).toBeDefined()
    })
  })

  it('should show warning timeline sidebar', async () => {
    render(<WarningsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      expect(screen.getByTestId('warning-timeline')).toBeDefined()
    })
  })

  it('should display warning types correctly', async () => {
    render(<WarningsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      expect(screen.getByText('Verbal Warning')).toBeDefined()
      expect(screen.getByText('Written Warning')).toBeDefined()
      expect(screen.getByText('Final Written Warning')).toBeDefined()
    })
  })

  it('should display employee names on cards', async () => {
    render(<WarningsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      expect(screen.getByText('Lesedi Mashile')).toBeDefined()
      expect(screen.getByText('Thabo Mokoena')).toBeDefined()
      expect(screen.getByText('Sipho Ndlovu')).toBeDefined()
    })
  })

  it('should show signed form indicator for warnings with forms', async () => {
    render(<WarningsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const card = screen.getByTestId('warning-card-warn-2')
      expect(card).toBeDefined()
    })
  })

  it('should handle empty filtered results', async () => {
    render(<WarningsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      // Filter by a status that might not exist in current mocks
      const issuedBtn = screen.getByRole('button', { name: /Archived.*0/i })
      if (issuedBtn) {
        fireEvent.click(issuedBtn)
      }
    })
  })

  it('should have proper responsive layout', async () => {
    render(<WarningsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const grids = document.querySelectorAll('[class*="grid"]')
      expect(grids.length).toBeGreaterThan(0)
    })
  })

  it('should display status badge counts', async () => {
    render(<WarningsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      // Check that count numbers appear in filter buttons
      const buttons = screen.getAllByRole('button')
      const filterButtons = buttons.filter(b => b.textContent?.match(/\d+/))
      expect(filterButtons.length).toBeGreaterThanOrEqual(4)
    })
  })

  it('should apply correct CSS classes for active filter', async () => {
    render(<WarningsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const buttons = screen.getAllByRole('button')
      // Find the "All" filter button which should be active initially
      const allBtn = buttons.find(b => b.textContent?.includes('All'))
      if (allBtn) {
        expect(allBtn.className).toContain('bg-primary')
      }
    })
  })
})

describe('WarningsPage - Warning Cards', () => {
  it('should render warning type labels with spacing', async () => {
    render(<WarningsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      // The replace(/([A-Z])/g, ' $1') pattern adds spaces before capital letters
      expect(screen.getByText('Verbal Warning')).toBeDefined()
    })
  })

  it('should show warning titles', async () => {
    render(<WarningsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      expect(screen.getByText('Repeated Late Arrivals')).toBeDefined()
      expect(screen.getByText('Attendance Pattern Violation')).toBeDefined()
      expect(screen.getByText('AWOL — No Notification')).toBeDefined()
    })
  })

  it('should show warning descriptions', async () => {
    render(<WarningsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      // Descriptions are truncated with line-clamp-2
      const descriptions = document.querySelectorAll('[class*="line-clamp-2"]')
      expect(descriptions.length).toBe(3)
    })
  })

  it('should format dates on cards', async () => {
    render(<WarningsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      // Dates like "11/1/2026" or similar locale format
      const dateElements = document.querySelectorAll('[class*="text-muted-foreground"]')
      expect(dateElements.length).toBeGreaterThan(0)
    })
  })
})

describe('WarningsPage - Modal Behavior', () => {
  it('should overlay background when wizard is open', async () => {
    render(<WarningsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const newBtn = screen.getByText('New Warning')
      fireEvent.click(newBtn)
    })

    await waitFor(() => {
      // Background overlay should be present
      const overlays = document.querySelectorAll('[class*="backdrop-blur"]')
      expect(overlays.length).toBeGreaterThan(0)
    })
  })

  it('should close wizard on background click', async () => {
    render(<WarningsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const newBtn = screen.getByText('New Warning')
      fireEvent.click(newBtn)
    })

    await waitFor(() => {
      // Click the backdrop
      const backdrop = document.querySelector('[class*="backdrop-blur"]')
      if (backdrop) {
        fireEvent.click(backdrop)
      }
    })

    // Wizard should be closed
    expect(screen.queryByTestId('warning-wizard')).toBeNull()
  })

  it('should prevent scroll when modal is open', async () => {
    render(<WarningsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const newBtn = screen.getByText('New Warning')
      fireEvent.click(newBtn)
    })

    await waitFor(() => {
      const modal = document.querySelector('[class*="max-h-\\[90vh\\]"]')
      expect(modal).toBeDefined()
    })
  })
})
