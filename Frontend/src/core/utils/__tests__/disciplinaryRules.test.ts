import { describe, it, expect } from 'vitest'
import type { WarningType, WarningLetter } from '@/core/api/types'

// ─── Disciplinary Rules Helpers ─────────────────────────────────────────────

const WARNING_SEVERITY_MAP: Record<WarningType, number> = {
  Counselling: 1,
  VerbalWarning: 2,
  WrittenWarning: 3,
  FinalWrittenWarning: 4,
}

/**
 * Map exception type to warning severity level.
 */
function getViolationSeverity(exceptionType: string): number {
  const severityMap: Record<string, number> = {
    LateArrival: 1,
    EarlyDeparture: 1,
    MissingClockIn: 2,
    MissingClockOut: 1,
    UnscheduledClockIn: 1,
    UnscheduledClockOut: 1,
    AWOL: 3,
    UnplannedAbsenteeism: 3,
    AttendanceException: 2,
    SickLeave: 0,
    FamilyResponsibilityLeave: 0,
    ShiftCancellation: 0,
    WorkshopAttendance: 0,
    Training: 0,
  }
  return severityMap[exceptionType] ?? 0
}

/**
 * Calculate warning expiry based on progressive discipline ladder.
 * - Counselling: 90 days
 * - Verbal Warning: 120 days
 * - Written Warning: 180 days
 * - Final Written Warning: 365 days (permanent)
 */
function calculateWarningExpiry(issuedAt: string, warningType: WarningType): string {
  const issuedDate = new Date(issuedAt)
  const daysMap: Record<WarningType, number> = {
    Counselling: 90,
    VerbalWarning: 120,
    WrittenWarning: 180,
    FinalWrittenWarning: 365,
  }
  const validityDays = daysMap[warningType] ?? 90
  const expiry = new Date(issuedDate.getTime() + validityDays * 24 * 60 * 60 * 1000)
  return expiry.toISOString()
}

function isWarningExpired(expiryDate: string, currentDate?: string): boolean {
  const now = currentDate ? new Date(currentDate) : new Date()
  return new Date(expiryDate) < now
}

function getWarningStatus(issuedAt: string, warningType: WarningType, status: string, currentDate?: string): string {
  if (status === 'Archived') return 'Archived'
  if (status === 'Draft') return 'Draft'

  const expiryDate = calculateWarningExpiry(issuedAt, warningType)
  if (isWarningExpired(expiryDate, currentDate)) return 'Expired'
  return 'Active'
}

/**
 * Calculate points for each warning type (progressive discipline).
 */
function calculatePoints(warningType: WarningType): number {
  const pointsMap: Record<WarningType, number> = {
    Counselling: 1,
    VerbalWarning: 3,
    WrittenWarning: 6,
    FinalWrittenWarning: 10,
  }
  return pointsMap[warningType] ?? 0
}

/**
 * Determine disciplinary step based on accumulated points.
 * Based on the progressive discipline ladder convention.
 * 
 * Total Points → Disciplinary Step:
 *   0-1:     Counselling (no action needed)
 *   2-3:     Verbal Warning
 *   4-6:     Written Warning
 *   7-9:     Final Written Warning
 *   10+:     Dismissal Hearing
 */
function getDisciplinaryStep(totalPoints: number): string {
  if (totalPoints >= 10) return 'Dismissal Hearing'
  if (totalPoints >= 7) return 'Final Written Warning'
  if (totalPoints >= 4) return 'Written Warning'
  if (totalPoints >= 2) return 'Verbal Warning'
  if (totalPoints >= 1) return 'Counselling'
  return 'None'
}

/**
 * Check if a user has the role required to issue warnings.
 * Only Managers and above can issue warnings.
 */
function canIssueWarning(userRole: string): boolean {
  const minRoles = ['Manager', 'SeniorManager', 'SuperAdmin']
  return minRoles.includes(userRole)
}

/**
 * Get next warning type in the progression chain.
 */
function getNextWarningType(currentType: WarningType | null): WarningType | null {
  const progression: Array<WarningType | null> = [null, 'Counselling', 'VerbalWarning', 'WrittenWarning', 'FinalWrittenWarning']
  const currentIndex = progression.indexOf(currentType ?? 'Counselling')
  if (currentIndex < 0 || currentIndex >= progression.length - 1) return null
  return progression[currentIndex + 1]
}

