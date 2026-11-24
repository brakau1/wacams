// Mock data for WACAMS frontend mock mode
// Provides realistic test data for all features

export const mockDepartments = [
  { id: '1', name: 'Production', code: 'PROD', description: 'Main production department', supervisorId: '2', supervisorName: 'John Manager', employeeCount: 25, targetAttendance: 95, createdAt: '2024-01-01T00:00:00Z', updatedAt: '2024-01-01T00:00:00Z' },
  { id: '2', name: 'Quality Control', code: 'QC', description: 'Quality assurance and control', supervisorId: '27', supervisorName: 'Sarah Quality', employeeCount: 10, targetAttendance: 98, createdAt: '2024-01-01T00:00:00Z', updatedAt: '2024-01-01T00:00:00Z' },
  { id: '3', name: 'Maintenance', code: 'MAINT', description: 'Equipment and facility maintenance', supervisorId: '38', supervisorName: 'Mike Maintenance', employeeCount: 8, targetAttendance: 92, createdAt: '2024-01-01T00:00:00Z', updatedAt: '2024-01-01T00:00:00Z' },
  { id: '4', name: 'Logistics', code: 'LOG', description: 'Warehouse and logistics operations', supervisorId: '46', supervisorName: 'Lisa Logistics', employeeCount: 12, targetAttendance: 94, createdAt: '2024-01-01T00:00:00Z', updatedAt: '2024-01-01T00:00:00Z' },
  { id: '5', name: 'Administration', code: 'ADMIN', description: 'Administrative and support staff', supervisorId: '1', supervisorName: 'Development User', employeeCount: 5, targetAttendance: 100, createdAt: '2024-01-01T00:00:00Z', updatedAt: '2024-01-01T00:00:00Z' }
]

export const mockEmployees = Array.from({ length: 50 }, (_, i) => {
  const id = String(i + 1)
  const deptId = String((i % 5) + 1)
  const dept = mockDepartments.find(d => d.id === deptId)!
  const isSupervisor = i === 1 || i === 26 || i === 37 || i === 45 || i === 0
  
  return {
    id,
    employeeId: `EMP${String(i + 1).padStart(3, '0')}`,
    firstName: ['Alice', 'Bob', 'Carol', 'David', 'Eve', 'Frank', 'Grace', 'Henry', 'Iris', 'Jack', 'Karen', 'Leo', 'Mia', 'Noah', 'Olivia', 'Paul', 'Quinn', 'Ryan', 'Sophia', 'Tom', 'Uma', 'Victor', 'Wendy', 'Xavier', 'Yara', 'Zane', 'Amara', 'Brian', 'Chloe', 'Derek', 'Elena', 'Felix', 'Gina', 'Hugo', 'Ivy', 'James', 'Kara', 'Liam', 'Nora', 'Oscar', 'Penny', 'Reed', 'Stella', 'Troy', 'Ursula', 'Vince', 'Willa', 'Xena', 'Yuri', 'Zara'][i],
    lastName: ['Johnson', 'Smith', 'Davis', 'Wilson', 'Brown', 'Taylor', 'Anderson', 'Martinez', 'Thomas', 'Jackson', 'White', 'Harris', 'Clark', 'Lewis', 'Robinson', 'Walker', 'Young', 'Allen', 'King', 'Wright', 'Scott', 'Torres', 'Nguyen', 'Hill', 'Flores', 'Green', 'Adams', 'Nelson', 'Baker', 'Hall', 'Rivera', 'Campbell', 'Mitchell', 'Carter', 'Roberts', 'Gomez', 'Phillips', 'Evans', 'Turner', 'Diaz', 'Parker', 'Cruz', 'Edwards', 'Collins', 'Reyes', 'Stewart', 'Morris', 'Morales', 'Murphy', 'Cook'][i],
    displayName: `${['Alice', 'Bob', 'Carol', 'David', 'Eve', 'Frank', 'Grace', 'Henry', 'Iris', 'Jack', 'Karen', 'Leo', 'Mia', 'Noah', 'Olivia', 'Paul', 'Quinn', 'Ryan', 'Sophia', 'Tom', 'Uma', 'Victor', 'Wendy', 'Xavier', 'Yara', 'Zane', 'Amara', 'Brian', 'Chloe', 'Derek', 'Elena', 'Felix', 'Gina', 'Hugo', 'Ivy', 'James', 'Kara', 'Liam', 'Nora', 'Oscar', 'Penny', 'Reed', 'Stella', 'Troy', 'Ursula', 'Vince', 'Willa', 'Xena', 'Yuri', 'Zara'][i]} ${['Johnson', 'Smith', 'Davis', 'Wilson', 'Brown', 'Taylor', 'Anderson', 'Martinez', 'Thomas', 'Jackson', 'White', 'Harris', 'Clark', 'Lewis', 'Robinson', 'Walker', 'Young', 'Allen', 'King', 'Wright', 'Scott', 'Torres', 'Nguyen', 'Hill', 'Flores', 'Green', 'Adams', 'Nelson', 'Baker', 'Hall', 'Rivera', 'Campbell', 'Mitchell', 'Carter', 'Roberts', 'Gomez', 'Phillips', 'Evans', 'Turner', 'Diaz', 'Parker', 'Cruz', 'Edwards', 'Collins', 'Reyes', 'Stewart', 'Morris', 'Morales', 'Murphy', 'Cook'][i]}`,
    email: `employee${i + 1}@wacams.local`,
    departmentId: dept.id,
    departmentName: dept.name,
    managerId: isSupervisor ? null : dept.supervisorId,
    managerName: isSupervisor ? null : dept.supervisorName,
    jobTitle: isSupervisor ? 'Supervisor' : 'Operator',
    status: (['Active', 'Active', 'Active', 'Active', 'Active', 'Active', 'Active', 'Active', 'OnLeave', 'Active'][i % 10] as any),
    hireDate: `202${Math.floor(i / 10)}-${String((i % 12) + 1).padStart(2, '0')}-15`,
    shiftCode: ['DAY', 'AFTERNOON', 'NIGHT'][i % 3],
    isClockIn: i % 3 === 0,
    clockInTime: i % 3 === 0 ? new Date(Date.now() - (i % 8) * 3600000).toISOString() : null,
    clockOutTime: null,
    createdAt: '2024-01-01T00:00:00Z',
    updatedAt: new Date().toISOString()
  }
})

