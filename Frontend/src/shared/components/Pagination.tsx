import { cn } from '@/core/utils/cn'

interface PaginationProps {
  currentPage: number
  totalPages: number
  onPageChange: (page: number) => void
  pageSize?: number
  totalItems?: number
}

export default function Pagination({
  currentPage,
  totalPages,
  onPageChange,
  pageSize,
  totalItems
}: PaginationProps) {
  if (totalPages <= 1) return null

  const startItem = (currentPage - 1) * (pageSize || 20) + 1
  const endItem = Math.min(currentPage * (pageSize || 20), totalItems || Infinity)

  // Generate page numbers to show
  const getPageNumbers = (): (number | string)[] => {
    const pages: (number | string)[] = []
    const delta = 2

    for (let i = 1; i <= totalPages; i++) {
      if (
        i === 1 ||
        i === totalPages ||
        (i >= currentPage - delta && i <= currentPage + delta)
      ) {
        pages.push(i)
      } else if (pages[pages.length - 1] !== '...') {
        pages.push('...')
      }
    }
    return pages
  }

  return (
    <div className="flex flex-col sm:flex-row items-center justify-between gap-3">
      <p className="text-sm text-muted-foreground">
        Showing {startItem}–{endItem} of {totalItems ?? '?'} results
      </p>
      <nav className="flex items-center gap-1">
        <button
          onClick={() => onPageChange(currentPage - 1)}
          disabled={currentPage === 1}
          className={cn(
            'px-3 py-1.5 rounded-md border border-border text-sm transition-colors',
            currentPage === 1 ? 'opacity-50 cursor-not-allowed' : 'hover:bg-muted'
          )}
        >
          Previous
        </button>

        {getPageNumbers().map((page, idx) =>
          typeof page === 'string' ? (
            <span key={`ellipsis-${idx}`} className="px-2 text-muted-foreground">
              …
            </span>
          ) : (
            <button
              key={page}
              onClick={() => onPageChange(page)}
              className={cn(
                'w-8 h-8 rounded-md text-sm flex items-center justify-center transition-colors',
                currentPage === page
                  ? 'bg-primary text-primary-foreground font-semibold'
                  : 'border border-border hover:bg-muted'
              )}
            >
              {page}
            </button>
          )
        )}

        <button
          onClick={() => onPageChange(currentPage + 1)}
          disabled={currentPage === totalPages}
          className={cn(
            'px-3 py-1.5 rounded-md border border-border text-sm transition-colors',
            currentPage === totalPages ? 'opacity-50 cursor-not-allowed' : 'hover:bg-muted'
          )}
        >
          Next
        </button>
      </nav>
    </div>
  )
}