/**
 * Count active warnings within rolling period for an employee.
 */
function countActiveWarnings(warnings: WarningLetter[], targetType: WarningType, currentDate?: string): number {
  const now = currentDate ? new Date(currentDate) : new Date()
  return warnings.filter(w => {
    if (w.warningType !== targetType) return false
    if (w.status === 'Archived') return false
    const expiryDate = calculateWarningExpiry(w.createdAt, w.warningType)
    return !isWarningExpired(expiryDate, currentDate)
  }).length
}

// ─── Test Cases ─────────────────────────────────────────────────────────────

describe('getViolationSeverity', () => {
  it('should assign low severity for time-related offences', () => {
    expect(getViolationSeverity('LateArrival')).toBe(1)
    expect(getViolationSeverity('EarlyDeparture')).toBe(1)
    expect(getViolationSeverity('MissingClockIn')).toBe(2)
    expect(getViolationSeverity('MissingClockOut')).toBe(1)
  })

  it('should assign high severity for serious offences', () => {
    expect(getViolationSeverity('AWOL')).toBe(3)
    expect(getViolationSeverity('UnplannedAbsenteeism')).toBe(3)
  })

  it('should assign zero severity for approved absences', () => {
    expect(getViolationSeverity('SickLeave')).toBe(0)
    expect(getViolationSeverity('FamilyResponsibilityLeave')).toBe(0)
  })

  it('should handle unknown types gracefully', () => {
    expect(getViolationSeverity('UnknownType')).toBe(0)
  })

  it('should handle empty string', () => {
    expect(getViolationSeverity('')).toBe(0)
  })

  it('should match all defined exception types', () => {
    const types = [
      'LateArrival', 'EarlyDeparture', 'MissingClockIn', 'MissingClockOut',
      'UnscheduledClockIn', 'UnscheduledClockOut', 'AWOL', 'UnplannedAbsenteeism',
      'AttendanceException', 'SickLeave', 'FamilyResponsibilityLeave',
      'ShiftCancellation', 'WorkshopAttendance', 'Training',
    ]
    types.forEach(type => {
      const severity = getViolationSeverity(type)
      expect(typeof severity).toBe('number')
      expect(severity).toBeGreaterThanOrEqual(0)
    })
  })
})

describe('calculateWarningExpiry', () => {
  it('should set 90-day expiry for Counselling', () => {
    const issuedAt = '2026-01-01T00:00:00Z'
    const expiry = calculateWarningExpiry(issuedAt, 'Counselling')
    const expiryDate = new Date(expiry)
    const issuedDate = new Date(issuedAt)
    const diffDays = Math.floor((expiryDate.getTime() - issuedDate.getTime()) / (24 * 60 * 60 * 1000))
    expect(diffDays).toBe(90)
  })

  it('should set 120-day expiry for Verbal Warning', () => {
    const issuedAt = '2026-01-01T00:00:00Z'
    const expiry = calculateWarningExpiry(issuedAt, 'VerbalWarning')
    const expiryDate = new Date(expiry)
    const issuedDate = new Date(issuedAt)
    const diffDays = Math.floor((expiryDate.getTime() - issuedDate.getTime()) / (24 * 60 * 60 * 1000))
    expect(diffDays).toBe(120)
  })

  it('should set 180-day expiry for Written Warning', () => {
    const issuedAt = '2026-01-01T00:00:00Z'
    const expiry = calculateWarningExpiry(issuedAt, 'WrittenWarning')
    const expiryDate = new Date(expiry)
    const issuedDate = new Date(issuedAt)
    const diffDays = Math.floor((expiryDate.getTime() - issuedDate.getTime()) / (24 * 60 * 60 * 1000))
    expect(diffDays).toBe(180)
  })

  it('should set 365-day expiry for Final Written Warning', () => {
    const issuedAt = '2026-01-01T00:00:00Z'
    const expiry = calculateWarningExpiry(issuedAt, 'FinalWrittenWarning')
    const expiryDate = new Date(expiry)
    const issuedDate = new Date(issuedAt)
    const diffDays = Math.floor((expiryDate.getTime() - issuedDate.getTime()) / (24 * 60 * 60 * 1000))
    expect(diffDays).toBe(365)
  })

  it('should return ISO string format', () => {
    const expiry = calculateWarningExpiry('2026-01-01T00:00:00Z', 'Counselling')
    expect(expiry).toMatch(/\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/)
  })
})

