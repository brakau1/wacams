// ============================================================================
// WACAMS Frontend — Escalation Fixtures for Testing
// ============================================================================
// Mock escalation chains, timelines, and overdue scenarios.

import type { Escalation, EscalationStatus } from '@/core/api/types';

const generateId = (): string => `ESC-${Math.random().toString(36).substring(2, 9)}`;

// ─── Complete Escalation Chain ───────────────────────────────────────────────

/**
 * Creates a full escalation chain across 3 levels for a FinalWrittenWarning case.
 */
export function createCompleteEscalationChain(): Escalation[] {
  const now = new Date();

  return [
    // Level 1 — Direct Supervisor (resolved)
    {
      id: generateId(),
      relatedExceptionId: 'EXC-001',
      relatedWarningId: 'WRN-004',
      fromEmployeeId: 'EMP005',
      fromEmployeeName: 'Samu Nkosi',
      toEmployeeId: 'EMP002',
      toEmployeeName: 'Botlhale Rakau',
      toRole: 'Manager',
      escalationLevel: 1,
      status: 'Completed',
      message: 'FinalWrittenWarning issued for employee EMP005 — 4th attendance exception.',
      respondedAt: new Date(now.getTime() - 5 * 24 * 60 * 60 * 1000).toISOString(), // 5 days ago
      respondedBy: 'EMP002',
      responseNotes: 'Supervisor reviewed case with employee. Improvement plan signed.',
      deadline: new Date(now.getTime() - 6 * 24 * 60 * 60 * 1000).toISOString(),
      completedAt: new Date(now.getTime() - 5 * 24 * 60 * 60 * 1000).toISOString(),
      createdAt: new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000).toISOString(),
    },
    // Level 2 — Production Manager (completed after auto-escalation)
    {
      id: generateId(),
      relatedExceptionId: 'EXC-001',
      relatedWarningId: 'WRN-004',
      fromEmployeeId: 'EMP002',
      fromEmployeeName: 'Botlhale Rakau',
      toEmployeeId: 'EMP009',
      toEmployeeName: 'Jeremiah Bezuidenhout',
      toRole: 'SeniorManager',
      escalationLevel: 2,
      status: 'Completed',
      message: 'Auto-escalated from Level 1 after 24h timeout. Production manager review required.',
      respondedAt: new Date(now.getTime() - 3 * 24 * 60 * 60 * 1000).toISOString(),
      respondedBy: 'EMP009',
      responseNotes: 'Production manager approved disciplinary action. Case documented.',
      deadline: new Date(now.getTime() - 4 * 24 * 60 * 60 * 1000).toISOString(),
      completedAt: new Date(now.getTime() - 3 * 24 * 60 * 60 * 1000).toISOString(),
      createdAt: new Date(now.getTime() - 5 * 24 * 60 * 60 * 1000).toISOString(),
    },
    // Level 3 — HR Director (pending)
    {
      id: generateId(),
      relatedExceptionId: 'EXC-001',
      relatedWarningId: 'WRN-004',
      fromEmployeeId: 'EMP009',
      fromEmployeeName: 'Jeremiah Bezuidenhout',
      toEmployeeId: 'EMP043',
      toEmployeeName: 'Nomvula Motaung',
      toRole: 'Coordinator',
      escalationLevel: 3,
      status: 'Pending',
      message: 'Final escalation level — HR Director final review required.',
      respondedAt: null,
      respondedBy: null,
      responseNotes: null,
      deadline: new Date(now.getTime() + 19 * 24 * 60 * 60 * 1000).toISOString(), // 19 hours remaining
      completedAt: null,
      createdAt: new Date(now.getTime() - 3 * 24 * 60 * 60 * 1000).toISOString(),
    },
  ];
}

// ─── Overdue Escalation Scenarios ────────────────────────────────────────────

/**
 * Creates an overdue escalation that should trigger auto-escalation.
 * This is Pending at Level 1 for more than 24 hours.
 */
