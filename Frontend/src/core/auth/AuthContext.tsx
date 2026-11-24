import {
  createContext,
  useContext,
  useState,
  useEffect,
  useCallback,
  type ReactNode
} from 'react'
import { useNavigate } from 'react-router-dom'
import { User, Role, LoginRequest, ApiResponse } from '@/core/api/types'
import { getAccessToken, setAccessToken, setRefreshToken, clearTokens, removeAccessToken } from '@/core/api/apiClient'
import { ENDPOINTS } from '@/core/api/endpoints'
import apiClient from '@/core/api/apiClient'
import { isMockMode } from '@/mocks/mockHandler'

interface AuthState {
  user: User | null
  isAuthenticated: boolean
  isLoading: boolean
  error: string | null
}

interface AuthContextType extends AuthState {
  login: (credentials: LoginRequest) => Promise<boolean>
  logout: () => Promise<void>
  register: (data: Omit<LoginRequest, 'password'> & { password: string; firstName: string; lastName: string; employeeId: string; roleId: string }) => Promise<boolean>
  hasRole: (role: Role | Role[]) => boolean
  hasAnyRole: (...roles: Role[]) => boolean
  hasAllRoles: (...roles: Role[]) => boolean
  refreshUser: () => Promise<void>
  clearError: () => void
}

const AuthContext = createContext<AuthContextType | undefined>(undefined)

export function AuthProvider({ children }: { children: ReactNode }) {
  const navigate = useNavigate()
  const [state, setState] = useState<AuthState>({
    user: null,
    isAuthenticated: false,
    isLoading: true,
    error: null
  })

  // Check if user is already logged in on mount
  useEffect(() => {
    // Auto-login in mock mode
    if (isMockMode()) {
      setState({
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
        } as User,
        isAuthenticated: true,
        isLoading: false,
        error: null
      })
      return
    }

    const token = getAccessToken()
    if (token) {
      refreshUser()
    } else {
      setState(prev => ({ ...prev, isLoading: false }))
    }
  }, [])

  const refreshUser = useCallback(async () => {
    try {
      setState(prev => ({ ...prev, isLoading: true, error: null }))
      const response = await apiClient.get<ApiResponse<User>>(ENDPOINTS.auth.me)
      // Response interceptor unwraps {success, data} → response.data is already the user
      const user = response.data as unknown as User
      
      setState({
        user: user as User,
        isAuthenticated: true,
        isLoading: false,
        error: null
      })
    } catch {
      clearTokens()
      setState({
        user: null,
        isAuthenticated: false,
        isLoading: false,
        error: 'Session expired. Please log in again.'
      })
    }
  }, [])

  const login = useCallback(async (credentials: LoginRequest): Promise<boolean> => {
    try {
      setState(prev => ({ ...prev, isLoading: true, error: null }))
      const response = await apiClient.post<ApiResponse<{ accessToken: string; refreshToken: string; user: User }>>(
        ENDPOINTS.auth.login,
        credentials
      )
      // Response interceptor unwraps {success, data} → response.data is already the payload
      const data = response.data as unknown as { accessToken: string; refreshToken: string; user: User }
      
      setAccessToken(data.accessToken)
      setRefreshToken(data.refreshToken)
      
      setState({
        user: data.user,
        isAuthenticated: true,
        isLoading: false,
        error: null
      })
      
      navigate('/dashboard', { replace: true })
      return true
    } catch (error: unknown) {
      const msg = error instanceof Error ? error.message : 'Login failed. Please check your credentials.'
      setState(prev => ({
        ...prev,
        isLoading: false,
        error: msg
      }))
      return false
    }
  }, [navigate])

  const logout = useCallback(async () => {
    try {
      await apiClient.post(ENDPOINTS.auth.logout)
    } catch {
      // Ignore logout errors — still clear local state
    } finally {
      clearTokens()
      setState({
        user: null,
        isAuthenticated: false,
        isLoading: false,
        error: null
      })
      navigate('/login', { replace: true })
    }
  }, [navigate])

  const register = useCallback(async (data: Omit<LoginRequest, 'password'> & { password: string; firstName: string; lastName: string; employeeId: string; roleId: string }): Promise<boolean> => {
    try {
      setState(prev => ({ ...prev, isLoading: true, error: null }))
      await apiClient.post<ApiResponse<User>>(ENDPOINTS.auth.register, {
        email: data.email,
        password: data.password,
        firstName: data.firstName,
        lastName: data.lastName,
        employeeId: data.employeeId,
        roleId: data.roleId
      })
      
      // Auto-login after registration
      return login({ email: data.email, password: data.password })
    } catch (error: unknown) {
      const msg = error instanceof Error ? error.message : 'Registration failed.'
      setState(prev => ({
        ...prev,
        isLoading: false,
        error: msg
      }))
      return false
    }
  }, [login])

  const hasRole = useCallback((role: Role | Role[]): boolean => {
    if (!state.user) return false
    const roles = Array.isArray(role) ? role : [role]
    const roleHierarchy: Record<Role, number> = {
      SuperAdmin: 6,
      SeniorManager: 5,
      Manager: 4,
      Coordinator: 3,
      TeamLeader: 2,
      Operator: 1
    }
    const userLevel = roleHierarchy[state.user.roleName] ?? 0
    return roles.some(r => (roleHierarchy[r] ?? 0) <= userLevel)
  }, [state.user])

  const hasAnyRole = useCallback((...roles: Role[]): boolean => {
    return hasRole(roles)
  }, [hasRole])

  const hasAllRoles = useCallback((...roles: Role[]): boolean => {
    if (!state.user) return false
    const roleHierarchy: Record<Role, number> = {
      SuperAdmin: 6,
      SeniorManager: 5,
      Manager: 4,
      Coordinator: 3,
      TeamLeader: 2,
      Operator: 1
    }
    const userLevel = roleHierarchy[state.user.roleName] ?? 0
    return roles.every(r => (roleHierarchy[r] ?? 0) <= userLevel)
  }, [state.user])

  const clearError = useCallback(() => {
    setState(prev => ({ ...prev, error: null }))
  }, [])

  return (
    <AuthContext.Provider value={{
      ...state,
      login,
      logout,
      register,
      hasRole,
      hasAnyRole,
      hasAllRoles,
      refreshUser,
      clearError
    }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth(): AuthContextType {
  const context = useContext(AuthContext)
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
