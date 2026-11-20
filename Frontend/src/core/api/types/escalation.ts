// Escalation types aligned with backend model

export type EscalationLevel = 
  | 'ManagerLevel1'
  | 'ManagerLevel2'
  | 'ManagerLevel3'
  | 'ManagerLevel4'
  | 'ManagerLevel5'
  | 'HR'
  | 'Admin';

export type EscalationStatus = 
  | 'Pending'
  | 'Acknowledged'
  | 'ActionTaken'
  | 'Escalated'
  | 'AutoClosed'
  | 'Resolved';

export interface Escalation {
  id: string;
  exceptionId: string;
  employeeId: string;
  currentLevel: EscalationLevel;
  assignedTo: string;
  status: EscalationStatus;
  createdAt: string;
  acknowledgedAt?: string;
  actionTakenAt?: string;
  escalatedAt?: string;
  resolvedAt?: string;
  autoClosedAt?: string;
  nextEscalationTime: string;
  documentUploaded: boolean;
  documentUrl?: string;
  documentUploadedAt?: string;
  notes?: string;
  actionTakenNotes?: string;
  createdBy: string;
  updatedBy?: string;
  updatedAt?: string;
  history?: EscalationHistory[];
}

export interface EscalationHistory {
  id: string;
  escalationId: string;
  fromLevel?: EscalationLevel;
  toLevel?: EscalationLevel;
  fromManagerId?: string;
  fromManagerName?: string;
  toManagerId?: string;
  toManagerName?: string;
  action: string;
  comments?: string;
  timestamp: string;
}

export interface EscalationDashboardStats {
  totalActive: number;
  pendingCount: number;
  acknowledgedCount: number;
  overdueCount: number;
  resolvedToday: number;
  avgResolutionHours: number;
  byLevel: Record<EscalationLevel, number>;
}
