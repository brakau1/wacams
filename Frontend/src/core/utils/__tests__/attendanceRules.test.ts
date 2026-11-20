import { describe, it, expect } from 'vitest'
import type { AttendanceStatus } from '@/core/api/types'
import { SHIFTS, getCurrentShift, isWithinShift, type ShiftDefinition } from '@/shared/constants/shifts'

// ─── Attendance Status Determination Helpers ────────────────────────────────

/**
 * Determine attendance status based on clock-in/out times and shift info.
 * Mirrors backend AttendanceService logic.
 */
function getAttendanceStatus(
  scheduledShift: string,
  clockInTime: string | null,
  clockOutTime: string | null,
  lateThresholdMinutes: number = 5,
  earlyDepartureThresholdMinutes: number = 5,
  gracePeriodMinutes: number = 5
): AttendanceStatus {
  // No clock-in recorded
  if (!clockInTime) {
    return 'Absent'
  }

  const [clockInHour, clockInMinute] = clockInTime.split(':').map(Number)
  const clockInTotalMinutes = clockInHour * 60 + clockInMinute

  // Get shift start time
  const shiftDef = getShiftByCode(scheduledShift)
  if (!shiftDef) return 'Absent'

  const [startHour, startMinute] = shiftDef.startTime.split(':').map(Number)
  const shiftStartMinutes = startHour * 60 + startMinute

  // Calculate late minutes
  const lateMinutes = Math.max(0, clockInTotalMinutes - shiftStartMinutes - gracePeriodMinutes)

  // Late arrival?
  if (lateMinutes > lateThresholdMinutes) {
    return 'Late'
  }

  // Check early departure
  if (clockOutTime) {
    const [coHour, coMinute] = clockOutTime.split(':').map(Number)
    const clockOutTotalMinutes = coHour * 60 + coMinute

    const [endHour, endMinute] = shiftDef.endTime.split(':').map(Number)
    let shiftEndMinutes = endHour * 60 + endMinute

    // Handle overnight shifts
    if (shiftEndMinutes <= shiftStartMinutes) {
      shiftEndMinutes += 24 * 60
    }

    let actualMinutes = clockOutTotalMinutes - clockInTotalMinutes
    if (actualMinutes < 0) actualMinutes += 24 * 60

    const earlyMinutes = Math.max(0, shiftEndMinutes - clockOutTotalMinutes - earlyDepartureThresholdMinutes)

    if (earlyMinutes > 0 && actualMinutes < (shiftEndMinutes - shiftStartMinutes) * 0.9) {
      return 'EarlyDeparture'
    }
  }

  return 'Present'
}

function getShiftByCode(code: string): ShiftDefinition | undefined {
  for (const key of Object.keys(SHIFTS)) {
    if (SHIFTS[key].code === code) return SHIFTS[key]
  }
  return undefined
}

// ─── Grace Period Calculation ────────────────────────────────────────────────

function calculateGracePeriod(clockInTime: string, shiftStartTime: string, graceMinutes: number): {
  isGrace: boolean
  effectiveDeadline: string
  lateMinutes: number
} {
  const [inH, inM] = clockInTime.split(':').map(Number)
  const [sh, sm] = shiftStartTime.split(':').map(Number)

  const clockInMinutes = inH * 60 + inM
  const shiftStartMinutes = sh * 60 + sm
  const deadlineMinutes = shiftStartMinutes + graceMinutes

  const lateMinutes = Math.max(0, clockInMinutes - shiftStartMinutes)
  const isGrace = clockInMinutes <= deadlineMinutes

  const deadlineH = Math.floor(deadlineMinutes / 60) % 24
  const deadlineM = deadlineMinutes % 60

  return {
    isGrace,
    effectiveDeadline: `${String(deadlineH).padStart(2, '0')}:${String(deadlineM).padStart(2, '0')}`,
    lateMinutes,
  }
}

// ─── Overtime Calculation ────────────────────────────────────────────────────

function calculateOvertimeHours(
  clockInTime: string,
  clockOutTime: string,
  shiftDurationHours: number
): number {
  const [inH, inM] = clockInTime.split(':').map(Number)
  const [outH, outM] = clockOutTime.split(':').map(Number)

  const clockInMinutes = inH * 60 + inM
  const clockOutMinutes = outH * 60 + outM

  let workedMinutes = clockOutMinutes - clockInMinutes
  if (workedMinutes < 0) workedMinutes += 24 * 60

  const scheduledMinutes = shiftDurationHours * 60
  const overtimeMinutes = Math.max(0, workedMinutes - scheduledMinutes)

  return Math.round((overtimeMinutes / 60) * 10) / 10
}

// ─── Test Cases ─────────────────────────────────────────────────────────────

