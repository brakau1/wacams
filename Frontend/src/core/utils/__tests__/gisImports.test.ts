import { describe, it, expect } from 'vitest'

// ─── GIS Import Helpers ─────────────────────────────────────────────────────

/**
 * Parse a GIS shift code to an internal representation.
 */
function parseGISShiftCode(code: string): {
  valid: boolean
  internalCode: string
  name: string
  startTime: string
  endTime: string
} {
  const mapping: Record<string, { internalCode: string; name: string; startTime: string; endTime: string }> = {
    ES: { internalCode: 'ES', name: 'Early Shift / Morning', startTime: '06:00', endTime: '14:00' },
    MS: { internalCode: 'MS', name: 'Middle Shift / Afternoon', startTime: '14:00', endTime: '22:00' },
    NS: { internalCode: 'NS', name: 'Night Shift', startTime: '22:00', endTime: '06:00' },
    LDS: { internalCode: 'LDS', name: 'Long Day Shift', startTime: '06:00', endTime: '18:00' },
    DS: { internalCode: 'DS', name: 'Day Shift', startTime: '08:00', endTime: '17:00' },
    FDS: { internalCode: 'FDS', name: 'Full Day Shift', startTime: '07:00', endTime: '19:00' },
    zdy: { internalCode: 'zdy', name: 'Custom Shift', startTime: '00:00', endTime: '00:00' },
  }

  if (!mapping[code]) {
    return {
      valid: false,
      internalCode: '',
      name: 'Unknown',
      startTime: '',
      endTime: '',
    }
  }

  return {
    valid: true,
    ...mapping[code],
  }
}

/**
 * Map GIS shift code to internal system shift definition.
 */
function mapGISShiftToInternal(gisCode: string): {
  success: boolean
  shiftCode: string
  shiftName: string
  startTime: string
  endTime: string
  isOvernight: boolean
} {
  const parsed = parseGISShiftCode(gisCode)
  if (!parsed.valid) {
    return {
      success: false,
      shiftCode: '',
      shiftName: 'Unknown',
      startTime: '',
      endTime: '',
      isOvernight: false,
    }
  }

  // Check if shift crosses midnight
  const [startH] = parsed.startTime.split(':').map(Number)
  const [endH] = parsed.endTime.split(':').map(Number)
  const isOvernight = endH <= startH && (startH >= 18 || endH < 6)

  return {
    success: true,
    shiftCode: parsed.internalCode,
    shiftName: parsed.name,
    startTime: parsed.startTime,
    endTime: parsed.endTime,
    isOvernight,
  }
}

/**
 * Parse a single attendance record from GIS export data.
 */
function parseGISAttendanceRecord(row: Record<string, unknown>): {
  success: boolean
  employeeId: string
  date: string
  clockIn: string | null
  clockOut: string | null
  shiftCode: string
  status: string
  error?: string
} {
  const errors: string[] = []

  const empId = String(row['EmployeeID'] ?? row['employee_id'] ?? '')
  const dateStr = String(row['Date'] ?? row['date'] ?? '')
  const clockIn = String(row['ClockIn'] ?? row['clock_in'] ?? '')
  const clockOut = String(row['ClockOut'] ?? row['clock_out'] ?? '')
  const shiftCode = String(row['ShiftCode'] ?? row['shift_code'] ?? '')

  // Validate required fields
  if (!empId) errors.push('Missing Employee ID')
  if (!dateStr) errors.push('Missing Date')
  if (!shiftCode) errors.push('Missing Shift Code')

  // Parse time values
  let parsedClockIn: string | null = null
  let parsedClockOut: string | null = null

  if (clockIn && clockIn !== '-' && clockIn !== 'N/A') {
    if (/^\d{1,2}:\d{2}$/.test(clockIn)) {
      parsedClockIn = clockIn.padStart(5, '0')
    } else {
      errors.push(`Invalid clock-in format: ${clockIn}`)
    }
  }

  if (clockOut && clockOut !== '-' && clockOut !== 'N/A') {
    if (/^\d{1,2}:\d{2}$/.test(clockOut)) {
      parsedClockOut = clockOut.padStart(5, '0')
    } else {
      errors.push(`Invalid clock-out format: ${clockOut}`)
    }
  }

  // Determine status
  let status = 'Present'
  if (!clockIn || !parsedClockIn) {
    status = 'MissingClockIn'
  } else if (!clockOut || !parsedClockOut) {
    status = 'MissingClockOut'
  }

  if (errors.length > 0) {
    return {
      success: false,
      employeeId: empId,
      date: dateStr,
      clockIn: parsedClockIn,
      clockOut: parsedClockOut,
      shiftCode,
      status,
      error: errors.join('; '),
    }
  }

  return {
    success: true,
    employeeId: empId,
    date: dateStr,
    clockIn: parsedClockIn,
    clockOut: parsedClockOut,
    shiftCode,
    status,
  }
}

