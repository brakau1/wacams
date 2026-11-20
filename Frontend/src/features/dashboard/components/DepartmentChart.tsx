import { BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Cell } from 'recharts'
import { Building2 } from 'lucide-react'

interface DepartmentData {
  departmentName: string
  attendancePercentage: number
  presentCount: number
  totalEmployees: number
}

interface DepartmentChartProps {
  data: DepartmentData[]
  title?: string
  height?: number
}

const COLOR_MAP: Record<string, string> = {
  Production: '#3b82f6',
  Quality: '#22c55e',
  Engineering: '#a855f7',
  Logistics: '#f97316',
  Office: '#eab308',
  Maintenance: '#ec4899',
  HR: '#06b6d4',
  Security: '#64748b'
}

export default function DepartmentChart({ data, title = 'Department Attendance %', height = 280 }: DepartmentChartProps) {
  const chartData = data.map(d => ({
    ...d,
    color: COLOR_MAP[d.departmentName] || '#6366f1'
  }))

  return (
    <div className="rounded-xl border border-border bg-card p-5">
      <div className="flex items-center gap-2 mb-4">
        <Building2 className="w-5 h-5 text-muted-foreground" />
        <h3 className="font-semibold">{title}</h3>
      </div>

      <ResponsiveContainer width="100%" height={height}>
        <BarChart data={chartData} margin={{ top: 5, right: 20, bottom: 5, left: 0 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" opacity={0.3} />
          <XAxis
            dataKey="departmentName"
            tick={{ fontSize: 11, fill: 'hsl(var(--muted-foreground))' }}
            interval={0}
            angle={-30}
            textAnchor="end"
            height={60}
          />
          <YAxis
            tick={{ fontSize: 11, fill: 'hsl(var(--muted-foreground))' }}
            domain={[0, 100]}
            tickFormatter={(v) => `${v}%`}
          />
          <Tooltip
            content={({ active, payload }) => {
              if (active && payload?.[0]) {
                const d = payload[0].payload as DepartmentData
                return (
                  <div className="bg-card border border-border rounded-lg p-3 shadow-lg text-sm">
                    <p className="font-semibold mb-1">{d.departmentName}</p>
                    <p>Attendance: <span className="text-primary font-semibold">{d.attendancePercentage}%</span></p>
                    <p>Present: {d.presentCount}/{d.totalEmployees}</p>
                  </div>
                )
              }
              return null
            }}
          />
          <Bar dataKey="attendancePercentage" radius={[4, 4, 0, 0]}>
            {chartData.map((entry, index) => (
              <Cell key={`cell-${index}`} fill={entry.color} />
            ))}
          </Bar>
        </BarChart>
      </ResponsiveContainer>

      {/* Legend */}
      <div className="flex flex-wrap gap-4 mt-3 pt-3 border-t border-border">
        {chartData.map(d => (
          <div key={d.departmentName} className="flex items-center gap-1.5 text-xs">
            <div className="w-3 h-3 rounded" style={{ backgroundColor: d.color }} />
            <span className="text-muted-foreground">{d.departmentName}</span>
          </div>
        ))}
      </div>
    </div>
  )
}
