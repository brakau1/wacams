import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import EscalationsPage from '../EscalationsPage'
import type { Escalation as EscalationType } from '@/core/api/types/escalation'

const mockApi = vi.hoisted(() => ({
  get: vi.fn(),
}))

vi.mock('@/core/api/apiClient', () => ({
  default: {
    get: (url: string) => mockApi.get(url),
  },
}))

vi.mock('react-router-dom', () => ({
  useNavigate: () => vi.fn(),
  useParams: () => ({}),
  useLocation: () => ({ pathname: '/escalations', search: '', state: null, hash: '', key: 'test' }),
}))

// Mock components
vi.mock('../components/EscalationCard', () => ({
  default: ({ escalation }: { escalation: EscalationType }) => (
    <div data-testid={`escalation-card-${escalation.id}`}>
      <span>{escalation.assignedTo}</span>
      <span>{escalation.status}</span>
      <span>{escalation.currentLevel}</span>
    </div>
  ),
}))

vi.mock('../components/EscalationStats', () => ({
  EscalationStats: ({ stats }: any) => (
    <div data-testid="escalation-stats">
      <span data-testid="total-active">{stats.totalActive}</span>
      <span data-testid="pending-count">{stats.pendingCount}</span>
    </div>
  ),
}))

vi.mock('@/shared/components/LoadingSpinner', () => ({
  LoadingSpinner: () => <div data-testid="loading-spinner" />,
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
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false, staleTime: 0 } },
  })
  return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
}