/**
 * Validate raw GIS import data before processing.
 */
function validateGISData(rows: Record<string, unknown>[]): {
  valid: boolean
  totalRows: number
  validRows: number
  invalidRows: number
  errors: string[]
} {
  const errors: string[] = []
  let validCount = 0
  let invalidCount = 0

  rows.forEach((row, index) => {
    const parsed = parseGISAttendanceRecord(row)
    if (parsed.success) {
      validCount++
    } else {
      invalidCount++
      if (parsed.error) {
        errors.push(`Row ${index + 1}: ${parsed.error}`)
      }
    }
  })

  return {
    valid: invalidCount === 0,
    totalRows: rows.length,
    validRows: validCount,
    invalidRows: invalidCount,
    errors,
  }
}

/**
 * Get import status based on validation results.
 */
function getGISImportStatus(validationResult: ReturnType<typeof validateGISData>): {
  label: string
  color: string
  message: string
} {
  if (validationResult.totalRows === 0) {
    return {
      label: 'Empty',
      color: 'text-gray-400',
      message: 'No data rows found in import file.',
    }
  }

  if (validationResult.invalidRows === 0) {
    return {
      label: 'All Valid',
      color: 'text-green-400',
      message: `All ${validationResult.totalRows} rows are valid.`,
    }
  }

  if (validationResult.validRows === 0) {
    return {
      label: 'All Invalid',
      color: 'text-red-400',
      message: `${validationResult.totalRows} rows failed validation.`,
    }
  }

  return {
    label: 'Partial',
    color: 'text-yellow-400',
    message: `${validationResult.validRows}/${validationResult.totalRows} valid (${validationResult.invalidRows} invalid).`,
  }
}

/**
 * Normalize employee ID for consistency.
 */
function normalizeEmployeeId(id: string | undefined | null): string {
  if (!id) return ''
  // Remove non-digit characters
  const digits = id.replace(/\D/g, '')
  // Pad with leading zeros if needed
  return digits.padStart(6, '0')
}

/**
 * Validate date format (YYYY-MM-DD).
 */
function validateDateFormat(dateStr: string): boolean {
  if (!dateStr) return false
  const regex = /^\d{4}-\d{2}-\d{2}$/
  if (!regex.test(dateStr)) return false

  const parts = dateStr.split('-')
  const year = parseInt(parts[0], 10)
  const month = parseInt(parts[1], 10)
  const day = parseInt(parts[2], 10)

  if (year < 2000 || year > 2100) return false
  if (month < 1 || month > 12) return false
  if (day < 1 || day > 31) return false

  // Verify the date actually exists
  const dateObj = new Date(year, month - 1, day)
  return dateObj.getFullYear() === year && dateObj.getMonth() === month - 1 && dateObj.getDate() === day
}

/**
 * Validate time format (HH:mm).
 */
function validateTimeFormat(timeStr: string): boolean {
  if (!timeStr) return false
  const regex = /^(\d{1,2}):(\d{2})$/
  const match = timeStr.match(regex)
  if (!match) return false

  const hour = parseInt(match[1], 10)
  const minute = parseInt(match[2], 10)

  return hour >= 0 && hour <= 23 && minute >= 0 && minute <= 59
}

// ─── Test Cases ─────────────────────────────────────────────────────────────

