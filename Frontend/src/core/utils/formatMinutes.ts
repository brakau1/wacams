/**
 * Convert total minutes to hours and minutes string.
 */
export function formatMinutes(totalMinutes: number | null | undefined): string {
  if (totalMinutes === null || totalMinutes === undefined || totalMinutes < 0) return '0m'
  const hours = Math.floor(totalMinutes / 60)
  const minutes = Math.round(totalMinutes % 60)
  if (hours === 0) return `${minutes}m`
  if (minutes === 0) return `${hours}h`
  return `${hours}h ${minutes}m`
}

/**
 * Convert total minutes to a decimal hour string.
 */
export function formatDecimalHours(totalMinutes: number | null | undefined): string {
  if (totalMinutes === null || totalMinutes === undefined || totalMinutes < 0) return '0.0'
  const hours = totalMinutes / 60
  return hours.toFixed(1)
}

/**
 * Calculate lost production minutes from attendance data.
 */
export function calculateLostMinutes(lateMinutes: number, earlyMinutes: number): number {
  return (lateMinutes || 0) + (earlyMinutes || 0)
}

/**
 * Calculate department attendance percentage.
 */
export function calculateAttendancePercentage(
  present: number,
  total: number
): number {
  if (total === 0) return 0
  return Math.round((present / total) * 100)
}

/**
 * Calculate employee daily attendance percentage based on clock-in/out times.
 */
export function calculateEmployeeDailyAttendance(
  scheduledHours: number,
  actualHours: number
): number {
  if (scheduledHours === 0) return 0
  const pct = Math.min((actualHours / scheduledHours) * 100, 100)
  return Math.round(pct)
}

/**
 * Format a percentage value with symbol.
 */
export function formatPercent(value: number | null | undefined): string {
  if (value === null || value === undefined) return '—'
  return `${Math.round(value)}%`
}
