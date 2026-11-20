// ============================================================================
// WACAMS Frontend — Warning Letter Fixtures for Testing
// ============================================================================
// Mock warning letters, counts, and progressive discipline scenarios.

import type { WarningLetter, WarningType } from '@/core/api/types';

const generateId = (): string => `WRN-${Math.random().toString(36).substring(2, 9)}`;

// ─── Progressive Discipline Chain ────────────────────────────────────────────

/**
 * Creates a full progressive discipline chain for an employee who has received
 * all 4 levels of warnings (Counselling → VerbalWarning → WrittenWarning → FinalWrittenWarning).
 */
export function createProgressiveDisciplineChain(): WarningLetter[] {
  const now = new Date();

  return [
    // 1st — Counselling (most recent, acknowledged)
    {
      id: generateId(),
      employeeId: 'EMP005',
      employeeName: 'Samu Nkosi',
      displayName: 'Samu Nkosi',
      departmentId: 'DEPT-PROD',
      departmentName: 'Production',
      warningType: 'Counselling',
      relatedExceptionId: 'EXC-001',
      title: 'First Late Arrival — Counselling Session',
      description: 'Employee arrived 25 minutes late without prior notice. First disciplinary action issued.',
      severity: 1,
      signedFormUrl: '/signatures/warning_emp005_001.pdf',
      signedBy: 'EMP002',
      signedAt: new Date(now.getTime() - 28 * 24 * 60 * 60 * 1000).toISOString(),
      issuedBy: 'EMP002',
      issuedByName: 'Botlhale Rakau',
      status: 'Acknowledged',
      escalationLevel: 0,
      createdAt: new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000).toISOString(),
      updatedAt: new Date(now.getTime() - 28 * 24 * 60 * 60 * 1000).toISOString(),
    },
    // 2nd — VerbalWarning (acknowledged)
    {
      id: generateId(),
      employeeId: 'EMP005',
      employeeName: 'Samu Nkosi',
      displayName: 'Samu Nkosi',
      departmentId: 'DEPT-PROD',
      departmentName: 'Production',
      warningType: 'VerbalWarning',
      relatedExceptionId: 'EXC-002',
      title: 'Repeat Late Arrival — Verbal Warning',
      description: 'Second late arrival within 90 days. Arrived 45 minutes late despite counselling session.',
      severity: 2,
      signedFormUrl: '/signatures/warning_emp005_002.pdf',
      signedBy: 'EMP002',
      signedAt: new Date(now.getTime() - 58 * 24 * 60 * 60 * 1000).toISOString(),
      issuedBy: 'EMP002',
      issuedByName: 'Botlhale Rakau',
      status: 'Acknowledged',
      escalationLevel: 0,
      createdAt: new Date(now.getTime() - 60 * 24 * 60 * 60 * 1000).toISOString(),
      updatedAt: new Date(now.getTime() - 58 * 24 * 60 * 60 * 1000).toISOString(),
    },
    // 3rd — WrittenWarning (unacknowledged)
    {
      id: generateId(),
      employeeId: 'EMP005',
      employeeName: 'Samu Nkosi',
      displayName: 'Samu Nkosi',
      departmentId: 'DEPT-PROD',
      departmentName: 'Production',
      warningType: 'WrittenWarning',
      relatedExceptionId: 'EXC-003',
      title: 'Third Offence — Written Warning',
      description: 'Third disciplinary action. Pattern of lateness continues despite verbal warning. Arrived 60 minutes late.',
      severity: 3,
      signedFormUrl: '/signatures/warning_emp005_003.pdf',
      signedBy: null,
      signedAt: null,
      issuedBy: 'EMP002',
      issuedByName: 'Botlhale Rakau',
      status: 'Issued',
      escalationLevel: 0,
      createdAt: new Date(now.getTime() - 120 * 24 * 60 * 60 * 1000).toISOString(),
      updatedAt: new Date(now.getTime() - 120 * 24 * 60 * 60 * 1000).toISOString(),
    },
    // 4th — FinalWrittenWarning (escalated)
    {
      id: generateId(),
      employeeId: 'EMP005',
      employeeName: 'Samu Nkosi',
      displayName: 'Samu Nkosi',
      departmentId: 'DEPT-PROD',
      departmentName: 'Production',
      warningType: 'FinalWrittenWarning',
      relatedExceptionId: 'EXC-004',
      title: 'Fourth Escalation — Final Written Warning',
      description: 'Fourth escalation. Final written warning issued due to continued attendance violations. Next offence may result in termination hearing.',
      severity: 4,
      signedFormUrl: '/signatures/warning_emp005_004.pdf',
      signedBy: 'EMP002',
      signedAt: new Date(now.getTime() - 178 * 24 * 60 * 60 * 1000).toISOString(),
      issuedBy: 'EMP002',
      issuedByName: 'Botlhale Rakau',
      status: 'Acknowledged',
      escalationLevel: 1,
      createdAt: new Date(now.getTime() - 180 * 24 * 60 * 60 * 1000).toISOString(),
      updatedAt: new Date(now.getTime() - 178 * 24 * 60 * 60 * 1000).toISOString(),
    },
  ];
}