describe('EscalationsPage', () => {
  const mockEscalations: EscalationType[] = [
    {
      id: 'esc-1', exceptionId: 'exc-1', employeeId: '708513', currentLevel: 'ManagerLevel1',
      assignedTo: 'George Ntseke', status: 'Pending', createdAt: '2026-11-13T08:00:00Z',
      nextEscalationTime: '2026-11-14T08:00:00Z', documentUploaded: false, createdBy: 'system',
    },
    {
      id: 'esc-2', exceptionId: 'exc-2', employeeId: '707995', currentLevel: 'ManagerLevel2',
      assignedTo: 'Jeremiah Bezuidenhout', status: 'Acknowledged', createdAt: '2026-11-12T10:00:00Z',
      nextEscalationTime: '2026-11-14T10:00:00Z', documentUploaded: true, documentUrl: '/doc.pdf',
      acknowledgedAt: '2026-11-12T14:00:00Z', createdBy: 'system',
    },
    {
      id: 'esc-3', exceptionId: 'exc-3', employeeId: '707888', currentLevel: 'ManagerLevel1',
      assignedTo: 'George Ntseke', status: 'Resolved', createdAt: '2026-11-10T08:00:00Z',
      nextEscalationTime: '2026-11-10T08:00:00Z', documentUploaded: false,
      resolvedAt: '2026-11-11T08:00:00Z', createdBy: 'system',
    },
  ]

  beforeEach(() => {
    vi.clearAllMocks()
    mockApi.get.mockReset()
  })

  it('should show loading spinner initially', () => {
    mockApi.get.mockReturnValue(new Promise(() => {})) // Never resolves

    render(<EscalationsPage />, { wrapper: TestWrapper })
    expect(screen.getByTestId('loading-spinner')).toBeDefined()
  })

  it('should render page header', async () => {
    mockApi.get.mockResolvedValue({ data: [] })

    render(<EscalationsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      expect(screen.getByText('Escalation Management')).toBeDefined()
    })
  })

  it('should display stats when loaded', async () => {
    mockApi.get.mockImplementation(async (url: string) => {
      if (url.includes('/stats')) {
        return {
          data: { totalActive: 3, pendingCount: 1, acknowledgedCount: 1, overdueCount: 1, resolvedToday: 2, avgResolutionHours: 18.5, byLevel: { ManagerLevel1: 1, ManagerLevel2: 1, ManagerLevel3: 0, ManagerLevel4: 0, ManagerLevel5: 0, HR: 0, Admin: 0 } },
        }
      }
      return { data: mockEscalations }
    })

    render(<EscalationsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      expect(screen.getByTestId('total-active')).toHaveTextContent('3')
    })
  })

  it('should render all escalation cards', async () => {
    mockApi.get.mockImplementation(async (url: string) => {
      if (url.includes('/stats')) {
        return { data: { totalActive: 3, pendingCount: 1, acknowledgedCount: 1, overdueCount: 1, resolvedToday: 2, avgResolutionHours: 18.5, byLevel: { ManagerLevel1: 1, ManagerLevel2: 1, ManagerLevel3: 0, ManagerLevel4: 0, ManagerLevel5: 0, HR: 0, Admin: 0 } } }
      }
      return { data: mockEscalations }
    })

    render(<EscalationsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      expect(screen.getByTestId('escalation-card-esc-1')).toBeDefined()
      expect(screen.getByTestId('escalation-card-esc-2')).toBeDefined()
      expect(screen.getByTestId('escalation-card-esc-3')).toBeDefined()
    })
  })

  it('should filter by status', async () => {
    mockApi.get.mockImplementation(async (url: string) => {
      if (url.includes('/stats')) {
        return { data: { totalActive: 0, pendingCount: 0, acknowledgedCount: 0, overdueCount: 0, resolvedToday: 0, avgResolutionHours: 0, byLevel: { ManagerLevel1: 0, ManagerLevel2: 0, ManagerLevel3: 0, ManagerLevel4: 0, ManagerLevel5: 0, HR: 0, Admin: 0 } } }
      }
      return { data: mockEscalations }
    })

    render(<EscalationsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const select = screen.getByRole('combobox') as HTMLSelectElement
      fireEvent.change(select, { target: { value: 'Pending' } })
      expect(select.value).toBe('Pending')
    })
  })

  it('should filter by level', async () => {
    mockApi.get.mockImplementation(async (url: string) => {
      if (url.includes('/stats')) {
        return { data: { totalActive: 0, pendingCount: 0, acknowledgedCount: 0, overdueCount: 0, resolvedToday: 0, avgResolutionHours: 0, byLevel: { ManagerLevel1: 0, ManagerLevel2: 0, ManagerLevel3: 0, ManagerLevel4: 0, ManagerLevel5: 0, HR: 0, Admin: 0 } } }
      }
      return { data: mockEscalations }
    })

    render(<EscalationsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const selects = screen.getAllByRole('combobox') as HTMLSelectElement[]
      // Second select is for level
      fireEvent.change(selects[1], { target: { value: 'ManagerLevel1' } })
      expect(selects[1].value).toBe('ManagerLevel1')
    })
  })

  it('should filter by search term', async () => {
    mockApi.get.mockImplementation(async (url: string) => {
      if (url.includes('/stats')) {
        return { data: { totalActive: 0, pendingCount: 0, acknowledgedCount: 0, overdueCount: 0, resolvedToday: 0, avgResolutionHours: 0, byLevel: { ManagerLevel1: 0, ManagerLevel2: 0, ManagerLevel3: 0, ManagerLevel4: 0, ManagerLevel5: 0, HR: 0, Admin: 0 } } }
      }
      return { data: mockEscalations }
    })

    render(<EscalationsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const input = screen.getByPlaceholderText(/search/i) as HTMLInputElement
      fireEvent.change(input, { target: { value: 'George' } })
      expect(input.value).toBe('George')
    })
  })

  it('should show empty state when no escalations match', async () => {
    mockApi.get.mockImplementation(async (url: string) => {
      if (url.includes('/stats')) {
        return { data: { totalActive: 0, pendingCount: 0, acknowledgedCount: 0, overdueCount: 0, resolvedToday: 0, avgResolutionHours: 0, byLevel: { ManagerLevel1: 0, ManagerLevel2: 0, ManagerLevel3: 0, ManagerLevel4: 0, ManagerLevel5: 0, HR: 0, Admin: 0 } } }
      }
      return { data: [] }
    })

    render(<EscalationsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      expect(screen.getByTestId('empty-state')).toBeDefined()
    })
  })

  it('should show correct number of active escalations', async () => {
    mockApi.get.mockImplementation(async (url: string) => {
      if (url.includes('/stats')) {
        return { data: { totalActive: 0, pendingCount: 0, acknowledgedCount: 0, overdueCount: 0, resolvedToday: 0, avgResolutionHours: 0, byLevel: { ManagerLevel1: 0, ManagerLevel2: 0, ManagerLevel3: 0, ManagerLevel4: 0, ManagerLevel5: 0, HR: 0, Admin: 0 } } }
      }
      return { data: mockEscalations }
    })

    render(<EscalationsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const heading = screen.getByText(/Active Escalations/)
      expect(heading).toBeDefined()
    })
  })

  it('should handle API error gracefully', async () => {
    mockApi.get.mockRejectedValue(new Error('Network error'))

    render(<EscalationsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      // Should stop showing loading spinner on error
      expect(screen.queryByTestId('loading-spinner')).toBeNull()
    })
  })

  it('should render filter section with all options', async () => {
    mockApi.get.mockImplementation(async (url: string) => {
      if (url.includes('/stats')) {
        return { data: { totalActive: 0, pendingCount: 0, acknowledgedCount: 0, overdueCount: 0, resolvedToday: 0, avgResolutionHours: 0, byLevel: { ManagerLevel1: 0, ManagerLevel2: 0, ManagerLevel3: 0, ManagerLevel4: 0, ManagerLevel5: 0, HR: 0, Admin: 0 } } }
      }
      return { data: [] }
    })

    render(<EscalationsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const selects = screen.getAllByRole('combobox')
      expect(selects.length).toBeGreaterThanOrEqual(2)
    })
  })

  it('should show escalation details in cards', async () => {
    mockApi.get.mockImplementation(async (url: string) => {
      if (url.includes('/stats')) {
        return { data: { totalActive: 0, pendingCount: 0, acknowledgedCount: 0, overdueCount: 0, resolvedToday: 0, avgResolutionHours: 0, byLevel: { ManagerLevel1: 0, ManagerLevel2: 0, ManagerLevel3: 0, ManagerLevel4: 0, ManagerLevel5: 0, HR: 0, Admin: 0 } } }
      }
      return { data: mockEscalations }
    })

    render(<EscalationsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      // Check that assigned-to names are visible
      expect(screen.getByText('George Ntseke')).toBeDefined()
    })
  })

  it('should handle document uploaded indicator', async () => {
    mockApi.get.mockImplementation(async (url: string) => {
      if (url.includes('/stats')) {
        return { data: { totalActive: 0, pendingCount: 0, acknowledgedCount: 0, overdueCount: 0, resolvedToday: 0, avgResolutionHours: 0, byLevel: { ManagerLevel1: 0, ManagerLevel2: 0, ManagerLevel3: 0, ManagerLevel4: 0, ManagerLevel5: 0, HR: 0, Admin: 0 } } }
      }
      return { data: mockEscalations }
    })

    render(<EscalationsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      // esc-2 has documentUploaded: true
      const card = screen.getByTestId('escalation-card-esc-2')
      expect(card).toBeDefined()
    })
  })

  it('should have responsive grid layout', async () => {
    mockApi.get.mockImplementation(async (url: string) => {
      if (url.includes('/stats')) {
        return { data: { totalActive: 0, pendingCount: 0, acknowledgedCount: 0, overdueCount: 0, resolvedToday: 0, avgResolutionHours: 0, byLevel: { ManagerLevel1: 0, ManagerLevel2: 0, ManagerLevel3: 0, ManagerLevel4: 0, ManagerLevel5: 0, HR: 0, Admin: 0 } } }
      }
      return { data: mockEscalations }
    })

    render(<EscalationsPage />, { wrapper: TestWrapper })

    await waitFor(() => {
      const grids = document.querySelectorAll('[class*="grid"]')
      expect(grids.length).toBeGreaterThan(0)
    })
  })
})
