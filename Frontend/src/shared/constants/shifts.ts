/**
 * WACAMS — Shift Definitions
 */

export interface ShiftDefinition {
  code: string
  name: string
  startTime: string // HH:mm format
  endTime: string   // HH:mm format (next day if crossing midnight)
  description: string
}

export const SHIFTS: Record<string, ShiftDefinition> = {
  MORNING: {
    code: 'ES',
    name: 'Early Shift / Morning',
    startTime: '06:00',
    endTime: '14:00',
    description: 'Morning shift (06:00 – 14:00)'
  },
  AFTERNOON: {
    code: 'MS',
    name: 'Middle Shift / Afternoon',
    startTime: '14:00',
    endTime: '22:00',
    description: 'Afternoon shift (14:00 – 22:00)'
  },
  NIGHT: {
    code: 'NS',
    name: 'Night Shift',
    startTime: '22:00',
    endTime: '06:00',
    description: 'Night shift (22:00 – 06:00 next day)'
  },
  LONG_DAY: {
    code: 'LDS',
    name: 'Long Day Shift',
    startTime: '06:00',
    endTime: '18:00',
    description: 'Extended day shift (06:00 – 18:00)'
  },
  STANDARD: {
    code: 'DS',
    name: 'Day Shift',
    startTime: '08:00',
    endTime: '17:00',
    description: 'Standard business hours (08:00 – 17:00)'
  },
  FULL_DAY: {
    code: 'FDS',
    name: 'Full Day Shift',
    startTime: '07:00',
    endTime: '19:00',
    description: 'Full day coverage shift (07:00 – 19:00)'
  },
  CUSTOM: {
    code: 'zdy',
    name: 'Custom Shift',
    startTime: '00:00',
    endTime: '00:00',
    description: 'System-defined custom schedule'
  }
} as const

export const SHIFT_ORDER = ['MORNING', 'AFTERNOON', 'NIGHT', 'LONG_DAY', 'STANDARD', 'FULL_DAY', 'CUSTOM']

/**
 * Get the current shift based on the time of day.
 */
export function getCurrentShift(hour: number): ShiftDefinition {
  if (hour >= 6 && hour < 14) return SHIFTS.MORNING
  if (hour >= 14 && hour < 22) return SHIFTS.AFTERNOON
  return SHIFTS.NIGHT
}

/**
 * Check if a given time falls within a specific shift window.
 */
export function isWithinShift(time: Date, shift: ShiftDefinition): boolean {
  const h = time.getHours()
  const m = time.getMinutes()
  const currentMinutes = h * 60 + m

  const [startH, startM] = shift.startTime.split(':').map(Number)
  const [endH, endM] = shift.endTime.split(':').map(Number)
  let startMinutes = startH * 60 + startM
  let endMinutes = endH * 60 + endM

  // Handle overnight shifts
  if (endMinutes <= startMinutes) {
    endMinutes += 24 * 60
  }

  if (startMinutes <= endMinutes) {
    // Same-day shift
    return currentMinutes >= startMinutes && currentMinutes < endMinutes
  } else {
    // Overnight shift
    return currentMinutes >= startMinutes || currentMinutes < endMinutes - 24 * 60
  }
}

/**
 * Get shift display color for attendance status.
 */
export function getShiftColor(shiftCode: string): string {
  switch (shiftCode) {
    case 'ES':
    case 'MORNING':
      return 'text-green-400 bg-green-500/10 border-green-500/30'
    case 'MS':
    case 'AFTERNOON':
      return 'text-orange-400 bg-orange-500/10 border-orange-500/30'
    case 'NS':
    case 'NIGHT':
      return 'text-blue-400 bg-blue-500/10 border-blue-500/30'
    case 'LDS':
      return 'text-purple-400 bg-purple-500/10 border-purple-500/30'
    case 'DS':
      return 'text-cyan-400 bg-cyan-500/10 border-cyan-500/30'
    case 'FDS':
      return 'text-teal-400 bg-teal-500/10 border-teal-500/30'
    default:
      return 'text-gray-400 bg-gray-500/10 border-gray-500/30'
  }
}
