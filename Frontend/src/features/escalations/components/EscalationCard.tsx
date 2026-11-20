import { cn } from '@/core/utils/cn'
import { ArrowUpCircle, User, Clock, Calendar } from 'lucide-react'
import type { Escalation } from '@/core/api/types/escalation'

interface EscalationCardProps {
  escalation: Escalation
  onClick?: () => void
}

export default function EscalationCard({ escalation, onClick }: EscalationCardProps) {
  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Pending': return 'border-yellow-500/30 bg-yellow-500/5'
      case 'Acknowledged': return 'border-blue-500/30 bg-blue-500/5'
      case 'ActionTaken': return 'border-green-500/30 bg-green-500/5'
      case 'Escalated': return 'border-orange-500/30 bg-orange-500/5'
      case 'AutoClosed': return 'border-gray-500/30 bg-gray-500/5'
      case 'Resolved': return 'border-emerald-500/30 bg-emerald-500/5'
      default: return 'border-border bg-card'
    }
  }

  const getLevelLabel = (level: string) => {
    const labels: Record<string, string> = {
      'ManagerLevel1': 'Level 1',
      'ManagerLevel2': 'Level 2',
      'ManagerLevel3': 'Level 3',
      'ManagerLevel4': 'Level 4',
      'ManagerLevel5': 'Level 5',
      'HR': 'HR',
      'Admin': 'Admin'
    }
    return labels[level] || level
  }

  const isOverdue = new Date(escalation.nextEscalationTime) < new Date() && 
                    !['Resolved', 'AutoClosed'].includes(escalation.status)

  return (
    <div
      onClick={onClick}
      className={cn(
        'rounded-xl border p-4 cursor-pointer transition-all hover:shadow-md',
        getStatusColor(escalation.status)
      )}
    >
      <div className="flex items-start justify-between mb-3">
        <div className="flex items-center gap-2">
          <ArrowUpCircle className="w-5 h-5 text-orange-400" />
          <span className="font-semibold text-sm">{getLevelLabel(escalation.currentLevel)}</span>
        </div>
        <span className={cn(
          'px-2 py-1 rounded-full text-xs font-medium',
          escalation.status === 'Pending' && 'bg-yellow-500/20 text-yellow-700',
          escalation.status === 'Acknowledged' && 'bg-blue-500/20 text-blue-700',
          escalation.status === 'ActionTaken' && 'bg-green-500/20 text-green-700',
          escalation.status === 'Escalated' && 'bg-orange-500/20 text-orange-700',
          escalation.status === 'AutoClosed' && 'bg-gray-500/20 text-gray-700',
          escalation.status === 'Resolved' && 'bg-emerald-500/20 text-emerald-700'
        )}>
          {escalation.status}
        </span>
      </div>

      <div className="space-y-2 text-xs">
        <div className="flex items-center gap-1 text-muted-foreground">
          <User className="w-3 h-3" />
          Assigned to: <span className="font-medium text-foreground">{escalation.assignedTo}</span>
        </div>
        
        <div className="flex items-center gap-1 text-muted-foreground">
          <Calendar className="w-3 h-3" />
          Created: <span className="font-medium text-foreground">{new Date(escalation.createdAt).toLocaleDateString()}</span>
        </div>

        <div className={cn(
          'flex items-center gap-1',
          isOverdue ? 'text-red-600' : 'text-muted-foreground'
        )}>
          <Clock className="w-3 h-3" />
          {isOverdue ? 'Overdue: ' : 'Next escalation: '}
          <span className="font-medium">
            {new Date(escalation.nextEscalationTime).toLocaleString()}
          </span>
        </div>

        {escalation.documentUploaded && (
          <div className="pt-2 mt-2 border-t border-border text-green-600 text-xs">
            ✓ Document uploaded
          </div>
        )}
      </div>
    </div>
  )
}
