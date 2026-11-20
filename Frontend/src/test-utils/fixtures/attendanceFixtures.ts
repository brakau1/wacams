// ============================================================================
// WACAMS Frontend — Attendance Fixtures for Testing
// ============================================================================
// Mock attendance records, summaries, and shift schedules for UI testing.

import type { AttendanceRecord, AttendanceSummary } from '@/core/api/types';

const generateId = (): string => `ATT-${Math.random().toString(36).substring(2, 9)}`;

// ─── Attendance Records ──────────────────────────────────────────────────────

/**
 * Creates a realistic set of attendance records for a single employee over 7 days.
 */
export function createEmployeeAttendanceRecords(employeeId: string, displayName: string): AttendanceRecord[] {
  const records: AttendanceRecord[] = [];
  const now = new Date();

  const scenarios: Array<{
    dateOffset: number;
    status: AttendanceRecord['status'];
    lateMinutes: number;
    earlyMinutes: number;
    clockIn: string | null;
    clockOut: string | null;
    scheduledShift: string;
  }> = [
    { dateOffset: -6, status: 'Present', lateMinutes: 0, earlyMinutes: 0, clockIn: '05:55:00', clockOut: '14:05:00', scheduledShift: 'Morning' },
    { dateOffset: -5, status: 'Late', lateMinutes: 25, earlyMinutes: 0, clockIn: '06:25:00', clockOut: '14:10:00', scheduledShift: 'Afternoon' },
    { dateOffset: -4, status: 'Present', lateMinutes: 0, earlyMinutes: 0, clockIn: '13:58:00', clockOut: '22:02:00', scheduledShift: 'Afternoon' },
    { dateOffset: -3, status: 'EarlyDeparture', lateMinutes: 0, earlyMinutes: 45, clockIn: '13:55:00', clockOut: '21:15:00', scheduledShift: 'Night' },
    { dateOffset: -2, status: 'Absent', lateMinutes: 0, earlyMinutes: 0, clockIn: null, clockOut: null, scheduledShift: 'Morning' },
    { dateOffset: -1, status: 'AWOL', lateMinutes: 0, earlyMinutes: 0, clockIn: null, clockOut: null, scheduledShift: 'Afternoon' },
    { dateOffset: 0, status: 'Present', lateMinutes: 3, earlyMinutes: 0, clockIn: '06:03:00', clockOut: '14:00:00', scheduledShift: 'Morning' },
  ];

  scenarios.forEach((s) => {
    const date = new Date(now);
    date.setDate(date.getDate() + s.dateOffset);

    records.push({
      id: generateId(),
      employeeId,
      employeeName: displayName,
      displayName,
      departmentId: 'DEPT-PROD',
      departmentName: 'Production',
      date: date.toISOString().split('T')[0],
      scheduledShift: s.scheduledShift,
      shiftStartTime: s.scheduledShift === 'Morning' ? '06:00' : s.scheduledShift === 'Afternoon' ? '14:00' : '22:00',
      shiftEndTime: s.scheduledShift === 'Morning' ? '14:00' : s.scheduledShift === 'Afternoon' ? '22:00' : '06:00',
      clockInTime: s.clockIn,
      clockOutTime: s.clockOut,
      status: s.status,
      lateMinutes: s.lateMinutes,
      earlyMinutes: s.earlyMinutes,
      totalHours: s.clockIn && s.clockOut ? calculateHours(s.clockIn, s.clockOut) : 0,
      notes: s.status === 'AWOL' ? 'No contact received — AWOL pattern detected.' : null,
      correctedBy: null,
      correctionReason: null,
    });
  });

  return records;
}

/**
 * Creates attendance records for multiple employees (for list/table testing).
 */
