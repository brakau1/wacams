import { useAuth } from '@/core/auth/useAuth'
import LoginForm from './components/LoginForm'
import { Shield, Clock, AlertTriangle, FileWarning } from 'lucide-react'

export default function LoginPage() {
  const { isAuthenticated } = useAuth()

  if (isAuthenticated) {
    return null // Redirect handled by ProtectedRoute
  }

  return (
    <div className="min-h-screen flex bg-background">
      {/* Left panel — branding */}
      <div className="hidden lg:flex lg:w-1/2 relative overflow-hidden bg-gradient-to-br from-[#0f3460] to-[#1a1a2e]">
        {/* Decorative grid */}
        <div className="absolute inset-0 opacity-5" style={{
          backgroundImage: 'radial-gradient(circle at 1px 1px, white 1px, transparent 0)',
          backgroundSize: '40px 40px'
        }} />

        <div className="relative z-10 flex flex-col justify-center px-16 text-white">
          <div className="mb-8">
            <div className="flex items-center gap-3 mb-6">
              <div className="w-14 h-14 rounded-xl bg-primary flex items-center justify-center shadow-lg">
                <Shield className="w-8 h-8 text-white" />
              </div>
              <div>
                <h1 className="text-3xl font-bold">WACAMS</h1>
                <p className="text-sm text-blue-200">Workforce Management</p>
              </div>
            </div>
            <h2 className="text-4xl font-bold leading-tight mb-4">
              Attendance &<br />Corrective Action<br />Management
            </h2>
            <p className="text-lg text-blue-200 max-w-md">
              Monitor workforce attendance in real-time, track exceptions, manage corrective actions, and ensure compliance.
            </p>
          </div>

          {/* Feature highlights */}
          <div className="space-y-4 mt-8">
            {[
              { icon: Clock, label: 'Real-time Attendance Tracking' },
              { icon: AlertTriangle, label: 'Exception Detection & Investigation' },
              { icon: FileWarning, label: 'Automated Warning Generation' },
              { icon: Shield, label: 'Audit Trail & Compliance' }
            ].map(({ icon: Icon, label }, i) => (
              <div key={i} className="flex items-center gap-3 p-3 rounded-lg bg-white/5 backdrop-blur">
                <Icon className="w-5 h-5 text-blue-300" />
                <span className="text-sm">{label}</span>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Right panel — login form */}
      <div className="flex-1 flex items-center justify-center p-8">
        <div className="w-full max-w-md">
          {/* Mobile logo */}
          <div className="lg:hidden flex items-center gap-3 mb-8 justify-center">
            <div className="w-10 h-10 rounded-lg bg-primary flex items-center justify-center">
              <Shield className="w-6 h-6 text-white" />
            </div>
            <span className="text-xl font-bold">WACAMS</span>
          </div>

          <div className="mb-8">
            <h2 className="text-2xl font-bold mb-2">Welcome back</h2>
            <p className="text-muted-foreground">Sign in to your account to continue</p>
          </div>

          <LoginForm />

          <p className="mt-6 text-center text-xs text-muted-foreground">
            By signing in, you agree to our Terms of Service and Privacy Policy.
          </p>
        </div>
      </div>
    </div>
  )
}
