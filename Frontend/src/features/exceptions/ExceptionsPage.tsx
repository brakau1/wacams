import { useState } from 'react'
import { cn } from '@/core/utils/cn'
import ExceptionCard from './components/ExceptionCard'
import StatusTimeline, { getDefaultTimeline } from './components/StatusTimeline'
import InvestigationForm from './components/InvestigationForm'
import BulkInvestigateModal from './components/BulkInvestigateModal'
import EmptyState from '@/shared/components/EmptyState'
import { LoadingSpinner } from '@/shared/components/LoadingSpinner'
import { useExceptions } from '@/core/hooks/useDashboardQueries'
import type { AttendanceException } from '@/core/api/types'

export default function ExceptionsPage() {
  const [selectedException, setSelectedException] = useState<AttendanceException | null>(null)
  const [showBulkModal, setShowBulkModal] = useState(false)
  const [filterStatus, setFilterStatus] = useState<string>('all')
  const [investigationOpen, setInvestigationOpen] = useState(false)

  const { data: allExceptions = [], isLoading } = useExceptions()

  const filteredExceptions = filterStatus === 'all'
    ? allExceptions
    : allExceptions.filter(e => e.status === filterStatus)

  const openCount = allExceptions.filter(e => e.status === 'Open').length

  if (isLoading) {
    return <LoadingSpinner size="lg" label="Loading exceptions..." />
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Exceptions</h1>
          <p className="text-sm text-muted-foreground mt-1">Manage attendance exceptions and investigations</p>
        </div>
        <div className="flex items-center gap-3">
          {openCount > 0 && (
            <button
              onClick={() => setShowBulkModal(true)}
              className="px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90"
            >
              Bulk Investigate ({openCount})
            </button>
          )}
        </div>
      </div>

      {/* Status filter pills */}
      <div className="flex flex-wrap gap-2">
        {[
          { label: 'All', value: 'all', count: allExceptions.length },
          { label: 'Open', value: 'Open', count: allExceptions.filter((e: AttendanceException) => e.status === 'Open').length },
          { label: 'Under Review', value: 'UnderReview', count: allExceptions.filter((e: AttendanceException) => e.status === 'UnderReview').length },
          { label: 'Escalated', value: 'Escalated', count: allExceptions.filter((e: AttendanceException) => e.status === 'Escalated').length },
          { label: 'Investigated', value: 'Investigated', count: allExceptions.filter((e: AttendanceException) => e.status === 'Investigated').length },
          { label: 'Closed', value: 'Closed', count: allExceptions.filter((e: AttendanceException) => e.status === 'Closed').length }
        ].map(filter => (
          <button
            key={filter.value}
            onClick={() => setFilterStatus(filter.value)}
            className={cn(
              'px-3 py-1.5 rounded-full text-xs font-medium transition-colors border',
              filterStatus === filter.value
                ? 'bg-primary text-primary-foreground border-primary'
                : 'bg-muted text-muted-foreground border-border hover:bg-muted/80'
            )}
          >
            {filter.label} ({filter.count})
          </button>
        ))}
      </div>

      {/* Exception grid */}
      {filteredExceptions.length === 0 ? (
        <EmptyState
          icon="check"
          title="No exceptions found"
          description="All attendance records are in order."
        />
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
          {filteredExceptions.map(exc => (
            <ExceptionCard
              key={exc.id}
              exception={exc}
              onClick={() => setSelectedException(exc)}
            />
          ))}
        </div>
      )}

      {/* Detail Modal */}
      {selectedException && (
        <div className="fixed inset-0 z-[60] flex items-center justify-center p-4">
          <div className="absolute inset-0 bg-black/60 backdrop-blur-sm" onClick={() => setSelectedException(null)} />
          <div className="relative w-full max-w-2xl rounded-xl border border-border bg-card shadow-2xl animate-fade-in max-h-[90vh] overflow-y-auto">
            <div className="p-6">
              <div className="flex items-start justify-between mb-4">
                <div>
                  <h2 className="text-xl font-bold">{selectedException.displayName}</h2>
                  <p className="text-sm text-muted-foreground">{selectedException.departmentName}</p>
                </div>
                <button onClick={() => setSelectedException(null)} className="p-1 rounded-md hover:bg-muted">✕</button>
              </div>

              {/* Timeline */}
              <div className="mb-6">
                <h3 className="font-semibold mb-3">Investigation Timeline</h3>
                <StatusTimeline steps={getDefaultTimeline()} />
              </div>

              {/* Investigation form toggle */}
              {!investigationOpen ? (
                <button
                  onClick={() => setInvestigationOpen(true)}
                  className="w-full px-4 py-3 rounded-lg border-2 border-dashed border-primary/50 text-primary text-sm font-medium hover:bg-primary/5 transition-colors"
                >
                  Start Investigation
                </button>
              ) : (
                <InvestigationForm
                  exceptionId={selectedException.id}
                  onSubmit={async () => {}}
                  onCancel={() => setInvestigationOpen(false)}
                />
              )}
            </div>
          </div>
        </div>
      )}

      {/* Bulk modal */}
      <BulkInvestigateModal
        isOpen={showBulkModal}
        onClose={() => setShowBulkModal(false)}
        exceptions={allExceptions.filter(e => ['Open', 'UnderReview'].includes(e.status))}
        onBulkSubmit={async (_ids, _notes) => {}}
      />
    </div>
  )
}

