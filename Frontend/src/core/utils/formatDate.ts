import { format, parseISO, isToday, isYesterday, isThisWeek } from 'date-fns'

/**
 * Format a date string or Date object into a human-readable format.
 */
export function formatDate(date: string | Date | null | undefined, formatStr = 'PPP'): string {
  if (!date) return '—'
  const d = typeof date === 'string' ? parseISO(date) : date
  return format(d, formatStr)
}

/**
 * Format a datetime string into time-only format.
 */
export function formatTime(date: string | Date | null | undefined, formatStr = 'p'): string {
  if (!date) return '—'
  const d = typeof date === 'string' ? parseISO(date) : date
  return format(d, formatStr)
}

/**
 * Format relative time (e.g., "2 hours ago", "3 days ago").
 */
export function formatRelativeTime(date: string | Date): string {
  const d = typeof date === 'string' ? parseISO(date) : date
  const now = new Date()
  const diffMs = now.getTime() - d.getTime()
  const diffMins = Math.floor(diffMs / 60000)
  const diffHours = Math.floor(diffMins / 60)
  const diffDays = Math.floor(diffHours / 24)

  if (diffMins < 1) return 'Just now'
  if (diffMins < 60) return `${diffMins}m ago`
  if (diffHours < 24) return `${diffHours}h ago`
  if (diffDays < 7) return `${diffDays}d ago`
  return format(d, 'MMM dd')
}

/**
 * Format date for display in headers/cards.
 */
export function formatDisplayDate(date: string | Date | null | undefined): string {
  if (!date) return '—'
  const d = typeof date === 'string' ? parseISO(date) : date

  if (isToday(d)) return `Today, ${format(d, 'p')}`
  if (isYesterday(d)) return `Yesterday, ${format(d, 'p')}`
  if (isThisWeek(d)) return format(d, 'EEEE, p')
  return format(d, 'EEE MMM dd, yyyy • p')
}

/**
 * Get start of day as ISO string.
 */
export function getStartOfDay(date?: Date): string {
  const d = date || new Date()
  return format(d, 'yyyy-MM-dd') + 'T00:00:00'
}

/**
 * Get end of day as ISO string.
 */
export function getEndOfDay(date?: Date): string {
  const d = date || new Date()
  return format(d, 'yyyy-MM-dd') + 'T23:59:59'
}

/**
 * Format date range for display.
 */
export function formatDateRange(start: string | Date, end: string | Date): string {
  const s = typeof start === 'string' ? parseISO(start) : start
  const e = typeof end === 'string' ? parseISO(end) : end
  if (isSameMonth(s, e)) {
    return `${format(s, 'MMM dd')} – ${format(e, 'dd, yyyy')}`
  }
  return `${format(s, 'MMM dd, yyyy')} – ${format(e, 'MMM dd, yyyy')}`
}

function isSameMonth(a: Date, b: Date): boolean {
  return a.getMonth() === b.getMonth() && a.getFullYear() === b.getFullYear()
}
