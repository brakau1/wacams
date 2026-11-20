import { useState } from 'react'
import { useLocation } from 'react-router-dom'
import { useAuth } from '@/core/auth/useAuth'
import { useTheme } from '@/core/theme/ThemeProvider'
import { cn } from '@/core/utils/cn'
import { Menu, X } from 'lucide-react'
import Sidebar from './Sidebar'
import Header from './Header'

interface LayoutProps {
  children?: React.ReactNode
}

export default function Layout({ children }: LayoutProps) {
  const [sidebarOpen, setSidebarOpen] = useState(false)
  const location = useLocation()
  const { user } = useAuth()
  const { theme } = useTheme()

  // Close sidebar on route change (mobile)
  const handleNavigate = () => {
    if (window.innerWidth < 1024) {
      setSidebarOpen(false)
    }
  }

  return (
    <div className={cn('min-h-screen bg-background text-foreground flex')}>
      {/* Desktop Sidebar */}
      <aside className="hidden lg:flex lg:flex-col lg:w-64 lg:fixed lg:inset-y-0 border-r border-border bg-card">
        <Sidebar />
      </aside>

      {/* Mobile overlay */}
      {sidebarOpen && (
        <div
          className="fixed inset-0 z-40 bg-black/50 lg:hidden"
          onClick={() => setSidebarOpen(false)}
        />
      )}

      {/* Mobile Sidebar */}
      <div className={cn(
        'fixed inset-y-0 left-0 z-50 w-64 bg-card border-r border-transform transition-transform duration-200 ease-in-out',
        sidebarOpen ? 'translate-x-0' : '-translate-x-full',
        'lg:hidden'
      )}>
        <div className="flex items-center justify-between p-4 border-b border-border">
          <span className="text-lg font-bold text-primary">WACAMS</span>
          <button
            onClick={() => setSidebarOpen(false)}
            className="p-2 rounded-md hover:bg-muted"
          >
            <X className="w-5 h-5" />
          </button>
        </div>
        <Sidebar onNavigate={handleNavigate} />
      </div>

      {/* Main content */}
      <div className="flex-1 lg:ml-64 flex flex-col min-h-screen">
        <Header onMenuClick={() => setSidebarOpen(true)} />
        <main className="flex-1 p-4 sm:p-6 lg:p-8 overflow-auto">
          <div className="max-w-7xl mx-auto animate-fade-in">
            {children}
          </div>
        </main>
        <footer className="border-t border-border px-6 py-3 text-xs text-muted-foreground">
          <div className="max-w-7xl mx-auto flex items-center justify-between">
            <span>© 2026 WACAMS — Workforce Attendance & Corrective Action Management</span>
            <span className="hidden sm:inline">{user?.displayName || ''} · {user?.roleName || ''}</span>
          </div>
        </footer>
      </div>
    </div>
  )
}
