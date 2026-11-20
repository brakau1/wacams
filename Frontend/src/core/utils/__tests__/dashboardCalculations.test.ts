import { describe, it, expect } from 'vitest'
import type { DepartmentKPI, TrendPoint, TopLateEntry, AttendanceSummary, AttendanceRecord } from '@/core/api/types'
import { formatMinutes, calculateAttendancePercentage } from '../formatMinutes'

// ─── Dashboard Calculation Helpers ──────────────────────────────────────────

function calculateAttendanceRate(records: AttendanceRecord[]): number {
  const total = records.length
  if (total === 0) return 0
  const present = records.filter(r => r.status === 'Present').length
  return Math.round((present / total) * 100)
}

function calculateAbsenteeismRate(records: AttendanceRecord[]): number {
  const total = records.length
  if (total === 0) return 0
  const absent = records.filter(r => r.status === 'Absent' || r.status === 'AWOL').length
  return Math.round((absent / total) * 100)
}

function calculateLateRate(records: AttendanceRecord[]): number {
  const total = records.length
  if (total === 0) return 0
  const late = records.filter(r => r.status === 'Late').length
  return Math.round((late / total) * 100)
}

function calculateOvertimeHours(records: AttendanceRecord[]): number {
  // Simplified: based on totalHours vs expected 8h shift
  const expectedHours = 8
  const overtimeRecords = records.filter(r => r.totalHours > expectedHours)
  const totalOT = overtimeRecords.reduce((sum, r) => sum + (r.totalHours - expectedHours), 0)
  return Math.round(totalOT * 10) / 10
}

function calculateAverageWorkedHours(records: AttendanceRecord[]): number {
  if (records.length === 0) return 0
  const total = records.reduce((sum, r) => sum + r.totalHours, 0)
  return Math.round((total / records.length) * 10) / 10
}

function calculateHeadcount(departments: DepartmentKPI[]): number {
  return departments.reduce((sum, d) => sum + d.totalEmployees, 0)
}

function calculateDepartmentStats(
  departments: DepartmentKPI[],
  targetPercentage: number = 95
): { atRisk: DepartmentKPI[]; healthy: DepartmentKPI[]; avgAttendance: number } {
  const atRisk = departments.filter(d => d.attendancePercentage < targetPercentage)
  const healthy = departments.filter(d => d.attendancePercentage >= targetPercentage)
  const avgAttendance = departments.length > 0
    ? Math.round(departments.reduce((sum, d) => sum + d.attendancePercentage, 0) / departments.length)
    : 0

  return { atRisk, healthy, avgAttendance }
}

function calculateShiftStats(records: AttendanceRecord[]): Record<string, { count: number; lateCount: number }> {
  const stats: Record<string, { count: number; lateCount: number }> = {}

  for (const record of records) {
    const shift = record.scheduledShift || 'Unknown'
    if (!stats[shift]) {
      stats[shift] = { count: 0, lateCount: 0 }
    }
    stats[shift].count++
    if (record.status === 'Late') {
      stats[shift].lateCount++
    }
  }

  return stats
}

function calculateTrendData(points: TrendPoint[]): { direction: 'up' | 'down' | 'stable'; change: number } {
  if (points.length < 2) return { direction: 'stable', change: 0 }

  const recentAvg = points.slice(-3).reduce((sum, p) => sum + p.onTimePercentage, 0) / 3
  const previousAvg = points.slice(-6, -3).reduce((sum, p) => sum + p.onTimePercentage, 0) / 3

  const change = Math.round((recentAvg - previousAvg) * 10) / 10

  if (change > 1) return { direction: 'up', change }
  if (change < -1) return { direction: 'down', change }
  return { direction: 'stable', change }
}

function formatPercentage(value: number): string {
  return `${Math.round(value)}%`
}

function formatHours(decimalHours: number): string {
  const hours = Math.floor(decimalHours)
  const minutes = Math.round((decimalHours - hours) * 60)
  if (hours === 0) return `${minutes}m`
  if (minutes === 0) return `${hours}h`
  return `${hours}h ${minutes}m`
}

// ─── Test Cases ─────────────────────────────────────────────────────────────

