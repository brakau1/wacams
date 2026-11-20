import { useState } from 'react'
import { useAuth } from '@/core/auth/useAuth'
import { useNavigate, useSearchParams } from 'react-router-dom'
import PasswordInput from './components/PasswordInput'
import { LoadingSpinner } from '@/shared/components/LoadingSpinner'
import { Shield, UserPlus } from 'lucide-react'

export default function RegisterPage() {
  const { register, isLoading, error, clearError } = useAuth()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const [formData, setFormData] = useState({
    email: '',
    password: '',
    firstName: '',
    lastName: '',
    employeeId: ''
  })
  const [submitting, setSubmitting] = useState(false)

  // Only allow access if user is admin (handled by ProtectedRoute with role check)
  const isAdmin = searchParams.get('admin') === 'true'

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!Object.values(formData).every(v => v.trim())) return

    setSubmitting(true)
    clearError()
    try {
      const success = await register({ ...formData, roleId: 'Operator' })
      if (success) {
        navigate('/dashboard')
      }
    } finally {
      setSubmitting(false)
    }
  }

  if (!isAdmin) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="text-center">
          <Shield className="w-16 h-16 text-destructive mx-auto mb-4" />
          <h2 className="text-xl font-bold mb-2">Access Denied</h2>
          <p className="text-muted-foreground mb-4">Registration is restricted to administrators only.</p>
          <button onClick={() => navigate('/login')} className="px-4 py-2 rounded-lg bg-primary text-primary-foreground">
            Back to Login
          </button>
        </div>
      </div>
    )
  }

  return (
    <div className="min-h-screen flex bg-background">
      {/* Left panel */}
      <div className="hidden lg:flex lg:w-1/2 relative overflow-hidden bg-gradient-to-br from-[#0f3460] to-[#1a1a2e]">
        <div className="absolute inset-0 opacity-5" style={{
          backgroundImage: 'radial-gradient(circle at 1px 1px, white 1px, transparent 0)',
          backgroundSize: '40px 40px'
        }} />
        <div className="relative z-10 flex flex-col justify-center px-16 text-white">
          <UserPlus className="w-14 h-14 mb-6 text-blue-300" />
          <h2 className="text-3xl font-bold mb-4">Create Account</h2>
          <p className="text-lg text-blue-200 max-w-md">
            Register a new user in the WACAMS system. This requires administrator privileges.
          </p>
        </div>
      </div>

      {/* Right panel — registration form */}
      <div className="flex-1 flex items-center justify-center p-8">
        <div className="w-full max-w-md">
          <div className="mb-8">
            <h2 className="text-2xl font-bold mb-2">New Registration</h2>
            <p className="text-muted-foreground">Fill in the details to create a new account</p>
          </div>

          {error && (
            <div className="p-3 rounded-lg bg-destructive/10 border border-destructive/20 text-destructive text-sm mb-5">
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <label htmlFor="firstName" className="text-sm font-medium">First Name</label>
                <input
                  id="firstName"
                  type="text"
                  value={formData.firstName}
                  onChange={(e) => setFormData(prev => ({ ...prev, firstName: e.target.value }))}
                  placeholder="John"
                  required
                  className="w-full px-4 py-3 rounded-lg border border-border bg-background text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary/50"
                />
              </div>
              <div className="space-y-2">
                <label htmlFor="lastName" className="text-sm font-medium">Last Name</label>
                <input
                  id="lastName"
                  type="text"
                  value={formData.lastName}
                  onChange={(e) => setFormData(prev => ({ ...prev, lastName: e.target.value }))}
                  placeholder="Doe"
                  required
                  className="w-full px-4 py-3 rounded-lg border border-border bg-background text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary/50"
                />
              </div>
            </div>

            <div className="space-y-2">
              <label htmlFor="employeeId" className="text-sm font-medium">Employee ID</label>
              <input
                id="employeeId"
                type="text"
                value={formData.employeeId}
                onChange={(e) => setFormData(prev => ({ ...prev, employeeId: e.target.value }))}
                placeholder="e.g., 708513"
                required
                className="w-full px-4 py-3 rounded-lg border border-border bg-background text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary/50"
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="email" className="text-sm font-medium">Email Address</label>
              <input
                id="email"
                type="email"
                value={formData.email}
                onChange={(e) => setFormData(prev => ({ ...prev, email: e.target.value }))}
                placeholder="john@company.com"
                required
                autoComplete="email"
                className="w-full px-4 py-3 rounded-lg border border-border bg-background text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary/50"
              />
            </div>

            <div className="space-y-2">
              <label htmlFor="password" className="text-sm font-medium">Password</label>
              <PasswordInput
                id="password"
                value={formData.password}
                onChange={(v) => setFormData(prev => ({ ...prev, password: v }))}
                placeholder="Min. 8 characters"
              />
            </div>

            <button
              type="submit"
              disabled={submitting || isLoading}
              className="w-full py-3 rounded-lg bg-primary text-primary-foreground font-semibold hover:opacity-90 transition-opacity disabled:opacity-50 disabled:cursor-not-allowed flex items-center justify-center gap-2 mt-6"
            >
              {submitting || isLoading ? (
                <>
                  <LoadingSpinner size="sm" />
                  Creating account...
                </>
              ) : (
                'Create Account'
              )}
            </button>
          </form>

          <button onClick={() => navigate('/login')} className="mt-4 text-sm text-primary hover:underline w-full text-center block">
            Already have an account? Sign in
          </button>
        </div>
      </div>
    </div>
  )
}
