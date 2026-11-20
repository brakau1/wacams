import KPICard from './components/KPICard'
import DepartmentChart from './components/DepartmentChart'
import TrendChart from './components/TrendChart'
import TopLateList from './components/TopLateList'
import ExceptionTable from './components/ExceptionTable'
import StatsGrid, { StatItem } from './components/StatsGrid'
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
  RefreshCw
} from 'lucide-react'
import { useDashboardKPI } from '@/core/hooks/useDashboardQueries'

export default function DashboardPage() {
  const { data, isLoading, isError, error, refetch, isFetching } = useDashboardKPI()

  if (isLoading) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold">Dashboard</h1>
        <LoadingSpinner size="lg" label="Loading dashboard data..." />
      </div>
    )
  }

  if (isError) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold">Dashboard</h1>
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

  const departments = data?.kpi || []
  const trends = data?.trends || []
  const topLate = data?.topLate || []
  const exceptions = data?.exceptions || []

  // Calculate summary values
  const totalEmployees = departments.reduce((sum, d) => sum + d.totalEmployees, 0)
  const avgOnTime = Math.round(departments.reduce((sum, d) => sum + d.attendancePercentage, 0) / departments.length)
  const totalLateMinutes = topLate.reduce((sum, e) => sum + e.totalLateMinutes, 0)
  const activeExceptions = exceptions.filter(e => ['Open', 'UnderReview', 'Escalated'].includes(e.status)).length

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Dashboard</h1>
          <p className="text-sm text-muted-foreground mt-1">Real-time workforce attendance overview</p>
        </div>
        <div className="flex items-center gap-2 text-sm text-muted-foreground">
          <Calendar className="w-4 h-4" />
          <span>{new Date().toLocaleDateString('en-US', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}</span>
        </div>
      </div>

      {/* KPI Cards Row */}
      <StatsGrid cols={5}>
        <KPICard title="Total Employees" value={totalEmployees} icon={Users} color="blue" subtitle={`${departments.length} departments`} />
        <KPICard title="On-time Rate" value={`${avgOnTime}%`} icon={CheckCircle2} color="green" trend="up" trendValue="+2.1%" subtitle="vs last week" />
        <KPICard title="Avg Late Minutes" value={Math.round(totalLateMinutes / Math.max(topLate.length, 1))} icon={Clock} color="yellow" trend="down" trendValue="-5m" subtitle="vs last week" />
        <KPICard title="Active Exceptions" value={activeExceptions} icon={AlertTriangle} color="red" subtitle={`${exceptions.filter(e => e.status === 'Open').length} open`} />
        <KPICard title="Pending Warnings" value={3} icon={FileWarning} color="purple" trend="neutral" subtitle="Awaiting action" />
      </StatsGrid>

      {/* Charts Row */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <DepartmentChart data={departments} />
        <TrendChart data={trends} />
      </div>

      {/* Bottom Row */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2">
          <ExceptionTable
            exceptions={exceptions}
            onViewDetail={(id) => console.log('View exception:', id)}
          />
        </div>
        <TopLateList data={topLate} maxItems={5} />
      </div>

      {/* Quick Stats Bar */}
      <div className="rounded-xl border border-border bg-card p-5">
        <h3 className="font-semibold mb-4 flex items-center gap-2">
          <TrendingUp className="w-5 h-5 text-muted-foreground" />
          Quick Overview
        </h3>
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
          {[
            { label: 'Present Today', value: departments.reduce((s, d) => s + d.presentCount, 0), icon: <CheckCircle2 className="w-4 h-4" />, color: 'bg-green-500/20' },
            { label: 'Late Today', value: departments.reduce((s, d) => s + d.lateCount, 0), icon: <Clock className="w-4 h-4" />, color: 'bg-yellow-500/20' },
            { label: 'Absent Today', value: departments.reduce((s, d) => s + d.absentCount, 0), icon: <Users2 className="w-4 h-4" />, color: 'bg-red-500/20' },
            { label: 'Total Lost Min', value: `${Math.round(totalLateMinutes / 60)}h`, icon: <ArrowUpRight className="w-4 h-4" />, color: 'bg-orange-500/20' }
          ].map(stat => (
            <StatItem key={stat.label} label={stat.label} value={stat.value} icon={stat.icon} color={stat.color} />
          ))}
        </div>
      </div>
    </div>
  )
}
