# Frontend Completion Roadmap

**Date**: 2026-11-17  
**Purpose**: Define the execution timeline and milestones for frontend API integration  
**Status**: Ready for Implementation  
**SubAgent**: SubAgent B - Frontend API Integration

---

## Executive Summary

This roadmap provides a detailed timeline for completing the frontend API integration across all 13 feature modules. The plan is divided into 5 phases over 15 working days, with clear milestones, deliverables, and success criteria.

**Current State**: 38% frontend completeness (6 modules with mock data)  
**Target State**: 100% API integration  
**Timeline**: 15 working days  
**Dependencies**: Backend APIs must be fully implemented and tested

---

## Phase Overview

| Phase | Duration | Focus | Modules |
|-------|----------|-------|---------|
| Phase 1 | Day 1-2 | Infrastructure | Query client, hooks, error handling |
| Phase 2 | Day 3-6 | Critical Modules | Attendance, Exceptions, Warnings |
| Phase 3 | Day 7-9 | Dashboard Modules | Dashboard, Employees, Manager Dashboard |
| Phase 4 | Day 10-12 | Enhancement | Escalations, Audit, Reports, Executive, HR, Admin, Auth |
| Phase 5 | Day 13-15 | Testing & Validation | Integration tests, UAT, bug fixes |

---

## Phase 1: Infrastructure Setup (Days 1-2)

### Objectives
- Set up React Query client configuration
- Create hook directory structure
- Implement shared error handling utilities
- Create loading state components
- Establish testing patterns

### Deliverables

#### Day 1: Core Infrastructure
- [ ] Create `src/core/api/queryClient.ts` with global configuration
- [ ] Create `src/core/api/hooks/` directory structure
- [ ] Create `src/core/api/queryKeys.ts` with query key factory
- [ ] Create `src/core/api/errorHandler.ts` with global error handler
- [ ] Create `src/shared/components/ErrorBoundary.tsx`
- [ ] Create `src/shared/components/SkeletonLoader.tsx`
- [ ] Create `src/shared/components/QueryErrorBoundary.tsx`
- [ ] Update `src/core/api/index.ts` to export query client

**Success Criteria**: Infrastructure components compile without errors

#### Day 2: Testing & Documentation
- [ ] Create `src/test-utils/queryClient.ts` for test utilities
- [ ] Create `src/test-utils/renderWithQuery.tsx` helper
- [ ] Write unit tests for error handler
- [ ] Write unit tests for loading components
- [ ] Document hook patterns in README
- [ ] Create hook implementation templates

**Success Criteria**: All tests pass, documentation complete

### Dependencies
- React Query already installed
- Backend APIs available (or mock server for development)

### Risk Mitigation
- **Risk**: Backend APIs not ready
  - **Mitigation**: Use mock data as fallback during development
- **Risk**: TypeScript type mismatches
  - **Mitigation**: Define all interfaces before implementation

---

## Phase 2: Critical Modules (Days 3-6)

### Objectives
- Migrate 3 high-priority modules from mock data to API
- Implement React Query hooks for each module
- Add proper error handling and loading states
- Ensure department scoping works correctly

### Module 1: Attendance (Days 3-4)

#### Day 3: Hooks & Types
- [ ] Create `src/core/api/hooks/useAttendance.ts`
- [ ] Create `src/core/api/hooks/useAttendanceSummary.ts`
- [ ] Define TypeScript interfaces for attendance data
- [ ] Update `src/core/api/types.ts` with attendance types
- [ ] Write unit tests for hooks

**Success Criteria**: Hooks fetch data correctly, types match backend DTOs

#### Day 4: Component Migration
- [ ] Update `AttendancePage.tsx` to use `useAttendance` hook
- [ ] Remove `MOCK_RECORDS` constant
- [ ] Add loading states (initial load, filter change, pagination)
- [ ] Add error handling with retry button
- [ ] Update `CalendarView` component to handle API data
- [ ] Update `DailyAttendanceTable` component
- [ ] Update `LostTimeSummary` component
- [ ] Test with different user roles

**Success Criteria**: Page displays real data, loading states work, errors handled

**Effort**: 2 days

### Module 2: Exceptions (Days 5-6)

