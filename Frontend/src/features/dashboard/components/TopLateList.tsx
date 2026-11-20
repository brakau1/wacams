import { cn } from '@/core/utils/cn'
import { Clock } from 'lucide-react'

interface TopLateEntry {
  employeeId: string
  displayName: string
  departmentName: string
  lateCount: number
  totalLateMinutes: number
}

interface TopLateListProps {
  data: TopLateEntry[]
  title?: string
  maxItems?: number
}

export default function TopLateList({ data, title = 'Top Late Arrivals', maxItems = 5 }: TopLateListProps) {
  const items = data.slice(0, maxItems)

  return (
    <div className="rounded-xl border border-border bg-card p-5">
      <div className="flex items-center gap-2 mb-4">
        <Clock className="w-5 h-5 text-yellow-500" />
        <h3 className="font-semibold">{title}</h3>
      </div>

      {items.length === 0 ? (
        <p className="text-sm text-muted-foreground text-center py-6">No late arrivals recorded.</p>
      ) : (
        <div className="space-y-2">
          {items.map((entry, index) => (
            <div key={entry.employeeId} className="flex items-center justify-between p-3 rounded-lg bg-muted/30 hover:bg-muted/50 transition-colors">
              <div className="flex items-center gap-3">
                <span className="w-6 h-6 rounded-full bg-primary/10 text-primary flex items-center justify-center text-xs font-bold">
                  {index + 1}
                </span>
                <div>
                  <p className="text-sm font-medium">{entry.displayName}</p>
                  <p className="text-xs text-muted-foreground">{entry.departmentName}</p>
                </div>
              </div>
              <div className="text-right">
                <p className="text-sm font-semibold text-yellow-500">{entry.lateCount} times</p>
                <p className="text-xs text-muted-foreground">{Math.round(entry.totalLateMinutes / 60)}h{entry.totalLateMinutes % 60}m total</p>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
