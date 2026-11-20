import { 
  Users, 
  UserX, 
  TrendingUp, 
  Clock, 
  AlertTriangle, 
  Search, 
  FileWarning,
  Trophy,
  ArrowUp,
  ArrowDown,
  Minus
} from 'lucide-react';
import { 
  useExecutiveSummary, 
  useExecutiveHeatmap, 
  useExecutiveLeaderboard 
} from '../../core/hooks/useDashboardQueries';
import type { 
  DepartmentHeatmap, 
  DepartmentLeaderboard 
} from '../../core/api/types';

export default function ExecutiveDashboardPage() {
  const { data: summary, isLoading: summaryLoading, dataUpdatedAt: summaryUpdatedAt } = useExecutiveSummary();
  const { data: heatmap = [], isLoading: heatmapLoading } = useExecutiveHeatmap();
  const { data: leaderboard = [], isLoading: leaderboardLoading } = useExecutiveLeaderboard();

  const isLoading = summaryLoading || heatmapLoading || leaderboardLoading;
  const lastUpdated = summaryUpdatedAt ? new Date(summaryUpdatedAt) : new Date();

  if (isLoading && !summary) {
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="text-center">
          <div className="animate-spin rounded-full h-16 w-16 border-b-2 border-blue-600 mx-auto"></div>
          <p className="mt-4 text-gray-600">Loading executive dashboard...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-slate-900 p-8">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="mb-8">
          <h1 className="text-4xl font-bold text-white mb-2">Executive Dashboard</h1>
          <p className="text-slate-400">
            Real-time workforce analytics • Last updated: {lastUpdated.toLocaleTimeString()}
          </p>
        </div>

        {/* KPI Cards */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
          <KPICard
            title="Employees Present"
            value={summary?.employeesPresent || 0}
            subtitle={`of ${summary?.totalActiveEmployees || 0} total`}
            icon={<Users className="w-8 h-8" />}
            color="blue"
          />
          <KPICard
            title="Current Absenteeism"
            value={summary?.currentAbsenteeism || 0}
            subtitle="Absent + AWOL today"
            icon={<UserX className="w-8 h-8" />}
            color="red"
          />
          <KPICard
            title="Attendance Rate"
            value={`${summary?.attendancePercentage || 0}%`}
            subtitle="Today's performance"
            icon={<TrendingUp className="w-8 h-8" />}
            color="green"
          />
          <KPICard
            title="Lost Time"
            value={`${summary?.lostHours || 0}h`}
            subtitle={`${summary?.lostMinutes || 0} minutes total`}
            icon={<Clock className="w-8 h-8" />}
            color="orange"
          />
          <KPICard
            title="Open Escalations"
            value={summary?.openEscalations || 0}
            subtitle="Pending management action"
            icon={<AlertTriangle className="w-8 h-8" />}
            color="yellow"
          />
          <KPICard
            title="Pending Investigations"
            value={summary?.pendingInvestigations || 0}
            subtitle="Under review"
            icon={<Search className="w-8 h-8" />}
            color="purple"
          />
          <KPICard
            title="Corrective Actions"
            value={summary?.correctiveActions || 0}
            subtitle="This month"
            icon={<FileWarning className="w-8 h-8" />}
            color="pink"
          />
        </div>

        {/* Department Heatmap */}
        <div className="mb-8">
          <div className="bg-slate-800/50 backdrop-blur-sm rounded-2xl border border-slate-700 p-6">
            <div className="flex items-center justify-between mb-6">
              <h2 className="text-2xl font-bold text-white flex items-center gap-3">
                <div className="p-2 bg-blue-600/20 rounded-lg">
                  <Users className="w-6 h-6 text-blue-400" />
                </div>
                Department Heatmap
              </h2>
              <div className="flex gap-2 text-xs">
                <span className="flex items-center gap-1.5 px-3 py-1 bg-emerald-500/20 text-emerald-400 rounded-full">
                  <div className="w-2 h-2 bg-emerald-500 rounded-full"></div>
                  On Target
                </span>
                <span className="flex items-center gap-1.5 px-3 py-1 bg-amber-500/20 text-amber-400 rounded-full">
                  <div className="w-2 h-2 bg-amber-500 rounded-full"></div>
                  Watch
                </span>
                <span className="flex items-center gap-1.5 px-3 py-1 bg-orange-500/20 text-orange-400 rounded-full">
                  <div className="w-2 h-2 bg-orange-500 rounded-full"></div>
                  Risk
                </span>
                <span className="flex items-center gap-1.5 px-3 py-1 bg-red-500/20 text-red-400 rounded-full">
                  <div className="w-2 h-2 bg-red-500 rounded-full"></div>
                  Critical
                </span>
              </div>
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {heatmap.map((dept) => (
                <HeatmapCard key={dept.departmentId} department={dept} />
              ))}
            </div>
          </div>
        </div>

        {/* Department Leaderboard */}
        <div className="bg-slate-800/50 backdrop-blur-sm rounded-2xl border border-slate-700 p-6">
          <h2 className="text-2xl font-bold text-white flex items-center gap-3 mb-6">
            <div className="p-2 bg-amber-600/20 rounded-lg">
              <Trophy className="w-6 h-6 text-amber-400" />
            </div>
            Department Leaderboard
          </h2>
          <div className="space-y-3">
            {leaderboard.map((dept) => (
              <LeaderboardRow key={dept.departmentId} department={dept} />
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

// KPI Card Component
interface KPICardProps {
  title: string;
  value: string | number;
  subtitle: string;
  icon: React.ReactNode;
  color: 'blue' | 'red' | 'green' | 'orange' | 'yellow' | 'purple' | 'pink';
}

function KPICard({ title, value, subtitle, icon, color }: KPICardProps) {
  const colorClasses = {
    blue: 'from-blue-600/20 to-blue-700/20 border-blue-500/30 text-blue-400',
    red: 'from-red-600/20 to-red-700/20 border-red-500/30 text-red-400',
    green: 'from-emerald-600/20 to-emerald-700/20 border-emerald-500/30 text-emerald-400',
    orange: 'from-orange-600/20 to-orange-700/20 border-orange-500/30 text-orange-400',
    yellow: 'from-yellow-600/20 to-yellow-700/20 border-yellow-500/30 text-yellow-400',
    purple: 'from-purple-600/20 to-purple-700/20 border-purple-500/30 text-purple-400',
    pink: 'from-pink-600/20 to-pink-700/20 border-pink-500/30 text-pink-400',
  };

  return (
    <div className={`bg-gradient-to-br ${colorClasses[color]} backdrop-blur-sm rounded-2xl border p-6 hover:scale-105 transition-transform duration-200`}>
      <div className="flex items-start justify-between mb-4">
        <div className={colorClasses[color].split(' ').pop()}>{icon}</div>
      </div>
      <h3 className="text-slate-300 text-sm font-medium mb-2">{title}</h3>
      <p className="text-4xl font-bold text-white mb-1">{value}</p>
      <p className="text-slate-400 text-sm">{subtitle}</p>
    </div>
  );
}

// Heatmap Card Component
function HeatmapCard({ department }: { department: DepartmentHeatmap }) {
  const heatColors = {
    green: 'border-emerald-500/50 bg-emerald-500/10',
    yellow: 'border-amber-500/50 bg-amber-500/10',
    orange: 'border-orange-500/50 bg-orange-500/10',
    red: 'border-red-500/50 bg-red-500/10',
  };

  const statusColors = {
    green: 'text-emerald-400',
    yellow: 'text-amber-400',
    orange: 'text-orange-400',
    red: 'text-red-400',
  };

  return (
    <div className={`rounded-xl border-2 ${heatColors[department.heatLevel]} p-5 hover:scale-105 transition-all duration-200`}>
      <div className="flex items-start justify-between mb-3">
        <div>
          <h3 className="text-lg font-bold text-white">{department.departmentName}</h3>
          <p className="text-slate-400 text-sm">{department.departmentCode}</p>
        </div>
        <div className={`text-3xl font-bold ${statusColors[department.heatLevel]}`}>
          {department.attendancePercentage}%
        </div>
      </div>
      <div className="space-y-2 text-sm">
        <div className="flex justify-between text-slate-300">
          <span>Present:</span>
          <span className="font-semibold">{department.presentCount}/{department.totalEmployees}</span>
        </div>
        <div className="flex justify-between text-slate-300">
          <span>Late:</span>
          <span className="font-semibold text-amber-400">{department.lateCount}</span>
        </div>
        <div className="flex justify-between text-slate-300">
          <span>Absent:</span>
          <span className="font-semibold text-red-400">{department.absentCount}</span>
        </div>
        <div className="flex justify-between text-slate-300">
          <span>Lost Time:</span>
          <span className="font-semibold text-orange-400">{department.lostMinutes}m</span>
        </div>
        <div className="pt-2 border-t border-slate-700">
          <div className="flex justify-between text-xs">
            <span className="text-slate-400">Target: {department.targetPercentage}%</span>
            <span className={department.gapToTarget >= 0 ? 'text-emerald-400' : 'text-red-400'}>
              {department.gapToTarget > 0 ? '+' : ''}{department.gapToTarget}%
            </span>
          </div>
        </div>
      </div>
    </div>
  );
}

// Leaderboard Row Component
function LeaderboardRow({ department }: { department: DepartmentLeaderboard }) {
  const rankColors = {
    1: 'bg-amber-500/20 text-amber-400 border-amber-500/50',
    2: 'bg-slate-400/20 text-slate-300 border-slate-400/50',
    3: 'bg-orange-600/20 text-orange-400 border-orange-600/50',
  };

  const trendIcon = {
    up: <ArrowUp className="w-4 h-4 text-emerald-400" />,
    down: <ArrowDown className="w-4 h-4 text-red-400" />,
    stable: <Minus className="w-4 h-4 text-slate-400" />,
  };

  return (
    <div className="flex items-center gap-4 p-4 bg-slate-700/30 rounded-xl hover:bg-slate-700/50 transition-colors">
      <div className={`w-12 h-12 rounded-full flex items-center justify-center font-bold text-lg border-2 ${
        rankColors[department.rank as keyof typeof rankColors] || 'bg-slate-600/20 text-slate-400 border-slate-600/50'
      }`}>
        #{department.rank}
      </div>
      <div className="flex-1">
        <div className="flex items-center gap-2 mb-1">
          <h3 className="text-lg font-semibold text-white">{department.departmentName}</h3>
          <span className="text-slate-400 text-sm">({department.departmentCode})</span>
        </div>
        <div className="flex items-center gap-4 text-sm text-slate-400">
          <span>{department.totalEmployees} employees</span>
          <span>•</span>
          <span>{department.exceptionsCount} exceptions</span>
          <span>•</span>
          <span>{department.totalLostHours}h lost</span>
        </div>
      </div>
      <div className="text-right">
        <div className="text-2xl font-bold text-white mb-1">
          {department.attendancePercentage}%
        </div>
        <div className="flex items-center gap-1 text-sm">
          {trendIcon[department.trend as keyof typeof trendIcon]}
          <span className="text-slate-400">Target: {department.targetPercentage}%</span>
        </div>
      </div>
    </div>
  );
}
