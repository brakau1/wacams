import { PieChart, Pie, Cell, ResponsiveContainer, Tooltip, Legend } from 'recharts'
import { AlertTriangle } from 'lucide-react'

export interface ExceptionData {
  exceptionType: string
  count: number
  percentage: number
  lostMinutes: number
}

interface ExceptionAnalysisChartProps {
  data: ExceptionData[]
  title?: string
  height?: number
}

const EXCEPTION_COLORS: Record<string, string> = {
  'LateArrival': '#eab308',
  'EarlyDeparture': '#f97316',
  'MissingClockIn': '#ef4444',
  'MissingClockOut': '#dc2626',
  'UnplannedAbsenteeism': '#991b1b',
  'AWOL': '#7f1d1d',
  'SickLeave': '#06b6d4',
  'FamilyResponsibilityLeave': '#0891b2',
  'AnnualLeave': '#22c55e',
  'Training': '#3b82f6',
  'Workshop': '#8b5cf6',
  'ShiftCancellation': '#a855f7'
}

const DEFAULT_COLOR = '#6b7280'

export default function ExceptionAnalysisChart({ 
  data, 
  title = 'Exception Analysis', 
  height = 320 
}: ExceptionAnalysisChartProps) {
  const chartData = data.map(d => ({
    ...d,
    color: EXCEPTION_COLORS[d.exceptionType] || DEFAULT_COLOR
  }))

  return (
    <div className="rounded-xl border border-border bg-card p-5">
      <div className="flex items-center gap-2 mb-4">
        <AlertTriangle className="w-5 h-5 text-orange-500" />
        <h3 className="font-semibold">{title}</h3>
      </div>

      <ResponsiveContainer width="100%" height={height}>
        <PieChart>
          <Pie
            data={chartData}
            cx="50%"
            cy="50%"
            labelLine={false}
            label={({ exceptionType, percentage }) => `${exceptionType}: ${percentage.toFixed(1)}%`}
            outerRadius={100}
            fill="#8884d8"
            dataKey="count"
          >
            {chartData.map((entry, index) => (
              <Cell key={`cell-${index}`} fill={entry.color} />
            ))}
          </Pie>
          <Tooltip
            content={({ active, payload }) => {
              if (active && payload?.[0]) {
                const d = payload[0].payload as ExceptionData
                return (
                  <div className="bg-card border border-border rounded-lg p-3 shadow-lg text-sm">
                    <p className="font-semibold mb-1">{d.exceptionType}</p>
                    <p>Count: <span className="text-primary font-semibold">{d.count}</span></p>
                    <p>Percentage: <span className="text-primary font-semibold">{d.percentage.toFixed(1)}%</span></p>
                    <p>Lost Time: <span className="text-orange-500 font-semibold">{d.lostMinutes} min</span></p>
                  </div>
                )
              }
              return null
            }}
          />
          <Legend 
            wrapperStyle={{ fontSize: '12px' }}
            formatter={(value) => <span className="text-muted-foreground">{value}</span>}
          />
        </PieChart>
      </ResponsiveContainer>

      {/* Summary Stats */}
      <div className="grid grid-cols-3 gap-4 mt-4 pt-4 border-t border-border">
        <div className="text-center">
          <p className="text-xs text-muted-foreground">Total Exceptions</p>
          <p className="text-lg font-bold">{data.reduce((sum, d) => sum + d.count, 0)}</p>
        </div>
        <div className="text-center">
          <p className="text-xs text-muted-foreground">Exception Types</p>
          <p className="text-lg font-bold">{data.length}</p>
        </div>
        <div className="text-center">
          <p className="text-xs text-muted-foreground">Total Lost Time</p>
          <p className="text-lg font-bold text-orange-500">
            {Math.round(data.reduce((sum, d) => sum + d.lostMinutes, 0) / 60)}h
          </p>
        </div>
      </div>
    </div>
  )
}
