import { Progress } from '@radix-ui/react-progress'
import { cn } from '@/core/utils/cn'

interface ThresholdData {
  currentCount: number
  nextLevel: string
  thresholdForNext: number
  totalHistory: number
}

interface ThresholdProgressProps {
  data: ThresholdData
}

export default function ThresholdProgress({ data }: ThresholdProgressProps) {
  const percentage = Math.min((data.currentCount / data.thresholdForNext) * 100, 100)

  return (
    <div className="rounded-xl border border-border bg-card p-5">
      <h3 className="font-semibold mb-4">Warning Threshold Progress</h3>

      {/* Current status */}
      <div className="mb-4">
        <div className="flex items-center justify-between mb-2">
          <span className="text-sm text-muted-foreground">Current warnings</span>
          <span className="text-sm font-bold">{data.currentCount}/{data.thresholdForNext}</span>
        </div>
        <div className="w-full h-3 rounded-full bg-muted overflow-hidden">
          <div
            className={cn(
              'h-full rounded-full transition-all',
              percentage >= 100 ? 'bg-red-500' :
              percentage >= 75 ? 'bg-orange-500' :
              percentage >= 50 ? 'bg-yellow-500' :
              'bg-blue-500'
            )}
            style={{ width: `${percentage}%` }}
          />
        </div>
      </div>

      {/* Next level info */}
      <div className="p-3 rounded-lg bg-muted/30 space-y-2">
        <div className="flex items-center justify-between">
          <span className="text-xs text-muted-foreground">Next level:</span>
          <span className="text-sm font-medium text-primary">{data.nextLevel}</span>
        </div>
        <div className="flex items-center justify-between">
          <span className="text-xs text-muted-foreground">Required:</span>
          <span className="text-xs font-mono">{data.thresholdForNext} total warnings</span>
        </div>
        {percentage >= 100 && (
          <div className="mt-2 p-2 rounded bg-destructive/10 border border-destructive/20 text-xs text-destructive">
            ⚠️ Threshold reached — next violation triggers automatic {data.nextLevel}
          </div>
        )}
      </div>

      {/* Total history */}
      <div className="mt-3 pt-3 border-t border-border flex items-center justify-between text-xs text-muted-foreground">
        <span>Total warnings on record</span>
        <span className="font-medium">{data.totalHistory}</span>
      </div>
    </div>
  )
}
