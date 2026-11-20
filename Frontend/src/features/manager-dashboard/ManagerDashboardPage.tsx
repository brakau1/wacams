import { useState } from 'react'
import KPICard from '../dashboard/components/KPICard'
import DepartmentChart from '../dashboard/components/DepartmentChart'
import TrendChart from '../dashboard/components/TrendChart'
import TopLateList from '../dashboard/components/TopLateList'
import ExceptionTable from '../dashboard/components/ExceptionTable'
import StatsGrid, { StatItem } from '../dashboard/components/StatsGrid'
import { LoadingSpinner } from '@/shared/components/LoadingSpinner'
import EmptyState from '@/shared/components/EmptyState'
import {
  Users,
  CheckCircle2,
  Clock,
  AlertTriangle,
  FileWarning,
  ArrowUpRight,
  Users2,
  TrendingUp,
  Calendar,
  UserCheck,
  UserX,
  Timer,
  ShieldAlert,
  FileX,
  TrendingDown,
  Target,
  RefreshCw
} from 'lucide-react'
import { useManagerDashboard } from '@/core/hooks/useDashboardQueries'

type ViewMode = 'team' | 'department' | 'offenders' | 'trend'

export default function ManagerDashboardPage() {
  const [viewMode, setViewMode] = useState<ViewMode>('team')
  const { data, isLoading, isError, error, refetch, isFetching } = useManagerDashboard()

  if (isLoading) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold">Manager Dashboard</h1>
        <LoadingSpinner size="lg" label="Loading manager dashboard..." />
      </div>
    )
  }

  if (isError) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold">Manager Dashboard</h1>
        <EmptyState
          icon="alert"
          title="Failed to load dashboard"
          description={error?.message || 'An error occurred while loading dashboard data'}
          actionLabel="Retry"
          onAction={() => refetch()}
        />
      </div>
    )
  }

  const summary = data?.summary ?? { departmentId: '', departmentName: '—', totalEmployees: 0, presentCount: 0, lateCount: 0, absentCount: 0, awolCount: 0, sickLeaveCount: 0, familyResponsibilityCount: 0, annualLeaveCount: 0, attendancePercentage: 0, totalLateMinutes: 0, totalEarlyMinutes: 0, totalLostMinutes: 0 }
  const teamSummary = data?.teamSummary ?? { departmentId: '', departmentName: '—', totalEmployees: 0, presentCount: 0, lateCount: 0, absentCount: 0, awolCount: 0, sickLeaveCount: 0, familyResponsibilityCount: 0, annualLeaveCount: 0, attendancePercentage: 0, totalLateMinutes: 0, totalEarlyMinutes: 0, totalLostMinutes: 0 }
  const departments = data?.departments || []
  const trends = data?.trends || []
  const topOffenders = data?.topOffenders || []
  const exceptions = data?.exceptions || []

  // Calculate KPIs
  const openCorrectiveActions = exceptions.filter(e => ['Open', 'UnderReview'].includes(e.status)).length
  const activeEscalations = exceptions.filter(e => e.status === 'Escalated').length

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Manager Dashboard</h1>
          <p className="text-sm text-muted-foreground mt-1">
            {viewMode === 'team' && `My Team: ${teamSummary.departmentName}`}
            {viewMode === 'department' && `Department: ${summary.departmentName}`}
            {viewMode === 'offenders' && 'Top Offenders Analysis'}
            {viewMode === 'trend' && 'Attendance Trends'}
          </p>
        </div>
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Calendar className="w-4 h-4" />
          <span>{new Date().toLocaleDateString('en-US', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}</span>
        </div>
      </div>

      {/* View Mode Tabs */}
      <div className="flex gap-2 border-b border-border">
        {[
          { key: 'team', label: 'My Team', icon: Users },
          { key: 'department', label: 'My Department', icon: Target },
          { key: 'offenders', label: 'Top Offenders', icon: AlertTriangle },
          { key: 'trend', label: 'Attendance Trend', icon: TrendingUp }
        ].map(tab => {
          const Icon = tab.icon
          return (
            <button
              key={tab.key}
              onClick={() => setViewMode(tab.key as ViewMode)}
              className={`flex items-center gap-2 px-4 py-2 border-b-2 transition-colors ${
                viewMode === tab.key
                  ? 'border-primary text-primary font-medium'
                  : 'border-transparent text-muted-foreground hover:text-foreground'
              }`}
            >
              <Icon className="w-4 h-4" />
              {tab.label}
            </button>
          )
        })}
      </div>

      {/* 8 KPI Cards */}
      <StatsGrid cols={4}>
        <KPICard
          title="Present"
          value={viewMode === 'team' ? teamSummary.presentCount : summary.presentCount}
          icon={UserCheck}
          color="green"
          subtitle={`of ${viewMode === 'team' ? teamSummary.totalEmployees : summary.totalEmployees} total`}
        />
        <KPICard
          title="Absent"
          value={viewMode === 'team' ? teamSummary.absentCount : summary.absentCount}
          icon={UserX}
          color="red"
          subtitle={`${viewMode === 'team' ? teamSummary.awolCount : summary.awolCount} AWOL`}
        />
        <KPICard
          title="Late"
          value={viewMode === 'team' ? teamSummary.lateCount : summary.lateCount}
          icon={Clock}
          color="yellow"
          subtitle={`${viewMode === 'team' ? teamSummary.totalLateMinutes : summary.totalLateMinutes} min lost`}
        />
        <KPICard
          title="Early Departure"
          value={Math.round((viewMode === 'team' ? teamSummary.totalEarlyMinutes : summary.totalEarlyMinutes) / 15)}
          icon={Timer}
          color="orange"
          subtitle={`${viewMode === 'team' ? teamSummary.totalEarlyMinutes : summary.totalEarlyMinutes} min lost`}
        />
        <KPICard
          title="Attendance Exceptions"
          value={exceptions.length}
          icon={AlertTriangle}
          color="purple"
          subtitle={`${openCorrectiveActions} open`}
        />
        <KPICard
          title="Open Corrective Actions"
          value={openCorrectiveActions}
          icon={FileWarning}
          color="blue"
          subtitle="Requires action"
        />
        <KPICard
          title="Escalations"
          value={activeEscalations}
          icon={ShieldAlert}
          color="red"
          subtitle="Active escalations"
        />
        <KPICard
          title="Lost Production"
          value={`${Math.round((viewMode === 'team' ? teamSummary.totalLostMinutes : summary.totalLostMinutes) / 60)}h`}
          icon={TrendingDown}
          color="orange"
          subtitle={`${viewMode === 'team' ? teamSummary.totalLostMinutes : summary.totalLostMinutes} minutes`}
        />
      </StatsGrid>

      {/* View-specific content */}
      {viewMode === 'team' && (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2">
            <ExceptionTable
              exceptions={exceptions.filter(e => 
                e.departmentName === teamSummary.departmentName || 
                e.departmentName.includes('Line A')
              )}
              onViewDetail={(id) => console.log('View exception:', id)}
            />
          </div>
          <div className="space-y-6">
            <div className="rounded-xl border border-border bg-card p-5">
              <h3 className="font-semibold mb-4 flex items-center gap-2">
                <Users className="w-5 h-5 text-muted-foreground" />
                Team Overview
              </h3>
              <div className="space-y-3">
                <StatItem
                  label="Attendance Rate"
                  value={`${teamSummary.attendancePercentage}%`}
                  icon={<CheckCircle2 className="w-4 h-4" />}
                  color="bg-green-500/20"
                />
                <StatItem
                  label="On Leave"
                  value={teamSummary.sickLeaveCount + teamSummary.familyResponsibilityCount + teamSummary.annualLeaveCount}
                  icon={<FileX className="w-4 h-4" />}
                  color="bg-blue-500/20"
                />
                <StatItem
                  label="Lost Time"
                  value={`${Math.round(teamSummary.totalLostMinutes / 60)}h ${teamSummary.totalLostMinutes % 60}m`}
                  icon={<ArrowUpRight className="w-4 h-4" />}
                  color="bg-orange-500/20"
                />
              </div>
            </div>
          </div>
        </div>
      )}

      {viewMode === 'department' && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <DepartmentChart data={departments} />
          <div className="rounded-xl border border-border bg-card p-5">
            <h3 className="font-semibold mb-4 flex items-center gap-2">
              <Target className="w-5 h-5 text-muted-foreground" />
              Department Summary
            </h3>
            <div className="space-y-3">
              <StatItem
                label="Total Employees"
                value={summary.totalEmployees}
                icon={<Users className="w-4 h-4" />}
                color="bg-blue-500/20"
              />
              <StatItem
                label="Attendance Rate"
                value={`${summary.attendancePercentage}%`}
                icon={<CheckCircle2 className="w-4 h-4" />}
                color="bg-green-500/20"
              />
              <StatItem
                label="Total Lost Time"
                value={`${Math.round(summary.totalLostMinutes / 60)}h`}
                icon={<TrendingDown className="w-4 h-4" />}
                color="bg-orange-500/20"
              />
              <StatItem
                label="Open Exceptions"
                value={openCorrectiveActions}
                icon={<AlertTriangle className="w-4 h-4" />}
                color="bg-yellow-500/20"
              />
            </div>
          </div>
        </div>
      )}

      {viewMode === 'offenders' && (
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2">
            <TopLateList data={topOffenders} maxItems={10} />
          </div>
          <div className="rounded-xl border border-border bg-card p-5">
            <h3 className="font-semibold mb-4 flex items-center gap-2">
              <AlertTriangle className="w-5 h-5 text-muted-foreground" />
              Offender Summary
            </h3>
            <div className="space-y-3">
              <StatItem
                label="Total Offenders"
                value={topOffenders.length}
                icon={<Users2 className="w-4 h-4" />}
                color="bg-red-500/20"
              />
              <StatItem
                label="Total Late Minutes"
                value={`${topOffenders.reduce((s, o) => s + o.totalLateMinutes, 0)}m`}
                icon={<Clock className="w-4 h-4" />}
                color="bg-yellow-500/20"
              />
              <StatItem
                label="Avg Late Count"
                value={Math.round(topOffenders.reduce((s, o) => s + o.lateCount, 0) / topOffenders.length)}
                icon={<TrendingDown className="w-4 h-4" />}
                color="bg-orange-500/20"
              />
            </div>
          </div>
        </div>
      )}

      {viewMode === 'trend' && (
        <div className="grid grid-cols-1 gap-6">
          <TrendChart data={trends} />
          <div className="rounded-xl border border-border bg-card p-5">
            <h3 className="font-semibold mb-4 flex items-center gap-2">
              <TrendingUp className="w-5 h-5 text-muted-foreground" />
              Trend Summary
            </h3>
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
              <StatItem
                label="Avg On-Time"
                value={`${Math.round(trends.reduce((s, t) => s + t.onTimePercentage, 0) / trends.length)}%`}
                icon={<CheckCircle2 className="w-4 h-4" />}
                color="bg-green-500/20"
              />
              <StatItem
                label="Total Late"
                value={trends.reduce((s, t) => s + t.lateCount, 0)}
                icon={<Clock className="w-4 h-4" />}
                color="bg-yellow-500/20"
              />
              <StatItem
                label="Total Exceptions"
                value={trends.reduce((s, t) => s + t.exceptionCount, 0)}
                icon={<AlertTriangle className="w-4 h-4" />}
                color="bg-red-500/20"
              />
              <StatItem
                label="Days Tracked"
                value={trends.length}
                icon={<Calendar className="w-4 h-4" />}
                color="bg-blue-500/20"
              />
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
