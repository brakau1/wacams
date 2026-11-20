// ============================================================================
// WACAMS Frontend — Dashboard Fixtures for Testing
// ============================================================================
// Mock KPI data, trend data, and department statistics for dashboard UI testing.

import type {
  DashboardKPI,
  DepartmentKPI,
  TrendPoint,
  TopLateEntry,
  AttendanceException,
  HRCommandCentreData,
  ExecutiveSummary,
  DepartmentHeatmap,
  DepartmentLeaderboard,
} from '@/core/api/types';

const generateId = (): string => `DASH-${Math.random().toString(36).substring(2, 9)}`;

// ─── Dashboard KPI Data ──────────────────────────────────────────────────────

/**
 * Creates a realistic set of dashboard KPIs for today's attendance overview.
 */
export function createDashboardKPI(): DashboardKPI {
  return {
    totalEmployees: 50,
    onTimePercentage: 78.5,
    averageLateMinutes: 22.4,
    activeExceptions: 7,
    pendingWarnings: 3,
    departmentStats: [
      {
        departmentId: 'DEPT-PROD',
        departmentName: 'Production',
        attendancePercentage: 75.0,
        totalEmployees: 12,
        presentCount: 9,
        lateCount: 2,
        absentCount: 1,
      },
      {
        departmentId: 'DEPT-QA',
        departmentName: 'Quality Assurance',
        attendancePercentage: 100.0,
        totalEmployees: 10,
        presentCount: 10,
        lateCount: 0,
        absentCount: 0,
      },
      {
        departmentId: 'DEPT-LOG',
        departmentName: 'Logistics & Warehouse',
        attendancePercentage: 70.0,
        totalEmployees: 10,
        presentCount: 7,
        lateCount: 2,
        absentCount: 1,
      },
      {
        departmentId: 'DEPT-MAINT',
        departmentName: 'Maintenance & Engineering',
        attendancePercentage: 80.0,
        totalEmployees: 10,
        presentCount: 8,
        lateCount: 1,
        absentCount: 1,
      },
      {
        departmentId: 'DEPT-ADMIN',
        departmentName: 'Administration & HR',
        attendancePercentage: 87.5,
        totalEmployees: 8,
        presentCount: 7,
        lateCount: 1,
        absentCount: 0,
      },
    ],
    trendData: createTrendData(30),
    topLateArrivals: [
      {
        employeeId: 'EMP005',
        displayName: 'Samu Nkosi',
        departmentName: 'Production',
        lateCount: 8,
        totalLateMinutes: 320,
      },
      {
        employeeId: 'EMP010',
        displayName: 'Clean Record Employee',
        departmentName: 'Quality Assurance',
        lateCount: 5,
        totalLateMinutes: 180,
      },
      {
        employeeId: 'EMP020',
        displayName: 'Refilwe Pilane',
        departmentName: 'Logistics & Warehouse',
        lateCount: 4,
        totalLateMinutes: 150,
      },
    ],
    recentExceptions: [
      {
        id: generateId(),
        employeeId: 'EMP005',
        employeeName: 'Samu Nkosi',
        displayName: 'Samu Nkosi',
        departmentId: 'DEPT-PROD',
        departmentName: 'Production',
        exceptionType: 'LateArrival',
        date: new Date().toISOString().split('T')[0],
        description: 'Arrived 30 minutes late.',
        lostProductionMinutes: 30,
        status: 'Investigated',
        assignedTo: 'EMP002',
        assignedToName: 'Botlhale Rakau',
        investigatorNotes: 'Traffic delay confirmed.',
        investigatedAt: new Date().toISOString(),
        investigatedBy: 'EMP002',
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      },
      {
        id: generateId(),
        employeeId: 'EMP020',
        employeeName: 'Refilwe Pilane',
        displayName: 'Refilwe Pilane',
        departmentId: 'DEPT-LOG',
        departmentName: 'Logistics & Warehouse',
        exceptionType: 'AWOL',
        date: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
        description: 'Absent without notice — AWOL pattern detected.',
        lostProductionMinutes: 480,
        status: 'UnderReview',
        assignedTo: 'EMP023',
        assignedToName: 'Thabo Dlamini',
        investigatorNotes: null,
        investigatedAt: null,
        investigatedBy: null,
        createdAt: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000).toISOString(),
        updatedAt: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000).toISOString(),
      },
    ],
  };
}

