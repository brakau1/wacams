import { cn } from '@/core/utils/cn'
import { useState } from 'react'
import type { Employee } from '@/core/api/types'
import StatusChip from '../../attendance/components/StatusChip'
// Badge removed - using inline styles

interface EmployeeTableProps {
  employees: Employee[]
  onRowClick?: (employee: Employee) => void
  searchPlaceholder?: string
}

export default function EmployeeTable({ employees, onRowClick, searchPlaceholder }: EmployeeTableProps) {
  const [searchTerm, setSearchTerm] = useState('')
  const [departmentFilter, setDepartmentFilter] = useState<string>('all')

  // Get unique departments
  const departments = Array.from(new Set(employees.map(e => e.departmentName)))

  const filteredEmployees = employees.filter(emp => {
    const matchesSearch = !searchTerm ||
      emp.displayName.toLowerCase().includes(searchTerm.toLowerCase()) ||
      emp.employeeId.toLowerCase().includes(searchTerm.toLowerCase()) ||
      emp.email.toLowerCase().includes(searchTerm.toLowerCase())
    const matchesDept = departmentFilter === 'all' || emp.departmentName === departmentFilter
    return matchesSearch && matchesDept
  })

  const getStatusColor = (emp: Employee): string => {
    if (emp.status === 'OnLeave') return 'bg-blue-500/10 text-blue-400 border-blue-500/30'
    if (emp.status === 'Inactive' || emp.status === 'Terminated') return 'bg-gray-500/10 text-gray-400 border-gray-500/30'
    if (!emp.isClockIn) return 'bg-red-500/10 text-red-400 border-red-500/30'
    return 'bg-green-500/10 text-green-400 border-green-500/30'
  }

  const statusLabel = (emp: Employee): string => {
    if (emp.status !== 'Active') return emp.status
    return emp.isClockIn ? 'Clocked In' : 'Not Clocked In'
  }

  return (
    <div className="space-y-4">
      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-3">
        <input
          type="text"
          placeholder={searchPlaceholder || 'Search by name, ID, or email...'}
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          className="flex-1 px-4 py-2 rounded-lg border border-border bg-background text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary/50"
        />
        <select
          value={departmentFilter}
          onChange={(e) => setDepartmentFilter(e.target.value)}
          className="px-4 py-2 rounded-lg border border-border bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary/50"
        >
          <option value="all">All Departments</option>
          {departments.map(dept => (
            <option key={dept} value={dept}>{dept}</option>
          ))}
        </select>
      </div>

      {/* Table */}
      <div className="overflow-x-auto rounded-lg border border-border">
        <table className="w-full text-sm">
          <thead className="bg-muted/50">
            <tr>
              <th className="px-4 py-3 text-left font-semibold text-muted-foreground text-xs uppercase tracking-wider">Employee</th>
              <th className="px-4 py-3 text-left font-semibold text-muted-foreground text-xs uppercase tracking-wider hidden md:table-cell">ID</th>
              <th className="px-4 py-3 text-left font-semibold text-muted-foreground text-xs uppercase tracking-wider hidden lg:table-cell">Department</th>
              <th className="px-4 py-3 text-left font-semibold text-muted-foreground text-xs uppercase tracking-wider">Manager</th>
              <th className="px-4 py-3 text-left font-semibold text-muted-foreground text-xs uppercase tracking-wider">Status</th>
              <th className="px-4 py-3 text-right font-semibold text-muted-foreground text-xs uppercase tracking-wider hidden sm:table-cell">Shift</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border">
            {filteredEmployees.length === 0 ? (
              <tr>
                <td colSpan={6} className="px-4 py-8 text-center text-muted-foreground">No employees found.</td>
              </tr>
            ) : (
              filteredEmployees.map(emp => (
                <tr
                  key={emp.id}
                  onClick={() => onRowClick?.(emp)}
                  className="cursor-pointer hover:bg-muted/30 transition-colors"
                >
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-3">
                      <div className="w-8 h-8 rounded-full bg-primary/10 flex items-center justify-center flex-shrink-0">
                        <span className="text-xs font-bold text-primary">{emp.displayName.charAt(0)}</span>
                      </div>
                      <div>
                        <p className="font-medium">{emp.displayName}</p>
                        <p className="text-xs text-muted-foreground md:hidden">{emp.employeeId}</p>
                      </div>
                    </div>
                  </td>
                  <td className="px-4 py-3 text-xs font-mono text-muted-foreground hidden md:table-cell">{emp.employeeId}</td>
                  <td className="px-4 py-3 text-xs hidden lg:table-cell">{emp.departmentName}</td>
                  <td className="px-4 py-3 text-xs">{emp.managerName || '—'}</td>
                  <td className="px-4 py-3">
                    <span className={cn(
                      'inline-flex px-2 py-0.5 rounded-full text-xs font-medium border',
                      getStatusColor(emp)
                    )}>
                      {statusLabel(emp)}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right text-xs hidden sm:table-cell">{emp.shiftCode}</td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Results count */}
      <p className="text-xs text-muted-foreground">
        Showing {filteredEmployees.length} of {employees.length} employees
      </p>
    </div>
  )
}
