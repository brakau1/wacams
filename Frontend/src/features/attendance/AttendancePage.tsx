import { useState } from 'react'
import CalendarView from './components/CalendarView'
import DailyAttendanceTable from './components/DailyAttendanceTable'
import LostTimeSummary from './components/LostTimeSummary'
import EmployeeFilters from '../employees/components/EmployeeFilters'
import { LoadingSpinner } from '@/shared/components/LoadingSpinner'
import { useAttendanceList } from '@/core/hooks/useDashboardQueries'

export default function AttendancePage() {
  const [selectedDept, setSelectedDept] = useState('all')
  const [statusFilter, setStatusFilter] = useState('')
  const [month] = useState(new Date().getMonth())
  const [year] = useState(new Date().getFullYear())

  const { data: records = [], isLoading: loading } = useAttendanceList(
    new Date(year, month, 1).toISOString().split('T')[0],
    selectedDept === 'all' ? undefined : selectedDept
  )

  // Generate calendar days
  const calendarDays = Array.from({ length: 30 }, (_, i) => {
    const dayNum = i + 1
    const dateStr = `${year}-${String(month + 1).padStart(2, '0')}-${String(dayNum).padStart(2, '0')}`
    const record = records.find(r => r.date === dateStr)
    return {
      date: dateStr,
      status: record?.status || (dayNum <= new Date().getDate() ? 'Rest' : ''),
      clockIn: record?.clockInTime ?? undefined,
      clockOut: record?.clockOutTime ?? undefined
    }
  })

  // Calculate lost time
  const totalLate = records.reduce((s, r) => s + r.lateMinutes, 0)
  const totalEarly = records.reduce((s, r) => s + r.earlyMinutes, 0)
  const deptBreakdown = ['Production', 'Quality', 'Engineering', 'Logistics'].map(dept => ({
    name: dept,
    minutes: Math.floor(Math.random() * 300)
  }))

  if (loading) {
    return <LoadingSpinner size="lg" label="Loading attendance data..." />
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Attendance</h1>
          <p className="text-sm text-muted-foreground mt-1">Daily attendance tracking and calendar view</p>
        </div>
        <button className="px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90">
          Export CSV
        </button>
      </div>

      {/* Filters */}
      <EmployeeFilters
        departments={['All Departments', 'Production', 'Quality', 'Engineering', 'Logistics']}
        selectedDepartment={selectedDept}
        onDepartmentChange={setSelectedDept}
        statusFilter={statusFilter}
        onStatusChange={setStatusFilter}
      />

      {/* Two-column layout */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2">
          <CalendarView
            days={calendarDays}
            month={month}
            year={year}
            onDayClick={(date) => console.log('Selected:', date)}
          />
        </div>
        <LostTimeSummary data={{
          totalLateMinutes: totalLate,
          totalEarlyMinutes: totalEarly,
          totalLostMinutes: totalLate + totalEarly,
          departmentBreakdown: deptBreakdown
        }} />
      </div>

      {/* Daily table */}
      <DailyAttendanceTable records={records.slice(0, 10)} />
    </div>
  )
}