const today = new Date()
const todayStr = today.toISOString().split('T')[0]

export const mockAttendanceRecords = Array.from({ length: 350 }, (_, i) => {
  const empIdx = i % 50
  const emp = mockEmployees[empIdx]
  const dayOffset = Math.floor(i / 50)
  const date = new Date(Date.now() - dayOffset * 86400000).toISOString().split('T')[0]
  const status = (['Present', 'Present', 'Present', 'Present', 'Present', 'Present', 'Late', 'EarlyDeparture', 'Absent', 'SickLeave'][i % 10] as any)
  const isLate = status === 'Late'
  const isAbsent = status === 'Absent' || status === 'SickLeave'
  const isEarly = status === 'EarlyDeparture'
  
  return {
    id: `ATT-${i}`,
    employeeId: emp.id,
    employeeName: emp.displayName,
    displayName: emp.displayName,
    departmentId: emp.departmentId,
    departmentName: emp.departmentName,
    date,
    scheduledShift: emp.shiftCode,
    shiftStartTime: emp.shiftCode === 'DAY' ? '06:00' : emp.shiftCode === 'AFTERNOON' ? '14:00' : '22:00',
    shiftEndTime: emp.shiftCode === 'DAY' ? '14:00' : emp.shiftCode === 'AFTERNOON' ? '22:00' : '06:00',
    clockInTime: isAbsent ? null : `${date}T${isLate ? `0${6 + ((empIdx + dayOffset) % 3)}:${String(15 + (empIdx % 45)).padStart(2, '0')}` : `0${6 + (empIdx % 3)}:${String(empIdx % 60).padStart(2, '0')}`}:00Z`,
    clockOutTime: isAbsent ? null : `${date}T${isEarly ? '12:30' : '14:05'}:00Z`,
    status,
    lateMinutes: isLate ? 10 + (empIdx % 20) : 0,
    earlyMinutes: isEarly ? 15 + (empIdx % 30) : 0,
    totalHours: isAbsent ? 0 : isEarly ? 6.5 : 8,
    notes: status === 'SickLeave' ? 'Sick leave - medical certificate provided' : null,
    correctedBy: null,
    correctionReason: null
  }
})

