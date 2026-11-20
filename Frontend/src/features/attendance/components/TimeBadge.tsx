import { cn } from '@/core/utils/cn'

interface TimeBadgeProps {
  time: string | null
  type?: 'clockIn' | 'clockOut'
  size?: 'sm' | 'md'
}

export default function TimeBadge({ time, type = 'clockIn', size = 'md' }: TimeBadgeProps) {
  if (!time) {
    return (
      <span className={cn(
        'inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium border border-dashed',
        'text-muted-foreground border-muted bg-transparent'
      )}>
        {type === 'clockIn' ? 'Not clocked in' : 'Not clocked out'}
      </span>
    )
  }

  const colorClass = type === 'clockIn'
    ? 'bg-green-500/10 text-green-400 border-green-500/30'
    : 'bg-orange-500/10 text-orange-400 border-orange-500/30'

  return (
    <span className={cn('inline-flex items-center gap-1 px-2 py-0.5 rounded-full text-xs font-medium border', colorClass)}>
      {type === 'clockIn' && <span className="w-1.5 h-1.5 rounded-full bg-green-400" />}
      {type === 'clockOut' && <span className="w-1.5 h-1.5 rounded-full bg-orange-400" />}
      {time}
    </span>
  )
}
