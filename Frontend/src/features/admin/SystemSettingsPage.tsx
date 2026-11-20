import { useState } from 'react';
import useSWR, { useSWRConfig } from 'swr';
import { api } from '../../core/api/apiClient';
import { ENDPOINTS } from '../../core/api/endpoints';
import { LoadingSpinner } from '@/shared/components/LoadingSpinner';
import EmptyState from '@/shared/components/EmptyState';
import Toast from '@/shared/components/Toast';

interface SystemSettings {
  gracePeriodMinutes: number;
  counsellingTriggerCount: number;
  verbalWarningTriggerCount: number;
  writtenWarningTriggerCount: number;
  finalWrittenWarningTriggerCount: number;
  escalationOnFinalWarning: boolean;
  shiftConfigs: ShiftConfig[];
}

interface ShiftConfig {
  code: string;
  name: string;
  startTime: string;
  endTime: string;
}

export default function SystemSettingsPage() {
  const [toast, setToast] = useState<{ message: string; type: 'success' | 'error' } | null>(null);
  const { mutate } = useSWRConfig();
  const [saving, setSaving] = useState(false);

  const { data, error, isLoading } = useSWR(
    ['admin/settings'],
    () => api.get(ENDPOINTS.settings.list).then(res => res.data),
  );

  const settings: SystemSettings = data ?? {};

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    try {
      const form = e.target as HTMLFormElement;
      const formData = new FormData(form);
      const payload: Record<string, unknown> = {};
      formData.forEach((value, key) => {
        if (key === 'escalationOnFinalWarning') {
          payload[key] = value === 'on';
        } else {
          payload[key] = parseInt(value as string, 10);
        }
      });
      await api.put(ENDPOINTS.settings.list, payload);
      mutate(['admin/settings']);
      setToast({ message: 'System settings updated successfully', type: 'success' });
    } catch {
      setToast({ message: 'Failed to update system settings', type: 'error' });
    } finally {
      setSaving(false);
    }
  };

  if (isLoading) return <LoadingSpinner />;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-foreground">System Settings</h1>
        <p className="text-muted-foreground mt-1">Configure grace periods, warning thresholds, and shift definitions</p>
      </div>

      <form onSubmit={handleSave} className="space-y-8">
        {/* Grace Period */}
        <section className="bg-card p-6 rounded-lg border space-y-4">
          <h2 className="text-lg font-semibold">Grace Period</h2>
          <div>
            <label className="block text-sm font-medium mb-1">Grace Period (minutes)</label>
            <input
              name="gracePeriodMinutes"
              type="number"
              min={0}
              max={30}
              defaultValue={settings.gracePeriodMinutes ?? 10}
              className="w-32 px-3 py-2 rounded-md border bg-background text-foreground"
              required
            />
            <p className="text-xs text-muted-foreground mt-1">
              Minutes after expected clock-in before counting as late arrival. Default: 10 minutes.
            </p>
          </div>
        </section>

        {/* Warning Thresholds */}
        <section className="bg-card p-6 rounded-lg border space-y-4">
          <h2 className="text-lg font-semibold">Warning Generation Thresholds</h2>
          <p className="text-sm text-muted-foreground">
            Number of attendance exceptions per month that trigger each warning level.
          </p>
          <div className="grid gap-4 md:grid-cols-2">
            {[
              { name: 'counsellingTriggerCount', label: 'Counselling', defaultVal: 3 },
              { name: 'verbalWarningTriggerCount', label: 'Verbal Warning', defaultVal: 5 },
              { name: 'writtenWarningTriggerCount', label: 'Written Warning', defaultVal: 7 },
              { name: 'finalWrittenWarningTriggerCount', label: 'Final Written Warning', defaultVal: 9 },
            ].map(field => (
              <div key={field.name}>
                <label className="block text-sm font-medium mb-1">{field.label}</label>
                <input
                  name={field.name}
                  type="number"
                  min={1}
                  max={20}
                  defaultValue={String(settings[field.name as keyof SystemSettings] ?? field.defaultVal ?? '')}
                  className="w-24 px-3 py-2 rounded-md border bg-background text-foreground"
                  required
                />
                <p className="text-xs text-muted-foreground mt-1">Exceptions per month to auto-trigger this warning level.</p>
              </div>
            ))}
          </div>
        </section>

        {/* Escalation */}
        <section className="bg-card p-6 rounded-lg border space-y-4">
          <h2 className="text-lg font-semibold">Escalation Rules</h2>
          <div className="flex items-center gap-3">
            <input
              id="escalationOnFinalWarning"
              name="escalationOnFinalWarning"
              type="checkbox"
              defaultChecked={settings.escalationOnFinalWarning ?? true}
              className="w-4 h-4 rounded"
            />
            <label htmlFor="escalationOnFinalWarning" className="text-sm">
              Auto-create escalation when Final Written Warning is generated
            </label>
          </div>
          <p className="text-xs text-muted-foreground">
            When enabled, a Level 2 escalation is automatically created at the Production Manager level.
          </p>
        </section>

        {/* Shift Configurations */}
        <section className="bg-card p-6 rounded-lg border space-y-4">
          <h2 className="text-lg font-semibold">Shift Configurations</h2>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b">
                  <th className="text-left py-2 pr-4">Code</th>
                  <th className="text-left py-2 pr-4">Name</th>
                  <th className="text-left py-2 pr-4">Start Time</th>
                  <th className="text-left py-2 pr-4">End Time</th>
                </tr>
              </thead>
              <tbody>
                {(settings.shiftConfigs ?? []).map(shift => (
                  <tr key={shift.code} className="border-b">
                    <td className="py-2 pr-4 font-mono">{shift.code}</td>
                    <td className="py-2 pr-4">{shift.name}</td>
                    <td className="py-2 pr-4">{shift.startTime}</td>
                    <td className="py-2 pr-4">{shift.endTime}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <p className="text-xs text-muted-foreground">
            Shift configurations are managed via database seed data. Contact SuperAdmin to modify.
          </p>
        </section>

        {/* Save button */}
        <div className="flex justify-end">
          <button
            type="submit"
            disabled={saving}
            className="px-6 py-2 rounded-md bg-primary text-primary-foreground hover:opacity-90 transition-opacity disabled:opacity-50"
          >
            {saving ? 'Saving...' : 'Save Settings'}
          </button>
        </div>
      </form>

      {/* Toast */}
      {toast && <div className={`fixed bottom-4 right-4 z-50 p-4 rounded-lg border shadow-lg ${toast.type === "success" ? "bg-green-500/10 border-green-500/20 text-green-400" : "bg-red-500/10 border-red-500/20 text-red-400"}`}>{toast.message}<button onClick={() => setToast(null)} className="ml-2">×</button></div>}
    </div>
  );
}
