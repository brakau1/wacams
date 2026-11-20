import { cn } from '@/core/utils/cn'
import { type LucideIcon, ArrowUpRight, ArrowDownRight } from 'lucide-react'

interface KPICardProps {
  title: string
  value: string | number
  subtitle?: string
  icon: LucideIcon
  trend?: 'up' | 'down' | 'neutral'
  trendValue?: string
  color?: 'blue' | 'green' | 'yellow' | 'red' | 'purple' | 'orange'
  size?: 'sm' | 'md' | 'lg'
}

export default function KPICard({
  title,
  value,
  subtitle,
  icon: Icon,
  trend,
  trendValue,
  color = 'blue',
  size = 'md'
}: KPICardProps) {
  const colorMap = {
    blue: { bg: 'bg-blue-500/10 border-blue-500/20 text-blue-400', iconBg: 'bg-blue-500/20' },
    green: { bg: 'bg-green-500/10 border-green-500/20 text-green-400', iconBg: 'bg-green-500/20' },
    yellow: { bg: 'bg-yellow-500/10 border-yellow-500/20 text-yellow-400', iconBg: 'bg-yellow-500/20' },
    red: { bg: 'bg-red-500/10 border-red-500/20 text-red-400', iconBg: 'bg-red-500/20' },
    purple: { bg: 'bg-purple-500/10 border-purple-500/20 text-purple-400', iconBg: 'bg-purple-500/20' },
    orange: { bg: 'bg-orange-500/10 border-orange-500/20 text-orange-400', iconBg: 'bg-orange-500/20' }
  }

  const colors = colorMap[color]
  const sizes = {
    sm: { card: 'p-4', title: 'text-xs', value: 'text-xl', subtitle: 'text-[10px]' },
    md: { card: 'p-5', title: 'text-sm', value: 'text-2xl', subtitle: 'text-xs' },
    lg: { card: 'p-6', title: 'text-base', value: 'text-3xl', subtitle: 'text-sm' }
  }[size]

  return (
    <div className={cn(
      'rounded-xl border p-5 transition-all hover:shadow-md hover:shadow-black/20',
      colors.bg
    )}>
      {/* Header row */}
      <div className="flex items-start justify-between mb-3">
        <p className={cn('font-medium text-muted-foreground', sizes.title)}>{title}</p>
        <div className={cn('w-9 h-9 rounded-lg flex items-center justify-center', colors.iconBg)}>
          <Icon className="w-5 h-5" />
        </div>
      </div>

      {/* Value */}
      <p className={cn('font-bold tracking-tight', sizes.value)}>{value}</p>

      {/* Footer row */}
      <div className="flex items-center justify-between mt-3">
        {subtitle && <p className={cn('text-muted-foreground', sizes.subtitle)}>{subtitle}</p>}
        {trend && trendValue && (
          <div className={cn(
            'flex items-center gap-1 text-xs font-medium',
            trend === 'up' ? 'text-green-400' : trend === 'down' ? 'text-red-400' : 'text-muted-foreground'
          )}>
            {trend === 'up' ? <ArrowUpRight className="w-3 h-3" /> : <ArrowDownRight className="w-3 h-3" />}
            {trendValue}
          </div>
        )}
      </div>
    </div>
  )
}
