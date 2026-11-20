import { cn } from '@/core/utils/cn'
import { Inbox, Search, AlertTriangle, CheckCircle } from 'lucide-react'

interface EmptyStateProps {
  icon?: 'inbox' | 'search' | 'alert' | 'check'
  title: string
  description?: string
  actionLabel?: string
  onAction?: () => void
  className?: string
}

export default function EmptyState({
  icon = 'inbox',
  title,
  description,
  actionLabel,
  onAction,
  className
}: EmptyStateProps) {
  const icons = {
    inbox: <Inbox className="w-12 h-12 text-muted-foreground/40" />,
    search: <Search className="w-12 h-12 text-muted-foreground/40" />,
    alert: <AlertTriangle className="w-12 h-12 text-yellow-500/60" />,
    check: <CheckCircle className="w-12 h-12 text-green-500/60" />
  }

  return (
    <div className={cn('flex flex-col items-center justify-center py-12 px-4 text-center', className)}>
      <div className="mb-4">{icons[icon]}</div>
      <h3 className="text-lg font-semibold mb-1">{title}</h3>
      {description && <p className="text-sm text-muted-foreground max-w-md mb-4">{description}</p>}
      {actionLabel && onAction && (
        <button
          onClick={onAction}
          className="px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity"
        >
          {actionLabel}
        </button>
      )}
    </div>
  )
}