describe('getAttendanceStatus', () => {
  it('should return "Present" when clocked in on time', () => {
    const status = getAttendanceStatus('ES', '05:55', '14:10')
    expect(status).toBe('Present')
  })

  it('should return "Late" when clocked in past threshold', () => {
    const status = getAttendanceStatus('ES', '06:35', '14:05')
    expect(status).toBe('Late')
  })

  it('should return "Absent" when no clock-in', () => {
    const status = getAttendanceStatus('ES', null, null)
    expect(status).toBe('Absent')
  })

  it('should handle ES (Morning) shift correctly', () => {
    expect(getAttendanceStatus('ES', '06:00', '14:00')).toBe('Present')
    expect(getAttendanceStatus('ES', '06:10', '14:00')).toBe('Late')
    expect(getAttendanceStatus('ES', null, '14:00')).toBe('Absent')
  })

  it('should handle MS (Afternoon) shift correctly', () => {
    expect(getAttendanceStatus('MS', '14:00', '22:00')).toBe('Present')
    expect(getAttendanceStatus('MS', '14:30', '22:00')).toBe('Late')
  })

  it('should handle NS (Night) shift correctly', () => {
    expect(getAttendanceStatus('NS', '22:00', '06:00')).toBe('Present')
    expect(getAttendanceStatus('NS', '22:30', '06:00')).toBe('Late')
  })

  it('should detect early departure', () => {
    const status = getAttendanceStatus('ES', '05:50', '13:15')
    expect(status).toBe('EarlyDeparture')
  })

  it('should return Present when within grace period', () => {
    const status = getAttendanceStatus('ES', '06:04', '14:10', 5, 5, 5)
    expect(status).toBe('Present')
  })
})

describe('calculateGracePeriod', () => {
  it('should return isGrace true when within grace period', () => {
    const result = calculateGracePeriod('06:03', '06:00', 5)
    expect(result.isGrace).toBe(true)
    expect(result.effectiveDeadline).toBe('06:05')
  })

  it('should return isGrace false when past grace period', () => {
    const result = calculateGracePeriod('06:15', '06:00', 5)
    expect(result.isGrace).toBe(false)
    expect(result.lateMinutes).toBe(15)
  })

  it('should handle exact boundary', () => {
    const result = calculateGracePeriod('06:05', '06:00', 5)
    expect(result.isGrace).toBe(true)
    expect(result.lateMinutes).toBe(5)
  })

  it('should handle different shift codes', () => {
    const msResult = calculateGracePeriod('14:04', '14:00', 5)
    expect(msResult.isGrace).toBe(true)

    const nsResult = calculateGracePeriod('22:03', '22:00', 5)
    expect(nsResult.isGrace).toBe(true)
  })

  it('should calculate correct late minutes', () => {
    const result = calculateGracePeriod('07:00', '06:00', 5)
    expect(result.lateMinutes).toBe(60)
  })
})

describe('calculateOvertimeHours', () => {
  it('should return 0 when no overtime', () => {
    const ot = calculateOvertimeHours('06:00', '14:00', 8)
    expect(ot).toBe(0)
  })

  it('should calculate overtime correctly', () => {
    const ot = calculateOvertimeHours('06:00', '15:00', 8)
    expect(ot).toBe(1)
  })

  it('should handle partial hour overtime', () => {
    const ot = calculateOvertimeHours('06:00', '14:30', 8)
    expect(ot).toBe(0.5)
  })

  it('should handle significant overtime', () => {
    const ot = calculateOvertimeHours('06:00', '16:00', 8)
    expect(ot).toBe(2)
  })

  it('should handle negative overtime (worked less)', () => {
    const ot = calculateOvertimeHours('06:00', '13:00', 8)
    expect(ot).toBe(0)
  })

  it('should handle overnight shift overtime', () => {
    // Clock in at 22:00, out at 07:00 = 9 hours
    const ot = calculateOvertimeHours('22:00', '07:00', 8)
    expect(ot).toBe(1)
  })
})

describe('Attendance Status Edge Cases', () => {
  it('should handle midnight crossing for night shift clock-in', () => {
    const status = getAttendanceStatus('NS', '21:55', '06:10')
    expect(status).toBe('Present')
  })

  it('should handle very late clock-in', () => {
    const status = getAttendanceStatus('ES', '08:00', '14:00')
    expect(status).toBe('Late')
  })

  it('should handle zero threshold grace', () => {
    const status = getAttendanceStatus('ES', '06:01', '14:00', 5, 5, 0)
    expect(status).toBe('Late')
  })

  it('should handle large grace period', () => {
    const status = getAttendanceStatus('ES', '06:30', '14:00', 5, 5, 30)
    expect(status).toBe('Present')
  })

  it('should handle unknown shift code', () => {
    const status = getAttendanceStatus('UNKNOWN', '06:00', '14:00')
    expect(status).toBe('Absent')
  })
})

describe('Integration with shift constants', () => {
  it('should use shift definitions for status calculation', () => {
    // Morning shift starts at 06:00
    expect(getAttendanceStatus('ES', '06:00', '14:00')).toBe('Present')
    // Afternoon shift starts at 14:00
    expect(getAttendanceStatus('MS', '14:00', '22:00')).toBe('Present')
    // Night shift starts at 22:00
    expect(getAttendanceStatus('NS', '22:00', '06:00')).toBe('Present')
  })

  it('should work with all defined shift codes', () => {
    const shiftCodes = ['ES', 'MS', 'NS', 'LDS', 'DS', 'FDS']
    shiftCodes.forEach(code => {
      const status = getAttendanceStatus(code, '06:00', '14:00')
      expect(typeof status).toBe('string')
    })
  })
})
