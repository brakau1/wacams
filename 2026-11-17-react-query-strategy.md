# React Query Strategy

**Date**: 2026-11-17  
**Purpose**: Define React Query patterns, caching strategy, and hook architecture  
**Status**: Ready for Implementation

---

## Overview

This document defines the React Query (TanStack Query) strategy for the WACAMS frontend. It covers hook architecture, caching configuration, mutation patterns, error handling, and performance optimization.

---

## Query Client Configuration

```typescript
// src/core/api/queryClient.ts
import { QueryClient } from '@tanstack/react-query';

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,        // 30 seconds
      gcTime: 5 * 60_000,       // 5 minutes (garbage collection)
      retry: 2,
      retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30_000),
      refetchOnWindowFocus: true,
      refetchOnReconnect: true,
    },
    mutations: {
      retry: 1,
    },
  },
});
```

---

## Hook Architecture

### Directory Structure

```
src/core/api/hooks/
├── useAttendance.ts
├── useDashboard.ts
├── useExceptions.ts
├── useWarnings.ts
├── useEmployees.ts
├── useManagerDashboard.ts
├── useEscalations.ts
├── useAudit.ts
├── useReports.ts
├── useExecutiveDashboard.ts
├── useHRCommandCentre.ts
├── useAdmin.ts
└── index.ts              // barrel export
```

### Hook Naming Convention

| Pattern | Example | Purpose |
|---------|---------|---------|
| `use[Entity]` | `useEmployees` | Fetch list of entities |
| `use[Entity]Detail` | `useEmployeeDetail` | Fetch single entity |
| `use[Entity]Summary` | `useAttendanceSummary` | Fetch aggregated data |
| `useCreate[Entity]` | `useCreateWarning` | Mutation: create |
| `useUpdate[Entity]` | `useUpdateException` | Mutation: update |
| `use[Action][Entity]` | `useInvestigateException` | Mutation: business action |

---

## Cache Strategy by Module

### High-Frequency Data (Real-time feel)

| Hook | Stale Time | Refetch Interval | Cache Time |
|------|-----------|------------------|------------|
| `useDashboardKPI` | 60s | 5 min | 10 min |
| `useManagerDashboard` | 60s | 5 min | 10 min |
| `useExecutiveDashboard` | 60s | 30s | 5 min |
| `useHRCommandCentre` | 60s | 5 min | 10 min |

### Medium-Frequency Data

| Hook | Stale Time | Refetch Interval | Cache Time |
|------|-----------|------------------|------------|
| `useAttendance` | 30s | None | 5 min |
| `useExceptions` | 30s | None | 5 min |
| `useEscalations` | 30s | None | 5 min |
| `useEmployees` | 60s | None | 10 min |

### Low-Frequency Data

| Hook | Stale Time | Refetch Interval | Cache Time |
|------|-----------|------------------|------------|
| `useWarnings` | 5 min | None | 30 min |
| `useAudit` | 5 min | None | 30 min |
| `useReports` | 10 min | None | 1 hour |

---

## Hook Implementation Examples

### Query Hook with Filters

```typescript
// src/core/api/hooks/useExceptions.ts
import { useQuery } from '@tanstack/react-query';
import { get } from '../apiClient';
import { ENDPOINTS } from '../endpoints';

interface ExceptionFilters {
  status?: string;
  departmentId?: string;
  exceptionType?: string;
  startDate?: string;
  endDate?: string;
}

export function useExceptions(filters: ExceptionFilters) {
  return useQuery({
    queryKey: ['exceptions', filters],
    queryFn: async () => {
      const params = new URLSearchParams();
      Object.entries(filters).forEach(([key, value]) => {
        if (value) params.set(key, value);
      });
      const response = await get(`${ENDPOINTS.exceptions.list}?${params}`);
      return response.data ?? response;
    },
    staleTime: 30_000,
  });
}
```

### Mutation Hook with Invalidation

