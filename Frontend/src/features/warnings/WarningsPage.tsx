import { useState } from 'react'
import WarningCard from './components/WarningCard'
import ThresholdProgress from './components/ThresholdProgress'
import WarningTimeline from './components/WarningTimeline'
import WarningGenerationWizard from './WarningGenerationWizard'
import EmptyState from '@/shared/components/EmptyState'
import { LoadingSpinner } from '@/shared/components/LoadingSpinner'
import { useWarnings, useEmployees, useExceptions } from '@/core/hooks/useDashboardQueries'

export default function WarningsPage() {
  const [showWizard, setShowWizard] = useState(false)
  const [filterStatus, setFilterStatus] = useState('all')

  const { data: allWarnings = [], isLoading } = useWarnings()
  const { data: employees = [] } = useEmployees()
  const { data: exceptions = [] } = useExceptions()

  const filteredWarnings = filterStatus === 'all' ? allWarnings : allWarnings.filter(w => w.status === filterStatus)

  // Compute sidebar data from live warnings
  const activeWarnings = allWarnings.filter(w => w.status === 'Issued' || w.status === 'Acknowledged')
  const thresholdData = {
    currentCount: activeWarnings.length,
    nextLevel: activeWarnings.length >= 3 ? 'Final Written Warning' : activeWarnings.length >= 1 ? 'Written Warning' : 'Counselling',
    thresholdForNext: activeWarnings.length >= 3 ? 5 : activeWarnings.length >= 1 ? 3 : 1,
    totalHistory: allWarnings.length
  }
  const timelineWarnings = [...allWarnings]
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
    .slice(0, 5)
    .map(w => ({
      id: w.id,
      warningType: w.warningType,
      date: w.createdAt,
      status: w.status,
      issuedBy: w.issuedByName || 'System'
    }))

  const handleWizardComplete = async () => {
    setShowWizard(false)
  }

  if (isLoading) {
    return <LoadingSpinner size="lg" label="Loading warnings..." />
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Warning Letters</h1>
          <p className="text-sm text-muted-foreground mt-1">Generate and manage disciplinary warnings</p>
        </div>
        <button onClick={() => setShowWizard(true)} className="px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90">
          New Warning
        </button>
      </div>

      {/* Status filters */}
      <div className="flex flex-wrap gap-2">
        {[
          { label: 'All', value: 'all', count: allWarnings.length },
          { label: 'Draft', value: 'Draft', count: allWarnings.filter(w => w.status === 'Draft').length },
          { label: 'Issued', value: 'Issued', count: allWarnings.filter(w => w.status === 'Issued').length },
          { label: 'Acknowledged', value: 'Acknowledged', count: allWarnings.filter(w => w.status === 'Acknowledged').length }
        ].map(f => (
          <button key={f.value} onClick={() => setFilterStatus(f.value)}
            className={`px-3 py-1.5 rounded-full text-xs font-medium border transition-colors ${filterStatus === f.value ? 'bg-primary text-primary-foreground border-primary' : 'bg-muted text-muted-foreground border-border hover:bg-muted/80'}`}>
            {f.label} ({f.count})
          </button>
        ))}
      </div>

      {/* Two-column layout */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2">
          {filteredWarnings.length === 0 ? (
            <EmptyState icon="check" title="No warnings" description="No warning letters match your criteria." />
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {filteredWarnings.map(w => (
                <WarningCard key={w.id} warning={w} />
              ))}
            </div>
          )}
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          <ThresholdProgress data={thresholdData} />
          <WarningTimeline warnings={timelineWarnings} />
        </div>
      </div>

      {/* Wizard Modal */}
      {showWizard && (
        <div className="fixed inset-0 z-[60] flex items-center justify-center p-4">
          <div className="absolute inset-0 bg-black/60 backdrop-blur-sm" onClick={() => setShowWizard(false)} />
          <div className="relative w-full max-w-2xl rounded-xl border border-border bg-card shadow-2xl animate-fade-in max-h-[90vh] overflow-y-auto">
            <div className="p-6">
              <div className="flex items-center justify-between mb-6">
                <h2 className="text-xl font-bold">Generate Warning Letter</h2>
                <button onClick={() => setShowWizard(false)} className="p-1 rounded-md hover:bg-muted">✕</button>
              </div>
              <WarningGenerationWizard
                employees={employees}
                exceptions={exceptions}
                onComplete={async () => {}}
                onCancel={() => setShowWizard(false)}
              />
            </div>
          </div>
        </div>
      )}
    </div>
  )
}

