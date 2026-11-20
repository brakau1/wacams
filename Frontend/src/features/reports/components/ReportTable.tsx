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

interface Props {
  results: ReportResult[];
}

export function ReportTable({ results }: Props) {
  const formatMinutes = (min: number): string => {
    const hours = Math.floor(min / 60);
    const minutes = min % 60;
    return `${hours}h ${minutes}m`;
  };

  const getAttendanceColor = (pct: number): string => {
    if (pct >= 95) return 'text-green-600 dark:text-green-400';
    if (pct >= 85) return 'text-yellow-600 dark:text-yellow-400';
    return 'text-red-600 dark:text-red-400';
  };

  return (
    <div className="overflow-x-auto rounded-lg border">
      <table className="w-full text-sm">
        <thead className="bg-muted/50">
          <tr>
            <th className="text-left py-3 px-4 font-semibold">Employee ID</th>
            <th className="text-left py-3 px-4 font-semibold">Name</th>
            <th className="text-left py-3 px-4 font-semibold">Department</th>
            <th className="text-right py-3 px-4 font-semibold">Total Days</th>
            <th className="text-right py-3 px-4 font-semibold">Present</th>
            <th className="text-right py-3 px-4 font-semibold">Late</th>
            <th className="text-right py-3 px-4 font-semibold">Absent</th>
            <th className="text-right py-3 px-4 font-semibold">Exceptions</th>
            <th className="text-right py-3 px-4 font-semibold">Lost Time</th>
            <th className="text-right py-3 px-4 font-semibold">Attendance %</th>
          </tr>
        </thead>
        <tbody className="divide-y">
          {results.map(r => (
            <tr key={r.employeeId} className="hover:bg-muted/30 transition-colors">
              <td className="py-2 px-4 font-mono">{r.employeeId}</td>
              <td className="py-2 px-4">{r.employeeName}</td>
              <td className="py-2 px-4">{r.department}</td>
              <td className="py-2 px-4 text-right">{r.totalDays}</td>
              <td className="py-2 px-4 text-right">{r.presentDays}</td>
              <td className="py-2 px-4 text-right">{r.lateDays}</td>
              <td className="py-2 px-4 text-right">{r.absentDays}</td>
              <td className="py-2 px-4 text-right">{r.exceptionsCount}</td>
              <td className="py-2 px-4 text-right">{formatMinutes(r.lostMinutes)}</td>
              <td className={`py-2 px-4 text-right font-semibold ${getAttendanceColor(r.attendancePercentage)}`}>
                {r.attendancePercentage.toFixed(1)}%
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