describe('calculateAttendanceRate', () => {
  it('should return 0 for empty array', () => {
    expect(calculateAttendanceRate([])).toBe(0)
  })

  it('should return 100 when all present', () => {
    const records: AttendanceRecord[] = [
      { id: '1', employeeId: '1', employeeName: 'A', displayName: 'A', departmentId: '1', departmentName: 'Production', date: '2026-11-13', scheduledShift: 'ES', shiftStartTime: '06:00', shiftEndTime: '14:00', clockInTime: '05:55', clockOutTime: '14:10', status: 'Present', lateMinutes: 0, earlyMinutes: 0, totalHours: 8.25, notes: null, correctedBy: null, correctionReason: null },
    ]
    expect(calculateAttendanceRate(records)).toBe(100)
  })

  it('should return correct percentage with mixed statuses', () => {
    const records: AttendanceRecord[] = [
      { ...createRecord({ status: 'Present' }), id: '1' },
      { ...createRecord({ status: 'Present' }), id: '2' },
      { ...createRecord({ status: 'Late' }), id: '3' },
      { ...createRecord({ status: 'Absent' }), id: '4' },
      { ...createRecord({ status: 'Present' }), id: '5' },
    ]
    const rate = calculateAttendanceRate(records)
    expect(rate).toBe(60) // 3/5 = 60%
  })

  it('should handle AWOL as non-present', () => {
    const records: AttendanceRecord[] = [
      createRecord({ status: 'AWOL' }),
      createRecord({ status: 'Present' }),
    ]
    expect(calculateAttendanceRate(records)).toBe(50)
  })
})

describe('calculateAbsenteeismRate', () => {
  it('should return 0 for empty array', () => {
    expect(calculateAbsenteeismRate([])).toBe(0)
  })

  it('should count Absent and AWOL', () => {
    const records: AttendanceRecord[] = [
      createRecord({ status: 'Absent' }),
      createRecord({ status: 'AWOL' }),
      createRecord({ status: 'Present' }),
      createRecord({ status: 'Present' }),
    ]
    expect(calculateAbsenteeismRate(records)).toBe(50) // 2/4 = 50%
  })

  it('should not count Late as absenteeism', () => {
    const records: AttendanceRecord[] = [
      createRecord({ status: 'Late' }),
      createRecord({ status: 'Late' }),
      createRecord({ status: 'Present' }),
    ]
    expect(calculateAbsenteeismRate(records)).toBe(0)
  })

  it('should handle 100% absenteeism', () => {
    const records: AttendanceRecord[] = [
      createRecord({ status: 'AWOL' }),
      createRecord({ status: 'Absent' }),
    ]
    expect(calculateAbsenteeismRate(records)).toBe(100)
  })
})

describe('calculateLateRate', () => {
  it('should return 0 for empty array', () => {
    expect(calculateLateRate([])).toBe(0)
  })

  it('should only count Late status', () => {
    const records: AttendanceRecord[] = [
      createRecord({ status: 'Late' }),
      createRecord({ status: 'Present' }),
      createRecord({ status: 'EarlyDeparture' }),
    ]
    expect(calculateLateRate(records)).toBe(33) // 1/3 ~ 33%
  })

  it('should handle no late arrivals', () => {
    const records: AttendanceRecord[] = Array.from({ length: 10 }, (_, i) =>
      createRecord({ status: 'Present' })
    )
    expect(calculateLateRate(records)).toBe(0)
  })

  it('should handle all late', () => {
    const records: AttendanceRecord[] = Array.from({ length: 5 }, (_, i) =>
      createRecord({ status: 'Late' })
    )
    expect(calculateLateRate(records)).toBe(100)
  })
})

describe('calculateOvertimeHours', () => {
  it('should return 0 for no overtime', () => {
    const records: AttendanceRecord[] = [
      createRecord({ totalHours: 8.0 }),
      createRecord({ totalHours: 7.5 }),
    ]
    expect(calculateOvertimeHours(records)).toBe(0)
  })

  it('should sum overtime across employees', () => {
    const records: AttendanceRecord[] = [
      createRecord({ totalHours: 9.0 }),   // +1h OT
      createRecord({ totalHours: 8.5 }),   // +0.5h OT
      createRecord({ totalHours: 8.0 }),   // 0h OT
    ]
    expect(calculateOvertimeHours(records)).toBe(1.5)
  })

  it('should ignore records under scheduled hours', () => {
    const records: AttendanceRecord[] = [
      createRecord({ totalHours: 7.0 }),
      createRecord({ totalHours: 6.5 }),
    ]
    expect(calculateOvertimeHours(records)).toBe(0)
  })
})

