import { useState } from 'react';
import useSWR from 'swr';
import { api } from '../../core/api/apiClient';
import { ENDPOINTS } from '../../core/api/endpoints';
import DataTable from '@/shared/components/DataTable';
import { LoadingSpinner } from '@/shared/components/LoadingSpinner';
import EmptyState from '@/shared/components/EmptyState';

interface AuditEntry {
  id: string;
  entityType: string;
  entityId: string;
  action: string;
  oldValue: string;
  newValue: string;
  performedBy: string;
  performedAt: string;
  ipAddress?: string;
}

const fetcher = async (url: string) => {
  const res = await api.get(url);
  return res.data;
};

export default function AuditLogPage() {
  const [page, setPage] = useState(1);
  const [pageSize] = useState(25);
  const [entityType, setEntityType] = useState<string>('');
  const [action, setAction] = useState<string>('');
  const [searchTerm, setSearchTerm] = useState('');

  const { data, error, isLoading } = useSWR(
    ['audit', page, pageSize, entityType, action, searchTerm],
    () => {
      const params = new URLSearchParams({
        page: String(page),
        pageSize: String(pageSize),
      });
      if (entityType) params.set('entityType', entityType);
      if (action) params.set('action', action);
      if (searchTerm) params.set('search', searchTerm);
      return fetcher(`${ENDPOINTS.audit.list}?${params}`);
    },
  );

  const entries: AuditEntry[] = data?.items ?? [];
  const totalItems = data?.totalCount ?? 0;

  if (isLoading) return <LoadingSpinner />;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-foreground">Audit Trail</h1>
        <p className="text-muted-foreground mt-1">Complete history of all system changes</p>
      </div>

      {/* Filters */}
      <div className="flex gap-4 flex-wrap">
        <input
          type="text"
          placeholder="Search by entity or performer..."
          value={searchTerm}
          onChange={e => setSearchTerm(e.target.value)}
          className="px-3 py-2 rounded-md border bg-background text-foreground min-w-[250px]"
        />
        <select
          value={entityType}
          onChange={e => setEntityType(e.target.value)}
          className="px-3 py-2 rounded-md border bg-background text-foreground"
        >
          <option value="">All Entities</option>
          <option value="Employee">Employee</option>
          <option value="AttendanceRecord">Attendance Record</option>
          <option value="AttendanceException">Exception</option>
          <option value="WarningLetter">Warning Letter</option>
          <option value="Escalation">Escalation</option>
          <option value="User">User</option>
        </select>
        <select
          value={action}
          onChange={e => setAction(e.target.value)}
          className="px-3 py-2 rounded-md border bg-background text-foreground"
        >
          <option value="">All Actions</option>
          <option value="Create">Create</option>
          <option value="Update">Update</option>
          <option value="Delete">Delete</option>
        </select>
      </div>

      {/* Table */}
      {entries.length === 0 ? (
        <EmptyState title="No audit entries found" description="No changes recorded in the selected filter range." />
      ) : (
        <DataTable<AuditEntry>
          columns={[
            { key: 'performedAt', header: 'Timestamp', render: (val: string) => new Date(val).toLocaleString() },
            { key: 'entityType', header: 'Entity' },
            { key: 'entityId', header: 'Entity ID' },
            { key: 'action', header: 'Action', render: (val: string) => (
              <span className={`inline-block px-2 py-0.5 rounded text-xs font-medium ${
                val === 'Create' ? 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200'
                : val === 'Update' ? 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200'
                : 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200'
              }`}>
                {val}
              </span>
            )},
            { key: 'performedBy', header: 'Performed By' },
            { key: 'ipAddress', header: 'IP Address' },
          ]}
          data={entries}
          currentPage={page}
          totalPages={Math.ceil(totalItems / pageSize)}
          onPageChange={setPage}
          onRowClick={(row) => console.log('View diff:', row.id)}
        />
      )}
    </div>
  );
}
