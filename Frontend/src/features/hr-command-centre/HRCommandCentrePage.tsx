import KPICard from '../dashboard/components/KPICard'
import TrendChart from '../dashboard/components/TrendChart'
import DepartmentChart from '../dashboard/components/DepartmentChart'
import TopLateList from '../dashboard/components/TopLateList'
import ExceptionAnalysisChart from './components/ExceptionAnalysisChart'
import { LoadingSpinner } from '@/shared/components/LoadingSpinner'
import EmptyState from '@/shared/components/EmptyState'
import {
  TrendingUp,
  Clock,
  AlertCircle,
  AlertTriangle,
  UserX,
  ArrowUpRight,
  Shield,
  Calendar,
  ShieldAlert,
  RefreshCw
} from 'lucide-react'
import { useHRCommandCentre } from '@/core/hooks/useDashboardQueries'

export default function HRCommandCentrePage() {
  const { data, isLoading, isError, error, refetch } = useHRCommandCentre()

  if (isLoading) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold">HR Command Centre</h1>
        <LoadingSpinner size="lg" label="Loading HR Command Centre..." />
      </div>
    )
  }

  if (isError) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold">HR Command Centre</h1>
        <EmptyState
          icon="alert"
          title="Failed to load HR Command Centre"
          description={error?.message || 'An error occurred while loading data'}
          actionLabel="Retry"
          onAction={() => refetch()}
        />
      </div>
    )
  }

  const kpis = data?.kpis || { plantAttendancePercentage: 0, lostProductionHours: 0, openWarnings: 0, finalWarnings: 0, awolCases: 0, escalations: 0, departmentsAtRisk: 0 }
  const attendanceTrend = data?.attendanceTrend || []
  const departmentRanking = data?.departmentRanking || []
  const topOffenders = data?.topOffenders || []
  const exceptionAnalysis = data?.exceptionAnalysis || []

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">HR Command Centre</h1>
          <p className="text-sm text-muted-foreground mt-1">
            Plant-wide attendance & disciplinary oversight
          </p>
        </div>
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Calendar className="w-4 h-4" />
          <span>{new Date().toLocaleDateString('en-US', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}</span>
        </div>
      </div>

      {/* 7 KPI Cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 lg:grid-cols-7 gap-4">
        <KPICard
          title="Plant Attendance"
          value={`${kpis.plantAttendancePercentage.toFixed(1)}%`}
          icon={TrendingUp}
          color={kpis.plantAttendancePercentage >= 95 ? 'green' : kpis.plantAttendancePercentage >= 90 ? 'yellow' : 'red'}
          subtitle={kpis.plantAttendancePercentage >= 95 ? 'On target' : 'Below target'}
        />
        <KPICard
          title="Lost Production"
          value={`${kpis.lostProductionHours}h`}
          icon={Clock}
          color="orange"
          subtitle="This month"
        />
        <KPICard
          title="Open Warnings"
          value={kpis.openWarnings}
          icon={AlertCircle}
          color="yellow"
          subtitle="Awaiting action"
        />
        <KPICard
          title="Final Warnings"
          value={kpis.finalWarnings}
          icon={AlertTriangle}
          color="red"
          subtitle="Critical"
        />
        <KPICard
          title="AWOL Cases"
          value={kpis.awolCases}
          icon={UserX}
          color="red"
          subtitle="Unauthorised absence"
        />
        <KPICard
          title="Escalations"
          value={kpis.escalations}
          icon={ArrowUpRight}
          color="purple"
          subtitle="Active cases"
        />
        <KPICard
          title="Depts at Risk"
          value={kpis.departmentsAtRisk}
          icon={Shield}
          color={kpis.departmentsAtRisk > 0 ? 'red' : 'green'}
          subtitle={kpis.departmentsAtRisk > 0 ? 'Needs attention' : 'All stable'}
        />
      </div>

      {/* Charts Row 1: Attendance Trend + Department Ranking */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <TrendChart data={attendanceTrend} title="Attendance Trend (Last 14 Days)" />
        <DepartmentChart data={departmentRanking} title="Department Ranking" />
      </div>

      {/* Charts Row 2: Top Offenders + Exception Analysis */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <TopLateList data={topOffenders} title="Top Offenders" maxItems={10} />
        <ExceptionAnalysisChart data={exceptionAnalysis} />
      </div>

      {/* Department Risk Table */}
      <div className="rounded-xl border border-border bg-card p-5">
        <div className="flex items-center gap-2 mb-4">
          <ShieldAlert className="w-5 h-5 text-muted-foreground" />
          <h3 className="font-semibold">Department Risk Assessment</h3>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full">
            <thead>
              <tr className="border-b border-border">
                <th className="text-left py-3 px-4 text-sm font-medium text-muted-foreground">Department</th>
                <th className="text-center py-3 px-4 text-sm font-medium text-muted-foreground">Employees</th>
                <th className="text-center py-3 px-4 text-sm font-medium text-muted-foreground">Present</th>
                <th className="text-center py-3 px-4 text-sm font-medium text-muted-foreground">Late</th>
                <th className="text-center py-3 px-4 text-sm font-medium text-muted-foreground">Absent</th>
                <th className="text-center py-3 px-4 text-sm font-medium text-muted-foreground">Exceptions</th>
                <th className="text-center py-3 px-4 text-sm font-medium text-muted-foreground">Attendance %</th>
                <th className="text-center py-3 px-4 text-sm font-medium text-muted-foreground">Risk Level</th>
              </tr>
            </thead>
            <tbody>
              {departmentRanking.map((dept) => (
                <tr key={dept.departmentId} className="border-b border-border hover:bg-muted/50 transition-colors">
                  <td className="py-3 px-4 text-sm font-medium">{dept.departmentName}</td>
                  <td className="text-center py-3 px-4 text-sm">{dept.totalEmployees}</td>
                  <td className="text-center py-3 px-4 text-sm text-green-500">{dept.presentCount}</td>
                  <td className="text-center py-3 px-4 text-sm text-yellow-500">{dept.lateCount}</td>
                  <td className="text-center py-3 px-4 text-sm text-red-500">{dept.absentCount}</td>
                  <td className="text-center py-3 px-4 text-sm text-orange-500">{dept.exceptionCount}</td>
                  <td className="text-center py-3 px-4 text-sm font-semibold">
                    {dept.attendancePercentage.toFixed(1)}%
                  </td>
                  <td className="text-center py-3 px-4">
                    <span
                      className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                        dept.riskLevel === 'low'
                          ? 'bg-green-500/10 text-green-500'
                          : dept.riskLevel === 'medium'
                          ? 'bg-yellow-500/10 text-yellow-500'
                          : dept.riskLevel === 'high'
                          ? 'bg-orange-500/10 text-orange-500'
                          : 'bg-red-500/10 text-red-500'
                      }`}
                    >
                      {dept.riskLevel.charAt(0).toUpperCase() + dept.riskLevel.slice(1)}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  )
}