describe('parseGISShiftCode', () => {
  it('should parse ES (Early Shift) correctly', () => {
    const result = parseGISShiftCode('ES')
    expect(result.valid).toBe(true)
    expect(result.internalCode).toBe('ES')
    expect(result.name).toBe('Early Shift / Morning')
    expect(result.startTime).toBe('06:00')
    expect(result.endTime).toBe('14:00')
  })

  it('should parse MS (Middle Shift) correctly', () => {
    const result = parseGISShiftCode('MS')
    expect(result.valid).toBe(true)
    expect(result.internalCode).toBe('MS')
    expect(result.name).toBe('Middle Shift / Afternoon')
  })

  it('should parse NS (Night Shift) correctly', () => {
    const result = parseGISShiftCode('NS')
    expect(result.valid).toBe(true)
    expect(result.internalCode).toBe('NS')
    expect(result.name).toBe('Night Shift')
    expect(result.startTime).toBe('22:00')
    expect(result.endTime).toBe('06:00')
  })

  it('should parse LDS (Long Day Shift) correctly', () => {
    const result = parseGISShiftCode('LDS')
    expect(result.valid).toBe(true)
    expect(result.internalCode).toBe('LDS')
    expect(result.startTime).toBe('06:00')
    expect(result.endTime).toBe('18:00')
  })

  it('should parse DS (Day Shift) correctly', () => {
    const result = parseGISShiftCode('DS')
    expect(result.valid).toBe(true)
    expect(result.internalCode).toBe('DS')
    expect(result.startTime).toBe('08:00')
    expect(result.endTime).toBe('17:00')
  })

  it('should parse FDS (Full Day Shift) correctly', () => {
    const result = parseGISShiftCode('FDS')
    expect(result.valid).toBe(true)
    expect(result.internalCode).toBe('FDS')
    expect(result.startTime).toBe('07:00')
    expect(result.endTime).toBe('19:00')
  })

  it('should parse zdy (Custom) correctly', () => {
    const result = parseGISShiftCode('zdy')
    expect(result.valid).toBe(true)
    expect(result.internalCode).toBe('zdy')
    expect(result.name).toBe('Custom Shift')
  })

  it('should return invalid for unknown codes', () => {
    const result = parseGISShiftCode('UNKNOWN')
    expect(result.valid).toBe(false)
    expect(result.internalCode).toBe('')
  })

  it('should handle empty string', () => {
    const result = parseGISShiftCode('')
    expect(result.valid).toBe(false)
  })

  it('should handle all known codes', () => {
    const codes = ['ES', 'MS', 'NS', 'LDS', 'DS', 'FDS', 'zdy']
    codes.forEach(code => {
      const result = parseGISShiftCode(code)
      expect(result.valid).toBe(true)
      expect(result.internalCode).toBe(code)
    })
  })
})

describe('mapGISShiftToInternal', () => {
  it('should map ES to morning shift', () => {
    const result = mapGISShiftToInternal('ES')
    expect(result.success).toBe(true)
    expect(result.isOvernight).toBe(false)
  })

  it('should map NS to overnight shift', () => {
    const result = mapGISShiftToInternal('NS')
    expect(result.success).toBe(true)
    expect(result.isOvernight).toBe(true)
  })

  it('should detect LDS as same-day shift', () => {
    const result = mapGISShiftToInternal('LDS')
    expect(result.success).toBe(true)
    expect(result.isOvernight).toBe(false)
  })

  it('should fail for unknown codes', () => {
    const result = mapGISShiftToInternal('XYZ')
    expect(result.success).toBe(false)
  })

  it('should set correct shift names', () => {
    expect(mapGISShiftToInternal('ES').shiftName).toContain('Morning')
    expect(mapGISShiftToInternal('MS').shiftName).toContain('Afternoon')
    expect(mapGISShiftToInternal('NS').shiftName).toBe('Night Shift')
  })
})

describe('parseGISAttendanceRecord', () => {
  it('should parse a valid record with standard keys', () => {
    const row = {
      EmployeeID: '708513',
      Date: '2026-11-13',
      ClockIn: '05:55',
      ClockOut: '14:10',
      ShiftCode: 'ES',
    }
    const result = parseGISAttendanceRecord(row)
    expect(result.success).toBe(true)
    expect(result.employeeId).toBe('708513')
    expect(result.date).toBe('2026-11-13')
    expect(result.clockIn).toBe('05:55')
    expect(result.clockOut).toBe('14:10')
    expect(result.shiftCode).toBe('ES')
    expect(result.status).toBe('Present')
  })

  it('should parse with lowercase keys', () => {
    const row = {
      employee_id: '708513',
      date: '2026-11-13',
      clock_in: '06:00',
      clock_out: '14:00',
      shift_code: 'MS',
    }
    const result = parseGISAttendanceRecord(row)
    expect(result.success).toBe(true)
    expect(result.employeeId).toBe('708513')
  })

  it('should detect missing clock-in', () => {
    const row = {
      EmployeeID: '708513',
      Date: '2026-11-13',
      ClockIn: '-',
      ClockOut: '14:00',
      ShiftCode: 'ES',
    }
    const result = parseGISAttendanceRecord(row)
    expect(result.success).toBe(true)
    expect(result.status).toBe('MissingClockIn')
  })

  it('should detect missing clock-out', () => {
    const row = {
      EmployeeID: '708513',
      Date: '2026-11-13',
      ClockIn: '06:00',
      ClockOut: '-',
      ShiftCode: 'ES',
    }
    const result = parseGISAttendanceRecord(row)
    expect(result.success).toBe(true)
    expect(result.status).toBe('MissingClockOut')
  })

  it('should mark as invalid when employee ID is missing', () => {
    const row = {
      Date: '2026-11-13',
      ClockIn: '06:00',
      ClockOut: '14:00',
      ShiftCode: 'ES',
    }
    const result = parseGISAttendanceRecord(row)
    expect(result.success).toBe(false)
    expect(result.error).toContain('Missing Employee ID')
  })

  it('should mark as invalid when date is missing', () => {
    const row = {
      EmployeeID: '708513',
      ClockIn: '06:00',
      ClockOut: '14:00',
      ShiftCode: 'ES',
    }
    const result = parseGISAttendanceRecord(row)
    expect(result.success).toBe(false)
    expect(result.error).toContain('Missing Date')
  })

  it('should mark as invalid when shift code is missing', () => {
    const row = {
      EmployeeID: '708513',
      Date: '2026-11-13',
      ClockIn: '06:00',
      ClockOut: '14:00',
    }
    const result = parseGISAttendanceRecord(row)
    expect(result.success).toBe(false)
    expect(result.error).toContain('Missing Shift Code')
  })

  it('should handle N/A values', () => {
    const row = {
      EmployeeID: '708513',
      Date: '2026-11-13',
      ClockIn: 'N/A',
      ClockOut: '14:00',
      ShiftCode: 'ES',
    }
    const result = parseGISAttendanceRecord(row)
    expect(result.success).toBe(true)
    expect(result.clockIn).toBeNull()
  })
})

