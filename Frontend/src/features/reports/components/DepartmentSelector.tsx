interface Props {
  selected: string[];
  onChange: (depts: string[]) => void;
}

const DEPARTMENTS = ['Production', 'Quality', 'Engineering', 'Maintenance', 'Logistics', 'Office', 'HR'];

export function DepartmentSelector({ selected, onChange }: Props) {
  const toggleDept = (dept: string) => {
    onChange(
      selected.includes(dept)
        ? selected.filter(d => d !== dept)
        : [...selected, dept]
    );
  };

  return (
    <div className="space-y-2">
      <label className="block text-sm font-medium text-muted-foreground">Departments</label>
      <div className="flex flex-wrap gap-2">
        {DEPARTMENTS.map(dept => (
          <button
            key={dept}
            type="button"
            onClick={() => toggleDept(dept)}
            className={`px-3 py-1 rounded-full text-xs font-medium transition-colors ${
              selected.includes(dept)
                ? 'bg-primary text-primary-foreground'
                : 'bg-muted text-muted-foreground hover:bg-muted/80'
            }`}
          >
            {dept}
          </button>
        ))}
      </div>
      {selected.length > 0 && (
        <button
          type="button"
          onClick={() => onChange([])}
          className="text-xs text-muted-foreground underline hover:text-foreground"
        >
          Clear all
        </button>
      )}
    </div>
  );
}
