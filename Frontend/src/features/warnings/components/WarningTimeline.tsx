import { FileWarning } from 'lucide-react'

interface WarningHistoryItem {
  id: string
  warningType: string
  date: string
  status: string
  issuedBy: string
}

interface WarningTimelineProps {
  warnings: WarningHistoryItem[]
}

export default function WarningTimeline({ warnings }: WarningTimelineProps) {
  if (warnings.length === 0) {
    return (
      <div className="text-center py-8 text-muted-foreground">
        <FileWarning className="w-8 h-8 mx-auto mb-2 opacity-50" />
        <p className="text-sm">No warning history</p>
      </div>
    )
  }

  const typeOrder = ['Counselling', 'VerbalWarning', 'WrittenWarning', 'FinalWrittenWarning']
  const sorted = [...warnings].sort((a, b) => typeOrder.indexOf(a.warningType) - typeOrder.indexOf(b.warningType))

  return (
    <div className="space-y-4">
      {sorted.map((w, index) => (
        <div key={w.id} className="flex gap-3">
          {/* Timeline line */}
          <div className="flex flex-col items-center">
            <div className="w-8 h-8 rounded-full bg-destructive/10 border border-destructive/30 flex items-center justify-center flex-shrink-0">
              <span className="text-xs font-bold text-destructive">{index + 1}</span>
            </div>
            {index < sorted.length - 1 && (
              <div className="w-0.5 h-8 bg-border" />
            )}
          </div>

          {/* Content */}
          <div className="pb-2">
            <p className="text-sm font-medium">{w.warningType.replace(/([A-Z])/g, ' $1').trim()}</p>
            <p className="text-xs text-muted-foreground">
              Issued by {w.issuedBy} · {new Date(w.date).toLocaleDateString()}
            </p>
          </div>
        </div>
      ))}
    </div>
  )
}