describe('calculateAverageWorkedHours', () => {
  it('should return 0 for empty array', () => {
    expect(calculateAverageWorkedHours([])).toBe(0)
  })

  it('should calculate average correctly', () => {
    const records: AttendanceRecord[] = [
      createRecord({ totalHours: 8.0 }),
      createRecord({ totalHours: 9.0 }),
      createRecord({ totalHours: 7.5 }),
    ]
    expect(calculateAverageWorkedHours(records)).toBe(8.2) // (8+9+7.5)/3 = 8.167 -> 8.2
  })

  it('should handle single record', () => {
    const records: AttendanceRecord[] = [createRecord({ totalHours: 8.5 })]
    expect(calculateAverageWorkedHours(records)).toBe(8.5)
  })

  it('should round to one decimal', () => {
    const records: AttendanceRecord[] = [
      createRecord({ totalHours: 8.333 }),
      createRecord({ totalHours: 7.667 }),
    ]
    expect(calculateAverageWorkedHours(records)).toBe(8.0)
  })
})

describe('calculateHeadcount', () => {
  it('should return 0 for empty array', () => {
    expect(calculateHeadcount([])).toBe(0)
  })

  it('should sum all department employees', () => {
    const departments: DepartmentKPI[] = [
      { departmentId: '1', departmentName: 'Production', attendancePercentage: 94, totalEmployees: 126, presentCount: 118, lateCount: 5, absentCount: 3 },
      { departmentId: '2', departmentName: 'Quality', attendancePercentage: 91, totalEmployees: 24, presentCount: 22, lateCount: 1, absentCount: 1 },
      { departmentId: '3', departmentName: 'Engineering', attendancePercentage: 88, totalEmployees: 18, presentCount: 16, lateCount: 1, absentCount: 1 },
    ]
    expect(calculateHeadcount(departments)).toBe(168) // 126 + 24 + 18
  })

  it('should handle large numbers', () => {
    const departments: DepartmentKPI[] = [
      { departmentId: '1', departmentName: 'Large Dept', attendancePercentage: 90, totalEmployees: 500, presentCount: 450, lateCount: 30, absentCount: 20 },
    ]
    expect(calculateHeadcount(departments)).toBe(500)
  })
})

describe('calculateDepartmentStats', () => {
  it('should classify departments by target', () => {
    const departments: DepartmentKPI[] = [
      { departmentId: '1', departmentName: 'Above Target', attendancePercentage: 96, totalEmployees: 10, presentCount: 9, lateCount: 0, absentCount: 1 },
      { departmentId: '2', departmentName: 'Below Target', attendancePercentage: 90, totalEmployees: 10, presentCount: 9, lateCount: 0, absentCount: 1 },
      { departmentId: '3', departmentName: 'At Target', attendancePercentage: 95, totalEmployees: 10, presentCount: 9, lateCount: 0, absentCount: 1 },
    ]

    const stats = calculateDepartmentStats(departments, 95)

    expect(stats.atRisk).toHaveLength(1)
    expect(stats.atRisk[0]?.departmentName).toBe('Below Target')
    expect(stats.healthy).toHaveLength(2)
    expect(stats.avgAttendance).toBe(94)
  })

  it('should return all at risk when below target', () => {
    const departments: DepartmentKPI[] = [
      { departmentId: '1', departmentName: 'Bad', attendancePercentage: 80, totalEmployees: 10, presentCount: 8, lateCount: 1, absentCount: 1 },
    ]
    const stats = calculateDepartmentStats(departments, 95)
    expect(stats.atRisk).toHaveLength(1)
    expect(stats.healthy).toHaveLength(0)
  })

  it('should handle empty departments', () => {
    const stats = calculateDepartmentStats([], 95)
    expect(stats.atRisk).toHaveLength(0)
    expect(stats.healthy).toHaveLength(0)
    expect(stats.avgAttendance).toBe(0)
  })

  it('should use custom target threshold', () => {
    const departments: DepartmentKPI[] = [
      { departmentId: '1', departmentName: 'Dept', attendancePercentage: 92, totalEmployees: 10, presentCount: 9, lateCount: 0, absentCount: 1 },
    ]
    const stats = calculateDepartmentStats(departments, 90)
    expect(stats.healthy).toHaveLength(1)
    expect(stats.atRisk).toHaveLength(0)
  })
})

describe('calculateShiftStats', () => {
  it('should group records by shift', () => {
    const records: AttendanceRecord[] = [
      createRecord({ scheduledShift: 'ES', status: 'Present' }),
      createRecord({ scheduledShift: 'ES', status: 'Late' }),
      createRecord({ scheduledShift: 'MS', status: 'Present' }),
    ]

    const stats = calculateShiftStats(records)

    expect(stats['ES']?.count).toBe(2)
    expect(stats['ES']?.lateCount).toBe(1)
    expect(stats['MS']?.count).toBe(1)
    expect(stats['MS']?.lateCount).toBe(0)
  })

  it('should handle unknown shifts', () => {
    const records: AttendanceRecord[] = [createRecord({ scheduledShift: '' })]
    const stats = calculateShiftStats(records)
    expect(stats['Unknown']?.count).toBe(1)
  })

  it('should return empty for no records', () => {
    const stats = calculateShiftStats([])
    expect(Object.keys(stats)).toHaveLength(0)
  })
})

