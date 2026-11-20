// Badge removed - using inline styles
import { AlertTriangle, Eye } from 'lucide-react'
import { cn } from '@/core/utils/cn'
import type { AttendanceException } from '@/core/api/types'

interface ExceptionTableProps {
  exceptions: AttendanceException[]
  onViewDetail?: (id: string) => void
  title?: string
  maxItems?: number
}

const STATUS_COLORS: Record<string, string> = {
  Open: 'bg-red-500/10 text-red-400 border-red-500/30',
  UnderReview: 'bg-yellow-500/10 text-yellow-400 border-yellow-500/30',
  Investigated: 'bg-blue-500/10 text-blue-400 border-blue-500/30',
  Closed: 'bg-green-500/10 text-green-400 border-green-500/30',
  Escalated: 'bg-purple-500/10 text-purple-400 border-purple-500/30'
}

const TYPE_LABELS: Record<string, string> = {
  LateArrival: 'Late Arrival',
  EarlyDeparture: 'Early Departure',
  UnplannedAbsenteeism: 'Absenteeism',
  AWOL: 'AWOL',
  SickLeave: 'Sick Leave',
  FamilyResponsibilityLeave: 'Family Resp.',
  MissingClockIn: 'Missing Clock In',
  MissingClockOut: 'Missing Clock Out',
  UnscheduledClockIn: 'Unscheduled In',
  UnscheduledClockOut: 'Unscheduled Out'
}

export default function ExceptionTable({
  exceptions,
  onViewDetail,
  title = 'Recent Exceptions',
  maxItems = 8
}: ExceptionTableProps) {
  const items = exceptions.slice(0, maxItems)

  return (
    <div className="rounded-xl border border-border bg-card p-5">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-2">
          <AlertTriangle className="w-5 h-5 text-orange-500" />
          <h3 className="font-semibold">{title}</h3>
        </div>
        {onViewDetail && (
          <button className="text-xs text-primary hover:underline">View all</button>
        )}
      </div>

      {items.length === 0 ? (
        <p className="text-sm text-muted-foreground text-center py-6">No recent exceptions.</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="border-b border-border">
              <tr>
                <th className="text-left pb-3 font-medium text-muted-foreground text-xs uppercase tracking-wider">Employee</th>
                <th className="text-left pb-3 font-medium text-muted-foreground text-xs uppercase tracking-wider hidden sm:table-cell">Type</th>
                <th className="text-left pb-3 font-medium text-muted-foreground text-xs uppercase tracking-wider">Date</th>
                <th className="text-left pb-3 font-medium text-muted-foreground text-xs uppercase tracking-wider">Status</th>
                <th className="text-right pb-3 font-medium text-muted-foreground text-xs uppercase tracking-wider">Action</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-border">
              {items.map(exc => (
                <tr key={exc.id} className="hover:bg-muted/30 transition-colors">
                  <td className="py-3">
                    <p className="font-medium">{exc.displayName}</p>
                    <p className="text-xs text-muted-foreground">{exc.departmentName}</p>
                  </td>
                  <td className="py-3 hidden sm:table-cell">
                    <span className="text-xs">{TYPE_LABELS[exc.exceptionType] || exc.exceptionType}</span>
                  </td>
                  <td className="py-3 text-xs text-muted-foreground">
                    {new Date(exc.date).toLocaleDateString()}
                  </td>
                  <td className="py-3">
                    <span className={cn(
                      'inline-flex px-2 py-0.5 rounded-full text-xs font-medium border',
                      STATUS_COLORS[exc.status] || 'bg-gray-500/10 text-gray-400 border-gray-500/30'
                    )}>
                      {exc.status}
                    </span>
                  </td>
                  <td className="py-3 text-right">
                    {onViewDetail && (
                      <button
                        onClick={() => onViewDetail(exc.id)}
                        className="p-1 rounded-md hover:bg-muted text-muted-foreground hover:text-foreground transition-colors"
                        aria-label="View details"
                      >
                        <Eye className="w-4 h-4" />
                      </button>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