describe('validateGISData', () => {
  it('should validate all good rows', () => {
    const rows = [
      { EmployeeID: '708513', Date: '2026-11-13', ClockIn: '06:00', ClockOut: '14:00', ShiftCode: 'ES' },
      { EmployeeID: '707995', Date: '2026-11-13', ClockIn: '06:10', ClockOut: '14:05', ShiftCode: 'ES' },
    ]
    const result = validateGISData(rows)
    expect(result.valid).toBe(true)
    expect(result.totalRows).toBe(2)
    expect(result.validRows).toBe(2)
    expect(result.invalidRows).toBe(0)
  })

  it('should detect invalid rows', () => {
    const rows = [
      { EmployeeID: '708513', Date: '2026-11-13', ClockIn: '06:00', ClockOut: '14:00', ShiftCode: 'ES' },
      { Date: '2026-11-13', ClockIn: '06:00', ClockOut: '14:00', ShiftCode: 'ES' }, // Missing ID
    ]
    const result = validateGISData(rows)
    expect(result.valid).toBe(false)
    expect(result.validRows).toBe(1)
    expect(result.invalidRows).toBe(1)
    expect(result.errors.length).toBeGreaterThan(0)
  })

  it('should return empty result for no rows', () => {
    const result = validateGISData([])
    expect(result.valid).toBe(true)
    expect(result.totalRows).toBe(0)
    expect(result.validRows).toBe(0)
    expect(result.invalidRows).toBe(0)
  })

  it('should collect multiple errors', () => {
    const rows = [
      { Date: '2026-11-13' }, // Missing ID and shift
      { EmployeeID: '708513' }, // Missing date and shift
    ]
    const result = validateGISData(rows)
    expect(result.valid).toBe(false)
    expect(result.invalidRows).toBe(2)
  })
})

describe('getGISImportStatus', () => {
  it('should return "All Valid" for perfect data', () => {
    const result = getGISImportStatus({ valid: true, totalRows: 10, validRows: 10, invalidRows: 0, errors: [] })
    expect(result.label).toBe('All Valid')
    expect(result.color).toContain('green')
  })

  it('should return "All Invalid" when all rows fail', () => {
    const result = getGISImportStatus({ valid: false, totalRows: 5, validRows: 0, invalidRows: 5, errors: ['Error 1'] })
    expect(result.label).toBe('All Invalid')
    expect(result.color).toContain('red')
  })

  it('should return "Partial" for mixed results', () => {
    const result = getGISImportStatus({ valid: false, totalRows: 10, validRows: 7, invalidRows: 3, errors: [] })
    expect(result.label).toBe('Partial')
    expect(result.color).toContain('yellow')
  })

  it('should return "Empty" for no data', () => {
    const result = getGISImportStatus({ valid: true, totalRows: 0, validRows: 0, invalidRows: 0, errors: [] })
    expect(result.label).toBe('Empty')
    expect(result.color).toContain('gray')
  })
})

