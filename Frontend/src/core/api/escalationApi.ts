import api from './apiClient';
import type { Escalation, EscalationDashboardStats } from './types/escalation';

export const escalationApi = {
  getAll: async (filters?: { status?: string; level?: string }): Promise<Escalation[]> => {
    const params = new URLSearchParams();
    if (filters?.status) params.append('status', filters.status);
    if (filters?.level) params.append('level', filters.level);
    
    const response = await api.get(`/api/escalations?${params}`);
    return response.data;
  },

  getById: async (id: string): Promise<Escalation> => {
    const response = await api.get(`/api/escalations/${id}`);
    return response.data;
  },

  getByException: async (exceptionId: string): Promise<Escalation[]> => {
    const response = await api.get(`/api/escalations/exception/${exceptionId}`);
    return response.data;
  },

  acknowledge: async (id: string, acknowledgedBy: string): Promise<Escalation> => {
    const response = await api.post(`/api/escalations/${id}/acknowledge`, { acknowledgedBy });
    return response.data;
  },

  markActionTaken: async (id: string, actionNotes: string, updatedBy: string): Promise<Escalation> => {
    const response = await api.post(`/api/escalations/${id}/action-taken`, { 
      actionNotes, 
      updatedBy 
    });
    return response.data;
  },

  escalate: async (id: string, escalatedBy: string): Promise<Escalation> => {
    const response = await api.post(`/api/escalations/${id}/escalate`, { escalatedBy });
    return response.data;
  },

  uploadDocument: async (id: string, documentUrl: string, uploadedBy: string): Promise<Escalation> => {
    const response = await api.post(`/api/escalations/${id}/upload-document`, { 
      documentUrl, 
      uploadedBy 
    });
    return response.data;
  },

  resolve: async (id: string, resolvedBy: string): Promise<Escalation> => {
    const response = await api.post(`/api/escalations/${id}/resolve`, { resolvedBy });
    return response.data;
  },

  getStats: async (): Promise<EscalationDashboardStats> => {
    const response = await api.get('/api/escalations/stats');
    return response.data;
  },
};
