import { NavLink } from 'react-router-dom'
import { useAuth } from '@/core/auth/useAuth'
import { cn } from '@/core/utils/cn'
import type { Role } from '@/core/api/types'
import {
  LayoutDashboard,
  Users,
  Clock,
  AlertTriangle,
  FileWarning,
  ArrowUpCircle,
  FileText,
  History,
  Settings,
  UserCog,
  Building2,
  Briefcase,
  Activity,
  Globe
} from 'lucide-react'

interface NavItem {
  label: string
  href: string
  icon: React.ReactNode
  requiredRole?: Role[]
}

export default function Sidebar({ onNavigate }: { onNavigate?: () => void }) {
  const { user, hasAnyRole } = useAuth()

  const navItems: NavItem[] = [
    { label: 'Dashboard', href: '/dashboard', icon: <LayoutDashboard className="w-5 h-5" /> },
    { label: 'Manager Dashboard', href: '/manager-dashboard', icon: <Briefcase className="w-5 h-5" />, requiredRole: ['Coordinator', 'Manager', 'SeniorManager', 'SuperAdmin'] },
    { label: 'HR Command Centre', href: '/hr-command-centre', icon: <Activity className="w-5 h-5" />, requiredRole: ['Coordinator', 'Manager', 'SeniorManager', 'SuperAdmin'] },
    { label: 'Executive Dashboard', href: '/executive-dashboard', icon: <LayoutDashboard className="w-5 h-5" />, requiredRole: ['Manager', 'SeniorManager', 'SuperAdmin'] },
    { label: 'Employees', href: '/employees', icon: <Users className="w-5 h-5" /> },
    { label: 'Clock In/Out', href: '/clock-in', icon: <Clock className="w-5 h-5" />, requiredRole: ['Operator', 'TeamLeader', 'Coordinator', 'Manager', 'SeniorManager', 'SuperAdmin'] },
    { label: 'Attendance', href: '/attendance', icon: <Clock className="w-5 h-5" /> },
    { label: 'Exceptions', href: '/exceptions', icon: <AlertTriangle className="w-5 h-5" /> },
    { label: 'Warnings', href: '/warnings', icon: <FileWarning className="w-5 h-5" /> },
    { label: 'Escalations', href: '/escalations', icon: <ArrowUpCircle className="w-5 h-5" /> },
    { label: 'Reports', href: '/reports', icon: <FileText className="w-5 h-5" /> },
    { label: 'Audit Log', href: '/audit', icon: <History className="w-5 h-5" /> }
  ]

  const adminItems: NavItem[] = [
    { label: 'User Management', href: '/admin/users', icon: <UserCog className="w-5 h-5" />, requiredRole: ['Manager', 'SeniorManager', 'SuperAdmin'] },
    { label: 'Departments', href: '/admin/departments', icon: <Building2 className="w-5 h-5" />, requiredRole: ['Manager', 'SeniorManager', 'SuperAdmin'] },
    { label: 'System Settings', href: '/admin/settings', icon: <Settings className="w-5 h-5" />, requiredRole: ['Manager', 'SeniorManager', 'SuperAdmin'] },
    { label: 'GIS Integration', href: '/admin/gis-integration', icon: <Globe className="w-5 h-5" />, requiredRole: ['SuperAdmin'] }
  ]

  // Filter items by role
  const filteredNavItems = navItems.filter(item =>
    !item.requiredRole || hasAnyRole(...(item.requiredRole as Role[]))
  )

  const filteredAdminItems = adminItems.filter(item =>
    !item.requiredRole || hasAnyRole(...(item.requiredRole as Role[]))
  )

  return (
    <nav className="flex flex-col h-full py-4 overflow-y-auto">
      {/* Logo */}
      <div className="px-4 mb-6">
        <NavLink to="/dashboard" onClick={onNavigate}>
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-lg bg-primary flex items-center justify-center">
              <span className="text-white font-bold text-sm">W</span>
            </div>
            <div>
              <h1 className="font-bold text-base leading-tight">WACAMS</h1>
              <p className="text-[10px] text-muted-foreground leading-tight">Workforce Management</p>
            </div>
          </div>
        </NavLink>
      </div>

      {/* Main Navigation */}
      <div className="px-3 mb-2">
        <p className="text-[11px] font-semibold uppercase tracking-wider text-muted-foreground px-3 mb-2">Main</p>
        <ul className="space-y-1">
          {filteredNavItems.map(item => (
            <li key={item.href}>
              <SidebarLink item={item} onClick={onNavigate} />
            </li>
          ))}
        </ul>
      </div>

      {/* Admin Section */}
      {filteredAdminItems.length > 0 && (
        <div className="px-3 mt-4 mb-2">
          <p className="text-[11px] font-semibold uppercase tracking-wider text-muted-foreground px-3 mb-2">Administration</p>
          <ul className="space-y-1">
            {filteredAdminItems.map(item => (
              <li key={item.href}>
                <SidebarLink item={item} onClick={onNavigate} />
              </li>
            ))}
          </ul>
        </div>
      )}

      {/* Bottom section - User info */}
      <div className="mt-auto px-4 py-3 border-t border-border">
        <div className="flex items-center gap-3">
          <div className="w-8 h-8 rounded-full bg-accent flex items-center justify-center flex-shrink-0">
            <span className="text-xs font-bold text-accent-foreground">
              {user?.displayName?.charAt(0).toUpperCase() || 'U'}
            </span>
          </div>
          <div className="min-w-0">
            <p className="text-sm font-medium truncate">{user?.displayName || 'User'}</p>
            <p className="text-xs text-muted-foreground truncate">{user?.roleName || ''}</p>
          </div>
        </div>
      </div>
    </nav>
  )
}

function SidebarLink({ item, onClick }: { item: NavItem; onClick?: () => void }) {
  return (
    <NavLink
      to={item.href}
      onClick={onClick}
      className={({ isActive }) => cn(
        'flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors',
        isActive
          ? 'bg-primary text-primary-foreground'
          : 'text-muted-foreground hover:bg-muted hover:text-foreground'
      )}
    >
      {item.icon}
      <span>{item.label}</span>
    </NavLink>
  )
}