#### Day 5: Hooks & Types
- [ ] Create `src/core/api/hooks/useExceptions.ts`
- [ ] Create `src/core/api/hooks/useInvestigateException.ts`
- [ ] Create `src/core/api/hooks/useBulkInvestigateExceptions.ts`
- [ ] Define TypeScript interfaces for exceptions
- [ ] Write unit tests for hooks

**Success Criteria**: Hooks fetch and mutate data correctly

#### Day 6: Component Migration
- [ ] Update `ExceptionsPage.tsx` to use `useExceptions` hook
- [ ] Remove `MOCK_EXCEPTIONS` constant
- [ ] Add loading states
- [ ] Add error handling
- [ ] Update `InvestigationForm` to use mutation hook
- [ ] Update `BulkInvestigateModal` to use mutation hook
- [ ] Add permission checks (only supervisors+ can investigate)
- [ ] Test investigation workflow end-to-end

**Success Criteria**: Investigation workflow works with real data

**Effort**: 2 days

### Module 3: Warnings (Day 7)

#### Day 7: Complete Migration
- [ ] Create `src/core/api/hooks/useWarnings.ts`
- [ ] Create `src/core/api/hooks/useGenerateWarning.ts`
- [ ] Update `WarningsPage.tsx` to use hooks
- [ ] Remove `MOCK_WARNINGS` constant
- [ ] Update `WarningGenerationWizard` to use mutation hook
- [ ] Add loading states
- [ ] Add error handling
- [ ] Test warning generation workflow

**Success Criteria**: Warning generation works with real data

**Effort**: 1 day

### Phase 2 Milestone Checkpoint
- [ ] All 3 critical modules use live API data
- [ ] Zero mock data in attendance, exceptions, warnings
- [ ] All hooks have unit tests
- [ ] Error handling tested
- [ ] Department scoping verified

**Success Criteria**: Phase 2 complete, ready for Phase 3

---

## Phase 3: Dashboard Modules (Days 8-10)

### Objectives
- Migrate 3 dashboard modules from mock data to API
- Implement real-time data refresh
- Optimize performance with caching

### Module 4: Dashboard (Day 8)

#### Day 8: Complete Migration
- [ ] Create `src/core/api/hooks/useDashboardKPI.ts`
- [ ] Update `DashboardPage.tsx` to use hook
- [ ] Remove all mock constants
- [ ] Add auto-refresh (5 minute interval)
- [ ] Add loading states
- [ ] Add error handling
- [ ] Test with different departments

**Success Criteria**: Dashboard shows real KPIs with auto-refresh

**Effort**: 1 day

### Module 5: Employees (Day 9)

#### Day 9: Complete Migration
- [ ] Create `src/core/api/hooks/useEmployees.ts`
- [ ] Update `EmployeeListPage.tsx` to use hook
- [ ] Remove `MOCK_EMPLOYEES` constant
- [ ] Add pagination support
- [ ] Add search functionality
- [ ] Add loading states
- [ ] Add error handling
- [ ] Test employee detail navigation

**Success Criteria**: Employee list works with real data and pagination

**Effort**: 1 day

### Module 6: Manager Dashboard (Day 10)

#### Day 10: Complete Migration
- [ ] Create `src/core/api/hooks/useManagerDashboard.ts`
- [ ] Update `ManagerDashboardPage.tsx` to use hook
- [ ] Remove all mock constants
- [ ] Add auto-refresh (5 minute interval)
- [ ] Add loading states
- [ ] Add error handling
- [ ] Test view mode switching (team/department/offenders/trend)

**Success Criteria**: Manager dashboard shows real data with view modes

**Effort**: 1 day

### Phase 3 Milestone Checkpoint
- [ ] All 3 dashboard modules use live API data
- [ ] Zero mock data in dashboard, employees, manager-dashboard
- [ ] Auto-refresh working on dashboards
- [ ] Pagination working on employee list
- [ ] Performance optimized (caching, stale time)

**Success Criteria**: Phase 3 complete, ready for Phase 4

---

## Phase 4: Enhancement (Days 11-13)

### Objectives
- Enhance 7 already-integrated modules
- Add error boundaries
- Implement retry logic
- Improve loading states
- Add performance optimizations

### Module 7: Escalations (Day 11, Morning)

