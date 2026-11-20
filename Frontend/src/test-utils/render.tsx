import { render, type RenderOptions } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { AuthProvider } from '@/core/auth/AuthContext'
import type { User } from '@/core/api/types'
import { mockSuperAdmin } from './mocks'
import React from 'react'

interface ProviderWrapperProps {
  user?: User
}

function TestProviderWrapper({ children, user }: { children: React.ReactNode; user?: User }) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false, staleTime: 0 },
    },
  })

  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>{children}</AuthProvider>
    </QueryClientProvider>
  )
}

/**
 * Custom render that wraps components with necessary providers.
 */
function customRender(
  ui: React.ReactElement,
  options: Omit<RenderOptions, 'wrapper'> & { user?: User } = {}
) {
  const { user = mockSuperAdmin, ...renderOptions } = options

  // Set up auth state via localStorage before rendering
  if (user) {
    window.localStorage.setItem('wacams_access_token', 'mock-token')
  }

  const Wrapper = ({ children }: { children: React.ReactNode }) => (
    <TestProviderWrapper user={user}>{children}</TestProviderWrapper>
  )

  const renderResult = render(ui, { ...renderOptions, wrapper: Wrapper })
  return { ...renderResult, rerender: undefined as any }
}

// Re-export everything from testing-library
export * from '@testing-library/react'

// Override render with our custom one
export { customRender as render }
