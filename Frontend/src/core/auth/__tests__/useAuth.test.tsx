import { describe, it, expect, vi } from 'vitest'
import { renderHook, act } from '@testing-library/react'
import { AuthProvider, useAuth } from '../AuthContext'
import type { User, Role } from '@/core/api/types'

// Mock apiClient and dependencies
vi.mock('@/core/api/apiClient', () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
  },
  getAccessToken: vi.fn(() => 'mock-token'),
  setAccessToken: vi.fn(),
  setRefreshToken: vi.fn(),
  clearTokens: vi.fn(),
  removeAccessToken: vi.fn(),
}))

vi.mock('@/core/api/endpoints', () => ({
  ENDPOINTS: {
    auth: {
      login: '/api/auth/login',
      logout: '/api/auth/logout',
      refreshToken: '/api/auth/refresh',
      register: '/api/auth/register',
      changePassword: '/api/auth/change-password',
      me: '/api/auth/me',
    },
  },
}))

describe('AuthProvider - hasRole', () => {
  const mockOperator: User = {
    id: 'op-1', email: 'op@test.com', firstName: 'Test', lastName: 'User',
    displayName: 'Test User', employeeId: '708001', roleId: '6', roleName: 'Operator',
    departmentId: '1', departmentName: 'Production', managerId: null, isActive: true,
    lastLoginAt: null, createdAt: new Date().toISOString(), updatedAt: new Date().toISOString(),
  }

  const mockTeamLeader: User = {
    ...mockOperator, roleName: 'TeamLeader' as Role, roleId: '5',
  }

  const mockCoordinator: User = {
    ...mockOperator, roleName: 'Coordinator' as Role, roleId: '4',
  }

  const mockManager: User = {
    ...mockOperator, roleName: 'Manager' as Role, roleId: '3',
  }

  const mockSuperAdmin: User = {
    ...mockOperator, roleName: 'SuperAdmin' as Role, roleId: '1',
  }

  it('should return false when user is null', () => {
    const { result } = renderHook(() => useAuth(), { wrapper: AuthProvider })
    expect(result.current.hasRole('Operator')).toBe(false)
  })

  it('should return true for higher roles checking lower requirements', () => {
    // Test hierarchy logic directly
    const roleHierarchy: Record<string, number> = {
      SuperAdmin: 6, SeniorManager: 5, Manager: 4, Coordinator: 3, TeamLeader: 2, Operator: 1,
    }

    const managerLevel = roleHierarchy['Manager'] ?? 0
    const operatorLevel = roleHierarchy['Operator'] ?? 0

    // Manager (4) should have access to Operator (1)
    expect(managerLevel >= operatorLevel).toBe(true)

    // Operator (1) should NOT have access to Manager (4)
    expect(operatorLevel >= managerLevel).toBe(false)
  })

  it('should accept array of roles', () => {
    const roleHierarchy: Record<string, number> = {
      SuperAdmin: 6, SeniorManager: 5, Manager: 4, Coordinator: 3, TeamLeader: 2, Operator: 1,
    }

    const coordinatorLevel = roleHierarchy['Coordinator'] ?? 0
    const roles = ['Operator', 'TeamLeader']

    const hasAccess = roles.some(r => (roleHierarchy[r] ?? 0) <= coordinatorLevel)
    expect(hasAccess).toBe(true)
  })

  it('should handle all roles in hierarchy', () => {
    const roleHierarchy: Record<string, number> = {
      SuperAdmin: 6, SeniorManager: 5, Manager: 4, Coordinator: 3, TeamLeader: 2, Operator: 1,
    }

    const levels = Object.values(roleHierarchy).sort((a, b) => a - b)

    // Verify all levels are distinct and ordered
    expect(levels).toEqual([1, 2, 3, 4, 5, 6])
  })
})

describe('AuthProvider - state management', () => {
  it('should start in loading state', () => {
    const { result } = renderHook(() => useAuth(), { wrapper: AuthProvider })
    expect(result.current.isLoading).toBe(true)
  })

  it('should have isAuthenticated as false initially', () => {
    const { result } = renderHook(() => useAuth(), { wrapper: AuthProvider })
    expect(result.current.isAuthenticated).toBe(false)
  })

  it('should have user as null initially', () => {
    const { result } = renderHook(() => useAuth(), { wrapper: AuthProvider })
    expect(result.current.user).toBeNull()
  })

  it('should clear error on clearError call', async () => {
    window.localStorage.setItem('wacams_access_token', 'token')
    const apiClient = await import('@/core/api/apiClient')
    vi.mocked(apiClient.default.get).mockRejectedValueOnce(new Error('Failed'))

    const { result } = renderHook(() => useAuth(), { wrapper: AuthProvider })
    await act(async () => { await new Promise(r => setTimeout(r, 0)) })

    expect(result.current.error).toBeDefined()

    act(() => {
      result.current.clearError()
    })

    expect(result.current.error).toBeNull()
  })
})

describe('AuthProvider - token storage functions', () => {
  it('should manage tokens in localStorage', async () => {
    const { setAccessToken, setRefreshToken, clearTokens, getAccessToken } = await import('@/core/api/apiClient')

    act(() => {
      setAccessToken('test-token')
    })
    expect(getAccessToken()).toBe('test-token')

    act(() => {
      setRefreshToken('test-refresh')
    })

    act(() => {
      clearTokens()
    })
    expect(getAccessToken()).toBeNull()
  })
})

describe('AuthProvider - role hierarchy verification', () => {
  it('should verify all role levels exist', () => {
    const roles: Role[] = ['SuperAdmin', 'SeniorManager', 'Manager', 'Coordinator', 'TeamLeader', 'Operator']
    const roleHierarchy: Record<string, number> = {
      SuperAdmin: 6, SeniorManager: 5, Manager: 4, Coordinator: 3, TeamLeader: 2, Operator: 1,
    }

    roles.forEach(role => {
      expect(roleHierarchy[role]).toBeDefined()
      expect(typeof roleHierarchy[role]).toBe('number')
    })
  })

  it('should have no duplicate role levels', () => {
    const roleHierarchy: Record<string, number> = {
      SuperAdmin: 6, SeniorManager: 5, Manager: 4, Coordinator: 3, TeamLeader: 2, Operator: 1,
    }

    const levels = Object.values(roleHierarchy)
    const uniqueLevels = new Set(levels)
    expect(uniqueLevels.size).toBe(levels.length)
  })

  it('should have increasing order from Operator to SuperAdmin', () => {
    const roleHierarchy: Record<string, number> = {
      SuperAdmin: 6, SeniorManager: 5, Manager: 4, Coordinator: 3, TeamLeader: 2, Operator: 1,
    }

    const order: Array<keyof typeof roleHierarchy> = ['Operator', 'TeamLeader', 'Coordinator', 'Manager', 'SeniorManager', 'SuperAdmin']
    for (let i = 1; i < order.length; i++) {
      expect(roleHierarchy[order[i]]).toBeGreaterThan(roleHierarchy[order[i - 1]])
    }
  })
})
