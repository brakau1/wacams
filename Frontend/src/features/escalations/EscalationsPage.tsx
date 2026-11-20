import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import api from '@/core/api/apiClient';
import EscalationCard from './components/EscalationCard';
import { EscalationStats } from './components/EscalationStats';
import { LoadingSpinner } from '@/shared/components/LoadingSpinner';
import EmptyState from '@/shared/components/EmptyState';
import { Search, Filter } from 'lucide-react';
import type { Escalation, EscalationStatus, EscalationLevel } from '@/core/api/types/escalation';

export default function EscalationsPage() {
  const [filterStatus, setFilterStatus] = useState<EscalationStatus | 'all'>('all');
  const [filterLevel, setFilterLevel] = useState<EscalationLevel | 'all'>('all');
  const [searchTerm, setSearchTerm] = useState('');

  const { data: escalations, isLoading } = useQuery({
    queryKey: ['escalations', filterStatus, filterLevel],
    queryFn: async () => {
      const params = new URLSearchParams();
      if (filterStatus !== 'all') params.set('status', filterStatus);
      if (filterLevel !== 'all') params.set('level', filterLevel);
      const response = await api.get(`/api/escalations?${params}`);
      return response.data;
    },
  });

  const { data: stats } = useQuery({
    queryKey: ['escalation-stats'],
    queryFn: async () => {
      const response = await api.get('/api/escalations/stats');
      return response.data;
    },
  });

  const filteredEscalations = (escalations ?? []).filter((e: Escalation) =>
    searchTerm === '' ||
    e.employeeId.toLowerCase().includes(searchTerm.toLowerCase()) ||
    e.assignedTo.toLowerCase().includes(searchTerm.toLowerCase())
  );

  if (isLoading) return <LoadingSpinner />;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Escalation Management</h1>
          <p className="text-muted-foreground mt-1">
            Monitor and manage attendance exception escalations
          </p>
        </div>
      </div>

      {/* Stats Cards */}
      {stats && <EscalationStats stats={stats} />}

      {/* Filters */}
      <div className="bg-card rounded-lg border p-4 space-y-4">
        <div className="flex items-center gap-2">
          <Filter className="w-5 h-5 text-muted-foreground" />
          <h2 className="font-semibold">Filters</h2>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {/* Search */}
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
            <input
              type="text"
              placeholder="Search by employee or manager..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full pl-10 pr-4 py-2 rounded-md border bg-background"
            />
          </div>

          {/* Status Filter */}
          <select
            value={filterStatus}
            onChange={(e) => setFilterStatus(e.target.value as any)}
            className="px-4 py-2 rounded-md border bg-background"
          >
            <option value="all">All Statuses</option>
            <option value="Pending">Pending</option>
            <option value="Acknowledged">Acknowledged</option>
            <option value="ActionTaken">Action Taken</option>
            <option value="Escalated">Escalated</option>
            <option value="AutoClosed">Auto Closed</option>
            <option value="Resolved">Resolved</option>
          </select>

          {/* Level Filter */}
          <select
            value={filterLevel}
            onChange={(e) => setFilterLevel(e.target.value as any)}
            className="px-4 py-2 rounded-md border bg-background"
          >
            <option value="all">All Levels</option>
            <option value="ManagerLevel1">Manager Level 1</option>
            <option value="ManagerLevel2">Manager Level 2</option>
            <option value="ManagerLevel3">Manager Level 3</option>
            <option value="ManagerLevel4">Manager Level 4</option>
            <option value="ManagerLevel5">Manager Level 5</option>
            <option value="HR">HR</option>
            <option value="Admin">Admin</option>
          </select>
        </div>
      </div>

      {/* Escalation List */}
      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-xl font-semibold">
            Active Escalations ({filteredEscalations.length})
          </h2>
        </div>

        {filteredEscalations.length === 0 ? (
          <EmptyState
            title="No Active Escalations"
            description="All exceptions have been resolved or are within their escalation timeframe."
          />
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {filteredEscalations.map((escalation: Escalation) => (
              <EscalationCard key={escalation.id} escalation={escalation} />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
