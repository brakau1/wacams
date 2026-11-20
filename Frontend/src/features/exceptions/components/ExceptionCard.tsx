import { cn } from '@/core/utils/cn'
import StatusChip from '../../attendance/components/StatusChip'
import { AlertTriangle, Clock, User } from 'lucide-react'
import type { AttendanceException } from '@/core/api/types'

interface ExceptionCardProps {
  exception: AttendanceException
  onClick?: () => void
}

export default function ExceptionCard({ exception, onClick }: ExceptionCardProps) {
  const TYPE_ICONS: Record<string, string> = {
    LateArrival: '⏰',
    EarlyDeparture: '🚪',
    UnplannedAbsenteeism: '❌',
    AWOL: '⚠️',
    SickLeave: '🤒',
    FamilyResponsibilityLeave: '👨‍👩‍👧',
    ShiftCancellation: '📅',
    WorkshopAttendance: '🎓',
    Training: '📚',
    MissingClockIn: '⏱️',
    MissingClockOut: '⏲️',
    UnscheduledClockIn: '🔓',
    UnscheduledClockOut: '🔒'
  }

  return (
    <div
      onClick={onClick}
      className={cn(
        'rounded-xl border p-4 cursor-pointer transition-all hover:shadow-md hover:shadow-black/20',
        exception.status === 'Open' ? 'border-red-500/30 bg-red-500/5' :
        exception.status === 'UnderReview' ? 'border-yellow-500/30 bg-yellow-500/5' :
        exception.status === 'Escalated' ? 'border-purple-500/30 bg-purple-500/5' :
        'border-border bg-card'
      )}
    >
      {/* Header */}
      <div className="flex items-start justify-between mb-3">
        <div className="flex items-center gap-2">
          <span className="text-lg">{TYPE_ICONS[exception.exceptionType] || '⚠️'}</span>
          <div>
            <p className="font-semibold text-sm">{exception.displayName}</p>
            <p className="text-xs text-muted-foreground">{exception.departmentName}</p>
          </div>
        </div>
        <StatusChip status={exception.status} size="sm" />
      </div>

      {/* Details */}
      <div className="space-y-2">
        <p className="text-xs text-muted-foreground">{exception.description}</p>
        <div className="flex items-center gap-4 text-xs text-muted-foreground">
          <span className="flex items-center gap-1">
            <Clock className="w-3 h-3" />
            {new Date(exception.date).toLocaleDateString()}
          </span>
          <span className="flex items-center gap-1">
            ⏱ {exception.lostProductionMinutes}m lost
          </span>
        </div>
        {exception.assignedToName && (
          <div className="flex items-center gap-1 text-xs text-muted-foreground pt-2 border-t border-border">
            <User className="w-3 h-3" />
            Assigned to: {exception.assignedToName}
          </div>
        )}
      </div>
    </div>
  )
}
