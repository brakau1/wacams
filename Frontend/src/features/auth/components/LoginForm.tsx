import { useState } from 'react'
import { useAuth } from '@/core/auth/useAuth'
import PasswordInput from './PasswordInput'
import { LoadingSpinner } from '@/shared/components/LoadingSpinner'

interface LoginFormProps {
  onSuccess?: () => void
}

export default function LoginForm({ onSuccess }: LoginFormProps) {
  const { login, isLoading, error, clearError } = useAuth()
  const [employeeId, setEmployeeId] = useState('')
  const [password, setPassword] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!employeeId.trim() || !password.trim()) return
    
    setSubmitting(true)
    clearError()
    try {
      await login({ employeeId, password })
      onSuccess?.()
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-5">
      {/* Error display */}
      {error && (
        <div className="p-3 rounded-lg bg-destructive/10 border border-destructive/20 text-destructive text-sm">
          {error}
        </div>
      )}

      {/* Employee ID */}
      <div className="space-y-2">
        <label htmlFor="login-employeeId" className="text-sm font-medium">
          Employee ID
        </label>
        <input
          id="login-employeeId"
          type="text"
          value={employeeId}
          onChange={(e) => setEmployeeId(e.target.value)}
          placeholder="e.g., 708513"
          required
          autoComplete="username"
          className="w-full px-4 py-3 rounded-lg border border-border bg-background text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary/50"
        />
      </div>

      {/* Password */}
      <div className="space-y-2">
        <label htmlFor="login-password" className="text-sm font-medium">
          Password
        </label>
        <PasswordInput
          id="login-password"
          value={password}
          onChange={setPassword}
          placeholder="Enter your password"
        />
      </div>

      {/* Remember me + Forgot password row */}
      <div className="flex items-center justify-between text-sm">
        <label className="flex items-center gap-2 cursor-pointer">
          <input type="checkbox" className="rounded border-border text-primary focus:ring-primary" />
          <span>Remember me</span>
        </label>
        <button type="button" className="text-primary hover:underline">
          Forgot password?
        </button>
      </div>

      {/* Submit button */}
      <button
        type="submit"
        disabled={submitting || isLoading}
        className="w-full py-3 rounded-lg bg-primary text-primary-foreground font-semibold hover:opacity-90 transition-opacity disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2"
      >
        {submitting || isLoading ? (
          <>
            <LoadingSpinner size="sm" />
            Signing in...
          </>
        ) : (
          'Sign In'
        )}
      </button>
    </form>
  )
}