describe('isWarningExpired', () => {
  it('should return true when past expiry date', () => {
    const expiryDate = new Date(Date.now() - 100 * 24 * 60 * 60 * 1000).toISOString() // 100 days ago
    expect(isWarningExpired(expiryDate)).toBe(true)
  })

  it('should return false when future expiry date', () => {
    const expiryDate = new Date(Date.now() + 100 * 24 * 60 * 60 * 1000).toISOString() // 100 days from now
    expect(isWarningExpired(expiryDate)).toBe(false)
  })

  it('should handle exact boundary', () => {
    const now = new Date().toISOString()
    expect(isWarningExpired(now, now)).toBe(false)
  })

  it('should use custom current date', () => {
    const expiryDate = new Date(Date.now() - 100 * 24 * 60 * 60 * 1000).toISOString()
    const futureDate = new Date(Date.now() + 100 * 24 * 60 * 60 * 1000).toISOString()
    expect(isWarningExpired(expiryDate, futureDate)).toBe(false)
  })
})

describe('getWarningStatus', () => {
  it('should return "Active" for valid counselling', () => {
    const status = getWarningStatus('2026-11-01T00:00:00Z', 'Counselling', 'Issued')
    expect(status).toBe('Active')
  })

  it('should return "Active" for valid verbal warning', () => {
    const status = getWarningStatus('2026-11-01T00:00:00Z', 'VerbalWarning', 'Issued')
    expect(status).toBe('Active')
  })

  it('should return "Expired" for expired written warning', () => {
    const status = getWarningStatus('2025-01-01T00:00:00Z', 'WrittenWarning', 'Issued')
    expect(status).toBe('Expired')
  })

  it('should return "Expired" for overdue verbal warning', () => {
    const status = getWarningStatus('2025-01-01T00:00:00Z', 'VerbalWarning', 'Issued')
    expect(status).toBe('Expired')
  })

  it('should return "Draft" for draft warnings', () => {
    const status = getWarningStatus('2026-11-01T00:00:00Z', 'Counselling', 'Draft')
    expect(status).toBe('Draft')
  })

  it('should return "Archived" for archived warnings', () => {
    const status = getWarningStatus('2025-01-01T00:00:00Z', 'Counselling', 'Archived')
    expect(status).toBe('Archived')
  })

  it('should return "Expired" for final written warning after 365 days', () => {
    const status = getWarningStatus('2025-01-01T00:00:00Z', 'FinalWrittenWarning', 'Issued')
    expect(status).toBe('Expired')
  })

  it('should still be Active for final written warning within year', () => {
    const status = getWarningStatus('2026-01-01T00:00:00Z', 'FinalWrittenWarning', 'Issued')
    expect(status).toBe('Active')
  })
})

describe('calculatePoints', () => {
  it('should return correct points for each warning type', () => {
    expect(calculatePoints('Counselling')).toBe(1)
    expect(calculatePoints('VerbalWarning')).toBe(3)
    expect(calculatePoints('WrittenWarning')).toBe(6)
    expect(calculatePoints('FinalWrittenWarning')).toBe(10)
  })

  it('should have increasing point values', () => {
    const progression: WarningType[] = ['Counselling', 'VerbalWarning', 'WrittenWarning', 'FinalWrittenWarning']
    const points = progression.map(t => calculatePoints(t))
    for (let i = 1; i < points.length; i++) {
      expect(points[i]).toBeGreaterThan(points[i - 1])
    }
  })

  it('should sum points correctly for multiple warnings', () => {
    const total = calculatePoints('Counselling') + calculatePoints('VerbalWarning') + calculatePoints('WrittenWarning')
    expect(total).toBe(1 + 3 + 6) // 10
  })
})

