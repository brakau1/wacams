interface Props {
  startDate: string;
  endDate: string;
  onStartDateChange: (date: string) => void;
  onEndDateChange: (date: string) => void;
}

export function ReportDatePicker({ startDate, endDate, onStartDateChange, onEndDateChange }: Props) {
  return (
    <div className="flex gap-4">
      <div>
        <label className="block text-sm font-medium mb-1">Start Date</label>
        <input
          type="date"
          value={startDate}
          onChange={e => onStartDateChange(e.target.value)}
          className="px-3 py-2 rounded-md border bg-background text-foreground"
        />
      </div>
      <div>
        <label className="block text-sm font-medium mb-1">End Date</label>
        <input
          type="date"
          value={endDate}
          onChange={e => onEndDateChange(e.target.value)}
          className="px-3 py-2 rounded-md border bg-background text-foreground"
        />
      </div>
    </div>
  );
}