describe('calculateTrendData', () => {
  it('should detect upward trend', () => {
    const points: TrendPoint[] = [
      { date: 'Jan 1', onTimePercentage: 80, lateCount: 10, exceptionCount: 5 },
      { date: 'Jan 2', onTimePercentage: 82, lateCount: 8, exceptionCount: 4 },
      { date: 'Jan 3', onTimePercentage: 85, lateCount: 6, exceptionCount: 3 },
      { date: 'Jan 4', onTimePercentage: 88, lateCount: 4, exceptionCount: 2 },
      { date: 'Jan 5', onTimePercentage: 90, lateCount: 3, exceptionCount: 1 },
      { date: 'Jan 6', onTimePercentage: 92, lateCount: 2, exceptionCount: 1 },
    ]

    const trend = calculateTrendData(points)
    expect(trend.direction).toBe('up')
    expect(trend.change).toBeGreaterThan(0)
  })

  it('should detect downward trend', () => {
    const points: TrendPoint[] = [
      { date: 'Jan 1', onTimePercentage: 92, lateCount: 2, exceptionCount: 1 },
      { date: 'Jan 2', onTimePercentage: 90, lateCount: 4, exceptionCount: 2 },
      { date: 'Jan 3', onTimePercentage: 88, lateCount: 6, exceptionCount: 3 },
      { date: 'Jan 4', onTimePercentage: 85, lateCount: 8, exceptionCount: 4 },
      { date: 'Jan 5', onTimePercentage: 82, lateCount: 10, exceptionCount: 5 },
      { date: 'Jan 6', onTimePercentage: 80, lateCount: 12, exceptionCount: 6 },
    ]

    const trend = calculateTrendData(points)
    expect(trend.direction).toBe('down')
    expect(trend.change).toBeLessThan(0)
  })

  it('should detect stable trend', () => {
    const points: TrendPoint[] = Array.from({ length: 6 }, () => ({
      date: new Date().toISOString(),
      onTimePercentage: 88 + Math.random() * 1,
      lateCount: 5,
      exceptionCount: 3,
    }))

    const trend = calculateTrendData(points)
    expect(trend.direction).toBe('stable')
  })

  it('should return stable for less than 2 points', () => {
    const trend = calculateTrendData([{ date: 'Jan 1', onTimePercentage: 90, lateCount: 5, exceptionCount: 3 }])
    expect(trend.direction).toBe('stable')
    expect(trend.change).toBe(0)
  })
})

describe('format helpers', () => {
  it('should format percentage correctly', () => {
    expect(formatPercentage(95.5)).toBe('96%')
    expect(formatPercentage(80)).toBe('80%')
    expect(formatPercentage(0)).toBe('0%')
  })

  it('should format decimal hours', () => {
    expect(formatHours(1.5)).toBe('1h 30m')
    expect(formatHours(2.0)).toBe('2h')
    expect(formatHours(0.5)).toBe('30m')
    expect(formatHours(0)).toBe('0m')
  })

  it('should integrate formatMinutes helper', () => {
    expect(formatMinutes(120)).toBe('2h')
    expect(formatMinutes(90)).toBe('1h 30m')
    expect(formatMinutes(45)).toBe('45m')
  })

  it('should integrate calculateAttendancePercentage helper', () => {
    expect(calculateAttendancePercentage(95, 100)).toBe(95)
    expect(calculateAttendancePercentage(0, 0)).toBe(0)
  })
})

// ─── Helper Functions ───────────────────────────────────────────────────────

function createRecord(overrides: Partial<AttendanceRecord> = {}): AttendanceRecord {
  return {
    id: `rec-${Date.now()}-${Math.random()}`,
    employeeId: '708001',
    employeeName: 'Test User',
    displayName: 'Test User',
    departmentId: '1',
    departmentName: 'Production',
    date: '2026-11-13',
    scheduledShift: 'ES',
    shiftStartTime: '06:00',
    shiftEndTime: '14:00',
    clockInTime: '05:55',
    clockOutTime: '14:10',
    status: 'Present',
    lateMinutes: 0,
    earlyMinutes: 0,
    totalHours: 8.25,
    notes: null,
    correctedBy: null,
    correctionReason: null,
    ...overrides,
  }
}
