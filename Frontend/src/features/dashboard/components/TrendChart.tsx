import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer, Legend } from 'recharts'

interface TrendPoint {
  date: string
  onTimePercentage: number
  lateCount: number
  exceptionCount: number
}

interface TrendChartProps {
  data: TrendPoint[]
  title?: string
  height?: number
}

export default function TrendChart({ data, title = 'Attendance Trends', height = 280 }: TrendChartProps) {
  return (
    <div className="rounded-xl border border-border bg-card p-5">
      <h3 className="font-semibold mb-4">{title}</h3>

      <ResponsiveContainer width="100%" height={height}>
        <LineChart data={data} margin={{ top: 5, right: 20, bottom: 5, left: 0 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" opacity={0.3} />
          <XAxis
            dataKey="date"
            tick={{ fontSize: 11, fill: 'hsl(var(--muted-foreground))' }}
            interval="preserveStartEnd"
          />
          <YAxis
            yAxisId="left"
            tick={{ fontSize: 11, fill: 'hsl(var(--muted-foreground))' }}
            domain={[0, 100]}
            tickFormatter={(v) => `${v}%`}
          />
          <YAxis
            yAxisId="right"
            orientation="right"
            tick={{ fontSize: 11, fill: 'hsl(var(--muted-foreground))' }}
          />
          <Tooltip
            content={({ active, payload }) => {
              if (active && payload?.[0]) {
                const d = payload[0].payload as TrendPoint
                return (
                  <div className="bg-card border border-border rounded-lg p-3 shadow-lg text-sm space-y-1">
                    <p className="font-semibold">{d.date}</p>
                    {payload.map((p) => (
                      <p key={p.name} style={{ color: p.color }}>
                        {p.name}: <span className="font-semibold">{p.value}{p.dataKey === 'onTimePercentage' ? '%' : ''}</span>
                      </p>
                    ))}
                  </div>
                )
              }
              return null
            }}
          />
          <Legend wrapperStyle={{ fontSize: '12px' }} />
          <Line
            yAxisId="left"
            type="monotone"
            dataKey="onTimePercentage"
            name="On-time %"
            stroke="#22c55e"
            strokeWidth={2}
            dot={{ r: 3 }}
            activeDot={{ r: 5 }}
          />
          <Line
            yAxisId="right"
            type="monotone"
            dataKey="lateCount"
            name="Late Count"
            stroke="#eab308"
            strokeWidth={2}
            dot={{ r: 3 }}
          />
          <Line
            yAxisId="right"
            type="monotone"
            dataKey="exceptionCount"
            name="Exceptions"
            stroke="#ef4444"
            strokeWidth={2}
            dot={{ r: 3 }}
          />
        </LineChart>
      </ResponsiveContainer>
    </div>
  )
}
