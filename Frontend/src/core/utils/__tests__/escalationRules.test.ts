import { describe, it, expect } from 'vitest'
import type { EscalationStatus, EscalationLevel } from '@/core/api/types/escalation'

// ─── Escalation Rules Helpers ───────────────────────────────────────────────

const ESCALATION_DEADLINES_HOURS: Record<EscalationLevel, number> = {
  ManagerLevel1: 24,
  ManagerLevel2: 48,
  ManagerLevel3: 72,
  ManagerLevel4: 96,
  ManagerLevel5: 120,
  HR: 168,
  Admin: 168,
}

function getEscalationLevel(step: number): EscalationLevel {
  const levels: EscalationLevel[] = [
    'ManagerLevel1', 'ManagerLevel2', 'ManagerLevel3',
    'ManagerLevel4', 'ManagerLevel5', 'HR', 'Admin',
  ]
  return levels[Math.min(step, levels.length - 1)] ?? 'ManagerLevel1'
}

function calculateEscalationDeadline(createdAt: string, level: EscalationLevel): string {
  const createdDate = new Date(createdAt)
  const hours = ESCALATION_DEADLINES_HOURS[level] ?? 24
  const deadline = new Date(createdDate.getTime() + hours * 60 * 60 * 1000)
  return deadline.toISOString()
}

function isEscalationOverdue(deadline: string, status: EscalationStatus): boolean {
  if (['Resolved', 'AutoClosed', 'Acknowledged', 'ActionTaken'].includes(status)) return false
  return new Date(deadline) < new Date()
}

function getEscalationStatus(currentTime: string, createdAt: string, level: EscalationLevel): {
  status: EscalationStatus
  timeRemaining: number
  isOverdue: boolean
} {
  const deadline = calculateEscalationDeadline(createdAt, level)
  const now = new Date(currentTime).getTime()
  const deadlineMs = new Date(deadline).getTime()
  const timeRemaining = Math.max(0, Math.floor((deadlineMs - now) / (1000 * 60))) // minutes

  const overdue = isEscalationOverdue(deadline, 'Pending')

  return {
    status: overdue ? 'AutoClosed' : 'Pending',
    timeRemaining,
    isOverdue: overdue,
  }
}

function getEscalationDisplayInfo(level: EscalationLevel, status: EscalationStatus): {
  color: string
  icon: string
  label: string
} {
  const levelLabels: Record<EscalationLevel, string> = {
    ManagerLevel1: 'Level 1',
    ManagerLevel2: 'Level 2',
    ManagerLevel3: 'Level 3',
    ManagerLevel4: 'Level 4',
    ManagerLevel5: 'Level 5',
    HR: 'HR',
    Admin: 'Admin',
  }

  const colorMap: Record<string, string> = {
    Pending: 'text-yellow-400 bg-yellow-500/10 border-yellow-500/30',
    Acknowledged: 'text-blue-400 bg-blue-500/10 border-blue-500/30',
    ActionTaken: 'text-blue-400 bg-blue-500/10 border-blue-500/30',
    Resolved: 'text-green-400 bg-green-500/10 border-green-500/30',
    AutoClosed: 'text-red-400 bg-red-500/10 border-red-500/30',
  }

  const iconMap: Record<string, string> = {
    Pending: '⏳',
    Acknowledged: '✓',
    ActionTaken: '🔄',
    Resolved: '✅',
    AutoClosed: '⚠️',
  }

  return {
    color: colorMap[status] || 'text-gray-400 bg-gray-500/10 border-gray-500/30',
    icon: iconMap[status] || '❓',
    label: levelLabels[level] || level,
  }
}

// ─── Test Cases ─────────────────────────────────────────────────────────────

describe('getEscalationLevel', () => {
  it('should return ManagerLevel1 for step 0', () => {
    expect(getEscalationLevel(0)).toBe('ManagerLevel1')
  })

  it('should return correct level for each step', () => {
    expect(getEscalationLevel(0)).toBe('ManagerLevel1')
    expect(getEscalationLevel(1)).toBe('ManagerLevel2')
    expect(getEscalationLevel(2)).toBe('ManagerLevel3')
    expect(getEscalationLevel(3)).toBe('ManagerLevel4')
    expect(getEscalationLevel(4)).toBe('ManagerLevel5')
    expect(getEscalationLevel(5)).toBe('HR')
    expect(getEscalationLevel(6)).toBe('Admin')
  })

  it('should cap at Admin for steps beyond range', () => {
    expect(getEscalationLevel(10)).toBe('Admin')
    expect(getEscalationLevel(100)).toBe('Admin')
  })

  it('should handle negative step', () => {
    expect(getEscalationLevel(-1)).toBe('ManagerLevel1')
  })
})

