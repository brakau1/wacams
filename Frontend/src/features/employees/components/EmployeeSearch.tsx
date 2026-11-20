import { useState, useEffect } from 'react'
import { useDebounce } from '@/core/hooks/useDebounce'
import { Search, X } from 'lucide-react'

interface EmployeeSearchProps {
  onSearch: (query: string) => void
  placeholder?: string
  className?: string
}

export default function EmployeeSearch({ onSearch, placeholder = 'Search employees...', className }: EmployeeSearchProps) {
  const [query, setQuery] = useState('')
  const debouncedQuery = useDebounce(query, 300)

  useEffect(() => {
    onSearch(debouncedQuery)
  }, [debouncedQuery, onSearch])

  return (
    <div className={className}>
      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-muted-foreground" />
        <input
          type="text"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder={placeholder}
          className="w-full pl-10 pr-10 py-2 rounded-lg border border-border bg-background text-foreground placeholder:text-muted-foreground focus:outline-none focus:ring-2 focus:ring-primary/50"
        />
        {query && (
          <button
            onClick={() => {
              setQuery('')
              onSearch('')
            }}
            className="absolute right-3 top-1/2 -translate-y-1/2 p-1 text-muted-foreground hover:text-foreground"
          >
            <X className="w-4 h-4" />
          </button>
        )}
      </div>
    </div>
  )
}