const exceptionTypes = ['LateArrival', 'EarlyDeparture', 'UnplannedAbsenteeism', 'MissingClockIn', 'MissingClockOut']
const exceptionStatuses = ['Open', 'Open', 'UnderReview', 'Investigated', 'Closed']

export const mockExceptions = Array.from({ length: 25 }, (_, i) => {
  const emp = mockEmployees[i % 50]
  const type = exceptionTypes[i % exceptionTypes.length]
  const status = exceptionStatuses[i % exceptionStatuses.length]
  
  return {
    id: `EXC-${String(i + 1).padStart(3, '0')}`,
    employeeId: emp.id,
    employeeName: emp.displayName,
    displayName: emp.displayName,
    departmentId: emp.departmentId,
    departmentName: emp.departmentName,
    exceptionType: type as any,
    date: new Date(Date.now() - (i % 7) * 86400000).toISOString().split('T')[0],
    description: `${type} - ${emp.displayName} on ${new Date(Date.now() - (i % 7) * 86400000).toISOString().split('T')[0]}`,
    lostProductionMinutes: type === 'UnplannedAbsenteeism' ? 480 : type === 'LateArrival' ? 15 + (i % 30) : 30,
    status: status as any,
    assignedTo: emp.managerId,
    assignedToName: emp.managerName,
    investigatorNotes: status === 'Investigated' || status === 'Closed' ? 'Reviewed and documented. Warning issued if applicable.' : null,
    investigatedAt: status === 'Investigated' || status === 'Closed' ? new Date(Date.now() - i * 2 * 3600000).toISOString() : null,
    investigatedBy: status === 'Investigated' || status === 'Closed' ? '2' : null,
    createdAt: new Date(Date.now() - i * 24 * 3600000).toISOString(),
    updatedAt: new Date(Date.now() - i * 12 * 3600000).toISOString()
  }
})

const warningTypes = ['Counselling', 'VerbalWarning', 'WrittenWarning', 'FinalWrittenWarning']
const warningStatuses = ['Draft', 'Issued', 'Acknowledged', 'Archived']

export const mockWarnings = Array.from({ length: 15 }, (_, i) => {
  const emp = mockEmployees[(i * 3) % 50]
  
  return {
    id: `WARN-${String(i + 1).padStart(3, '0')}`,
    employeeId: emp.id,
    employeeName: emp.displayName,
    displayName: emp.displayName,
    departmentId: emp.departmentId,
    departmentName: emp.departmentName,
    warningType: warningTypes[i % warningTypes.length] as any,
    relatedExceptionId: `EXC-${String(i + 1).padStart(3, '0')}`,
    title: `${warningTypes[i % warningTypes.length]} - ${emp.displayName}`,
    description: `Issued for repeated attendance violations. ${warningTypes[i % warningTypes.length]} level.`,
    severity: (i % 4) + 1,
    signedFormUrl: i % 3 === 0 ? '/mock/signed-forms/form-1.pdf' : null,
    signedBy: i % 3 === 0 ? emp.displayName : null,
    signedAt: i % 3 === 0 ? new Date(Date.now() - i * 12 * 3600000).toISOString() : null,
    issuedBy: '2',
    issuedByName: 'John Manager',
    status: warningStatuses[i % warningStatuses.length] as any,
    escalationLevel: i % 4,
    createdAt: new Date(Date.now() - i * 48 * 3600000).toISOString(),
    updatedAt: new Date(Date.now() - i * 24 * 3600000).toISOString()
  }
})

