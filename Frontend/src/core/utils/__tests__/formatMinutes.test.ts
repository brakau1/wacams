import { describe, it, expect } from 'vitest'
import { formatMinutes, formatDecimalHours, calculateLostMinutes, calculateAttendancePercentage, calculateEmployeeDailyAttendance, formatPercent } from '../formatMinutes'

describe('formatMinutes', () => {
  it('should return "0m" for null', () => {
    expect(formatMinutes(null)).toBe('0m')
  })

  it('should return "0m" for undefined', () => {
    expect(formatMinutes(undefined)).toBe('0m')
  })

  it('should return "0m" for negative values', () => {
    expect(formatMinutes(-5)).toBe('0m')
  })

  it('should return minutes only when less than an hour', () => {
    expect(formatMinutes(30)).toBe('30m')
  })

  it('should return minutes only for 0', () => {
    expect(formatMinutes(0)).toBe('0m')
  })

  it('should return hours only when exact hours', () => {
    expect(formatMinutes(60)).toBe('1h')
  })

  it('should return hours and minutes', () => {
    expect(formatMinutes(90)).toBe('1h 30m')
  })

  it('should handle large values', () => {
    expect(formatMinutes(150)).toBe('2h 30m')
  })

  it('should round remaining minutes', () => {
    expect(formatMinutes(65.7)).toBe('1h 6m')
  })
})

describe('formatDecimalHours', () => {
  it('should return "0.0" for null', () => {
    expect(formatDecimalHours(null)).toBe('0.0')
  })

  it('should return "0.0" for undefined', () => {
    expect(formatDecimalHours(undefined)).toBe('0.0')
  })

  it('should return "0.0" for negative values', () => {
    expect(formatDecimalHours(-10)).toBe('0.0')
  })

  it('should format to one decimal place', () => {
    expect(formatDecimalHours(45)).toBe('0.8')
  })

  it('should format exact hours', () => {
    expect(formatDecimalHours(120)).toBe('2.0')
  })

  it('should handle partial hours', () => {
    expect(formatDecimalHours(180)).toBe('3.0')
  })

  it('should handle non-round division', () => {
    expect(formatDecimalHours(45)).toBe('0.8')
  })
})

describe('calculateLostMinutes', () => {
  it('should sum late and early minutes', () => {
    expect(calculateLostMinutes(15, 10)).toBe(25)
  })

  it('should treat null as 0', () => {
    expect(calculateLostMinutes(null as any, 10)).toBe(10)
  })

  it('should treat undefined as 0', () => {
    expect(calculateLostMinutes(undefined as any, 10)).toBe(10)
  })

  it('should handle both null', () => {
    expect(calculateLostMinutes(null as any, undefined as any)).toBe(0)
  })

  it('should handle zero values', () => {
    expect(calculateLostMinutes(0, 0)).toBe(0)
  })

  it('should handle negative as 0', () => {
    expect(calculateLostMinutes(-5, 10)).toBe(5)
  })
})

describe('calculateAttendancePercentage', () => {
  it('should return correct percentage', () => {
    expect(calculateAttendancePercentage(90, 100)).toBe(90)
  })

  it('should return 0 when total is 0', () => {
    expect(calculateAttendancePercentage(0, 0)).toBe(0)
  })

  it('should handle partial attendance', () => {
    expect(calculateAttendancePercentage(85, 100)).toBe(85)
  })

  it('should round down', () => {
    expect(calculateAttendancePercentage(95, 100)).toBe(95)
  })

  it('should handle small numbers', () => {
    expect(calculateAttendancePercentage(1, 3)).toBe(33)
  })
})

describe('calculateEmployeeDailyAttendance', () => {
  it('should return correct percentage', () => {
    expect(calculateEmployeeDailyAttendance(8, 8)).toBe(100)
  })

  it('should return 0 when scheduled hours is 0', () => {
    expect(calculateEmployeeDailyAttendance(0, 5)).toBe(0)
  })

  it('should cap at 100%', () => {
    expect(calculateEmployeeDailyAttendance(8, 10)).toBe(100)
  })

  it('should handle partial attendance', () => {
    expect(calculateEmployeeDailyAttendance(8, 6)).toBe(75)
  })

  it('should handle exact calculation', () => {
    expect(calculateEmployeeDailyAttendance(8, 4)).toBe(50)
  })
})

describe('formatPercent', () => {
  it('should return formatted percentage', () => {
    expect(formatPercent(95.5)).toBe('96%')
  })

  it('should return "—" for null', () => {
    expect(formatPercent(null)).toBe('—')
  })

  it('should return "—" for undefined', () => {
    expect(formatPercent(undefined)).toBe('—')
  })

  it('should round to nearest integer', () => {
    expect(formatPercent(85.4)).toBe('85%')
  })

  it('should round up', () => {
    expect(formatPercent(85.6)).toBe('86%')
  })

  it('should handle zero', () => {
    expect(formatPercent(0)).toBe('0%')
  })
})
