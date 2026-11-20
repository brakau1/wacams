import { cn } from '@/core/utils/cn'
import StatusChip from '../../attendance/components/StatusChip'
import { FileWarning, AlertTriangle } from 'lucide-react'
import type { WarningLetter } from '@/core/api/types'

interface WarningCardProps {
  warning: WarningLetter
  onClick?: () => void
}

const WARNING_COLORS: Record<string, string> = {
  Counselling: 'bg-blue-500/10 text-blue-400 border-blue-500/30',
  VerbalWarning: 'bg-yellow-500/10 text-yellow-400 border-yellow-500/30',
  WrittenWarning: 'bg-orange-500/10 text-orange-400 border-orange-500/30',
  FinalWrittenWarning: 'bg-red-500/10 text-red-400 border-red-500/30'
}

export default function WarningCard({ warning, onClick }: WarningCardProps) {
  return (
    <div
      onClick={onClick}
      className={cn(
        'rounded-xl border p-4 cursor-pointer transition-all hover:shadow-md',
        WARNING_COLORS[warning.warningType] || 'border-border bg-card'
      )}
    >
      <div className="flex items-start justify-between mb-2">
        <div className="flex items-center gap-2">
          <FileWarning className="w-5 h-5" />
          <span className="font-semibold text-sm">{warning.warningType.replace(/([A-Z])/g, ' $1').trim()}</span>
        </div>
        <StatusChip status={warning.status} size="sm" />
      </div>

      <p className="text-sm font-medium mb-1">{warning.title}</p>
      <p className="text-xs text-muted-foreground line-clamp-2 mb-3">{warning.description}</p>

      <div className="flex items-center justify-between text-xs text-muted-foreground">
        <span>{warning.employeeName}</span>
        <span>{new Date(warning.createdAt).toLocaleDateString()}</span>
      </div>

      {warning.signedFormUrl && (
        <div className="mt-2 flex items-center gap-1 text-xs text-green-400">
          <span className="inline-block w-2 h-2 rounded-full bg-green-400" />
          Signed form uploaded
        </div>
      )}
    </div>
  )
}