export const mockEscalations = Array.from({ length: 8 }, (_, i) => {
  const emp = mockEmployees[(i * 5) % 50]
  const statuses = ['Pending', 'InProgress', 'Completed', 'Expired']
  
  return {
    id: `ESC-${String(i + 1).padStart(3, '0')}`,
    relatedExceptionId: `EXC-${String(i + 1).padStart(3, '0')}`,
    relatedWarningId: `WARN-${String(i + 1).padStart(3, '0')}`,
    fromEmployeeId: '2',
    fromEmployeeName: 'John Manager',
    toEmployeeId: '1',
    toEmployeeName: 'Development User',
    toRole: 'SuperAdmin',
    escalationLevel: (i % 3) + 1,
    status: statuses[i % statuses.length] as any,
    message: `Escalation for ${emp.displayName} - repeated violations require senior management review`,
    respondedAt: statuses[i % statuses.length] !== 'Pending' ? new Date(Date.now() - i * 6 * 3600000).toISOString() : null,
    respondedBy: statuses[i % statuses.length] !== 'Pending' ? '1' : null,
    responseNotes: statuses[i % statuses.length] === 'Completed' ? 'Reviewed and approved. Proceed with corrective action.' : null,
    deadline: new Date(Date.now() + (i + 1) * 24 * 3600000).toISOString(),
    completedAt: statuses[i % statuses.length] === 'Completed' ? new Date(Date.now() - i * 4 * 3600000).toISOString() : null,
    createdAt: new Date(Date.now() - i * 48 * 3600000).toISOString()
  }
})

export const mockAuditLogs = Array.from({ length: 30 }, (_, i) => {
  const actions = ['CREATE', 'UPDATE', 'LOGIN', 'CLOCK_IN', 'INVESTIGATE', 'GENERATE_WARNING', 'ESCALATE']
  const action = actions[i % actions.length]
  const emp = mockEmployees[i % 50]
  
  return {
    id: `AUDIT-${String(i + 1).padStart(4, '0')}`,
    entity_type: action === 'LOGIN' ? 'User' : action === 'CLOCK_IN' || action === 'CLOCK_OUT' ? 'AttendanceRecord' : action === 'INVESTIGATE' ? 'AttendanceException' : action === 'GENERATE_WARNING' ? 'WarningLetter' : action === 'ESCALATE' ? 'Escalation' : 'Employee',
    entity_id: `ENTITY-${i + 1}`,
    action: action as any,
    summary: `${action} performed by ${i % 2 === 0 ? 'Development User' : 'John Manager'} on ${emp.displayName}`,
    oldValue: null,
    newValue: { action, timestamp: new Date(Date.now() - i * 3600000).toISOString() },
    performed_by: i % 2 === 0 ? '1' : '2',
    performed_by_name: i % 2 === 0 ? 'Development User' : 'John Manager',
    ip_address: '192.168.1.100',
    user_agent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/120.0',
    created_at: new Date(Date.now() - i * 3600000).toISOString()
  }
})

export const mockDepartmentKPIs = mockDepartments.map((dept, i) => ({
  departmentId: dept.id,
  departmentName: dept.name,
  attendancePercentage: 88 + (i * 2.5),
  totalEmployees: i === 0 ? 25 : i === 1 ? 10 : i === 2 ? 8 : i === 3 ? 12 : 5,
  presentCount: Math.floor((88 + (i * 2.5)) / 100 * (i === 0 ? 25 : i === 1 ? 10 : i === 2 ? 8 : i === 3 ? 12 : 5)),
  lateCount: 2 + (i % 3),
  absentCount: 1 + (i % 2)
}))

export const mockTrendData = Array.from({ length: 14 }, (_, i) => ({
  date: new Date(Date.now() - (13 - i) * 86400000).toISOString().split('T')[0],
  onTimePercentage: 88 + Math.random() * 10,
  lateCount: Math.floor(Math.random() * 8) + 1,
  exceptionCount: Math.floor(Math.random() * 12) + 2
}))

export const mockTopLateEntries = Array.from({ length: 10 }, (_, i) => ({
  employeeId: mockEmployees[i].id,
  displayName: mockEmployees[i].displayName,
  departmentName: mockEmployees[i].departmentName,
  lateCount: 12 - i,
  totalLateMinutes: (12 - i) * (15 + i * 3)
}))

