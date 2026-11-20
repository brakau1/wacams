import { useState } from 'react'
import { ChevronLeft, ChevronRight } from 'lucide-react'
import { cn } from '@/core/utils/cn'

interface CalendarDay {
  date: string
  status: string
  clockIn?: string
  clockOut?: string
  notes?: string
}

interface CalendarViewProps {
  days: CalendarDay[]
  month: number
  year: number
  onDayClick?: (date: string) => void
}

const STATUS_COLORS: Record<string, string> = {
  Present: 'bg-green-500/20 border-green-500/40',
  Late: 'bg-yellow-500/20 border-yellow-500/40',
  EarlyDeparture: 'bg-orange-500/20 border-orange-500/40',
  Absent: 'bg-red-500/20 border-red-500/40',
  AWOL: 'bg-red-600/30 border-red-600/50',
  SickLeave: 'bg-blue-500/20 border-blue-500/40',
  FamilyResponsibilityLeave: 'bg-indigo-500/20 border-indigo-500/40',
  AnnualLeave: 'bg-sky-500/20 border-sky-500/40',
  Rest: 'bg-gray-500/10 border-gray-500/20',
  UnscheduledClockIn: 'bg-purple-500/20 border-purple-500/40',
  MissingClockIn: 'bg-yellow-500/20 border-yellow-500/40'
}

export default function CalendarView({ days, month, year, onDayClick }: CalendarViewProps) {
  const [selectedDate, setSelectedDate] = useState<string | null>(null)

  // Generate calendar grid
  const getDaysInMonth = (m: number, y: number) => new Date(y, m + 1, 0).getDate()
  const firstDayOfMonth = new Date(year, month, 1).getDay()
  const totalDays = getDaysInMonth(month, year)

  const dayMap = new Map(days.map(d => [d.date, d]))

  return (
    <div className="rounded-xl border border-border bg-card p-5">
      {/* Month header */}
      <div className="flex items-center justify-between mb-4">
        <button className="p-1 rounded-md hover:bg-muted">
          <ChevronLeft className="w-5 h-5" />
        </button>
        <h3 className="font-semibold">
          {new Date(year, month).toLocaleDateString('en-US', { month: 'long', year: 'numeric' })}
        </h3>
        <button className="p-1 rounded-md hover:bg-muted">
          <ChevronRight className="w-5 h-5" />
        </button>
      </div>

      {/* Day headers */}
      <div className="grid grid-cols-7 gap-1 mb-2">
        {['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'].map(day => (
          <div key={day} className="text-center text-xs font-medium text-muted-foreground py-1">
            {day}
          </div>
        ))}
      </div>

      {/* Calendar grid */}
      <div className="grid grid-cols-7 gap-1">
        {Array.from({ length: firstDayOfMonth }).map((_, i) => (
          <div key={`empty-${i}`} className="aspect-square" />
        ))}
        {Array.from({ length: totalDays }).map((_, i) => {
          const dayNum = i + 1
          const dateStr = `${year}-${String(month + 1).padStart(2, '0')}-${String(dayNum).padStart(2, '0')}`
          const dayData = dayMap.get(dateStr)
          const colorClass = dayData ? STATUS_COLORS[dayData.status] || 'bg-muted/30' : ''
          const isToday = dayNum === new Date().getDate() && month === new Date().getMonth() && year === new Date().getFullYear()

          return (
            <button
              key={dateStr}
              onClick={() => {
                setSelectedDate(dateStr)
                onDayClick?.(dateStr)
              }}
              className={cn(
                'aspect-square rounded-lg flex flex-col items-center justify-center text-sm transition-all relative',
                colorClass,
                selectedDate === dateStr && 'ring-2 ring-primary ring-offset-1 ring-offset-card',
                isToday && 'font-bold'
              )}
            >
              <span>{dayNum}</span>
              {dayData && dayData.clockIn && (
                <span className="text-[9px] text-muted-foreground">{dayData.clockIn}</span>
              )}
            </button>
          )
        })}
      </div>

      {/* Selected date details */}
      {selectedDate && dayMap.has(selectedDate) && (
        <div className="mt-4 pt-4 border-t border-border">
          <h4 className="font-semibold text-sm mb-2">
            {dayMap.get(selectedDate)?.status || 'No data'}
          </h4>
          <div className="grid grid-cols-2 gap-2 text-xs">
            <div>
              <span className="text-muted-foreground">Clock In:</span>{' '}
              <span className="font-medium">{dayMap.get(selectedDate)?.clockIn || '—'}</span>
            </div>
            <div>
              <span className="text-muted-foreground">Clock Out:</span>{' '}
              <span className="font-medium">{dayMap.get(selectedDate)?.clockOut || '—'}</span>
            </div>
          </div>
        </div>
      )}

      {/* Legend */}
      <div className="flex flex-wrap gap-2 mt-4 pt-3 border-t border-border">
        {[
          { label: 'Present', color: 'bg-green-500/20' },
          { label: 'Late', color: 'bg-yellow-500/20' },
          { label: 'Early Departure', color: 'bg-orange-500/20' },
          { label: 'Absent', color: 'bg-red-500/20' },
          { label: 'Sick Leave', color: 'bg-blue-500/20' },
          { label: 'Rest', color: 'bg-gray-500/10' }
        ].map(item => (
          <div key={item.label} className="flex items-center gap-1.5 text-xs">
            <div className={cn('w-3 h-3 rounded', item.color)} />
            <span className="text-muted-foreground">{item.label}</span>
          </div>
        ))}
      </div>
    </div>
  )
}