- [ ] Add error boundary to `EscalationsPage.tsx`
- [ ] Add retry logic to hooks
- [ ] Improve loading states
- [ ] Add empty state for no escalations
- [ ] Test filter functionality

**Effort**: 0.5 days

### Module 8: Audit (Day 11, Afternoon)

- [ ] Add error boundary to `AuditLogPage.tsx`
- [ ] Add retry logic
- [ ] Improve loading states
- [ ] Add empty state
- [ ] Test filter functionality

**Effort**: 0.5 days

### Module 9: Reports (Day 12, Morning)

- [ ] Add error boundary to `ReportsPage.tsx`
- [ ] Add retry logic
- [ ] Improve loading states
- [ ] Add empty state
- [ ] Test export functionality

**Effort**: 0.5 days

### Module 10: Executive Dashboard (Day 12, Afternoon)

- [ ] Add error boundary to `ExecutiveDashboardPage.tsx`
- [ ] Add retry logic
- [ ] Improve loading states
- [ ] Add auto-refresh (30 second interval)
- [ ] Test heatmap and leaderboard

**Effort**: 0.5 days

### Module 11: HR Command Centre (Day 13, Morning)

- [ ] Add error boundary to `HRCommandCentrePage.tsx`
- [ ] Add retry logic
- [ ] Improve loading states
- [ ] Add auto-refresh (5 minute interval)
- [ ] Test department risk table

**Effort**: 0.5 days

### Module 12: Admin (Day 13, Afternoon)

- [ ] Add error boundary to `UserManagementPage.tsx`
- [ ] Add error boundary to `DepartmentSettingsPage.tsx`
- [ ] Add error boundary to `SystemSettingsPage.tsx`
- [ ] Add retry logic
- [ ] Improve loading states
- [ ] Test user CRUD operations

**Effort**: 0.5 days

### Module 13: Auth (Day 13, Evening)

- [ ] Improve error handling in `LoginPage.tsx`
- [ ] Add loading state during login
- [ ] Add error message display
- [ ] Test login with invalid credentials
- [ ] Test session expiry handling

**Effort**: 0.5 days

### Phase 4 Milestone Checkpoint
- [ ] All 7 modules have error boundaries
- [ ] All modules have retry logic
- [ ] All modules have improved loading states
- [ ] All modules have empty states
- [ ] Performance optimized across all modules

**Success Criteria**: Phase 4 complete, ready for Phase 5

---

## Phase 5: Testing & Validation (Days 14-15)

### Objectives
- Comprehensive integration testing
- Error scenario testing
- Performance testing
- User acceptance testing
- Bug fixes and polish

### Day 14: Integration Testing

#### Morning: Automated Tests
- [ ] Run all unit tests for hooks
- [ ] Run all integration tests
- [ ] Check code coverage (target: 80%+)
- [ ] Fix any failing tests
- [ ] Add missing tests

#### Afternoon: Manual Testing
- [ ] Test all 13 modules with real data
- [ ] Test with different user roles (Operator, Supervisor, Manager, HR, Admin)
- [ ] Test department scoping
- [ ] Test permission enforcement
- [ ] Test error scenarios (API down, network error, timeout)

**Success Criteria**: All tests pass, no critical bugs

### Day 15: Performance & UAT

#### Morning: Performance Testing
- [ ] Test dashboard load time (target: < 2 seconds)
- [ ] Test list pagination performance
- [ ] Test auto-refresh performance
- [ ] Check memory usage
- [ ] Optimize bundle size
- [ ] Run Lighthouse audit

#### Afternoon: User Acceptance Testing
- [ ] Demo to stakeholders
- [ ] Collect feedback
- [ ] Fix critical issues
- [ ] Update documentation
- [ ] Prepare deployment checklist

**Success Criteria**: UAT passed, ready for deployment

---

## Milestone Summary

| Day | Milestone | Deliverables |
|-----|-----------|--------------|
| Day 2 | Infrastructure Complete | Query client, hooks, error handling, testing setup |
| Day 6 | Critical Modules Complete | Attendance, Exceptions, Warnings migrated |
| Day 10 | Dashboard Modules Complete | Dashboard, Employees, Manager Dashboard migrated |
| Day 13 | Enhancement Complete | All 7 modules enhanced |
| Day 15 | Testing Complete | All tests pass, UAT passed |

