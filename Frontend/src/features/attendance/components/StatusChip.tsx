import { cn } from '@/core/utils/cn'
import type { AttendanceStatus as StatusType } from '@/core/api/types'

interface StatusChipProps {
  status: string | StatusType
  color?: string
  size?: 'sm' | 'md'
}

const STATUS_COLORS: Record<string, string> = {
  Present: 'bg-green-500/10 text-green-400 border-green-500/30',
  OnTime: 'bg-green-500/10 text-green-400 border-green-500/30',
  Late: 'bg-yellow-500/10 text-yellow-400 border-yellow-500/30',
  EarlyDeparture: 'bg-orange-500/10 text-orange-400 border-orange-500/30',
  Absent: 'bg-red-500/10 text-red-400 border-red-500/30',
  AWOL: 'bg-red-600/10 text-red-500 border-red-600/30',
  SickLeave: 'bg-blue-500/10 text-blue-400 border-blue-500/30',
  FamilyResponsibilityLeave: 'bg-indigo-500/10 text-indigo-400 border-indigo-500/30',
  AnnualLeave: 'bg-sky-500/10 text-sky-400 border-sky-500/30',
  Rest: 'bg-gray-500/10 text-gray-400 border-gray-500/30',
  UnscheduledClockIn: 'bg-plum-500/10 text-purple-400 border-purple-500/30',
  MissingClockIn: 'bg-yellow-500/10 text-yellow-400 border-yellow-500/30',
  MissingClockOut: 'bg-orange-500/10 text-orange-400 border-orange-500/30',
  UnderReview: 'bg-yellow-500/10 text-yellow-400 border-yellow-500/30',
  Investigated: 'bg-blue-500/10 text-blue-400 border-blue-500/30',
  Closed: 'bg-green-500/10 text-green-400 border-green-500/30',
  Escalated: 'bg-purple-500/10 text-purple-400 border-purple-500/30',
  Open: 'bg-red-500/10 text-red-400 border-red-500/30',
  Draft: 'bg-gray-500/10 text-gray-400 border-gray-500/30',
  Issued: 'bg-orange-500/10 text-orange-400 border-orange-500/30',
  Acknowledged: 'bg-blue-500/10 text-blue-400 border-blue-500/30',
  Archived: 'bg-gray-500/10 text-gray-400 border-gray-500/30',
  Pending: 'bg-yellow-500/10 text-yellow-400 border-yellow-500/30',
  InProgress: 'bg-blue-500/10 text-blue-400 border-blue-500/30',
  Completed: 'bg-green-500/10 text-green-400 border-green-500/30',
  Expired: 'bg-red-500/10 text-red-400 border-red-500/30'
}

export default function StatusChip({ status, color, size = 'md' }: StatusChipProps) {
  const baseColor = color || STATUS_COLORS[status] || 'bg-gray-500/10 text-gray-400 border-gray-500/30'
  const sizes = {
    sm: 'px-1.5 py-0.5 text-[10px]',
    md: 'px-2 py-0.5 text-xs'
  }

  return (
    <span className={cn(
      'inline-flex items-center rounded-full font-medium border',
      baseColor,
      sizes[size]
    )}>
      {status.replace(/([A-Z])/g, ' $1').trim()}
    </span>
  )
}
