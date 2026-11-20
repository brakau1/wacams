# HR Command Centre - Implementation Summary

## Overview
Built a comprehensive HR Command Centre dashboard for plant-wide attendance and disciplinary oversight.

## Features Delivered

### 7 KPI Cards
1. **Plant Attendance %** - Overall attendance rate with color-coded status
2. **Lost Production Hours** - Total hours lost to attendance issues
3. **Open Warnings** - Warnings awaiting action
4. **Final Warnings** - Critical disciplinary cases
5. **AWOL Cases** - Unauthorised absences
6. **Escalations** - Active escalation cases
7. **Departments at Risk** - Departments needing attention

### 4 Charts
1. **Attendance Trend** - 14-day line chart showing attendance patterns
2. **Department Ranking** - Bar chart comparing department performance
3. **Top Offenders** - List of employees with most attendance issues
4. **Exception Analysis** - Pie chart breakdown of exception types

### Department Risk Table
- Comprehensive table showing all departments
- Metrics: Employees, Present, Late, Absent, Exceptions, Attendance %
- Risk level indicators (Low/Medium/High/Critical) with color coding

## Files Created

### Components
- `Frontend/src/features/hr-command-centre/HRCommandCentrePage.tsx` - Main page
- `Frontend/src/features/hr-command-centre/components/ExceptionAnalysisChart.tsx` - Pie chart component

### Types & API
- Added `HRCommandCentreData`, `HRCommandCentreKPIs`, `DepartmentRanking`, `ExceptionAnalysis` types to `types.ts`
- Added `hrCommandCentre` endpoint to `endpoints.ts`

### Routing
- Added route `/hr-command-centre` in `App.tsx`
- Added navigation link in `Sidebar.tsx` (visible to HR, Admin, SuperAdmin roles)

## Technical Details

### Data Structure
```typescript
interface HRCommandCentreData {
  kpis: HRCommandCentreKPIs
  attendanceTrend: TrendPoint[]
  departmentRanking: DepartmentRanking[]
  topOffenders: TopLateEntry[]
  exceptionAnalysis: ExceptionAnalysis[]
}
```

### Mock Data
Includes realistic mock data for development and testing:
- 5 departments with varying performance levels
- 7 top offenders with different severity
- 8 exception types with distribution percentages
- 14-day trend data

### Reused Components
Leverages existing dashboard components:
- `KPICard` - For KPI display
- `TrendChart` - For attendance trends
- `DepartmentChart` - For department ranking
- `TopLateList` - For top offenders

### New Component
- `ExceptionAnalysisChart` - Custom pie chart using Recharts
  - Color-coded by exception type
  - Interactive tooltips
  - Summary statistics

## Access Control
Visible only to users with roles:
- HR
- Admin
- SuperAdmin

## API Integration
Endpoint: `/api/reports/hr-command-centre`
- Fetches all KPIs and chart data in one call
- Falls back to mock data if API unavailable
- Loading state with spinner

## Build Status
✅ No TypeScript errors in new files
✅ Routes properly configured
✅ Navigation integrated
✅ Responsive design (mobile, tablet, desktop)

## Next Steps
1. Implement backend API endpoint `/api/reports/hr-command-centre`
2. Add real-time data refresh (optional)
3. Add export functionality (PDF/Excel)
4. Add drill-down capability for departments
5. Add historical comparison view
