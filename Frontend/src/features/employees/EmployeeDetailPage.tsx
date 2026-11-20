import { cn } from '@/core/utils/cn'
import { useParams, useNavigate } from 'react-router-dom'
import { ArrowLeft, Clock, Mail, Building2, User, AlertTriangle, FileWarning, ChevronRight, Edit3 } from 'lucide-react'
import StatusChip from '../attendance/components/StatusChip'
import type { Employee } from '@/core/api/types'
import { formatDisplayDate } from '@/core/utils/formatDate'
import EmptyState from '@/shared/components/EmptyState'
import { LoadingSpinner } from '@/shared/components/LoadingSpinner'
import { useEmployee, useEmployeeExceptions, useEmployeeWarnings } from '@/core/hooks/useDashboardQueries'

export default function EmployeeDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  
  const { data: employee, isLoading: loadingEmployee } = useEmployee(id || '')
  const { data: exceptions = [] } = useEmployeeExceptions(id || '')
  const { data: warnings = [] } = useEmployeeWarnings(id || '')

  if (loadingEmployee || !employee) {
    return <LoadingSpinner size="lg" label="Loading employee details..." />
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <button onClick={() => navigate(-1)} className="flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground transition-colors">
          <ArrowLeft className="w-4 h-4" />
          Back to employees
        </button>
        <button className="flex items-center gap-2 px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90">
          <Edit3 className="w-4 h-4" />
          Edit
        </button>
      </div>

      {/* Profile Card */}
      <div className="rounded-xl border border-border bg-card p-6">
        <div className="flex flex-col sm:flex-row gap-6">
          {/* Avatar */}
          <div className="flex-shrink-0">
            <div className="w-20 h-20 rounded-2xl bg-gradient-to-br from-primary to-[#e94560] flex items-center justify-center">
              <span className="text-2xl font-bold text-white">{employee.displayName.charAt(0)}</span>
            </div>
          </div>

          {/* Info */}
          <div className="flex-1 space-y-3">
            <div>
              <h2 className="text-2xl font-bold">{employee.displayName}</h2>
              <p className="text-muted-foreground">{employee.jobTitle}</p>
            </div>

            <div className="flex flex-wrap gap-3">
              <span className={cn('inline-flex px-3 py-1 rounded-full text-xs font-medium border', getStatusColor(employee))}>
                {employee.status}
              </span>
              <span className="px-3 py-1 rounded-full text-xs font-medium border bg-blue-500/10 text-blue-400 border-blue-500/30">
                {employee.shiftCode}
              </span>
              {employee.isClockIn && (
                <span className="px-3 py-1 rounded-full text-xs font-medium border bg-green-500/10 text-green-400 border-green-500/30">
                  Clocked In · {employee.clockInTime}
                </span>
              )}
            </div>

            <div className="grid grid-cols-2 sm:grid-cols-3 gap-4 pt-2">
              {[
                { icon: User, label: 'Employee ID', value: employee.employeeId },
                { icon: Mail, label: 'Email', value: employee.email },
                { icon: Building2, label: 'Department', value: employee.departmentName },
                { icon: User, label: 'Manager', value: employee.managerName || '—' },
                { icon: Clock, label: 'Hire Date', value: formatDisplayDate(employee.hireDate) },
                { icon: Clock, label: 'Shift', value: `${employee.shiftCode}` }
              ].map(({ icon: Icon, label, value }) => (
                <div key={label} className="flex items-center gap-2 text-sm">
                  <Icon className="w-4 h-4 text-muted-foreground flex-shrink-0" />
                  <div>
                    <p className="text-xs text-muted-foreground">{label}</p>
                    <p className="font-medium">{value}</p>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>

      {/* Manager Hierarchy */}
      <div className="rounded-xl border border-border bg-card p-5">
        <h3 className="font-semibold mb-4">Management Hierarchy</h3>
        <div className="flex items-center gap-2 flex-wrap">
          <span className="px-3 py-1.5 rounded-lg bg-primary/10 text-primary text-sm font-medium">You</span>
          <ChevronRight className="w-4 h-4 text-muted-foreground" />
          <span className="px-3 py-1.5 rounded-lg bg-muted text-foreground text-sm">{employee.managerName || 'N/A'}</span>
          <ChevronRight className="w-4 h-4 text-muted-foreground" />
          <span className="px-3 py-1.5 rounded-lg bg-muted/50 text-muted-foreground text-sm">Production Manager</span>
          <ChevronRight className="w-4 h-4 text-muted-foreground" />
          <span className="px-3 py-1.5 rounded-lg bg-muted/50 text-muted-foreground text-sm">Operations Director</span>
        </div>
      </div>

      {/* Two-column layout: Exceptions & Warnings */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Exception History */}
        <div className="rounded-xl border border-border bg-card p-5">
          <div className="flex items-center gap-2 mb-4">
            <AlertTriangle className="w-5 h-5 text-orange-500" />
            <h3 className="font-semibold">Exception History</h3>
          </div>
          {exceptions.length === 0 ? (
            <EmptyState icon="check" title="No exceptions" description="This employee has no recorded exceptions." />
          ) : (
            <div className="space-y-3">
              {exceptions.map(exc => (
                <div key={exc.id} className="p-3 rounded-lg bg-muted/30 border border-border">
                  <div className="flex items-center justify-between mb-1">
                    <span className="text-sm font-medium">{exc.exceptionType.replace(/([A-Z])/g, ' $1').trim()}</span>
                    <StatusChip status={exc.status} size="sm" />
                  </div>
                  <p className="text-xs text-muted-foreground">{exc.description}</p>
                  <p className="text-xs text-muted-foreground mt-1">{new Date(exc.date).toLocaleDateString()}</p>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Warning History */}
        <div className="rounded-xl border border-border bg-card p-5">
          <div className="flex items-center gap-2 mb-4">
            <FileWarning className="w-5 h-5 text-red-500" />
            <h3 className="font-semibold">Warning History</h3>
          </div>
          {warnings.length === 0 ? (
            <EmptyState icon="check" title="No warnings" description="This employee has no warning records." />
          ) : (
            <div className="space-y-3">
              {warnings.map(w => (
                <div key={w.id} className="p-3 rounded-lg bg-destructive/5 border border-destructive/20">
                  <div className="flex items-center justify-between mb-1">
                    <span className="text-sm font-medium text-destructive">{w.warningType.replace(/([A-Z])/g, ' $1').trim()}</span>
                    <StatusChip status={w.status} size="sm" color="green" />
                  </div>
                  <p className="text-xs text-muted-foreground">{w.title}</p>
                  <p className="text-xs text-muted-foreground mt-1">{new Date(w.createdAt).toLocaleDateString()}</p>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}

function getStatusColor(emp: Employee): string {
  if (emp.status === 'OnLeave') return 'bg-blue-500/10 text-blue-400 border-blue-500/30'
  if (['Inactive', 'Terminated'].includes(emp.status)) return 'bg-gray-500/10 text-gray-400 border-gray-500/30'
  return 'bg-green-500/10 text-green-400 border-green-500/30'
}