// ─── Trend Data ──────────────────────────────────────────────────────────────

/**
 * Creates 30 days of trend data for line chart rendering.
 */
export function createTrendData(days: number): TrendPoint[] {
  const data: TrendPoint[] = [];
  const now = new Date();

  for (let i = days - 1; i >= 0; i--) {
    const date = new Date(now);
    date.setDate(date.getDate() - i);

    // Simulate realistic variation
    const onTimePct = 70 + Math.random() * 25;
    const lateCnt = Math.floor(Math.random() * 8);
    const excCnt = Math.floor(Math.random() * 10);

    data.push({
      date: date.toISOString().split('T')[0],
      onTimePercentage: Math.round(onTimePct * 10) / 10,
      lateCount: lateCnt,
      exceptionCount: excCnt,
    });
  }

  return data;
}

// ─── HR Command Centre Data ──────────────────────────────────────────────────

/**
 * Creates full HR Command Centre dashboard data including KPIs, rankings, and exception analysis.
 */
export function createHRCommandCentreData(): HRCommandCentreData {
  return {
    kpis: {
      plantAttendancePercentage: 78.5,
      lostProductionHours: 3.0,
      openWarnings: 5,
      finalWarnings: 2,
      awolCases: 3,
      escalations: 4,
      departmentsAtRisk: 2,
    },
    attendanceTrend: createTrendData(14),
    departmentRanking: [
      {
        departmentId: 'DEPT-QA',
        departmentName: 'Quality Assurance',
        attendancePercentage: 100.0,
        totalEmployees: 10,
        presentCount: 10,
        lateCount: 0,
        absentCount: 0,
        exceptionCount: 0,
        riskLevel: 'low',
      },
      {
        departmentId: 'DEPT-ADMIN',
        departmentName: 'Administration & HR',
        attendancePercentage: 87.5,
        totalEmployees: 8,
        presentCount: 7,
        lateCount: 1,
        absentCount: 0,
        exceptionCount: 1,
        riskLevel: 'low',
      },
      {
        departmentId: 'DEPT-MAINT',
        departmentName: 'Maintenance & Engineering',
        attendancePercentage: 80.0,
        totalEmployees: 10,
        presentCount: 8,
        lateCount: 1,
        absentCount: 1,
        exceptionCount: 3,
        riskLevel: 'medium',
      },
      {
        departmentId: 'DEPT-PROD',
        departmentName: 'Production',
        attendancePercentage: 75.0,
        totalEmployees: 12,
        presentCount: 9,
        lateCount: 2,
        absentCount: 1,
        exceptionCount: 8,
        riskLevel: 'high',
      },
      {
        departmentId: 'DEPT-LOG',
        departmentName: 'Logistics & Warehouse',
        attendancePercentage: 70.0,
        totalEmployees: 10,
        presentCount: 7,
        lateCount: 2,
        absentCount: 1,
        exceptionCount: 5,
        riskLevel: 'critical',
      },
    ],
    topOffenders: [
      {
        employeeId: 'EMP005',
        displayName: 'Samu Nkosi',
        departmentName: 'Production',
        lateCount: 8,
        totalLateMinutes: 320,
      },
      {
        employeeId: 'EMP010',
        displayName: 'Clean Record Employee',
        departmentName: 'Quality Assurance',
        lateCount: 5,
        totalLateMinutes: 180,
      },
      {
        employeeId: 'EMP020',
        displayName: 'Refilwe Pilane',
        departmentName: 'Logistics & Warehouse',
        lateCount: 4,
        totalLateMinutes: 150,
      },
    ],
    exceptionAnalysis: [
      { exceptionType: 'LateArrival', count: 25, percentage: 45.5, lostMinutes: 750 },
      { exceptionType: 'EarlyDeparture', count: 12, percentage: 21.8, lostMinutes: 360 },
      { exceptionType: 'AWOL', count: 8, percentage: 14.5, lostMinutes: 3840 },
      { exceptionType: 'UnplannedAbsenteeism', count: 6, percentage: 10.9, lostMinutes: 2880 },
      { exceptionType: 'MissingClockOut', count: 4, percentage: 7.3, lostMinutes: 0 },
    ],
  };
}

