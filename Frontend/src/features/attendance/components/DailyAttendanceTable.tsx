import { cn } from '@/core/utils/cn'
import TimeBadge from './TimeBadge'
import StatusChip from './StatusChip'
import type { AttendanceRecord } from '@/core/api/types'

interface DailyAttendanceTableProps {
  records: AttendanceRecord[]
  date?: string
}

export default function DailyAttendanceTable({ records, date }: DailyAttendanceTableProps) {
  const displayDate = date || new Date().toLocaleDateString('en-US', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })

  return (
    <div className="rounded-xl border border-border bg-card p-5">
      <div className="flex items-center justify-between mb-4">
        <h3 className="font-semibold">Attendance — {displayDate}</h3>
        <p className="text-xs text-muted-foreground">{records.length} employees</p>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead className="border-b border-border">
            <tr>
              <th className="px-3 py-2 text-left font-medium text-muted-foreground text-xs uppercase tracking-wider">Employee</th>
              <th className="px-3 py-2 text-left font-medium text-muted-foreground text-xs uppercase tracking-wider hidden md:table-cell">Dept</th>
              <th className="px-3 py-2 text-left font-medium text-muted-foreground text-xs uppercase tracking-wider">Shift</th>
              <th className="px-3 py-2 text-left font-medium text-muted-foreground text-xs uppercase tracking-wider">Clock In</th>
              <th className="px-3 py-2 text-left font-medium text-muted-foreground text-xs uppercase tracking-wider hidden sm:table-cell">Clock Out</th>
              <th className="px-3 py-2 text-left font-medium text-muted-foreground text-xs uppercase tracking-wider">Status</th>
              <th className="px-3 py-2 text-left font-medium text-muted-foreground text-xs uppercase tracking-wider hidden lg:table-cell">Late Min</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {records.length === 0 ? (
              <tr>
                <td colSpan={7} className="px-4 py-8 text-center text-muted-foreground">No attendance records for this date.</td>
              </tr>
            ) : (
              records.map(record => (
                <tr key={record.id} className="hover:bg-muted/30 transition-colors">
                  <td className="px-3 py-2.5">
                    <p className="font-medium">{record.displayName}</p>
                    <p className="text-xs text-muted-foreground">{record.employeeId}</p>
                  </td>
                  <td className="px-3 py-2.5 text-xs hidden md:table-cell">{record.departmentName}</td>
                  <td className="px-3 py-2.5">
                    <span className="px-2 py-0.5 rounded text-xs font-mono bg-muted">{record.scheduledShift}</span>
                  </td>
                  <td className="px-3 py-2.5"><TimeBadge time={record.clockInTime} type="clockIn" size="sm" /></td>
                  <td className="px-3 py-2.5 hidden sm:table-cell"><TimeBadge time={record.clockOutTime} type="clockOut" size="sm" /></td>
                  <td className="px-3 py-2.5"><StatusChip status={record.status} size="sm" /></td>
                  <td className="px-3 py-2.5 text-xs hidden lg:table-cell">{record.lateMinutes > 0 ? `${record.lateMinutes}m` : '—'}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  )
}