```typescript
export function useInvestigateException() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: async ({ id, data }: { id: string; data: InvestigateRequest }) => {
      return put(`${ENDPOINTS.exceptions.list}/${id}/investigate`, data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['exceptions'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard-kpi'] });
      queryClient.invalidateQueries({ queryKey: ['manager-dashboard'] });
    },
    onError: (error) => {
      console.error('Investigation failed:', error);
    },
  });
}
```

### Paginated Query Hook

```typescript
export function useEmployeesPaginated(page: number, filters: EmployeeFilters) {
  return useQuery({
    queryKey: ['employees', page, filters],
    queryFn: async () => {
      const params = new URLSearchParams({
        page: String(page),
        pageSize: '50',
        ...Object.fromEntries(
          Object.entries(filters).filter(([_, v]) => v).map(([k, v]) => [k, String(v)])
        ),
      });
      const response = await get(`${ENDPOINTS.employees.list}?${params}`);
      return response.data ?? response;
    },
    keepPreviousData: true,
  });
}
```

---

## Error Handling Pattern

### Global Error Handler

```typescript
// src/core/api/errorHandler.ts
export function handleApiError(error: unknown): string {
  if (error instanceof Error) {
    if ('response' in error) {
      const status = (error as any).response?.status;
      switch (status) {
        case 401: return 'Session expired. Please log in again.';
        case 403: return 'You do not have permission to perform this action.';
        case 404: return 'The requested resource was not found.';
        case 429: return 'Too many requests. Please try again later.';
        case 500: return 'An internal error occurred. Please try again.';
        default: return 'An unexpected error occurred.';
      }
    }
    return error.message;
  }
  return 'An unknown error occurred.';
}
```

### Error Boundary Component

```typescript
// src/shared/components/ErrorBoundary.tsx
export function QueryErrorBoundary({ error, reset }: { error: Error; reset: () => void }) {
  return (
    <div className="p-6 rounded-lg border border-red-500/20 bg-red-500/5">
      <h3 className="text-lg font-semibold text-red-400 mb-2">Something went wrong</h3>
      <p className="text-sm text-muted-foreground mb-4">{handleApiError(error)}</p>
      <button onClick={reset} className="px-4 py-2 rounded-md bg-primary text-primary-foreground">
        Try Again
      </button>
    </div>
  );
}
```

---

## Loading State Pattern

### Skeleton Loader

```typescript
// src/shared/components/SkeletonLoader.tsx
export function TableSkeleton({ rows = 5, cols = 4 }: { rows?: number; cols?: number }) {
  return (
    <div className="space-y-3">
      {Array.from({ length: rows }).map((_, i) => (
        <div key={i} className="flex gap-4">
          {Array.from({ length: cols }).map((_, j) => (
            <div key={j} className="h-4 bg-muted rounded animate-pulse flex-1" />
          ))}
        </div>
      ))}
    </div>
  );
}
```

### Loading Pattern per Module

| Module | Initial Load | Filter Change | Pagination |
|--------|-------------|---------------|------------|
| Attendance | Full-page spinner | Inline spinner | Skeleton |
| Exceptions | Full-page spinner | Inline spinner | Skeleton |
| Dashboard | Full-page spinner | None | None |
| Employees | Full-page spinner | Inline spinner | Skeleton |
| Warnings | Full-page spinner | Inline spinner | None |

---

## Optimistic Updates

For mutations that should update UI immediately:

```typescript
export function useToggleUser() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: async ({ userId, isActive }: { userId: string; isActive: boolean }) => {
      return put(`${ENDPOINTS.users.list}/${userId}/toggle`, { isActive });
    },
    onMutate: async ({ userId, isActive }) => {
      await queryClient.cancelQueries({ queryKey: ['admin/users'] });
      const previous = queryClient.getQueryData(['admin/users']);
      queryClient.setQueryData(['admin/users'], (old: any) => ({
        ...old,
        items: old.items.map((u: any) => u.id === userId ? { ...u, isActive } : u),
      }));
      return { previous };
    },
    onError: (err, variables, context) => {
      queryClient.setQueryData(['admin/users'], context?.previous);
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['admin/users'] });
    },
  });
}
```