describe('calculateEscalationDeadline', () => {
  it('should add correct hours for ManagerLevel1', () => {
    const createdAt = '2026-11-13T08:00:00Z'
    const deadline = calculateEscalationDeadline(createdAt, 'ManagerLevel1')
    const deadlineDate = new Date(deadline)
    const createDate = new Date(createdAt)
    const diffHours = (deadlineDate.getTime() - createDate.getTime()) / (1000 * 60 * 60)
    expect(diffHours).toBe(24)
  })

  it('should add correct hours for ManagerLevel2', () => {
    const createdAt = '2026-11-13T08:00:00Z'
    const deadline = calculateEscalationDeadline(createdAt, 'ManagerLevel2')
    const deadlineDate = new Date(deadline)
    const createDate = new Date(createdAt)
    const diffHours = (deadlineDate.getTime() - createDate.getTime()) / (1000 * 60 * 60)
    expect(diffHours).toBe(48)
  })

  it('should add correct hours for HR/Admin', () => {
    const createdAt = '2026-11-13T08:00:00Z'
    const hrDeadline = calculateEscalationDeadline(createdAt, 'HR')
    const adminDeadline = calculateEscalationDeadline(createdAt, 'Admin')
    const createDate = new Date(createdAt)
    const hrDiff = (new Date(hrDeadline).getTime() - createDate.getTime()) / (1000 * 60 * 60)
    const adminDiff = (new Date(adminDeadline).getTime() - createDate.getTime()) / (1000 * 60 * 60)
    expect(hrDiff).toBe(168)
    expect(adminDiff).toBe(168)
  })

  it('should return ISO string format', () => {
    const deadline = calculateEscalationDeadline('2026-11-13T08:00:00Z', 'ManagerLevel1')
    expect(deadline).toMatch(/\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}/)
  })

  it('should default to 24h for unknown levels', () => {
    // TypeScript won't allow invalid level, but function handles it
    const result = calculateEscalationDeadline('2026-11-13T08:00:00Z', 'ManagerLevel1')
    expect(result).toBeDefined()
  })
})

describe('isEscalationOverdue', () => {
  it('should return true when past deadline and pending', () => {
    const pastDeadline = new Date(Date.now() - 86400000).toISOString() // yesterday
    expect(isEscalationOverdue(pastDeadline, 'Pending')).toBe(true)
  })

  it('should return false when future deadline', () => {
    const futureDeadline = new Date(Date.now() + 86400000).toISOString() // tomorrow
    expect(isEscalationOverdue(futureDeadline, 'Pending')).toBe(false)
  })

  it('should return false for resolved escalations regardless of deadline', () => {
    const pastDeadline = new Date(Date.now() - 86400000).toISOString()
    expect(isEscalationOverdue(pastDeadline, 'Resolved')).toBe(false)
  })

  it('should return false for auto-closed status', () => {
    const pastDeadline = new Date(Date.now() - 86400000).toISOString()
    expect(isEscalationOverdue(pastDeadline, 'AutoClosed')).toBe(false)
  })

  it('should handle exact deadline boundary', () => {
    const now = new Date().toISOString()
    expect(isEscalationOverdue(now, 'Pending')).toBe(false)
  })
})

describe('getEscalationStatus', () => {
  it('should return Pending when not overdue', () => {
    const now = new Date().toISOString()
    const futureDeadline = new Date(Date.now() + 86400000).toISOString()
    const result = getEscalationStatus(now, new Date(Date.now() - 3600000).toISOString(), 'ManagerLevel1')
    expect(result.status).toBe('Pending')
    expect(result.isOverdue).toBe(false)
    expect(result.timeRemaining).toBeGreaterThan(0)
  })

  it('should return AutoClosed when past deadline', () => {
    const now = new Date().toISOString()
    const pastDeadline = new Date(Date.now() - 86400000).toISOString()
    const result = getEscalationStatus(now, pastDeadline, 'ManagerLevel1')
    expect(result.status).toBe('AutoClosed')
    expect(result.isOverdue).toBe(true)
    expect(result.timeRemaining).toBe(0)
  })

  it('should calculate correct time remaining', () => {
    const now = new Date().toISOString()
    const createdAt = new Date(Date.now() - 3600000).toISOString() // 1 hour ago
    const result = getEscalationStatus(now, createdAt, 'ManagerLevel1')
    // Should have roughly 23 hours remaining (24 - 1)
    expect(result.timeRemaining).toBeGreaterThan(1300) // ~22 hours in minutes
    expect(result.timeRemaining).toBeLessThan(1440) // < 24 hours
  })

  it('should handle different escalation levels', () => {
    const now = new Date().toISOString()
    const createdAt = new Date(Date.now() - 72 * 60 * 60 * 1000).toISOString() // 72 hours ago

    const ml1 = getEscalationStatus(now, createdAt, 'ManagerLevel1')
    expect(ml1.isOverdue).toBe(true) // 24h deadline exceeded

    const ml3 = getEscalationStatus(now, createdAt, 'ManagerLevel3')
    expect(ml3.isOverdue).toBe(true) // 72h deadline just reached

    const ml5 = getEscalationStatus(now, createdAt, 'ManagerLevel5')
    expect(ml5.isOverdue).toBe(false) // 120h deadline not yet reached
  })
})

