import { useNavigate } from 'react-router-dom'
import EmployeeTable from './components/EmployeeTable'
import { LoadingSpinner } from '@/shared/components/LoadingSpinner'
import EmptyState from '@/shared/components/EmptyState'
import { useEmployees } from '@/core/hooks/useDashboardQueries'

export default function EmployeeListPage() {
  const navigate = useNavigate()
  const { data: employees = [], isLoading: loading } = useEmployees()

  if (loading) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold">Employees</h1>
        <LoadingSpinner size="lg" label="Loading employees..." />
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Employees</h1>
          <p className="text-sm text-muted-foreground mt-1">{employees.length} total employees</p>
        </div>
        <button className="px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity">
          Export List
        </button>
      </div>

      {employees.length === 0 ? (
        <EmptyState
          icon="search"
          title="No employees found"
          description="There are no employees matching your search criteria."
        />
      ) : (
        <EmployeeTable
          employees={employees}
          onRowClick={(emp) => navigate(`/employees/${emp.id}`)}
        />
      )}
    </div>
  )
}