describe('getDisciplinaryStep', () => {
  it('should return "None" for zero points', () => {
    expect(getDisciplinaryStep(0)).toBe('None')
  })

  it('should return "Counselling" for 1 point', () => {
    expect(getDisciplinaryStep(1)).toBe('Counselling')
  })

  it('should return "Verbal Warning" for 2-3 points', () => {
    expect(getDisciplinaryStep(2)).toBe('Verbal Warning')
    expect(getDisciplinaryStep(3)).toBe('Verbal Warning')
  })

  it('should return "Written Warning" for 4-6 points', () => {
    expect(getDisciplinaryStep(4)).toBe('Written Warning')
    expect(getDisciplinaryStep(5)).toBe('Written Warning')
    expect(getDisciplinaryStep(6)).toBe('Written Warning')
  })

  it('should return "Final Written Warning" for 7-9 points', () => {
    expect(getDisciplinaryStep(7)).toBe('Final Written Warning')
    expect(getDisciplinaryStep(8)).toBe('Final Written Warning')
    expect(getDisciplinaryStep(9)).toBe('Final Written Warning')
  })

  it('should return "Dismissal Hearing" for 10+ points', () => {
    expect(getDisciplinaryStep(10)).toBe('Dismissal Hearing')
    expect(getDisciplinaryStep(15)).toBe('Dismissal Hearing')
    expect(getDisciplinaryStep(20)).toBe('Dismissal Hearing')
  })

  it('should handle negative points', () => {
    expect(getDisciplinaryStep(-1)).toBe('None')
  })

  it('should follow progressive discipline ladder exactly', () => {
    // Simulate real-world accumulation
    // 1st offence: Counselling (1pt)
    // 2nd offence: Verbal Warning (1+3=4pts) -> Written Warning
    // 3rd offence: Written Warning (4+6=10pts) -> Dismissal Hearing
    expect(getDisciplinaryStep(1)).toBe('Counselling')
    expect(getDisciplinaryStep(4)).toBe('Written Warning')
    expect(getDisciplinaryStep(10)).toBe('Dismissal Hearing')
  })
})

describe('canIssueWarning', () => {
  it('should allow Manager to issue warnings', () => {
    expect(canIssueWarning('Manager')).toBe(true)
  })

  it('should allow SeniorManager to issue warnings', () => {
    expect(canIssueWarning('SeniorManager')).toBe(true)
  })

  it('should allow SuperAdmin to issue warnings', () => {
    expect(canIssueWarning('SuperAdmin')).toBe(true)
  })

  it('should deny TeamLeader from issuing warnings', () => {
    expect(canIssueWarning('TeamLeader')).toBe(false)
  })

  it('should deny Operator from issuing warnings', () => {
    expect(canIssueWarning('Operator')).toBe(false)
  })

  it('should deny Coordinator from issuing warnings', () => {
    expect(canIssueWarning('Coordinator')).toBe(false)
  })

  it('should deny QualityTechnician from issuing warnings', () => {
    expect(canIssueWarning('QualityTechnician')).toBe(false)
  })
})

describe('getNextWarningType', () => {
  it('should progress through the warning chain', () => {
    expect(getNextWarningType(null)).toBe('Counselling')
    expect(getNextWarningType('Counselling')).toBe('VerbalWarning')
    expect(getNextWarningType('VerbalWarning')).toBe('WrittenWarning')
    expect(getNextWarningType('WrittenWarning')).toBe('FinalWrittenWarning')
    expect(getNextWarningType('FinalWrittenWarning')).toBe(null)
  })

  it('should return null at the end of the chain', () => {
    expect(getNextWarningType('FinalWrittenWarning')).toBe(null)
  })
})

