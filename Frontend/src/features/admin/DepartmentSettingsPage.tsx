import { useState } from 'react';
import useSWR, { useSWRConfig } from 'swr';
import { api } from '../../core/api/apiClient';
import { ENDPOINTS } from '../../core/api/endpoints';
import DataTable from '@/shared/components/DataTable';
import Modal from '@/shared/components/Modal';
import { LoadingSpinner } from '@/shared/components/LoadingSpinner';
import EmptyState from '@/shared/components/EmptyState';
import Toast from '@/shared/components/Toast';

interface DepartmentItem {
  id: string;
  name: string;
  code: string;
  supervisorId?: string;
  supervisorName?: string;
  attendanceTargetPercentage: number;
}

const DEPARTMENT_CODES = ['Production', 'Quality', 'Engineering', 'Maintenance', 'Logistics', 'Office', 'HR'];

export default function DepartmentSettingsPage() {
  const [showEditModal, setShowEditModal] = useState(false);
  const [editingDept, setEditingDept] = useState<DepartmentItem | null>(null);
  const [toast, setToast] = useState<{ message: string; type: 'success' | 'error' } | null>(null);
  const { mutate } = useSWRConfig();

  const { data, error, isLoading } = useSWR(
    ['admin/departments'],
    () => api.get(ENDPOINTS.departments.list).then(res => res.data),
  );

  const departments: DepartmentItem[] = data?.items ?? [];

  const handleSaveTarget = async (deptId: string, targetPct: number) => {
    try {
      await api.put(`${ENDPOINTS.departments.list}/${deptId}`, { attendanceTargetPercentage: targetPct });
      mutate(['admin/departments']);
      setToast({ message: 'Department target updated successfully', type: 'success' });
      setShowEditModal(false);
      setEditingDept(null);
    } catch {
      setToast({ message: 'Failed to update department target', type: 'error' });
    }
  };

  if (isLoading) return <LoadingSpinner />;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-foreground">Department Settings</h1>
        <p className="text-muted-foreground mt-1">Configure departments and their attendance targets</p>
      </div>

      {/* Departments Table */}
      {departments.length === 0 ? (
        <EmptyState title="No departments configured" description="Departments will be auto-seeded on first database migration." />
      ) : (
        <DataTable<DepartmentItem>
          columns={[
            { key: 'code', header: 'Code' },
            { key: 'name', header: 'Name' },
            { key: 'supervisorName', header: 'Supervisor' },
            {
              key: 'attendanceTargetPercentage',
              header: 'Attendance Target',
              render: (val: number) => (
                <div className="flex items-center gap-2">
                  <span className="font-semibold">{val}%</span>
                  <div className="w-24 h-2 bg-gray-200 dark:bg-gray-700 rounded-full overflow-hidden">
                    <div
                      className="h-full bg-primary rounded-full"
                      style={{ width: `${Math.min(val, 100)}%` }}
                    />
                  </div>
                </div>
              ),
            },
          ]}
          data={departments}
          actions={(row: any) => (
            <button
              onClick={() => { setEditingDept(row); setShowEditModal(true); }}
              className="px-2 py-1 rounded text-xs bg-blue-100 text-blue-700 hover:bg-blue-200"
            >
              Edit Target
            </button>
          )}
        />
      )}

      {/* Edit Target Modal */}
      <Modal isOpen={showEditModal} onClose={() => { setShowEditModal(false); setEditingDept(null); }} title={`Edit ${editingDept?.name}`}>
        {editingDept && (
          <form
            onSubmit={e => {
              e.preventDefault();
              const form = e.target as HTMLFormElement;
              const input = form.elements.namedItem('target') as HTMLInputElement;
              handleSaveTarget(editingDept.id, parseFloat(input.value));
            }}
            className="space-y-4"
          >
            <div>
              <label className="block text-sm font-medium mb-1">Attendance Target (%)</label>
              <input
                name="target"
                type="number"
                min={0}
                max={100}
                step={0.5}
                defaultValue={editingDept.attendanceTargetPercentage}
                className="w-full px-3 py-2 rounded-md border bg-background text-foreground"
                required
              />
              <p className="text-xs text-muted-foreground mt-1">
                Minimum attendance percentage for this department. Production typically requires 95%.
              </p>
            </div>
            <div className="flex justify-end gap-2 pt-4">
              <button type="button" onClick={() => { setShowEditModal(false); setEditingDept(null); }} className="px-4 py-2 rounded-md border">
                Cancel
              </button>
              <button type="submit" className="px-4 py-2 rounded-md bg-primary text-primary-foreground">
                Save Target
              </button>
            </div>
          </form>
        )}
      </Modal>

      {/* Toast */}
      {toast && <div className={`fixed bottom-4 right-4 z-50 p-4 rounded-lg border shadow-lg ${toast.type === "success" ? "bg-green-500/10 border-green-500/20 text-green-400" : "bg-red-500/10 border-red-500/20 text-red-400"}`}>{toast.message}<button onClick={() => setToast(null)} className="ml-2">×</button></div>}
    </div>
  );
}
