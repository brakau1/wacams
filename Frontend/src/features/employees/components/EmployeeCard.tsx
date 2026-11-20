import { cn } from '@/core/utils/cn'
import type { Employee } from '@/core/api/types'

interface EmployeeCardProps {
  employee: Employee
  onClick?: () => void
}

export default function EmployeeCard({ employee, onClick }: EmployeeCardProps) {
  return (
    <div
      onClick={onClick}
      className="rounded-xl border border-border bg-card p-4 cursor-pointer hover:shadow-md hover:shadow-black/20 transition-all"
    >
      <div className="flex items-center gap-3 mb-3">
        <div className="w-10 h-10 rounded-full bg-primary/10 flex items-center justify-center flex-shrink-0">
          <span className="text-sm font-bold text-primary">{employee.displayName.charAt(0)}</span>
        </div>
        <div className="min-w-0">
          <p className="font-semibold truncate">{employee.displayName}</p>
          <p className="text-xs text-muted-foreground">{employee.employeeId} · {employee.departmentName}</p>
        </div>
      </div>

      <div className="flex items-center justify-between">
        <span className="text-xs text-muted-foreground">{employee.jobTitle || 'No title'}</span>
        <span className={cn(
          'px-2 py-0.5 rounded-full text-xs font-medium border',
          employee.status === 'Active' && !employee.isClockIn ? 'bg-red-500/10 text-red-400 border-red-500/30' :
          employee.status === 'OnLeave' ? 'bg-blue-500/10 text-blue-400 border-blue-500/30' :
          'bg-green-500/10 text-green-400 border-green-500/30'
        )}>
          {employee.status}
        </span>
      </div>
    </div>
  )
}
