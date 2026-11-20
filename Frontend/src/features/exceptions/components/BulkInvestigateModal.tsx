import { useState } from 'react'
import Modal from '@/shared/components/Modal'
import StatusChip from '../../attendance/components/StatusChip'
import { cn } from '@/core/utils/cn'
import { CheckSquare, Square, Loader2 } from 'lucide-react'
import type { AttendanceException } from '@/core/api/types'

interface BulkInvestigateModalProps {
  isOpen: boolean
  onClose: () => void
  exceptions: AttendanceException[]
  onBulkSubmit: (ids: string[], notes: string) => Promise<void>
}

export default function BulkInvestigateModal({ isOpen, onClose, exceptions, onBulkSubmit }: BulkInvestigateModalProps) {
  const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set())
  const [notes, setNotes] = useState('')
  const [submitting, setSubmitting] = useState(false)

  const toggleSelect = (id: string) => {
    setSelectedIds(prev => {
      const next = new Set(prev)
      if (next.has(id)) next.delete(id)
      else next.add(id)
      return next
    })
  }

  const selectAll = () => {
    if (selectedIds.size === exceptions.length) {
      setSelectedIds(new Set())
    } else {
      setSelectedIds(new Set(exceptions.map(e => e.id)))
    }
  }

  const handleSubmit = async () => {
    if (selectedIds.size === 0) return
    setSubmitting(true)
    try {
      await onBulkSubmit(Array.from(selectedIds), notes)
      onClose()
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Bulk Investigate Exceptions"
      description={`Select up to ${exceptions.length} exceptions to investigate at once`}
      size="lg"
      footer={
        <>
          <button onClick={onClose} className="px-4 py-2 rounded-lg border border-border text-sm font-medium hover:bg-muted">
            Cancel
          </button>
          <button
            onClick={handleSubmit}
            disabled={selectedIds.size === 0 || submitting}
            className="px-4 py-2 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:opacity-90 disabled:opacity-50 flex items-center gap-2"
          >
            {submitting && <Loader2 className="w-4 h-4 animate-spin" />}
            Investigate ({selectedIds.size})
          </button>
        </>
      }
    >
      {/* Select all row */}
      <div className="flex items-center justify-between mb-3 pb-3 border-b border-border">
        <button
          onClick={selectAll}
          className="text-xs text-primary hover:underline"
        >
          {selectedIds.size === exceptions.length ? 'Deselect All' : 'Select All'}
        </button>
        <span className="text-xs text-muted-foreground">{selectedIds.size} selected of {exceptions.length}</span>
      </div>

      {/* Exception list */}
      <div className="space-y-2 max-h-[400px] overflow-y-auto">
        {exceptions.map(exc => (
          <button
            key={exc.id}
            onClick={() => toggleSelect(exc.id)}
            className={cn(
              'w-full flex items-center gap-3 p-3 rounded-lg border transition-colors text-left',
              selectedIds.has(exc.id) ? 'bg-primary/5 border-primary/50' : 'border-border hover:bg-muted/50'
            )}
          >
            {selectedIds.has(exc.id) ? (
              <CheckSquare className="w-5 h-5 text-primary flex-shrink-0" />
            ) : (
              <Square className="w-5 h-5 text-muted-foreground flex-shrink-0" />
            )}
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium truncate">{exc.displayName}</p>
              <p className="text-xs text-muted-foreground truncate">{exc.description}</p>
            </div>
            <StatusChip status={exc.status} size="sm" />
          </button>
        ))}
      </div>

      {/* Notes field */}
      <div className="mt-4 pt-4 border-t border-border">
        <label className="text-sm font-medium mb-2 block">Investigation Notes (applies to all selected)</label>
        <textarea
          value={notes}
          onChange={(e) => setNotes(e.target.value)}
          rows={3}
          placeholder="Enter investigation notes..."
          className="w-full px-3 py-2 rounded-lg border border-border bg-background text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary/50 resize-none text-sm"
        />
      </div>
    </Modal>
  )
}