---

## Performance Optimization

### Prefetching

```typescript
// Prefetch dashboard data on login
export function usePrefetchDashboard() {
  const queryClient = useQueryClient();
  
  useEffect(() => {
    queryClient.prefetchQuery({
      queryKey: ['dashboard-kpi'],
      queryFn: () => get(ENDPOINTS.reports.dashboardKPI),
    });
  }, [queryClient]);
}
```

### Dependent Queries

```typescript
// Fetch employee details only after employee list loads
const { data: employees } = useEmployees({});
const { data: employeeDetail } = useEmployeeDetail(
  employees?.[0]?.id,
  { enabled: !!employees?.[0]?.id }
);
```

### Infinite Query (for large lists)

```typescript
export function useAuditInfinite() {
  return useInfiniteQuery({
    queryKey: ['audit-infinite'],
    queryFn: async ({ pageParam = 1 }) => {
      const response = await get(`${ENDPOINTS.audit.list}?page=${pageParam}&pageSize=25`);
      return response.data ?? response;
    },
    getNextPageParam: (lastPage) => {
      const totalPages = Math.ceil(lastPage.totalItems / 25);
      return lastPage.page < totalPages ? lastPage.page + 1 : undefined;
    },
  });
}
```

---

## Query Key Factory

```typescript
// src/core/api/queryKeys.ts
export const queryKeys = {
  attendance: {
    all: ['attendance'] as const,
    list: (filters: any) => ['attendance', 'list', filters] as const,
    summary: (filters: any) => ['attendance', 'summary', filters] as const,
    detail: (id: string) => ['attendance', 'detail', id] as const,
  },
  exceptions: {
    all: ['exceptions'] as const,
    list: (filters: any) => ['exceptions', 'list', filters] as const,
    detail: (id: string) => ['exceptions', 'detail', id] as const,
  },
  warnings: {
    all: ['warnings'] as const,
    list: (filters: any) => ['warnings', 'list', filters] as const,
    detail: (id: string) => ['warnings', 'detail', id] as const,
  },
  employees: {
    all: ['employees'] as const,
    list: (filters: any) => ['employees', 'list', filters] as const,
    detail: (id: string) => ['employees', 'detail', id] as const,
  },
  dashboard: {
    kpi: ['dashboard', 'kpi'] as const,
    manager: ['dashboard', 'manager'] as const,
    executive: ['dashboard', 'executive'] as const,
    hr: ['dashboard', 'hr'] as const,
  },
  escalations: {
    all: ['escalations'] as const,
    list: (filters: any) => ['escalations', 'list', filters] as const,
    stats: ['escalations', 'stats'] as const,
  },
  audit: {
    all: ['audit'] as const,
    list: (filters: any) => ['audit', 'list', filters] as const,
  },
  reports: {
    attendance: (filters: any) => ['reports', 'attendance', filters] as const,
  },
  admin: {
    users: (filters: any) => ['admin', 'users', filters] as const,
  },
};
```

---

## Testing Strategy

### Mock Query Client for Tests

```typescript
// src/test-utils/queryClient.ts
import { QueryClient } from '@tanstack/react-query';

export function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        gcTime: 0,
      },
    },
  });
}
```

### Test Pattern

```typescript
import { QueryClientProvider } from '@tanstack/react-query';
import { createTestQueryClient } from '@/test-utils/queryClient';

function renderWithQuery(ui: React.ReactElement) {
  const queryClient = createTestQueryClient();
  return render(
    <QueryClientProvider client={queryClient}>
      {ui}
    </QueryClientProvider>
  );
}
```

---

**Document Status**: Complete  
**Next Step**: Implement hooks following this strategy
