import { describe, it, expect } from 'vitest'
import { SHIFTS, getCurrentShift, isWithinShift, getShiftColor, SHIFT_ORDER } from '@/shared/constants/shifts'

describe('SHIFTS constants', () => {
  it('should have MORNING shift defined', () => {
    expect(SHIFTS.MORNING).toBeDefined()
    expect(SHIFTS.MORNING.code).toBe('ES')
    expect(SHIFTS.MORNING.startTime).toBe('06:00')
    expect(SHIFTS.MORNING.endTime).toBe('14:00')
  })

  it('should have AFTERNOON shift defined', () => {
    expect(SHIFTS.AFTERNOON).toBeDefined()
    expect(SHIFTS.AFTERNOON.code).toBe('MS')
    expect(SHIFTS.AFTERNOON.startTime).toBe('14:00')
    expect(SHIFTS.AFTERNOON.endTime).toBe('22:00')
  })

  it('should have NIGHT shift defined', () => {
    expect(SHIFTS.NIGHT).toBeDefined()
    expect(SHIFTS.NIGHT.code).toBe('NS')
    expect(SHIFTS.NIGHT.startTime).toBe('22:00')
    expect(SHIFTS.NIGHT.endTime).toBe('06:00')
  })

  it('should have LONG_DAY shift defined', () => {
    expect(SHIFTS.LONG_DAY).toBeDefined()
    expect(SHIFTS.LONG_DAY.code).toBe('LDS')
    expect(SHIFTS.LONG_DAY.startTime).toBe('06:00')
    expect(SHIFTS.LONG_DAY.endTime).toBe('18:00')
  })

  it('should have STANDARD shift defined', () => {
    expect(SHIFTS.STANDARD).toBeDefined()
    expect(SHIFTS.STANDARD.code).toBe('DS')
  })

  it('should have FULL_DAY shift defined', () => {
    expect(SHIFTS.FULL_DAY).toBeDefined()
    expect(SHIFTS.FULL_DAY.code).toBe('FDS')
  })

  it('should have CUSTOM shift defined', () => {
    expect(SHIFTS.CUSTOM).toBeDefined()
    expect(SHIFTS.CUSTOM.code).toBe('zdy')
  })

  it('should have exactly 7 shifts', () => {
    const keys = Object.keys(SHIFTS)
    expect(keys.length).toBe(7)
  })
})

describe('SHIFT_ORDER', () => {
  it('should define correct order', () => {
    expect(SHIFT_ORDER[0]).toBe('MORNING')
    expect(SHIFT_ORDER[1]).toBe('AFTERNOON')
    expect(SHIFT_ORDER[2]).toBe('NIGHT')
  })

  it('should include all shift keys', () => {
    for (const key of Object.keys(SHIFTS)) {
      expect(SHIFT_ORDER).toContain(key)
    }
  })
})

describe('getCurrentShift', () => {
  it('should return MORNING for hour 6-13', () => {
    expect(getCurrentShift(6)).toBe(SHIFTS.MORNING)
    expect(getCurrentShift(10)).toBe(SHIFTS.MORNING)
    expect(getCurrentShift(13)).toBe(SHIFTS.MORNING)
  })

  it('should return AFTERNOON for hour 14-21', () => {
    expect(getCurrentShift(14)).toBe(SHIFTS.AFTERNOON)
    expect(getCurrentShift(18)).toBe(SHIFTS.AFTERNOON)
    expect(getCurrentShift(21)).toBe(SHIFTS.AFTERNOON)
  })

  it('should return NIGHT for hour 0-5', () => {
    expect(getCurrentShift(0)).toBe(SHIFTS.NIGHT)
    expect(getCurrentShift(2)).toBe(SHIFTS.NIGHT)
    expect(getCurrentShift(5)).toBe(SHIFTS.NIGHT)
  })

  it('should return NIGHT for hour 22-23', () => {
    expect(getCurrentShift(22)).toBe(SHIFTS.NIGHT)
    expect(getCurrentShift(23)).toBe(SHIFTS.NIGHT)
  })

  it('should handle all 24 hours correctly', () => {
    for (let h = 0; h < 24; h++) {
      const shift = getCurrentShift(h)
      if (h >= 6 && h < 14) {
        expect(shift).toBe(SHIFTS.MORNING)
      } else if (h >= 14 && h < 22) {
        expect(shift).toBe(SHIFTS.AFTERNOON)
      } else {
        expect(shift).toBe(SHIFTS.NIGHT)
      }
    }
  })
})