// ─── Expired Warnings ────────────────────────────────────────────────────────

/**
 * Creates warnings that are beyond their validity period (should not count toward progression).
 */
export function createExpiredWarnings(): WarningLetter[] {
  const now = new Date();

  return [
    {
      id: generateId(),
      employeeId: 'EMP010',
      employeeName: 'Clean Record Employee',
      displayName: 'Clean Record Employee',
      departmentId: 'DEPT-QA',
      departmentName: 'Quality Assurance',
      warningType: 'Counselling',
      relatedExceptionId: 'EXC-OLD-001',
      title: 'Old Counselling Record',
      description: 'Counselling issued more than 90 days ago — expired per policy.',
      severity: 1,
      signedFormUrl: '/signatures/warning_expired_001.pdf',
      signedBy: 'EMP002',
      signedAt: new Date(now.getTime() - 100 * 24 * 60 * 60 * 1000).toISOString(),
      issuedBy: 'EMP002',
      issuedByName: 'Botlhale Rakau',
      status: 'Archived',
      escalationLevel: 0,
      createdAt: new Date(now.getTime() - 100 * 24 * 60 * 60 * 1000).toISOString(),
      updatedAt: new Date(now.getTime() - 100 * 24 * 60 * 60 * 1000).toISOString(),
    },
    {
      id: generateId(),
      employeeId: 'EMP010',
      employeeName: 'Clean Record Employee',
      displayName: 'Clean Record Employee',
      departmentId: 'DEPT-QA',
      departmentName: 'Quality Assurance',
      warningType: 'VerbalWarning',
      relatedExceptionId: 'EXC-OLD-002',
      title: 'Old Verbal Warning',
      description: 'Verbal warning issued more than 120 days ago — expired per policy.',
      severity: 2,
      signedFormUrl: '/signatures/warning_expired_002.pdf',
      signedBy: 'EMP002',
      signedAt: new Date(now.getTime() - 130 * 24 * 60 * 60 * 1000).toISOString(),
      issuedBy: 'EMP002',
      issuedByName: 'Botlhale Rakau',
      status: 'Archived',
      escalationLevel: 0,
      createdAt: new Date(now.getTime() - 130 * 24 * 60 * 60 * 1000).toISOString(),
      updatedAt: new Date(now.getTime() - 130 * 24 * 60 * 60 * 1000).toISOString(),
    },
  ];
}

// ─── Draft Warning (not yet issued) ──────────────────────────────────────────

/**
 * Creates a draft warning that hasn't been formally issued yet.
 */
export function createDraftWarning(): WarningLetter {
  return {
    id: generateId(),
    employeeId: 'EMP020',
    employeeName: 'Refilwe Pilane',
    displayName: 'Refilwe Pilane',
    departmentId: 'DEPT-LOG',
    departmentName: 'Logistics & Warehouse',
    warningType: 'WrittenWarning',
    relatedExceptionId: 'EXC-DRAFT-001',
    title: 'Pending Written Warning',
    description: 'Draft warning prepared pending manager investigation completion.',
    severity: 3,
    signedFormUrl: null,
    signedBy: null,
    signedAt: null,
    issuedBy: 'EMP023',
    issuedByName: 'Thabo Dlamini',
    status: 'Draft',
    escalationLevel: 0,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  };
}

// ─── Warning Counts for Dashboard ────────────────────────────────────────────

/**
 * Returns warning counts by type for a given employee.
 */
export function getWarningCounts(employeeId: string): Record<WarningType, number> {
  const allWarnings = [...createProgressiveDisciplineChain(), ...createExpiredWarnings()];
  const activeWarnings = allWarnings.filter((w) => w.employeeId === employeeId && w.status !== 'Archived');

  const counts: Record<WarningType, number> = {
    Counselling: 0,
    VerbalWarning: 0,
    WrittenWarning: 0,
    FinalWrittenWarning: 0,
  };

  activeWarnings.forEach((w) => {
    if (counts[w.warningType] !== undefined) {
      counts[w.warningType]++;
    }
  });

  return counts;
}

// ─── All Warnings Combined ───────────────────────────────────────────────────

/**
 * Returns a comprehensive set of warning letters covering all states.
 */
export function getAllWarnings(): WarningLetter[] {
  return [...createProgressiveDisciplineChain(), ...createExpiredWarnings(), createDraftWarning()];
}

/**
 * Filters warnings by status.
 */
export function filterWarningsByStatus(status: WarningLetter['status']): WarningLetter[] {
  return getAllWarnings().filter((w) => w.status === status);
}

/**
 * Gets the next expected warning type based on progressive discipline rules.
 */
export function getNextExpectedWarning(activeWarningCount: number): WarningType | null {
  switch (activeWarningCount) {
    case 0:
      return 'Counselling';
    case 1:
      return 'VerbalWarning';
    case 2:
      return 'WrittenWarning';
    case 3:
      return 'FinalWrittenWarning';
    default:
      return null; // Max level reached
  }
}