// ─── Executive Summary ───────────────────────────────────────────────────────

/**
 * Creates executive-level summary data for CTO/management view.
 */
export function createExecutiveSummary(): ExecutiveSummary {
  return {
    employeesPresent: 39,
    currentAbsenteeism: 11.0,
    attendancePercentage: 89.0,
    lostMinutes: 180,
    lostHours: 3.0,
    openEscalations: 4,
    pendingInvestigations: 7,
    correctiveActions: 5,
    totalActiveEmployees: 47,
    asOf: new Date().toISOString(),
  };
}

// ─── Department Heatmap ──────────────────────────────────────────────────────

/**
 * Creates heatmap data for department-level visual comparison.
 */
export function createDepartmentHeatmap(): DepartmentHeatmap[] {
  return [
    {
      departmentId: 'DEPT-PROD',
      departmentName: 'Production',
      departmentCode: 'PROD',
      totalEmployees: 12,
      presentCount: 9,
      lateCount: 2,
      absentCount: 1,
      lostMinutes: 75,
      attendancePercentage: 75.0,
      targetPercentage: 95.0,
      gapToTarget: -20.0,
      heatLevel: 'red',
    },
    {
      departmentId: 'DEPT-LOG',
      departmentName: 'Logistics & Warehouse',
      departmentCode: 'LOG',
      totalEmployees: 10,
      presentCount: 7,
      lateCount: 2,
      absentCount: 1,
      lostMinutes: 75,
      attendancePercentage: 70.0,
      targetPercentage: 93.0,
      gapToTarget: -23.0,
      heatLevel: 'red',
    },
    {
      departmentId: 'DEPT-MAINT',
      departmentName: 'Maintenance & Engineering',
      departmentCode: 'MAINT',
      totalEmployees: 10,
      presentCount: 8,
      lateCount: 1,
      absentCount: 1,
      lostMinutes: 20,
      attendancePercentage: 80.0,
      targetPercentage: 94.0,
      gapToTarget: -14.0,
      heatLevel: 'orange',
    },
    {
      departmentId: 'DEPT-ADMIN',
      departmentName: 'Administration & HR',
      departmentCode: 'ADMIN',
      totalEmployees: 8,
      presentCount: 7,
      lateCount: 1,
      absentCount: 0,
      lostMinutes: 10,
      attendancePercentage: 87.5,
      targetPercentage: 96.0,
      gapToTarget: -8.5,
      heatLevel: 'yellow',
    },
    {
      departmentId: 'DEPT-QA',
      departmentName: 'Quality Assurance',
      departmentCode: 'QA',
      totalEmployees: 10,
      presentCount: 10,
      lateCount: 0,
      absentCount: 0,
      lostMinutes: 0,
      attendancePercentage: 100.0,
      targetPercentage: 95.0,
      gapToTarget: 5.0,
      heatLevel: 'green',
    },
  ];
}

// ─── Department Leaderboard ──────────────────────────────────────────────────

/**
 * Creates leaderboard data sorted by attendance performance.
 */
export function createDepartmentLeaderboard(): DepartmentLeaderboard[] {
  const heatmap = createDepartmentHeatmap();

  return heatmap
    .sort((a, b) => b.attendancePercentage - a.attendancePercentage)
    .map((dept, index) => ({
      ...dept,
      totalLostHours: Math.round(dept.lostMinutes / 60 * 10) / 10,
      trend: index < 2 ? 'down' : index === 2 ? 'stable' : 'up',
      rank: index + 1,
      exceptionsCount: Math.floor(Math.random() * 5),
    }));
}

// ─── All Dashboard Data Combined ─────────────────────────────────────────────

/**
 * Returns all dashboard-related fixtures for comprehensive testing.
 */
export function getAllDashboardData() {
  return {
    kpi: createDashboardKPI(),
    hrCommandCentre: createHRCommandCentreData(),
    executiveSummary: createExecutiveSummary(),
    departmentHeatmap: createDepartmentHeatmap(),
    departmentLeaderboard: createDepartmentLeaderboard(),
    trendData: createTrendData(30),
  };
}
