import { cn } from '@/core/utils/cn'

interface EmployeeFiltersProps {
  departments: string[]
  selectedDepartment: string
  onDepartmentChange: (dept: string) => void
  statusFilter: string
  onStatusChange: (status: string) => void
}

export default function EmployeeFilters({
  departments,
  selectedDepartment,
  onDepartmentChange,
  statusFilter,
  onStatusChange
}: EmployeeFiltersProps) {
  const statuses = ['All', 'Active', 'OnLeave', 'Inactive']

  return (
    <div className="flex flex-col sm:flex-row gap-3">
      {/* Department filter */}
      <select
        value={selectedDepartment}
        onChange={(e) => onDepartmentChange(e.target.value)}
        className="px-4 py-2 rounded-lg border border-border bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary/50"
      >
        <option value="all">All Departments</option>
        {departments.map(dept => (
          <option key={dept} value={dept}>{dept}</option>
        ))}
      </select>

      {/* Status filter pills */}
      <div className="flex items-center gap-1">
        {statuses.map(status => (
          <button
            key={status}
            onClick={() => onStatusChange(status === 'All' ? '' : status)}
            className={cn(
              'px-3 py-1.5 rounded-full text-xs font-medium transition-colors',
              (status === 'All' && !statusFilter) || statusFilter === status
                ? 'bg-primary text-primary-foreground'
                : 'bg-muted text-muted-foreground hover:bg-muted/80'
            )}
          >
            {status}
          </button>
        ))}
      </div>
    </div>
  )
}
