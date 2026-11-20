import '@testing-library/jest-dom/vitest'
import { vi, beforeEach } from 'vitest'

// Mock react-router-dom
vi.mock('react-router-dom', async () => {
  const actual = await vi.importActual('react-router-dom')
  return {
    ...actual,
    useNavigate: vi.fn(() => vi.fn()),
    useParams: vi.fn(() => ({})),
    useLocation: vi.fn(() => ({ pathname: '/', search: '', state: null, hash: '', key: 'default' })),
    RouterProvider: vi.fn(),
    createBrowserRouter: vi.fn(),
    BrowserRouter: vi.fn(({ children }) => children),
    Routes: vi.fn(({ children }) => children),
    Route: vi.fn(({ element }) => element),
    Navigate: vi.fn(),
  }
})

// Mock Lucide icons
vi.mock('lucide-react', async () => {
  const actual = await vi.importActual('lucide-react')
  return {
    ...actual,
    Users: vi.fn().mockReturnValue(null),
    CheckCircle2: vi.fn().mockReturnValue(null),
    Clock: vi.fn().mockReturnValue(null),
    AlertTriangle: vi.fn().mockReturnValue(null),
    FileWarning: vi.fn().mockReturnValue(null),
    ArrowUpRight: vi.fn().mockReturnValue(null),
    Users2: vi.fn().mockReturnValue(null),
    TrendingUp: vi.fn().mockReturnValue(null),
    Calendar: vi.fn().mockReturnValue(null),
    ChevronLeft: vi.fn().mockReturnValue(null),
    ChevronRight: vi.fn().mockReturnValue(null),
    Search: vi.fn().mockReturnValue(null),
    Filter: vi.fn().mockReturnValue(null),
    ArrowDownLeft: vi.fn().mockReturnValue(null),
    Inbox: vi.fn().mockReturnValue(null),
    ArrowUpCircle: vi.fn().mockReturnValue(null),
    User: vi.fn().mockReturnValue(null),
    X: vi.fn().mockReturnValue(null),
  }
})

// Mock localStorage
const localStorageMock = (() => {
  let store: Record<string, string> = {}
  return {
    getItem: vi.fn((key: string) => store[key] || null),
    setItem: vi.fn((key: string, value: string) => { store[key] = value }),
    removeItem: vi.fn((key: string) => { delete store[key] }),
    clear: vi.fn(() => { store = {} }),
  }
})()
Object.defineProperty(window, 'localStorage', { value: localStorageMock })

// Mock window.location
Object.defineProperty(window, 'location', {
  value: {
    href: 'http://localhost/',
    assign: vi.fn(),
    replace: vi.fn(),
  },
  writable: true,
})

// Mock fetch
global.fetch = vi.fn()

// Mock IntersectionObserver
class MockIntersectionObserver {
  observe = vi.fn()
  disconnect = vi.fn()
  unobserve = vi.fn()
}
Object.defineProperty(window, 'IntersectionObserver', {
  writable: true,
  configurable: true,
  value: MockIntersectionObserver,
})

// Mock ResizeObserver
class MockResizeObserver {
  observe = vi.fn()
  disconnect = vi.fn()
  unobserve = vi.fn()
}
Object.defineProperty(window, 'ResizeObserver', {
  writable: true,
  configurable: true,
  value: MockResizeObserver,
})

// Reset mocks between tests
beforeEach(() => {
  vi.clearAllMocks()
  localStorageMock.clear()
})