describe('normalizeEmployeeId', () => {
  it('should return digits only', () => {
    expect(normalizeEmployeeId('708513')).toBe('708513')
    expect(normalizeEmployeeId('EMP-708513')).toBe('708513')
    expect(normalizeEmployeeId('708-513')).toBe('708513')
  })

  it('should pad with leading zeros', () => {
    expect(normalizeEmployeeId('7085')).toBe('007085')
    expect(normalizeEmployeeId('8513')).toBe('008513')
  })

  it('should return empty for null/undefined', () => {
    expect(normalizeEmployeeId(null!)).toBe('')
    expect(normalizeEmployeeId(undefined!)).toBe('')
    expect(normalizeEmployeeId('')).toBe('')
  })
})

describe('validateDateFormat', () => {
  it('should accept valid dates', () => {
    expect(validateDateFormat('2026-11-13')).toBe(true)
    expect(validateDateFormat('2026-01-01')).toBe(true)
    expect(validateDateFormat('2026-12-31')).toBe(true)
  })

  it('should reject invalid dates', () => {
    expect(validateDateFormat('2026-13-01')).toBe(false) // Month 13
    expect(validateDateFormat('2026-00-01')).toBe(false) // Month 0
    expect(validateDateFormat('2026-02-30')).toBe(false) // Feb 30 doesn't exist
    expect(validateDateFormat('2026-02-29')).toBe(false) // 2026 not leap year
    expect(validateDateFormat('2024-02-29')).toBe(true) // 2024 IS leap year
  })

  it('should reject invalid formats', () => {
    expect(validateDateFormat('13/11/2026')).toBe(false)
    expect(validateDateFormat('Nov 13, 2026')).toBe(false)
    expect(validateDateFormat('20261113')).toBe(false)
    expect(validateDateFormat('')).toBe(false)
  })
})

describe('validateTimeFormat', () => {
  it('should accept valid times', () => {
    expect(validateTimeFormat('06:00')).toBe(true)
    expect(validateTimeFormat('14:30')).toBe(true)
    expect(validateTimeFormat('22:00')).toBe(true)
    expect(validateTimeFormat('00:00')).toBe(true)
    expect(validateTimeFormat('23:59')).toBe(true)
  })

  it('should reject invalid times', () => {
    expect(validateTimeFormat('24:00')).toBe(false)
    expect(validateTimeFormat('25:00')).toBe(false)
    expect(validateTimeFormat('12:60')).toBe(false)
    expect(validateTimeFormat('12:61')).toBe(false)
    expect(validateTimeFormat('abc:def')).toBe(false)
  })

  it('should reject empty strings', () => {
    expect(validateTimeFormat('')).toBe(false)
    expect(validateTimeFormat(null!)).toBe(false)
  })
})

describe('GIS Import Integration', () => {
  it('should process a full import pipeline', () => {
    // Step 1: Validate
    const rows = [
      { EmployeeID: '708513', Date: '2026-11-13', ClockIn: '05:55', ClockOut: '14:10', ShiftCode: 'ES' },
      { EmployeeID: '707995', Date: '2026-11-13', ClockIn: '06:35', ClockOut: '14:05', ShiftCode: 'ES' },
    ]
    const validation = validateGISData(rows)
    expect(validation.valid).toBe(true)

    // Step 2: Parse records
    rows.forEach(row => {
      const parsed = parseGISAttendanceRecord(row)
      expect(parsed.success).toBe(true)
    })

    // Step 3: Map shifts
    rows.forEach(row => {
      const shift = mapGISShiftToInternal(String(row.ShiftCode))
      expect(shift.success).toBe(true)
    })

    // Step 4: Get status
    const status = getGISImportStatus(validation)
    expect(status.label).toBe('All Valid')
  })

  it('should handle real-world data with some errors', () => {
    const rows = [
      { EmployeeID: '708513', Date: '2026-11-13', ClockIn: '06:00', ClockOut: '14:00', ShiftCode: 'ES' },
      { EmployeeID: '', Date: '2026-11-13', ClockIn: '06:00', ClockOut: '14:00', ShiftCode: 'ES' }, // Bad
      { EmployeeID: '707995', Date: 'invalid-date', ClockIn: '06:00', ClockOut: '14:00', ShiftCode: 'MS' }, // Bad date
      { EmployeeID: '708102', Date: '2026-11-13', ClockIn: '06:00', ClockOut: '14:00', ShiftCode: 'NS' },
    ]
    const validation = validateGISData(rows)
    expect(validation.valid).toBe(false)
    expect(validation.validRows).toBe(2)
    expect(validation.invalidRows).toBe(2)
  })
})