describe('countActiveWarnings', () => {
  it('should count only matching type', () => {
    const warnings: WarningLetter[] = [
      { id: '1', employeeId: '1', employeeName: 'A', displayName: 'A', departmentId: '1', departmentName: 'Prod', warningType: 'Counselling', relatedExceptionId: null, title: 'Test', description: 'Test', severity: 1, signedFormUrl: null, signedBy: null, signedAt: null, issuedBy: 'mgr-1', issuedByName: 'Mgr', status: 'Issued', escalationLevel: 1, createdAt: '2026-11-01T00:00:00Z', updatedAt: '2026-11-01T00:00:00Z' },
      { id: '2', employeeId: '1', employeeName: 'A', displayName: 'A', departmentId: '1', departmentName: 'Prod', warningType: 'VerbalWarning', relatedExceptionId: null, title: 'Test', description: 'Test', severity: 2, signedFormUrl: null, signedBy: null, signedAt: null, issuedBy: 'mgr-1', issuedByName: 'Mgr', status: 'Issued', escalationLevel: 1, createdAt: '2026-11-10T00:00:00Z', updatedAt: '2026-11-10T00:00:00Z' },
    ]
    expect(countActiveWarnings(warnings, 'Counselling')).toBe(1)
    expect(countActiveWarnings(warnings, 'VerbalWarning')).toBe(1)
  })

  it('should exclude archived warnings', () => {
    const warnings: WarningLetter[] = [
      { ...createWarning({ status: 'Archived', warningType: 'Counselling' }), id: '1' },
      { ...createWarning({ status: 'Issued', warningType: 'Counselling' }), id: '2' },
    ]
    expect(countActiveWarnings(warnings, 'Counselling')).toBe(1)
  })

  it('should exclude expired warnings', () => {
    const oldWarning: WarningLetter = {
      ...createWarning({ warningType: 'Counselling' }),
      createdAt: '2025-01-01T00:00:00Z',
    }
    expect(countActiveWarnings([oldWarning], 'Counselling')).toBe(0)
  })

  it('should return 0 for empty array', () => {
    expect(countActiveWarnings([], 'Counselling')).toBe(0)
  })
})

describe('Progressive Discipline Ladder Integration', () => {
  it('should simulate full escalation path per convention', () => {
    // Per yfpoza-progressive-discipline-ladder-convention:
    // 1st: Counselling (90 days), 2nd: Verbal Warning (120 days),
    // 3rd: Written Warning (180 days), 4th: Final Written Warning (12 months),
    // 5th+: Dismissal Hearing

    let totalPoints = 0

    // First offence
    totalPoints += calculatePoints('Counselling')
    expect(getDisciplinaryStep(totalPoints)).toBe('Counselling')

    // Second offence
    totalPoints += calculatePoints('VerbalWarning')
    expect(getDisciplinaryStep(totalPoints)).toBe('Written Warning')

    // Third offence
    totalPoints += calculatePoints('WrittenWarning')
    expect(getDisciplinaryStep(totalPoints)).toBe('Dismissal Hearing')

    // Verify we never hit Final Written Warning in this path
    // because 1+3=4 (Written Warning threshold), then 4+6=10 (Dismissal threshold)
  })

  it('should verify severity levels are non-negative', () => {
    const types = ['LateArrival', 'AWOL', 'SickLeave'] as string[]
    types.forEach(type => {
      expect(getViolationSeverity(type)).toBeGreaterThanOrEqual(0)
    })
  })

  it('should verify expiry periods increase with severity', () => {
    const periods = [
      calculateWarningExpiry('2026-01-01T00:00:00Z', 'Counselling'),
      calculateWarningExpiry('2026-01-01T00:00:00Z', 'VerbalWarning'),
      calculateWarningExpiry('2026-01-01T00:00:00Z', 'WrittenWarning'),
      calculateWarningExpiry('2026-01-01T00:00:00Z', 'FinalWrittenWarning'),
    ]
    for (let i = 1; i < periods.length; i++) {
      expect(new Date(periods[i]).getTime()).toBeGreaterThan(new Date(periods[i - 1]).getTime())
    }
  })
})

// Helper
function createWarning(overrides: Partial<WarningLetter> = {}): WarningLetter {
  return {
    id: 'warn-test',
    employeeId: '708001',
    employeeName: 'Test User',
    displayName: 'Test User',
    departmentId: '1',
    departmentName: 'Production',
    warningType: 'Counselling',
    relatedExceptionId: null,
    title: 'Test Warning',
    description: 'Test Description',
    severity: 1,
    signedFormUrl: null,
    signedBy: null,
    signedAt: null,
    issuedBy: 'mgr-1',
    issuedByName: 'Manager',
    status: 'Issued',
    escalationLevel: 1,
    createdAt: '2026-11-01T00:00:00Z',
    updatedAt: '2026-11-01T00:00:00Z',
    ...overrides,
  }
}
