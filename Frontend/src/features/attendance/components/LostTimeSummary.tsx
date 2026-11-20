import { formatMinutes } from '@/core/utils/formatMinutes'
import { Clock, AlertTriangle, ArrowDownLeft } from 'lucide-react'

interface LostTimeData {
  totalLateMinutes: number
  totalEarlyMinutes: number
  totalLostMinutes: number
  departmentBreakdown: { name: string; minutes: number }[]
}

interface LostTimeSummaryProps {
  data: LostTimeData
  title?: string
}

export default function LostTimeSummary({ data, title = 'Lost Production Time' }: LostTimeSummaryProps) {
  const hours = Math.floor(data.totalLostMinutes / 60)
  const minutes = data.totalLostMinutes % 60

  return (
    <div className="rounded-xl border border-border bg-card p-5">
      <div className="flex items-center gap-2 mb-4">
        <Clock className="w-5 h-5 text-yellow-500" />
        <h3 className="font-semibold">{title}</h3>
      </div>

      {/* Summary row */}
      <div className="grid grid-cols-3 gap-4 mb-4">
        <div className="text-center p-3 rounded-lg bg-muted/30">
          <p className="text-xs text-muted-foreground mb-1">Late Time</p>
          <p className="text-lg font-bold text-yellow-400">{formatMinutes(data.totalLateMinutes)}</p>
        </div>
        <div className="text-center p-3 rounded-lg bg-muted/30">
          <p className="text-xs text-muted-foreground mb-1">Early Departure</p>
          <p className="text-lg font-bold text-orange-400">{formatMinutes(data.totalEarlyMinutes)}</p>
        </div>
        <div className="text-center p-3 rounded-lg bg-destructive/10 border border-destructive/20">
          <p className="text-xs text-muted-foreground mb-1">Total Lost</p>
          <p className="text-lg font-bold text-red-400">{formatMinutes(data.totalLostMinutes)}</p>
        </div>
      </div>

      {/* Department breakdown */}
      {data.departmentBreakdown.length > 0 && (
        <div className="space-y-2">
          <p className="text-xs font-medium text-muted-foreground uppercase tracking-wider">By Department</p>
          {data.departmentBreakdown.map(dept => (
            <div key={dept.name} className="flex items-center gap-3">
              <span className="text-xs w-24 truncate">{dept.name}</span>
              <div className="flex-1 h-2 rounded-full bg-muted overflow-hidden">
                <div
                  className="h-full rounded-full bg-primary/60 transition-all"
                  style={{ width: `${Math.min((dept.minutes / data.totalLostMinutes) * 100, 100)}%` }}
                />
              </div>
              <span className="text-xs font-mono text-muted-foreground w-16 text-right">{formatMinutes(dept.minutes)}</span>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