export function createMultipleEmployeeRecords(count: number): AttendanceRecord[] {
  const records: AttendanceRecord[] = [];
  const firstNames = ['Thabo', 'Nomsa', 'Sipho', 'Zanele', 'Mandla', 'Pulane', 'Kagiso', 'Lerato'];
  const lastNames = ['Dlamini', 'Ndlovu', 'Mokoena', 'Nkosi', 'Mthembu', 'Zulu', 'Shabalala', 'Motaung'];
  const statuses: AttendanceRecord['status'][] = ['Present', 'Late', 'EarlyDeparture', 'Absent', 'AWOL', 'Present', 'Present'];

  for (let i = 0; i < count; i++) {
    const empId = `EMP${(i + 1).toString().padStart(3, '0')}`;
    const displayName = `${firstNames[i % firstNames.length]} ${lastNames[i % lastNames.length]}`;
    const today = new Date().toISOString().split('T')[0];
    const status = statuses[i % statuses.length];

    records.push({
      id: generateId(),
      employeeId: empId,
      employeeName: displayName,
      displayName,
      departmentId: 'DEPT-PROD',
      departmentName: 'Production',
      date: today,
      scheduledShift: ['Morning', 'Afternoon', 'Night'][i % 3],
      shiftStartTime: ['06:00', '14:00', '22:00'][i % 3],
      shiftEndTime: ['14:00', '22:00', '06:00'][i % 3],
      clockInTime: status === 'Present' || status === 'Late' ? '06:00:00' : null,
      clockOutTime: status === 'Present' ? '14:00:00' : null,
      status,
      lateMinutes: status === 'Late' ? 25 : 0,
      earlyMinutes: status === 'EarlyDeparture' ? 30 : 0,
      totalHours: status === 'Present' ? 8.0 : 0,
      notes: null,
      correctedBy: null,
      correctionReason: null,
    });
  }

  return records;
}

/**
 * Creates edge-case attendance records for UI state testing.
 */
export function createEdgeCaseAttendanceRecords(): AttendanceRecord[] {
  const now = new Date();
  const today = now.toISOString().split('T')[0];

  return [
    // Midnight crossing — Night shift
    {
      id: generateId(),
      employeeId: 'EMP001',
      employeeName: 'Test Employee',
      displayName: 'Test Employee',
      departmentId: 'DEPT-PROD',
      departmentName: 'Production',
      date: today,
      scheduledShift: 'Night',
      shiftStartTime: '22:00',
      shiftEndTime: '06:00',
      clockInTime: '22:30:00',
      clockOutTime: '06:05:00',
      status: 'Late',
      lateMinutes: 30,
      earlyMinutes: 5,
      totalHours: 7.75,
      notes: null,
      correctedBy: null,
      correctionReason: null,
    },
    // Missing clock-out
    {
      id: generateId(),
      employeeId: 'EMP002',
      employeeName: 'Missing Clock Out',
      displayName: 'Missing Clock Out',
      departmentId: 'DEPT-PROD',
      departmentName: 'Production',
      date: today,
      scheduledShift: 'Morning',
      shiftStartTime: '06:00',
      shiftEndTime: '14:00',
      clockInTime: '05:58:00',
      clockOutTime: null,
      status: 'MissingClockOut',
      lateMinutes: 0,
      earlyMinutes: 0,
      totalHours: 0,
      notes: 'Clock-out not recorded.',
      correctedBy: null,
      correctionReason: null,
    },
  ];
}

// ─── Attendance Summaries ────────────────────────────────────────────────────

/**
 * Creates a mock attendance summary for a department.
 */
export function createDepartmentAttendanceSummary(): AttendanceSummary {
  return {
    departmentId: 'DEPT-PROD',
    departmentName: 'Production',
    totalEmployees: 12,
    presentCount: 9,
    lateCount: 2,
    absentCount: 1,
    awolCount: 0,
    sickLeaveCount: 0,
    familyResponsibilityCount: 0,
    annualLeaveCount: 0,
    attendancePercentage: 75.0,
    totalLateMinutes: 45,
    totalEarlyMinutes: 30,
    totalLostMinutes: 75,
  };
}

/**
 * Creates summaries for all departments (for dashboard testing).
 */