export function createOverdueEscalation(): Escalation {
  const now = new Date();

  return {
    id: generateId(),
    relatedExceptionId: 'EXC-010',
    relatedWarningId: 'WRN-010',
    fromEmployeeId: 'EMP006',
    fromEmployeeName: 'Clean Operator',
    toEmployeeId: 'EMP002',
    toEmployeeName: 'Botlhale Rakau',
    toRole: 'Manager',
    escalationLevel: 1,
    status: 'Pending',
    message: 'VerbalWarning issued — supervisor acknowledgment required within 24 hours.',
    respondedAt: null,
    respondedBy: null,
    responseNotes: null,
    deadline: new Date(now.getTime() - 2 * 60 * 60 * 1000).toISOString(), // 2 hours PAST deadline
    completedAt: null,
    createdAt: new Date(now.getTime() - 28 * 60 * 60 * 1000).toISOString(), // 28 hours ago
  };
}

/**
 * Creates a list of all pending escalations that are overdue.
 */
export function createOverdueEscalationsList(): Escalation[] {
  const now = new Date();
  const overdueEscalations: Escalation[] = [];

  // 3 different employees with overdue escalations
  for (let i = 0; i < 3; i++) {
    overdueEscalations.push({
      id: generateId(),
      relatedExceptionId: `EXC-${String(100 + i).padStart(3, '0')}`,
      relatedWarningId: `WRN-${String(100 + i).padStart(3, '0')}`,
      fromEmployeeId: `EMP${String(5 + i).padStart(3, '0')}`,
      fromEmployeeName: `Operator ${i + 1}`,
      toEmployeeId: 'EMP002',
      toEmployeeName: 'Botlhale Rakau',
      toRole: 'Manager',
      escalationLevel: 1,
      status: 'Pending' as EscalationStatus,
      message: `Overdue escalation #${i + 1} — exceeds 24-hour deadline.`,
      respondedAt: null,
      respondedBy: null,
      responseNotes: null,
      deadline: new Date(now.getTime() - (2 + i) * 60 * 60 * 1000).toISOString(),
      completedAt: null,
      createdAt: new Date(now.getTime() - (26 + i * 2) * 60 * 60 * 1000).toISOString(),
    });
  }

  return overdueEscalations;
}

// ─── In-Progress Escalation ──────────────────────────────────────────────────

/**
 * Creates an escalation that has been accepted but action is still in progress.
 */
export function createInProgressEscalation(): Escalation {
  const now = new Date();

  return {
    id: generateId(),
    relatedExceptionId: 'EXC-020',
    relatedWarningId: 'WRN-020',
    fromEmployeeId: 'EMP015',
    fromEmployeeName: 'Refilwe Pilane',
    toEmployeeId: 'EMP009',
    toEmployeeName: 'Jeremiah Bezuidenhout',
    toRole: 'SeniorManager',
    escalationLevel: 2,
    status: 'InProgress',
    message: 'WrittenWarning — production manager gathering evidence for disciplinary hearing.',
    respondedAt: new Date(now.getTime() - 12 * 60 * 60 * 1000).toISOString(), // 12 hours ago
    respondedBy: 'EMP009',
    responseNotes: 'Manager accepted escalation. Scheduling hearing for tomorrow.',
    deadline: new Date(now.getTime() + 12 * 60 * 60 * 1000).toISOString(), // 12 hours remaining
    completedAt: null,
    createdAt: new Date(now.getTime() - 24 * 60 * 60 * 1000).toISOString(),
  };
}

// ─── All Escalations Combined ────────────────────────────────────────────────

/**
 * Returns a comprehensive set of escalations covering all states.
 */
export function getAllEscalations(): Escalation[] {
  return [
    ...createCompleteEscalationChain(),
    createOverdueEscalation(),
    ...createOverdueEscalationsList(),
    createInProgressEscalation(),
  ];
}

/**
 * Filters escalations by status.
 */
export function filterEscalationsByStatus(status: EscalationStatus): Escalation[] {
  return getAllEscalations().filter((e) => e.status === status);
}
