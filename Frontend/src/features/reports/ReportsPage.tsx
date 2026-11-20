import { useState } from 'react';
import useSWR from 'swr';
import { api } from '../../core/api/apiClient';
import { ENDPOINTS } from '../../core/api/endpoints';
import { ReportDatePicker } from './components/ReportDatePicker';
import { DepartmentSelector } from './components/DepartmentSelector';
import { ReportTable } from './components/ReportTable';
import { ExportButton } from './components/ExportButton';
import { LoadingSpinner } from '@/shared/components/LoadingSpinner';
import EmptyState from '@/shared/components/EmptyState';

interface ReportResult {
  employeeId: string;
  employeeName: string;
  department: string;
  totalDays: number;
  presentDays: number;
  lateDays: number;
  absentDays: number;
  exceptionsCount: number;
  lostMinutes: number;
  attendancePercentage: number;
}

export default function ReportsPage() {
  const [startDate, setStartDate] = useState<string>('');
  const [endDate, setEndDate] = useState<string>('');
  const [selectedDepts, setSelectedDepts] = useState<string[]>([]);
  const [reportType, setReportType] = useState<'individual' | 'department'>('individual');

  const { data, error, isLoading } = useSWR(
    startDate && endDate ? ['reports', startDate, endDate, selectedDepts, reportType] : null,
    () => {
      const params = new URLSearchParams({
        startDate,
        endDate,
        reportType,
      });
      selectedDepts.forEach(d => params.append('departmentIds', d));
      return api.get(`${ENDPOINTS.reports.attendance.replace('/attendance','')}/attendance-summary?${params}`).then(res => res.data);
    },
  );

  const results: ReportResult[] = data?.items ?? [];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-foreground">Reports</h1>
          <p className="text-muted-foreground mt-1">Generate and export attendance reports</p>
        </div>
        <ExportButton
          disabled={results.length === 0}
          onExport={() => window.open(`${ENDPOINTS.reports.attendance.replace('/attendance','')}/export-csv?startDate=${startDate}&endDate=${endDate}`)}
        />
      </div>

      {/* Report configuration */}
      <div className="grid gap-4 md:grid-cols-4 bg-card p-4 rounded-lg border">
        <div className="md:col-span-1">
          <label className="block text-sm font-medium mb-1 text-muted-foreground">Start Date</label>
          <input
            type="date"
            value={startDate}
            onChange={e => setStartDate(e.target.value)}
            className="w-full px-3 py-2 rounded-md border bg-background text-foreground"
          />
        </div>
        <div className="md:col-span-1">
          <label className="block text-sm font-medium mb-1 text-muted-foreground">End Date</label>
          <input
            type="date"
            value={endDate}
            onChange={e => setEndDate(e.target.value)}
            className="w-full px-3 py-2 rounded-md border bg-background text-foreground"
          />
        </div>
        <div className="md:col-span-1">
          <label className="block text-sm font-medium mb-1 text-muted-foreground">Report Type</label>
          <select
            value={reportType}
            onChange={e => setReportType(e.target.value as 'individual' | 'department')}
            className="w-full px-3 py-2 rounded-md border bg-background text-foreground"
          >
            <option value="individual">Individual Report</option>
            <option value="department">Department Summary</option>
          </select>
        </div>
        <div className="md:col-span-1 flex items-end">
          <DepartmentSelector
            selected={selectedDepts}
            onChange={setSelectedDepts}
          />
        </div>
      </div>

      {/* Results */}
      {isLoading ? (
        <LoadingSpinner />
      ) : results.length === 0 ? (
        <EmptyState
          title="No data to display"
          description="Select a date range and click Generate to view attendance reports."
        />
      ) : (
        <ReportTable results={results} />
      )}
    </div>
  );
}
