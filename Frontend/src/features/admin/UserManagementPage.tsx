import { useState } from 'react';
import useSWR, { useSWRConfig } from 'swr';
import { api } from '../../core/api/apiClient';
import { ENDPOINTS } from '../../core/api/endpoints';
import DataTable from '@/shared/components/DataTable';
import Modal from '@/shared/components/Modal';
import { LoadingSpinner } from '@/shared/components/LoadingSpinner';
import EmptyState from '@/shared/components/EmptyState';
import Toast from '@/shared/components/Toast';

interface UserItem {
  id: string;
  employeeId: string;
  username: string;
  displayName: string;
  role: string;
  isActive: boolean;
  lastLoginAt?: string;
}

const ROLES = ['SuperAdmin', 'Admin', 'Supervisor', 'TeamLeader', 'HR', 'Operator'];

export default function UserManagementPage() {
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedRole, setSelectedRole] = useState('');
  const { mutate } = useSWRConfig();
  const [toast, setToast] = useState<{ message: string; type: 'success' | 'error' } | null>(null);

  const { data, error, isLoading } = useSWR(
    ['admin/users', searchTerm, selectedRole],
    () => {
      const params = new URLSearchParams({ search: searchTerm });
      if (selectedRole) params.set('role', selectedRole);
      return api.get(`${ENDPOINTS.users.list.replace('/users','')}/users?${params}`).then(res => res.data);
    },
  );

  const users: UserItem[] = data?.items ?? [];

  const handleToggleActive = async (userId: string, currentStatus: boolean) => {
    try {
      await api.put(`${ENDPOINTS.users.list.replace('/users','')}/users/${userId}/toggle`, { isActive: !currentStatus });
      mutate(['admin/users', searchTerm, selectedRole]);
      setToast({ message: `User ${!currentStatus ? 'activated' : 'deactivated'} successfully`, type: 'success' });
    } catch {
      setToast({ message: 'Failed to update user status', type: 'error' });
    }
  };

  if (isLoading) return <LoadingSpinner />;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">User Management</h1>
          <p className="text-muted-foreground mt-1">Register, edit, assign roles, disable and reset passwords</p>
        </div>
        <button
          onClick={() => setShowCreateModal(true)}
          className="px-4 py-2 rounded-md bg-primary text-primary-foreground hover:opacity-90 transition-opacity"
        >
          + Register User
        </button>
      </div>

      {/* Filters */}
      <div className="flex gap-4">
        <input
          type="text"
          placeholder="Search by username or name..."
          value={searchTerm}
          onChange={e => setSearchTerm(e.target.value)}
          className="flex-1 px-3 py-2 rounded-md border bg-background text-foreground"
        />
        <select
          value={selectedRole}
          onChange={e => setSelectedRole(e.target.value)}
          className="px-3 py-2 rounded-md border bg-background text-foreground"
        >
          <option value="">All Roles</option>
          {ROLES.map(r => <option key={r} value={r}>{r}</option>)}
        </select>
      </div>

      {/* Table */}
      {users.length === 0 ? (
        <EmptyState title="No users found" description="Register a new user to get started." />
      ) : (
        <DataTable<UserItem>
          columns={[
            { key: 'username', header: 'Username' },
            { key: 'displayName', header: 'Display Name' },
            { key: 'employeeId', header: 'Employee ID' },
            {
              key: 'role',
              header: 'Role',
              render: (val: string) => (
                <span className={`inline-block px-2 py-0.5 rounded text-xs font-medium ${
                  val === 'SuperAdmin' ? 'bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200'
                  : val === 'Admin' ? 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200'
                  : val === 'Supervisor' ? 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200'
                  : val === 'HR' ? 'bg-indigo-100 text-indigo-800 dark:bg-indigo-900 dark:text-indigo-200'
                  : 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-200'
                }`}>
                  {val}
                </span>
              ),
            },
            {
              key: 'isActive',
              header: 'Status',
              render: (val: boolean) => (
                <span className={`inline-block px-2 py-0.5 rounded text-xs font-medium ${val ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`}>
                  {val ? 'Active' : 'Disabled'}
                </span>
              ),
            },
            { key: 'lastLoginAt', header: 'Last Login', render: (val?: string) => val ? new Date(val).toLocaleDateString() : 'Never' },
          ]}
          data={users}
          actions={(row: any) => (
            <div className="flex gap-2">
              <button
                onClick={() => handleToggleActive(row.id, row.isActive)}
                className={`px-2 py-1 rounded text-xs ${row.isActive ? 'bg-red-100 text-red-700 hover:bg-red-200' : 'bg-green-100 text-green-700 hover:bg-green-200'}`}
              >
                {row.isActive ? 'Disable' : 'Enable'}
              </button>
              <button className="px-2 py-1 rounded text-xs bg-yellow-100 text-yellow-700 hover:bg-yellow-200">
                Reset Password
              </button>
            </div>
          )}
        />
      )}

      {/* Create User Modal */}
      <Modal isOpen={showCreateModal} onClose={() => setShowCreateModal(false)} title="Register New User">
        <form className="space-y-4">
          <div>
            <label className="block text-sm font-medium mb-1">Employee ID *</label>
            <input type="text" required className="w-full px-3 py-2 rounded-md border bg-background text-foreground" />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Username *</label>
            <input type="text" required className="w-full px-3 py-2 rounded-md border bg-background text-foreground" />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Password *</label>
            <input type="password" required className="w-full px-3 py-2 rounded-md border bg-background text-foreground" />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Role *</label>
            <select required className="w-full px-3 py-2 rounded-md border bg-background text-foreground">
              {ROLES.map(r => <option key={r} value={r}>{r}</option>)}
            </select>
          </div>
          <div className="flex justify-end gap-2 pt-4">
            <button type="button" onClick={() => setShowCreateModal(false)} className="px-4 py-2 rounded-md border">
              Cancel
            </button>
            <button type="submit" className="px-4 py-2 rounded-md bg-primary text-primary-foreground">
              Register
            </button>
          </div>
        </form>
      </Modal>

      {/* Toast */}
      {toast && <div className={`fixed bottom-4 right-4 z-50 p-4 rounded-lg border shadow-lg ${toast.type === "success" ? "bg-green-500/10 border-green-500/20 text-green-400" : "bg-red-500/10 border-red-500/20 text-red-400"}`}>{toast.message}<button onClick={() => setToast(null)} className="ml-2">×</button></div>}
    </div>
  );
}