export const mockDashboardKPI = {
  totalEmployees: 50,
  onTimePercentage: 91.2,
  averageLateMinutes: 12.5,
  activeExceptions: mockExceptions.filter(e => e.status === 'Open' || e.status === 'UnderReview').length,
  pendingWarnings: mockWarnings.filter(w => w.status === 'Draft' || w.status === 'Issued').length,
  departmentStats: mockDepartmentKPIs,
  trendData: mockTrendData,
  topLateArrivals: mockTopLateEntries,
  recentExceptions: mockExceptions.slice(0, 10)
}

export const mockAttendanceSummary = {
  departmentId: '1',
  departmentName: 'Production',
  totalEmployees: 50,
  presentCount: 42,
  lateCount: 4,
  absentCount: 2,
  awolCount: 1,
  sickLeaveCount: 1,
  familyResponsibilityCount: 0,
  annualLeaveCount: 2,
  attendancePercentage: 91.2,
  totalLateMinutes: 87,
  totalEarlyMinutes: 45,
  totalLostMinutes: 1320
}

export const mockHRCommandCentreData = {
  kpis: {
    plantAttendancePercentage: 91.2,
    lostProductionHours: 22,
    openWarnings: 5,
    finalWarnings: 2,
    awolCases: 1,
    escalations: 3,
    departmentsAtRisk: 2
  },
  attendanceTrend: mockTrendData,
  departmentRanking: mockDepartmentKPIs.map((dept, i) => ({
    departmentId: dept.departmentId,
    departmentName: dept.departmentName,
    attendancePercentage: dept.attendancePercentage,
    totalEmployees: dept.totalEmployees,
    presentCount: dept.presentCount,
    lateCount: dept.lateCount,
    absentCount: dept.absentCount,
    exceptionCount: dept.lateCount + dept.absentCount,
    riskLevel: (dept.attendancePercentage < 90 ? 'critical' : dept.attendancePercentage < 93 ? 'high' : dept.attendancePercentage < 96 ? 'medium' : 'low') as any
  })),
  topOffenders: mockTopLateEntries.slice(0, 5),
  exceptionAnalysis: [
    { exceptionType: 'LateArrival', count: 15, percentage: 35, lostMinutes: 225 },
    { exceptionType: 'EarlyDeparture', count: 8, percentage: 19, lostMinutes: 160 },
    { exceptionType: 'UnplannedAbsenteeism', count: 10, percentage: 23, lostMinutes: 4800 },
    { exceptionType: 'MissingClockIn', count: 5, percentage: 12, lostMinutes: 0 },
    { exceptionType: 'MissingClockOut', count: 5, percentage: 11, lostMinutes: 0 }
  ]
}

export const mockExecutiveSummary = {
  employeesPresent: 42,
  currentAbsenteeism: 8,
  attendancePercentage: 91.2,
  lostMinutes: 1320,
  lostHours: 22,
  openEscalations: 3,
  pendingInvestigations: 5,
  correctiveActions: 4,
  totalActiveEmployees: 50,
  asOf: new Date().toISOString()
}

export const mockDepartmentHeatmap = mockDepartmentKPIs.map(dept => ({
  departmentId: dept.departmentId,
  departmentName: dept.departmentName,
  departmentCode: mockDepartments.find(d => d.id === dept.departmentId)?.code || '',
  totalEmployees: dept.totalEmployees,
  presentCount: dept.presentCount,
  lateCount: dept.lateCount,
  absentCount: dept.absentCount,
  lostMinutes: (dept.lateCount + dept.absentCount) * 60,
  attendancePercentage: dept.attendancePercentage,
  targetPercentage: mockDepartments.find(d => d.id === dept.departmentId)?.targetAttendance || 95,
  gapToTarget: dept.attendancePercentage - (mockDepartments.find(d => d.id === dept.departmentId)?.targetAttendance || 95),
  heatLevel: (dept.attendancePercentage >= 95 ? 'green' : dept.attendancePercentage >= 90 ? 'yellow' : dept.attendancePercentage >= 85 ? 'orange' : 'red') as any
}))