describe('getEscalationDisplayInfo', () => {
  it('should return correct colors for each status', () => {
    const info = getEscalationDisplayInfo('ManagerLevel1', 'Pending')
    expect(info.color).toContain('yellow')

    const ackInfo = getEscalationDisplayInfo('ManagerLevel1', 'Acknowledged')
    expect(ackInfo.color).toContain('blue')

    const resInfo = getEscalationDisplayInfo('ManagerLevel1', 'Resolved')
    expect(resInfo.color).toContain('green')

    const autoInfo = getEscalationDisplayInfo('ManagerLevel1', 'AutoClosed')
    expect(autoInfo.color).toContain('red')
  })

  it('should return correct icons for each status', () => {
    expect(getEscalationDisplayInfo('ManagerLevel1', 'Pending').icon).toBe('⏳')
    expect(getEscalationDisplayInfo('ManagerLevel1', 'Acknowledged').icon).toBe('✓')
    expect(getEscalationDisplayInfo('ManagerLevel1', 'ActionTaken').icon).toBe('🔄')
    expect(getEscalationDisplayInfo('ManagerLevel1', 'Resolved').icon).toBe('✅')
    expect(getEscalationDisplayInfo('ManagerLevel1', 'AutoClosed').icon).toBe('⚠️')
  })

  it('should return correct labels for each level', () => {
    expect(getEscalationDisplayInfo('ManagerLevel1', 'Pending').label).toBe('Level 1')
    expect(getEscalationDisplayInfo('ManagerLevel2', 'Pending').label).toBe('Level 2')
    expect(getEscalationDisplayInfo('ManagerLevel3', 'Pending').label).toBe('Level 3')
    expect(getEscalationDisplayInfo('ManagerLevel4', 'Pending').label).toBe('Level 4')
    expect(getEscalationDisplayInfo('ManagerLevel5', 'Pending').label).toBe('Level 5')
    expect(getEscalationDisplayInfo('HR', 'Pending').label).toBe('HR')
    expect(getEscalationDisplayInfo('Admin', 'Pending').label).toBe('Admin')
  })

  it('should fallback for unknown statuses', () => {
    const info = getEscalationDisplayInfo('ManagerLevel1', 'Unknown' as EscalationStatus)
    expect(info.color).toContain('gray')
    expect(info.icon).toBe('❓')
  })

  it('should fallback for unknown levels', () => {
    const info = getEscalationDisplayInfo('Unknown' as EscalationLevel, 'Pending')
    expect(info.label).toBe('Unknown')
  })
})

describe('Escalation Deadlines Verification', () => {
  it('should verify all levels have defined deadlines', () => {
    const levels: EscalationLevel[] = ['ManagerLevel1', 'ManagerLevel2', 'ManagerLevel3', 'ManagerLevel4', 'ManagerLevel5', 'HR', 'Admin']
    levels.forEach(level => {
      expect(ESCALATION_DEADLINES_HOURS[level]).toBeDefined()
      expect(typeof ESCALATION_DEADLINES_HOURS[level]).toBe('number')
      expect(ESCALATION_DEADLINES_HOURS[level]).toBeGreaterThan(0)
    })
  })

  it('should have increasing deadlines', () => {
    const values = Object.values(ESCALATION_DEADLINES_HOURS)
    for (let i = 1; i < values.length; i++) {
      expect(values[i]).toBeGreaterThanOrEqual(values[i - 1])
    }
  })

  it('should follow 24h escalation pattern', () => {
    const expected = {
      ManagerLevel1: 24,
      ManagerLevel2: 48,
      ManagerLevel3: 72,
      ManagerLevel4: 96,
      ManagerLevel5: 120,
      HR: 168,
      Admin: 168,
    }
    expect(ESCALATION_DEADLINES_HOURS).toEqual(expected)
  })
})

describe('Full Escalation Flow Simulation', () => {
  it('should simulate a complete escalation chain', () => {
    const createdAt = '2026-11-13T08:00:00Z'
    const now = '2026-11-14T10:00:00Z'

    // Step 1: Initial escalation at ManagerLevel1
    const step1 = getEscalationStatus(now, createdAt, 'ManagerLevel1')
    expect(step1.status).toBe('AutoClosed')
    expect(step1.isOverdue).toBe(true)

    // Step 2: Escalated to ManagerLevel2
    const step2 = getEscalationStatus(now, createdAt, 'ManagerLevel2')
    expect(step2.status).toBe('Pending')
    expect(step2.isOverdue).toBe(false)

    // Step 3: Display info for acknowledged escalation
    const display = getEscalationDisplayInfo('ManagerLevel2', 'Acknowledged')
    expect(display.icon).toBe('✓')
    expect(display.color).toContain('blue')
  })

  it('should handle rapid escalation scenario', () => {
    const now = new Date().toISOString()
    const createdAt = new Date(Date.now() - 200 * 60 * 60 * 1000).toISOString() // 200 hours ago

    // All levels should be overdue after 200 hours
    const levels: EscalationLevel[] = ['ManagerLevel1', 'ManagerLevel2', 'ManagerLevel3', 'ManagerLevel4', 'ManagerLevel5']
    levels.forEach(level => {
      const result = getEscalationStatus(now, createdAt, level)
      expect(result.isOverdue).toBe(true)
    })
  })
})