---

## Risk Register

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Backend APIs not ready | Medium | High | Use mock server for development |
| API contract mismatch | Low | High | Define TypeScript interfaces first |
| Performance issues | Medium | Medium | Implement caching, optimize queries |
| Permission bugs | Medium | High | Test with all user roles |
| Time overrun | Low | Medium | Prioritize critical modules |

---

## Success Criteria

### Functional Requirements
- [ ] All 13 modules use live API data
- [ ] Zero mock data remaining
- [ ] All CRUD operations work
- [ ] All filters work
- [ ] All pagination works
- [ ] All exports work

### Non-Functional Requirements
- [ ] Dashboard load time < 2 seconds
- [ ] List pagination smooth (no jank)
- [ ] Auto-refresh working
- [ ] Error handling comprehensive
- [ ] Loading states clear
- [ ] Empty states helpful

### Quality Requirements
- [ ] Code coverage > 80%
- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] No TypeScript errors
- [ ] No ESLint errors
- [ ] Performance audit passed

### User Experience Requirements
- [ ] Loading states clear and helpful
- [ ] Error messages actionable
- [ ] Empty states informative
- [ ] Transitions smooth
- [ ] Responsive design working
- [ ] Accessibility compliant

---

## Deployment Checklist

### Pre-Deployment
- [ ] All tests pass
- [ ] Code review complete
- [ ] Documentation updated
- [ ] Environment variables configured
- [ ] API endpoints verified
- [ ] Database migrations applied

### Deployment
- [ ] Build production bundle
- [ ] Deploy to staging
- [ ] Smoke test staging
- [ ] Deploy to production
- [ ] Smoke test production
- [ ] Monitor for errors

### Post-Deployment
- [ ] Monitor error logs
- [ ] Monitor performance metrics
- [ ] Collect user feedback
- [ ] Fix any issues
- [ ] Update changelog

---

## Rollback Plan

If critical issues are discovered post-deployment:

1. **Immediate Rollback**: Revert to previous deployment
2. **Investigate**: Identify root cause
3. **Fix**: Implement fix in development
4. **Test**: Verify fix in staging
5. **Redeploy**: Deploy fixed version

---

## Communication Plan

### Daily Standups
- **Time**: 9:00 AM daily
- **Duration**: 15 minutes
- **Attendees**: SubAgent B, Project Manager, Backend Lead
- **Agenda**: Progress, blockers, next steps

### Weekly Reviews
- **Time**: Friday 3:00 PM
- **Duration**: 1 hour
- **Attendees**: SubAgent B, Project Manager, Backend Lead, QA Lead
- **Agenda**: Week summary, demo, next week plan

### Stakeholder Updates
- **Frequency**: Every 5 days (after each phase)
- **Format**: Email + demo
- **Content**: Progress, milestones, risks, next steps

---

## Resource Allocation

| Role | Allocation | Duration |
|------|-----------|----------|
| SubAgent B (Frontend) | 100% | 15 days |
| Backend Support | 20% | 15 days |
| QA Support | 30% | Days 14-15 |
| Project Manager | 10% | 15 days |

---

## Tools & Technologies

| Tool | Purpose |
|------|---------|
| React Query | Data fetching and caching |
| TypeScript | Type safety |
| Vitest | Unit testing |
| React Testing Library | Component testing |
| Lighthouse | Performance auditing |
| Sentry | Error tracking |
| GitHub | Version control |
| Jira | Task tracking |

---

## Appendix: Hook Implementation Checklist

For each module, verify:
- [ ] Hook created in `src/core/api/hooks/`
- [ ] TypeScript interfaces defined
- [ ] Query key added to `queryKeys.ts`
- [ ] Unit tests written
- [ ] Error handling implemented
- [ ] Loading states implemented
- [ ] Department scoping verified
- [ ] Permission checks added
- [ ] Component updated to use hook
- [ ] Mock data removed
- [ ] Manual testing completed

---

**Document Status**: Ready for Execution  
**Next Step**: Exit plan-file-mode and begin Phase 1  
**Estimated Start Date**: When implementation mode becomes available  
**Estimated Completion Date**: 15 working days after start
