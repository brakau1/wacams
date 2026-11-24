import axios, { AxiosError, InternalAxiosRequestConfig, AxiosResponse } from 'axios'
import { ApiError, ApiResponse } from './types'
import { isMockMode, handleMockRequest } from '@/mocks/mockHandler'

// ─── Axios Instance ────────────────────────────────────────────────────────

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json'
  }
})

// ─── Mock Mode Adapter ──────────────────────────────────────────────────────

if (isMockMode()) {
  console.log('[WACAMS] Running in MOCK MODE - all API calls will be intercepted')
  
  apiClient.defaults.adapter = async (config: InternalAxiosRequestConfig): Promise<AxiosResponse> => {
    try {
      const mockResponse = await handleMockRequest(config)
      return {
        data: mockResponse,
        status: 200,
        statusText: 'OK',
        headers: {},
        config
      }
    } catch (error: any) {
      if (error.response) {
        const axiosError = new Error(error.response.data?.message || 'Mock error') as AxiosError
        axiosError.config = config
        axiosError.request = {}
        axiosError.response = error.response
        throw axiosError
      }
      throw error
    }
  }
}

// ─── Token Storage ─────────────────────────────────────────────────────────

const TOKEN_KEY = 'wacams_access_token'
const REFRESH_TOKEN_KEY = 'wacams_refresh_token'

export function getAccessToken(): string | null {
  return localStorage.getItem(TOKEN_KEY)
}

export function setAccessToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token)
}

export function removeAccessToken(): void {
  localStorage.removeItem(TOKEN_KEY)
}

export function getRefreshToken(): string | null {
  return localStorage.getItem(REFRESH_TOKEN_KEY)
}

export function setRefreshToken(token: string): void {
  localStorage.setItem(REFRESH_TOKEN_KEY, token)
}

export function clearTokens(): void {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(REFRESH_TOKEN_KEY)
}

// ─── Request Interceptor ───────────────────────────────────────────────────

apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = getAccessToken()
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => Promise.reject(error)
)

// ─── Response Interceptor ──────────────────────────────────────────────────

let isRefreshing = false
let failedQueue: {
  resolve: (value: unknown) => void
  reject: (reason?: unknown) => void
  config: InternalAxiosRequestConfig
}[] = []

function processQueue(error: AxiosError | null, token: string | null = null): void {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error)
    } else if (token) {
      prom.resolve(token)
    }
  })
  failedQueue = []
}

apiClient.interceptors.response.use(
  (response: AxiosResponse<ApiResponse<unknown>>) => {
    // Normalize: if backend wraps in { success, data }, unwrap to standard AxiosResponse shape
    const body = response.data
    if (body && typeof body === 'object' && 'success' in body && 'data' in body) {
      ;(response as any).data = (body as ApiResponse<unknown>).data
    }
    return response
  },
  async (error: AxiosError<ApiError>) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean }

    // Handle 401 Unauthorized
    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject, config: originalRequest })
        }).then((token) => {
          if (originalRequest.headers) {
            originalRequest.headers.Authorization = `Bearer ${token}`
          }
          return apiClient(originalRequest)
        })
      }

      originalRequest._retry = true
      isRefreshing = true

      try {
        const refreshToken = getRefreshToken()
        if (!refreshToken) {
          throw new Error('No refresh token')
        }

        const { default: axios } = await import('axios')
        const { ENDPOINTS } = await import('./endpoints')

        const refreshResponse = await axios.post<ApiResponse<{ accessToken: string; refreshToken: string }>>(
          ENDPOINTS.auth.refreshToken,
          { refreshToken }
        )

        const newTokens = refreshResponse.data.data
        setAccessToken(newTokens.accessToken)
        setRefreshToken(newTokens.refreshToken)

        processQueue(null, newTokens.accessToken)

        if (originalRequest.headers) {
          originalRequest.headers.Authorization = `Bearer ${newTokens.accessToken}`
        }
        return apiClient(originalRequest)
      } catch (refreshError) {
        processQueue(error as AxiosError, null)
        clearTokens()
        // Redirect to login — use a safe approach
        if (typeof window !== 'undefined') {
          window.location.href = '/login?expired=true'
        }
        return Promise.reject(refreshError)
      } finally {
        isRefreshing = false
      }
    }

    // Handle other errors
    const apiError: ApiError = {
      statusCode: error.response?.status ?? 500,
      message: error.response?.data?.message ?? error.message ?? 'An unexpected error occurred'
    }

    // Show toast notification for common errors
    if (apiError.statusCode >= 400 && apiError.statusCode < 500) {
      // Client error — show user-friendly message
      console.warn(`API Client Error [${apiError.statusCode}]:`, apiError.message)
    } else if (apiError.statusCode >= 500) {
      console.error(`API Server Error [${apiError.statusCode}]:`, apiError.message)
    }

    return Promise.reject(apiError)
  }
)

// ─── Typed Service Functions ───────────────────────────────────────────────

/**
 * GET request with type safety
 */
export async function get<T>(url: string, params?: Record<string, string | number>): Promise<ApiResponse<T>> {
  const response = await apiClient.get<ApiResponse<T>>(url, { params })
  return response.data as unknown as ApiResponse<T>
}

/**
 * POST request with type safety
 */
export async function post<T, D = unknown>(
  url: string,
  data?: D
): Promise<ApiResponse<T>> {
  const response = await apiClient.post<ApiResponse<T>>(url, data)
  return response.data as unknown as ApiResponse<T>
}

/**
 * PUT request with type safety
 */
export async function put<T, D = unknown>(
  url: string,
  data?: D
): Promise<ApiResponse<T>> {
  const response = await apiClient.put<ApiResponse<T>>(url, data)
  return response.data as unknown as ApiResponse<T>
}

/**
 * PATCH request with type safety
 */
export async function patch<T, D = unknown>(
  url: string,
  data?: D
): Promise<ApiResponse<T>> {
  const response = await apiClient.patch<ApiResponse<T>>(url, data)
  return response.data as unknown as ApiResponse<T>
}

/**
 * DELETE request with type safety
 */
export async function del<T>(url: string): Promise<ApiResponse<T>> {
  const response = await apiClient.delete<ApiResponse<T>>(url)
  return response.data as unknown as ApiResponse<T>
}

/**
 * Upload multipart/form-data (e.g., signed warning form)
 */
export async function upload<T>(
  url: string,
  formData: FormData
): Promise<ApiResponse<T>> {
  const response = await apiClient.post<ApiResponse<T>>(url, formData, {
    headers: { 'Content-Type': 'multipart/form-data' }
  })
  return response.data as unknown as ApiResponse<T>
}

export default apiClient
export { apiClient as api }
