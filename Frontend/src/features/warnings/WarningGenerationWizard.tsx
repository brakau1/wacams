import { useState } from 'react'
import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { cn } from '@/core/utils/cn'
import { Loader2, ChevronRight, CheckCircle2 } from 'lucide-react'
import EmptyState from '@/shared/components/EmptyState'
import type { Employee, AttendanceException } from '@/core/api/types'

const warningSchema = z.object({
  employeeId: z.string().min(1, 'Select an employee'),
  exceptionIds: z.array(z.string()).min(1, 'Select at least one exception'),
  warningType: z.enum(['Counselling', 'VerbalWarning', 'WrittenWarning', 'FinalWrittenWarning']),
  title: z.string().min(5, 'Title is required'),
  description: z.string().min(20, 'Description must be at least 20 characters')
})

type WarningFormData = z.infer<typeof warningSchema>

const WARNING_STEPS = ['Employee', 'Exceptions', 'Type', 'Preview'] as const
type Step = typeof WARNING_STEPS[number]

interface WarningGenerationWizardProps {
  employees: Employee[]
  exceptions: AttendanceException[]
  onComplete: (data: WarningFormData) => Promise<void>
  onCancel: () => void
}

export default function WarningGenerationWizard({
  employees,
  exceptions,
  onComplete,
  onCancel
}: WarningGenerationWizardProps) {
  const [currentStep, setCurrentStep] = useState<Step>('Employee')
  const [submitting, setSubmitting] = useState(false)
  const [completed, setCompleted] = useState(false)
  const form = useForm<WarningFormData>({
    resolver: zodResolver(warningSchema),
    defaultValues: {
      employeeId: '',
      exceptionIds: [],
      warningType: 'Counselling',
      title: '',
      description: ''
    }
  })

  // Filter investigated exceptions for selected employee
  const investigatedExceptions = form.watch('employeeId')
    ? exceptions.filter(e => e.employeeId === form.watch('employeeId') && e.status === 'Investigated')
    : []

  const handleSubmit = async (data: WarningFormData) => {
    setSubmitting(true)
    try {
      await onComplete(data)
      setCompleted(true)
    } finally {
      setSubmitting(false)
    }
  }

  if (completed) {
    return (
      <div className="text-center py-12">
        <CheckCircle2 className="w-16 h-16 text-green-500 mx-auto mb-4" />
        <h3 className="text-xl font-bold mb-2">Warning Generated Successfully</h3>
        <p className="text-muted-foreground mb-6">The warning letter has been created and is ready for review.</p>
        <button onClick={onCancel} className="px-6 py-2 rounded-lg bg-primary text-primary-foreground">
          Done
        </button>
      </div>
    )
  }

  return (
    <div className="space-y-6">
      {/* Progress bar */}
      <div className="flex items-center gap-2">
        {WARNING_STEPS.map((step, i) => {
          const stepIndex = WARNING_STEPS.indexOf(step)
          const currentIndex = WARNING_STEPS.indexOf(currentStep)
          const isActive = stepIndex <= currentIndex
          const isCurrent = step === currentStep

          return (
            <div key={step} className="flex items-center flex-1">
              <div className={cn(
                'flex items-center gap-2 px-3 py-2 rounded-lg text-sm font-medium transition-colors',
                isActive ? 'bg-primary/10 text-primary' : 'bg-muted/30 text-muted-foreground'
              )}>
                {isActive && <CheckCircle2 className="w-4 h-4" />}
                <span>{step}</span>
              </div>
              {i < WARNING_STEPS.length - 1 && (
                <ChevronRight className="w-4 h-4 text-muted-foreground flex-shrink-0" />
              )}
            </div>
          )
        })}
      </div>

      {/* Step content */}
      <div className="rounded-xl border border-border bg-card p-6">
        {/* Employee Selection */}
        {currentStep === 'Employee' && (
          <div className="space-y-4">
            <div>
              <label className="text-sm font-medium mb-2 block">Select Employee</label>
              <select
                {...form.register('employeeId')}
                className="w-full px-4 py-3 rounded-lg border border-border bg-background text-foreground focus:outline-none focus:ring-2 focus:ring-primary/50"
              >
                <option value="">Choose an employee...</option>
                {employees.map(emp => (
                  <option key={emp.id} value={emp.id}>{emp.displayName} ({emp.employeeId})</option>
                ))}
              </select>
              {form.formState.errors.employeeId && (
                <p className="text-xs text-destructive mt-1">{form.formState.errors.employeeId.message}</p>
              )}
            </div>
          </div>
        )}

        {/* Exception Selection */}
        {currentStep === 'Exceptions' && (
          <div className="space-y-4">
            <p className="text-sm text-muted-foreground">
              Select investigated exceptions that support this warning ({investigatedExceptions.length} available)
            </p>
            {investigatedExceptions.length === 0 ? (
              <EmptyState icon="search" title="No investigated exceptions" description="This employee has no exceptions that are ready for warning generation." />
            ) : (
              <div className="space-y-2">
                {investigatedExceptions.map(exc => (
                  <label key={exc.id} className="flex items-start gap-3 p-3 rounded-lg border border-border cursor-pointer hover:bg-muted/30">
                    <input
                      type="checkbox"
                      checked={form.watch('exceptionIds').includes(exc.id)}
                      onChange={(e) => {
                        const current = form.getValues('exceptionIds')
                        if (e.target.checked) {
                          form.setValue('exceptionIds', [...current, exc.id])
                        } else {
                          form.setValue('exceptionIds', current.filter(id => id !== exc.id))
                        }
                      }}
                      className="mt-1 rounded border-border text-primary focus:ring-primary"
                    />
                    <div>
                      <p className="text-sm font-medium">{exc.description}</p>
                      <p className="text-xs text-muted-foreground">
                        {new Date(exc.date).toLocaleDateString()} · Lost {exc.lostProductionMinutes}m
                      </p>
                    </div>
                  </label>
                ))}
              </div>
            )}
            {form.formState.errors.exceptionIds && (
              <p className="text-xs text-destructive">{form.formState.errors.exceptionIds.message}</p>
            )}
          </div>
        )}

        {/* Warning Type */}
        {currentStep === 'Type' && (
          <div className="space-y-4">
            <label className="text-sm font-medium">Warning Level</label>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {[
                { value: 'Counselling', label: 'Counselling', desc: 'First level — informal discussion', color: 'border-blue-500/30 bg-blue-500/5' },
                { value: 'VerbalWarning', label: 'Verbal Warning', desc: 'Second level — documented verbal', color: 'border-yellow-500/30 bg-yellow-500/5' },
                { value: 'WrittenWarning', label: 'Written Warning', desc: 'Third level — formal written notice', color: 'border-orange-500/30 bg-orange-500/5' },
                { value: 'FinalWrittenWarning', label: 'Final Written Warning', desc: 'Fourth level — last chance notice', color: 'border-red-500/30 bg-red-500/5' }
              ].map(option => (
                <button
                  key={option.value}
                  type="button"
                  onClick={() => form.setValue('warningType', option.value as any)}
                  className={cn(
                    'p-4 rounded-lg border-2 text-left transition-all',
                    form.watch('warningType') === option.value
                      ? `${option.color} border-primary`
                      : 'border-border hover:border-primary/30'
                  )}
                >
                  <p className="font-semibold text-sm">{option.label}</p>
                  <p className="text-xs text-muted-foreground mt-1">{option.desc}</p>
                </button>
              ))}
            </div>
          </div>
        )}

        {/* Preview & Details */}
        {currentStep === 'Preview' && (
          <div className="space-y-4">
            <div className="p-4 rounded-lg bg-muted/30 space-y-3">
              <div>
                <p className="text-xs text-muted-foreground">Employee</p>
                <p className="font-medium">
                  {employees.find(e => e.id === form.watch('employeeId'))?.displayName || '—'}
                </p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Warning Type</p>
                <p className="font-medium">{form.watch('warningType')}</p>
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Title</p>
                <input
                  {...form.register('title')}
                  className="w-full px-3 py-1.5 rounded border border-border bg-background text-sm"
                  placeholder="Warning title..."
                />
              </div>
              <div>
                <p className="text-xs text-muted-foreground">Description</p>
                <textarea
                  {...form.register('description')}
                  rows={4}
                  className="w-full px-3 py-1.5 rounded border border-border bg-background text-sm resize-none"
                  placeholder="Detailed description of the violation..."
                />
                {form.formState.errors.description && (
                  <p className="text-xs text-destructive mt-1">{form.formState.errors.description.message}</p>
                )}
              </div>
            </div>
          </div>
        )}

        {/* Navigation buttons */}
        <div className="flex items-center justify-between mt-6 pt-4 border-t border-border">
          <button
            onClick={() => {
              const currentIndex = WARNING_STEPS.indexOf(currentStep)
              if (currentIndex > 0) setCurrentStep(WARNING_STEPS[currentIndex - 1])
              else onCancel()
            }}
            className="px-4 py-2 rounded-lg border border-border text-sm font-medium hover:bg-muted"
          >
            {WARNING_STEPS.indexOf(currentStep) > 0 ? 'Previous' : 'Cancel'}
          </button>
          <button
            onClick={() => {
              if (currentStep === 'Preview') {
                form.handleSubmit(handleSubmit)()
              } else {
                const currentIndex = WARNING_STEPS.indexOf(currentStep)
                if (currentIndex < WARNING_STEPS.length - 1) {
                  setCurrentStep(WARNING_STEPS[currentIndex + 1])
                }
              }
            }}
            disabled={submitting}
            className={cn(
              'px-6 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 disabled:opacity-50 flex items-center gap-2'
            )}
          >
            {submitting && <Loader2 className="w-4 h-4 animate-spin" />}
            {currentStep === 'Preview' ? 'Generate Warning' : 'Next'}
          </button>
        </div>
      </div>
    </div>
  )
}