export const mockDepartmentLeaderboard = [...mockDepartmentKPIs]
  .sort((a, b) => b.attendancePercentage - a.attendancePercentage)
  .map((dept, idx) => ({
    departmentId: dept.departmentId,
    departmentName: dept.departmentName,
    departmentCode: mockDepartments.find(d => d.id === dept.departmentId)?.code || '',
    totalEmployees: dept.totalEmployees,
    attendancePercentage: dept.attendancePercentage,
    targetPercentage: mockDepartments.find(d => d.id === dept.departmentId)?.targetAttendance || 95,
    totalLostHours: ((dept.lateCount + dept.absentCount) * 60) / 60,
    exceptionsCount: dept.lateCount + dept.absentCount,
    trend: (idx === 0 ? 'up' : idx === mockDepartmentKPIs.length - 1 ? 'down' : 'stable') as any,
    rank: idx + 1
  }))

export const mockSystemSettings = [
  { id: '1', key: 'attendance.grace_period_minutes', value: '10', category: 'attendance', description: 'Grace period for late arrivals in minutes', updatedAt: new Date().toISOString(), updatedBy: '1' },
  { id: '2', key: 'warnings.auto_escalation_days', value: '7', category: 'warnings', description: 'Days before automatic escalation of unaddressed warnings', updatedAt: new Date().toISOString(), updatedBy: '1' },
  { id: '3', key: 'notifications.email_enabled', value: 'true', category: 'notifications', description: 'Enable email notifications', updatedAt: new Date().toISOString(), updatedBy: '1' },
  { id: '4', key: 'attendance.late_threshold_minutes', value: '5', category: 'attendance', description: 'Minutes after shift start to classify as late', updatedAt: new Date().toISOString(), updatedBy: '1' },
  { id: '5', key: 'discipline.progressive_steps', value: '4', category: 'discipline', description: 'Number of progressive discipline steps', updatedAt: new Date().toISOString(), updatedBy: '1' }
]

export const mockGISEmployees = Array.from({ length: 50 }, (_, i) => ({
  id: `GIS-EMP-${i + 1}`,
  employeeId: mockEmployees[i].employeeId,
  name: mockEmployees[i].displayName,
  department: mockEmployees[i].departmentName,
  lastSync: new Date(Date.now() - (i % 24) * 3600000).toISOString(),
  status: 'Synced' as const
}))

export const mockGISTransactions = Array.from({ length: 100 }, (_, i) => ({
  id: `GIS-TXN-${String(i + 1).padStart(4, '0')}`,
  employeeId: mockEmployees[i % 50].employeeId,
  employeeName: mockEmployees[i % 50].displayName,
  type: i % 2 === 0 ? 'ClockIn' : 'ClockOut',
  timestamp: new Date(Date.now() - (i % 48) * 3600000).toISOString(),
  location: 'Main Gate',
  status: i % 10 === 0 ? 'Failed' : 'Success',
  errorMessage: i % 10 === 0 ? 'Network timeout' : null
}))

export const mockGISSyncResults = Array.from({ length: 7 }, (_, i) => ({
  id: `GIS-SYNC-${i + 1}`,
  syncDate: new Date(Date.now() - i * 86400000).toISOString().split('T')[0],
  totalRecords: 150 + Math.floor(Math.random() * 50),
  successfulImports: 145 + Math.floor(Math.random() * 45),
  failedImports: Math.floor(Math.random() * 5),
  duplicatesSkipped: Math.floor(Math.random() * 3),
  syncDuration: `${30 + Math.floor(Math.random() * 60)}s`,
  status: i === 0 ? 'Running' : 'Completed'
}))

export const mockGISExceptionResults = Array.from({ length: 20 }, (_, i) => ({
  id: `GIS-EXC-${String(i + 1).padStart(3, '0')}`,
  employeeId: mockEmployees[i % 50].employeeId,
  employeeName: mockEmployees[i % 50].displayName,
  exceptionType: exceptionTypes[i % exceptionTypes.length],
  detectedAt: new Date(Date.now() - i * 4 * 3600000).toISOString(),
  autoDetected: true,
  syncedToWACAMS: i % 3 !== 0,
  wacamsExceptionId: i % 3 !== 0 ? `EXC-${String(i + 1).padStart(3, '0')}` : null
}))
