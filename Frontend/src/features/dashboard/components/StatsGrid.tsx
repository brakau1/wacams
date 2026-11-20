import { cn } from '@/core/utils/cn'

interface StatsGridProps {
  children: React.ReactNode
  cols?: number
}

export default function StatsGrid({ children, cols = 4 }: StatsGridProps) {
  return (
    <div className={cn(
      'grid gap-4',
      cols === 2 && 'grid-cols-2',
      cols === 3 && 'grid-cols-3',
      cols === 4 && 'grid-cols-4',
      cols === 5 && 'grid-cols-5'
    )}>
      {children}
    </div>
  )
}

/**
 * Stat item for use within StatsGrid.
 */
export function StatItem({ label, value, icon, color = 'blue' }: {
  label: string
  value: string | number
  icon: React.ReactNode
  color?: string
}) {
  return (
    <div className="flex items-center gap-3 p-3 rounded-lg bg-card border border-border">
      <div className={cn('w-8 h-8 rounded-md flex items-center justify-center flex-shrink-0', color)}>
        {icon}
      </div>
      <div>
        <p className="text-xs text-muted-foreground">{label}</p>
        <p className="text-sm font-bold">{value}</p>
      </div>
    </div>
  )
}