export function createAllDepartmentSummaries(): AttendanceSummary[] {
  return [
    {
      departmentId: 'DEPT-PROD',
      departmentName: 'Production',
      totalEmployees: 12,
      presentCount: 9,
      lateCount: 2,
      absentCount: 1,
      awolCount: 0,
      sickLeaveCount: 0,
      familyResponsibilityCount: 0,
      annualLeaveCount: 0,
      attendancePercentage: 75.0,
      totalLateMinutes: 45,
      totalEarlyMinutes: 30,
      totalLostMinutes: 75,
    },
    {
      departmentId: 'DEPT-QA',
      departmentName: 'Quality Assurance',
      totalEmployees: 10,
      presentCount: 10,
      lateCount: 0,
      absentCount: 0,
      awolCount: 0,
      sickLeaveCount: 0,
      familyResponsibilityCount: 0,
      annualLeaveCount: 0,
      attendancePercentage: 100.0,
      totalLateMinutes: 0,
      totalEarlyMinutes: 0,
      totalLostMinutes: 0,
    },
    {
      departmentId: 'DEPT-LOG',
      departmentName: 'Logistics & Warehouse',
      totalEmployees: 10,
      presentCount: 7,
      lateCount: 2,
      absentCount: 1,
      awolCount: 0,
      sickLeaveCount: 0,
      familyResponsibilityCount: 0,
      annualLeaveCount: 0,
      attendancePercentage: 70.0,
      totalLateMinutes: 60,
      totalEarlyMinutes: 15,
      totalLostMinutes: 75,
    },
    {
      departmentId: 'DEPT-MAINT',
      departmentName: 'Maintenance & Engineering',
      totalEmployees: 10,
      presentCount: 8,
      lateCount: 1,
      absentCount: 1,
      awolCount: 0,
      sickLeaveCount: 0,
      familyResponsibilityCount: 0,
      annualLeaveCount: 0,
      attendancePercentage: 80.0,
      totalLateMinutes: 20,
      totalEarlyMinutes: 0,
      totalLostMinutes: 20,
    },
    {
      departmentId: 'DEPT-ADMIN',
      departmentName: 'Administration & HR',
      totalEmployees: 8,
      presentCount: 7,
      lateCount: 1,
      absentCount: 0,
      awolCount: 0,
      sickLeaveCount: 0,
      familyResponsibilityCount: 0,
      annualLeaveCount: 0,
      attendancePercentage: 87.5,
      totalLateMinutes: 10,
      totalEarlyMinutes: 0,
      totalLostMinutes: 10,
    },
  ];
}

// ─── Shift Schedules ─────────────────────────────────────────────────────────

/**
 * Mock shift schedule data for a week.
 */
export function createWeeklyShiftSchedule(): Array<{
  employeeId: string;
  employeeName: string;
  date: string;
  shiftCode: string;
  shiftName: string;
  startTime: string;
  endTime: string;
}> {
  const schedule: Array<{
    employeeId: string;
    employeeName: string;
    date: string;
    shiftCode: string;
    shiftName: string;
    startTime: string;
    endTime: string;
  }> = [];

  const employees = [
    { id: 'EMP001', name: 'Thabo Dlamini' },
    { id: 'EMP002', name: 'Nomsa Ndlovu' },
    { id: 'EMP003', name: 'Sipho Mokoena' },
  ];

  const shifts = [
    { code: 'ES', name: 'Morning', start: '06:00', end: '14:00' },
    { code: 'MS', name: 'Afternoon', start: '14:00', end: '22:00' },
    { code: 'NS', name: 'Night', start: '22:00', end: '06:00' },
  ];

  const now = new Date();
  for (let day = 0; day < 7; day++) {
    const date = new Date(now);
    date.setDate(date.getDate() - day);
    const dateStr = date.toISOString().split('T')[0];

    employees.forEach((emp, empIdx) => {
      const shift = shifts[(empIdx + day) % 3];
      schedule.push({
        employeeId: emp.id,
        employeeName: emp.name,
        date: dateStr,
        shiftCode: shift.code,
        shiftName: shift.name,
        startTime: shift.start,
        endTime: shift.end,
      });
    });
  }

  return schedule;
}

// ─── Helper Functions ────────────────────────────────────────────────────────

/**
 * Calculate total hours between two time strings (HH:mm:ss).
 */
function calculateHours(clockIn: string, clockOut: string): number {
  const [inH, inM, inS] = clockIn.split(':').map(Number);
  const [outH, outM, outS] = clockOut.split(':').map(Number);

  let inMinutes = inH * 60 + inM + inS / 60;
  let outMinutes = outH * 60 + outM + outS / 60;

  if (outMinutes <= inMinutes) {
    outMinutes += 24 * 60; // Crosses midnight
  }

  return Math.round(((outMinutes - inMinutes) / 60) * 100) / 100;
}