describe('isWithinShift', () => {
  it('should detect morning shift within window', () => {
    const time = new Date(2026, 10, 13, 10, 30)
    expect(isWithinShift(time, SHIFTS.MORNING)).toBe(true)
  })

  it('should detect afternoon shift within window', () => {
    const time = new Date(2026, 10, 13, 18, 0)
    expect(isWithinShift(time, SHIFTS.AFTERNOON)).toBe(true)
  })

  it('should detect night shift within window', () => {
    const time = new Date(2026, 10, 13, 23, 0)
    expect(isWithinShift(time, SHIFTS.NIGHT)).toBe(true)
  })

  it('should return false for outside shift window', () => {
    const time = new Date(2026, 10, 13, 3, 0)
    expect(isWithinShift(time, SHIFTS.MORNING)).toBe(false)
  })

  it('should return false at exact start boundary for different shift', () => {
    const time = new Date(2026, 10, 13, 6, 0)
    expect(isWithinShift(time, SHIFTS.AFTERNOON)).toBe(false)
  })

  it('should return true at exact start boundary for matching shift', () => {
    const time = new Date(2026, 10, 13, 14, 0)
    expect(isWithinShift(time, SHIFTS.AFTERNOON)).toBe(true)
  })

  it('should handle midnight crossing for night shift', () => {
    const earlyMorning = new Date(2026, 10, 13, 2, 0)
    expect(isWithinShift(earlyMorning, SHIFTS.NIGHT)).toBe(true)
  })

  it('should return false for night shift at noon', () => {
    const noon = new Date(2026, 10, 13, 12, 0)
    expect(isWithinShift(noon, SHIFTS.NIGHT)).toBe(false)
  })

  it('should handle LONG_DAY shift', () => {
    const time = new Date(2026, 10, 13, 12, 0)
    expect(isWithinShift(time, SHIFTS.LONG_DAY)).toBe(true)
  })

  it('should handle STANDARD shift', () => {
    const time = new Date(2026, 10, 13, 10, 0)
    expect(isWithinShift(time, SHIFTS.STANDARD)).toBe(true)
  })

  it('should return false for STANDARD outside business hours', () => {
    const time = new Date(2026, 10, 13, 20, 0)
    expect(isWithinShift(time, SHIFTS.STANDARD)).toBe(false)
  })

  it('should handle FULL_DAY shift', () => {
    const time = new Date(2026, 10, 13, 13, 0)
    expect(isWithinShift(time, SHIFTS.FULL_DAY)).toBe(true)
  })

  it('should return false at exact end boundary', () => {
    const time = new Date(2026, 10, 13, 14, 0)
    expect(isWithinShift(time, SHIFTS.MORNING)).toBe(false)
  })

  it('should handle minutes precisely', () => {
    const time = new Date(2026, 10, 13, 13, 59)
    expect(isWithinShift(time, SHIFTS.MORNING)).toBe(true)
  })

  it('should return false when minutes exceed shift end', () => {
    const time = new Date(2026, 10, 13, 14, 1)
    expect(isWithinShift(time, SHIFTS.MORNING)).toBe(false)
  })
})

describe('getShiftColor', () => {
  it('should return green for ES/MORNING', () => {
    expect(getShiftColor('ES')).toContain('green')
    expect(getShiftColor('MORNING')).toContain('green')
  })

  it('should return orange for MS/AFTERNOON', () => {
    expect(getShiftColor('MS')).toContain('orange')
    expect(getShiftColor('AFTERNOON')).toContain('orange')
  })

  it('should return blue for NS/NIGHT', () => {
    expect(getShiftColor('NS')).toContain('blue')
    expect(getShiftColor('NIGHT')).toContain('blue')
  })

  it('should return purple for LDS', () => {
    expect(getShiftColor('LDS')).toContain('purple')
  })

  it('should return cyan for DS', () => {
    expect(getShiftColor('DS')).toContain('cyan')
  })

  it('should return teal for FDS', () => {
    expect(getShiftColor('FDS')).toContain('teal')
  })

  it('should return gray for unknown codes', () => {
    expect(getShiftColor('UNKNOWN')).toContain('gray')
    expect(getShiftColor('xyz')).toContain('gray')
  })

  it('should return gray for custom shift', () => {
    expect(getShiftColor('zdy')).toContain('gray')
  })

  it('should return empty string for undefined input', () => {
    expect(getShiftColor('')).toContain('gray')
  })

  it('should return gray for null-like input', () => {
    expect(getShiftColor('null')).toContain('gray')
  })
})
