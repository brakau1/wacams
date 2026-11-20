import { cn } from '@/core/utils/cn'

interface LoadingSpinnerProps {
  size?: 'sm' | 'md' | 'lg'
  label?: string
  className?: string
}

export function LoadingSpinner({ size = 'md', label, className }: LoadingSpinnerProps) {
  const sizes = {
    sm: 'w-4 h-4 border-2',
    md: 'w-8 h-8 border-3',
    lg: 'w-12 h-12 border-4'
  }

  return (
    <div className={cn('flex flex-col items-center gap-3', className)}>
      <div className={cn(
        'rounded-full border-primary border-t-transparent animate-spin',
        sizes[size]
      )} />
      {label && <p className="text-sm text-muted-foreground">{label}</p>}
    </div>
  )
}

/**
 * Skeleton loader for tables/lists.
 */
export function Skeleton({ lines = 1, className }: { lines?: number; className?: string }) {
  return (
    <div className={cn('space-y-3', className)}>
      {Array.from({ length: lines }).map((_, i) => (
        <div key={i} className="animate-pulse">
          <div className="h-4 bg-muted rounded w-full" />
        </div>
      ))}
    </div>
  )
}
