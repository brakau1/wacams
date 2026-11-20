import { cn } from '@/core/utils/cn'
import { CheckCircle2, Clock, AlertTriangle, Eye, XCircle } from 'lucide-react'

type TimelineStep = {
  label: string
  status: 'completed' | 'current' | 'pending'
  date?: string
  by?: string
  icon?: React.ReactNode
}

interface StatusTimelineProps {
  steps: TimelineStep[]
}

const STEP_ICONS = {
  completed: <CheckCircle2 className="w-4 h-4" />,
  current: <Clock className="w-4 h-4 animate-pulse" />,
  pending: <AlertTriangle className="w-4 h-4 opacity-50" />
}

export default function StatusTimeline({ steps }: StatusTimelineProps) {
  return (
    <div className="space-y-0">
      {steps.map((step, index) => (
        <div key={index} className="flex gap-3">
          {/* Timeline line + icon */}
          <div className="flex flex-col items-center">
            <div className={cn(
              'w-8 h-8 rounded-full flex items-center justify-center flex-shrink-0 border-2',
              step.status === 'completed' ? 'bg-green-500/10 border-green-500 text-green-400' :
              step.status === 'current' ? 'bg-primary/10 border-primary text-primary' :
              'bg-muted/30 border-muted text-muted-foreground'
            )}>
              {step.icon || STEP_ICONS[step.status]}
            </div>
            {index < steps.length - 1 && (
              <div className={cn(
                'w-0.5 h-8',
                step.status === 'completed' ? 'bg-green-500/50' : 'bg-border'
              )} />
            )}
          </div>

          {/* Content */}
          <div className="pb-6">
            <p className={cn(
              'text-sm font-medium',
              step.status === 'completed' ? 'text-foreground' :
              step.status === 'current' ? 'text-primary' :
              'text-muted-foreground'
            )}>
              {step.label}
            </p>
            {step.date && (
              <p className="text-xs text-muted-foreground mt-0.5">
                {step.date}{step.by ? ` · ${step.by}` : ''}
              </p>
            )}
          </div>
        </div>
      ))}
    </div>
  )
}

// Default investigation timeline
export function getDefaultTimeline() {
  return [
    { label: 'Exception Detected', status: 'completed' as const, date: 'Nov 11, 06:30', by: 'System' },
    { label: 'Assigned to Team Leader', status: 'completed' as const, date: 'Nov 11, 07:00', by: 'Auto-assign' },
    { label: 'Under Investigation', status: 'current' as const },
    { label: 'Investigation Complete', status: 'pending' as const },
    { label: 'Action Taken & Closed', status: 'pending' as const }
  ]
}
