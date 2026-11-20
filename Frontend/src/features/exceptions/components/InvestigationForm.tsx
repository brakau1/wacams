import { useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { cn } from '@/core/utils/cn'
import { Loader2 } from 'lucide-react'

const investigationSchema = z.object({
  notes: z.string().min(10, 'Notes must be at least 10 characters'),
  actionTaken: z.string().min(5, 'Please describe the action taken'),
  resolveType: z.enum(['explained', 'documented', 'corrective', 'escalated', 'closed']),
  isVerified: z.boolean().optional()
})

type InvestigationFormData = z.infer<typeof investigationSchema>

interface InvestigationFormProps {
  exceptionId: string
  onSubmit: (data: InvestigationFormData) => Promise<void>
  onCancel: () => void
}

export default function InvestigationForm({ exceptionId, onSubmit, onCancel }: InvestigationFormProps) {
  const [submitting, setSubmitting] = useState(false)
  const form = useForm<InvestigationFormData>({
    resolver: zodResolver(investigationSchema),
    defaultValues: {
      notes: '',
      actionTaken: '',
      resolveType: 'explained',
      isVerified: false
    }
  })

  const handleSubmit = async (data: InvestigationFormData) => {
    setSubmitting(true)
    try {
      await onSubmit(data)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form onSubmit={form.handleSubmit(handleSubmit)} className="space-y-4">
      {/* Employee info banner */}
      <div className="p-3 rounded-lg bg-muted/30 border border-border flex items-center gap-3">
        <span className="text-lg">⚠️</span>
        <div>
          <p className="text-sm font-medium">Exception #{exceptionId}</p>
          <p className="text-xs text-muted-foreground">Investigation required before disciplinary action</p>
        </div>
      </div>

      {/* Verification checkbox */}
      <label className="flex items-start gap-3 cursor-pointer">
        <input
          type="checkbox"
          {...form.register('isVerified')}
          className="mt-1 rounded border-border text-primary focus:ring-primary"
        />
        <div>
          <p className="text-sm font-medium">Verify attendance records</p>
          <p className="text-xs text-muted-foreground">Confirm that clock-in/out times and shift data have been verified against source systems</p>
        </div>
      </label>

      {/* Notes */}
      <div className="space-y-2">
        <label className="text-sm font-medium">Investigation Notes</label>
        <textarea
          {...form.register('notes')}
          rows={4}
          placeholder="Describe your findings and observations..."
          className="w-full px-3 py-2 rounded-lg border border-border bg-background text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary/50 resize-none"
        />
        {form.formState.errors.notes && (
          <p className="text-xs text-destructive">{form.formState.errors.notes.message}</p>
        )}
      </div>

      {/* Action Taken */}
      <div className="space-y-2">
        <label className="text-sm font-medium">Action Taken</label>
        <select
          {...form.register('actionTaken')}
          className="w-full px-3 py-2 rounded-lg border border-border bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary/50"
        >
          <option value="">Select action...</option>
          <option value="Employee explained — no further action needed">Employee explanation received, no action needed</option>
          <option value="Documented for future reference">Documented for future reference</option>
          <option value="Corrective coaching session scheduled">Coaching session scheduled</option>
          <option value="Warning letter initiated">Warning letter initiated</option>
          <option value="Escalated to higher management">Escalated to higher management</option>
        </select>
        {form.formState.errors.actionTaken && (
          <p className="text-xs text-destructive">{form.formState.errors.actionTaken.message}</p>
        )}
      </div>

      {/* Resolution Type */}
      <div className="space-y-2">
        <label className="text-sm font-medium">Resolution Type</label>
        <div className="grid grid-cols-2 sm:grid-cols-3 gap-2">
          {[
            { value: 'explained', label: 'Explained' },
            { value: 'documented', label: 'Documented' },
            { value: 'corrective', label: 'Corrective' },
            { value: 'escalated', label: 'Escalated' },
            { value: 'closed', label: 'Closed' }
          ].map(option => (
            <button
              key={option.value}
              type="button"
              onClick={() => form.setValue('resolveType', option.value as any)}
              className={cn(
                'px-3 py-2 rounded-lg border text-sm font-medium transition-colors text-center',
                form.watch('resolveType') === option.value
                  ? 'bg-primary text-primary-foreground border-primary'
                  : 'border-border hover:bg-muted'
              )}
            >
              {option.label}
            </button>
          ))}
        </div>
      </div>

      {/* Submit row */}
      <div className="flex items-center justify-end gap-3 pt-2">
        <button
          type="button"
          onClick={onCancel}
          className="px-4 py-2 rounded-lg border border-border text-sm font-medium hover:bg-muted transition-colors"
        >
          Cancel
        </button>
        <button
          type="submit"
          disabled={submitting || !form.formState.isValid}
          className={cn(
            'px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 transition-opacity flex items-center gap-2',
            submitting && 'opacity-70'
          )}
        >
          {submitting && <Loader2 className="w-4 h-4 animate-spin" />}
          Submit Investigation
        </button>
      </div>
    </form>
  )
}
