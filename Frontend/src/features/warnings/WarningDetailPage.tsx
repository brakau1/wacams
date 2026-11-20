import { useParams, useNavigate } from 'react-router-dom'
import SignedFormUploader from './components/SignedFormUploader'
import WarningTimeline from './components/WarningTimeline'
import ThresholdProgress from './components/ThresholdProgress'
import StatusChip from '../attendance/components/StatusChip'
import { LoadingSpinner } from '@/shared/components/LoadingSpinner'
import { useWarning, useEmployeeWarnings } from '@/core/hooks/useDashboardQueries'

export default function WarningDetailPage() {
  const { id } = useParams<{ id: string }>()
  const navigate = useNavigate()
  const { data: warning, isLoading } = useWarning(id || '')
  const { data: employeeWarnings = [] } = useEmployeeWarnings(warning?.employeeId || '')

  // Compute sidebar data from employee's live warning history
  const activeWarnings = employeeWarnings.filter(w => w.status === 'Issued' || w.status === 'Acknowledged')
  const thresholdData = {
    currentCount: activeWarnings.length,
    nextLevel: activeWarnings.length >= 3 ? 'Final Written Warning' : activeWarnings.length >= 1 ? 'Written Warning' : 'Counselling',
    thresholdForNext: activeWarnings.length >= 3 ? 5 : activeWarnings.length >= 1 ? 3 : 1,
    totalHistory: employeeWarnings.length
  }
  const timelineWarnings = [...employeeWarnings]
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
    .slice(0, 5)
    .map(w => ({
      id: w.id,
      warningType: w.warningType,
      date: w.createdAt,
      status: w.status,
      issuedBy: w.issuedByName || 'System'
    }))

  if (isLoading || !warning) {
    return <LoadingSpinner size="lg" label="Loading warning details..." />
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <button onClick={() => navigate(-1)} className="flex items-center gap-2 text-sm text-muted-foreground hover:text-foreground">
          ← Back to Warnings
        </button>
        <div className="flex items-center gap-3">
          <StatusChip status={warning.status} />
          <button className="px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90">
            Edit Warning
          </button>
        </div>
      </div>

      {/* Two-column layout */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Main content */}
        <div className="lg:col-span-2 space-y-6">
          <div className="rounded-xl border border-border bg-card p-6">
            <h2 className="text-xl font-bold mb-1">{warning.title}</h2>
            <p className="text-sm text-muted-foreground mb-4">
              {warning.warningType.replace(/([A-Z])/g, ' $1').trim()} · #{warning.id}
            </p>
            <p className="text-sm leading-relaxed">{warning.description}</p>
          </div>

          {/* Details */}
          <div className="rounded-xl border border-border bg-card p-5">
            <h3 className="font-semibold mb-4">Details</h3>
            <div className="grid grid-cols-2 gap-4 text-sm">
              {[
                { label: 'Employee', value: warning.employeeName },
                { label: 'Department', value: warning.departmentName },
                { label: 'Issued By', value: warning.issuedByName },
                { label: 'Date Issued', value: new Date(warning.createdAt).toLocaleDateString() },
                { label: 'Severity', value: `Level ${warning.severity}` },
                { label: 'Escalation Level', value: String(warning.escalationLevel) }
              ].map(({ label, value }) => (
                <div key={label}>
                  <p className="text-xs text-muted-foreground">{label}</p>
                  <p className="font-medium mt-0.5">{value}</p>
                </div>
              ))}
            </div>
          </div>

          {/* Upload signed form */}
          <div className="rounded-xl border border-border bg-card p-5">
            <h3 className="font-semibold mb-4">Signed Form</h3>
            <SignedFormUploader existingUrl={warning.signedFormUrl} onUpload={async () => {}} />
          </div>
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          <ThresholdProgress data={thresholdData} />
          <WarningTimeline warnings={timelineWarnings} />
        </div>
      </div>
    </div>
  )
}
