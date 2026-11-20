import { describe, it, expect } from 'vitest'
import { formatDate, formatTime, formatRelativeTime, formatDisplayDate, getStartOfDay, getEndOfDay, formatDateRange } from '../formatDate'

describe('formatDate', () => {
  it('should return "—" for null', () => {
    expect(formatDate(null)).toBe('—')
  })

  it('should return "—" for undefined', () => {
    expect(formatDate(undefined)).toBe('—')
  })

  it('should format a date string with default format', () => {
    const result = formatDate('2026-11-13T10:30:00Z')
    expect(result).toBeDefined()
    expect(typeof result).toBe('string')
  })

  it('should format a Date object', () => {
    const date = new Date(2026, 10, 13)
    const result = formatDate(date)
    expect(result).toContain('Nov')
  })

  it('should respect custom format string', () => {
    const result = formatDate('2026-11-13T10:30:00Z', 'yyyy-MM-dd')
    expect(result).toBe('2026-11-13')
  })

  it('should handle ISO format correctly', () => {
    const result = formatDate('2026-11-13')
    expect(result).toContain('13')
  })
})

describe('formatTime', () => {
  it('should return "—" for null', () => {
    expect(formatTime(null)).toBe('—')
  })

  it('should return "—" for undefined', () => {
    expect(formatTime(undefined)).toBe('—')
  })

  it('should format time', () => {
    const date = new Date(2026, 10, 13, 14, 30)
    const result = formatTime(date)
    expect(result).toBeDefined()
  })

  it('should parse ISO string', () => {
    const result = formatTime('2026-11-13T14:30:00Z')
    expect(result).toBeDefined()
  })
})

describe('formatRelativeTime', () => {
  it('should return "Just now" for very recent dates', () => {
    const now = new Date()
    const result = formatRelativeTime(now)
    expect(result).toBe('Just now')
  })

  it('should show minutes ago', () => {
    const past = new Date(Date.now() - 5 * 60 * 1000)
    const result = formatRelativeTime(past)
    expect(result).toContain('m ago')
  })

  it('should show hours ago', () => {
    const past = new Date(Date.now() - 2 * 60 * 60 * 1000)
    const result = formatRelativeTime(past)
    expect(result).toContain('h ago')
  })

  it('should show days ago within a week', () => {
    const past = new Date(Date.now() - 3 * 24 * 60 * 60 * 1000)
    const result = formatRelativeTime(past)
    expect(result).toContain('d ago')
  })

  it('should fall back to MMM dd format for older dates', () => {
    const past = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000)
    const result = formatRelativeTime(past)
    expect(result).not.toContain('d ago')
  })

  it('should handle string input', () => {
    const past = new Date(Date.now() - 30 * 60 * 1000)
    const result = formatRelativeTime(past.toISOString())
    expect(result).toContain('m ago')
  })
})

describe('formatDisplayDate', () => {
  it('should return "—" for null', () => {
    expect(formatDisplayDate(null)).toBe('—')
  })

  it('should return "—" for undefined', () => {
    expect(formatDisplayDate(undefined)).toBe('—')
  })

  it('should show "Today" for today\'s date', () => {
    const today = new Date()
    const result = formatDisplayDate(today)
    expect(result).toContain('Today')
  })

  it('should show "Yesterday" for yesterday', () => {
    const yesterday = new Date(Date.now() - 24 * 60 * 60 * 1000)
    const result = formatDisplayDate(yesterday)
    expect(result).toContain('Yesterday')
  })

  it('should show day name for this week', () => {
    const thisWeek = new Date(Date.now() - 2 * 24 * 60 * 60 * 1000)
    const result = formatDisplayDate(thisWeek)
    expect(result).toBeDefined()
  })

  it('should show full date for older dates', () => {
    const oldDate = new Date(Date.now() - 20 * 24 * 60 * 60 * 1000)
    const result = formatDisplayDate(oldDate)
    expect(result).toContain(',')
  })
})

describe('getStartOfDay', () => {
  it('should return start of current day', () => {
    const result = getStartOfDay()
    expect(result).toMatch(/\dT00:00:00$/)
  })

  it('should accept a date parameter', () => {
    const date = new Date(2026, 10, 15)
    const result = getStartOfDay(date)
    expect(result).toBe('2026-11-15T00:00:00')
  })
})

describe('getEndOfDay', () => {
  it('should return end of current day', () => {
    const result = getEndOfDay()
    expect(result).toMatch(/\dT23:59:59$/)
  })

  it('should accept a date parameter', () => {
    const date = new Date(2026, 10, 15)
    const result = getEndOfDay(date)
    expect(result).toBe('2026-11-15T23:59:59')
  })
})

describe('formatDateRange', () => {
  it('should format same-month range', () => {
    const start = new Date(2026, 10, 1)
    const end = new Date(2026, 10, 15)
    const result = formatDateRange(start, end)
    expect(result).toContain('Nov')
  })

  it('should format cross-month range', () => {
    const start = new Date(2026, 10, 28)
    const end = new Date(2026, 11, 5)
    const result = formatDateRange(start, end)
    expect(result).toContain('Dec')
  })

  it('should accept string inputs', () => {
    const result = formatDateRange('2026-11-01', '2026-11-15')
    expect(result).toContain('Nov')
  })
})
