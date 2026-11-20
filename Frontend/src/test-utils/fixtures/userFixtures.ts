// ============================================================================
// WACAMS Frontend — User Fixtures for Testing
// ============================================================================
// Mock users for each role, with varying permission levels and department scopes.

import type { User } from '@/core/api/types';

/**
 * Generates a unique ID for test data (deterministic based on seed).
 */
let idCounter = 1000;
const generateId = (): string => `TEST-${idCounter++}`;

// ─── SuperAdmin ──────────────────────────────────────────────────────────────
export const superAdminUser: User = {
  id: generateId(),
  email: 'admin@yfpoza.com',
  firstName: 'System',
  lastName: 'Administrator',
  displayName: 'System Administrator',
  employeeId: 'EMP001',
  roleId: 'ROLE-ADMIN-001',
  roleName: 'SuperAdmin',
  departmentId: 'DEPT-ALL',
  departmentName: 'All Departments',
  managerId: null,
  isActive: true,
  lastLoginAt: new Date().toISOString(),
  createdAt: new Date(Date.now() - 365 * 24 * 60 * 60 * 1000).toISOString(),
  updatedAt: new Date().toISOString(),
};

// ─── Senior Manager ─────────────────────────────────────────────────────────
export const seniorManagerUser: User = {
  id: generateId(),
  email: 'jeremiah@yfpoza.com',
  firstName: 'Jeremiah',
  lastName: 'Bezuidenhout',
  displayName: 'Jeremiah Bezuidenhout',
  employeeId: 'EMP009',
  roleId: 'ROLE-MGR-001',
  roleName: 'SeniorManager',
  departmentId: 'DEPT-PROD',
  departmentName: 'Production',
  managerId: null,
  isActive: true,
  lastLoginAt: new Date().toISOString(),
  createdAt: new Date(Date.now() - 730 * 24 * 60 * 60 * 1000).toISOString(),
  updatedAt: new Date().toISOString(),
};

// ─── Manager ────────────────────────────────────────────────────────────────
export const managerUser: User = {
  id: generateId(),
  email: 'botlhale@yfpoza.com',
  firstName: 'Botlhale',
  lastName: 'Rakau',
  displayName: 'Botlhale Rakau',
  employeeId: 'EMP002',
  roleId: 'ROLE-MANAGER-001',
  roleName: 'Manager',
  departmentId: 'DEPT-PROD',
  departmentName: 'Production',
  managerId: 'EMP001',
  isActive: true,
  lastLoginAt: new Date().toISOString(),
  createdAt: new Date(Date.now() - 500 * 24 * 60 * 60 * 1000).toISOString(),
  updatedAt: new Date().toISOString(),
};

// ─── TeamLeader ─────────────────────────────────────────────────────────────
export const teamLeaderUser: User = {
  id: generateId(),
  email: 'thabiso@yfpoza.com',
  firstName: 'Thabiso',
  lastName: 'Mokoena',
  displayName: 'Thabiso Mokoena',
  employeeId: 'EMP003',
  roleId: 'ROLE-TL-001',
  roleName: 'TeamLeader',
  departmentId: 'DEPT-PROD',
  departmentName: 'Production',
  managerId: 'EMP002',
  isActive: true,
  lastLoginAt: new Date().toISOString(),
  createdAt: new Date(Date.now() - 300 * 24 * 60 * 60 * 1000).toISOString(),
  updatedAt: new Date().toISOString(),
};

// ─── HR ─────────────────────────────────────────────────────────────────────
export const hrUser: User = {
  id: generateId(),
  email: 'hr@yfpoza.com',
  firstName: 'Nomvula',
  lastName: 'Motaung',
  displayName: 'Nomvula Motaung',
  employeeId: 'EMP043',
  roleId: 'ROLE-HR-001',
  roleName: 'Coordinator',
  departmentId: 'DEPT-ADMIN',
  departmentName: 'Administration & Human Resources',
  managerId: null,
  isActive: true,
  lastLoginAt: new Date().toISOString(),
  createdAt: new Date(Date.now() - 400 * 24 * 60 * 60 * 1000).toISOString(),
  updatedAt: new Date().toISOString(),
} as unknown as User; // Cast to handle role type mismatch

// ─── Operator (Minimal permissions) ─────────────────────────────────────────
export const operatorUser: User = {
  id: generateId(),
  email: 'samu@yfpoza.com',
  firstName: 'Samu',
  lastName: 'Nkosi',
  displayName: 'Samu Nkosi',
  employeeId: 'EMP005',
  roleId: 'ROLE-OP-001',
  roleName: 'Operator',
  departmentId: 'DEPT-PROD',
  departmentName: 'Production',
  managerId: 'EMP002',
  isActive: true,
  lastLoginAt: new Date(Date.now() - 2 * 60 * 60 * 1000).toISOString(), // 2 hours ago
  createdAt: new Date(Date.now() - 180 * 24 * 60 * 60 * 1000).toISOString(),
  updatedAt: new Date().toISOString(),
};

// ─── Inactive Operator ──────────────────────────────────────────────────────
export const inactiveOperatorUser: User = {
  id: generateId(),
  email: 'inactive@yfpoza.com',
  firstName: 'Inactive',
  lastName: 'Employee',
  displayName: 'Inactive Employee',
  employeeId: 'EMP025',
  roleId: 'ROLE-OP-002',
  roleName: 'Operator',
  departmentId: 'DEPT-LOG',
  departmentName: 'Logistics & Warehouse',
  managerId: 'EMP023',
  isActive: false,
  lastLoginAt: new Date(Date.now() - 90 * 24 * 60 * 60 * 1000).toISOString(), // 90 days ago
  createdAt: new Date(Date.now() - 365 * 24 * 60 * 60 * 1000).toISOString(),
  updatedAt: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString(),
};

// ─── Full list of all test users ────────────────────────────────────────────
export const allTestUsers: User[] = [
  superAdminUser,
  seniorManagerUser,
  managerUser,
  teamLeaderUser,
  hrUser,
  operatorUser,
  inactiveOperatorUser,
];

/**
 * Get a user by role name.
 */
export function getUserByRole(roleName: User['roleName']): User | undefined {
  return allTestUsers.find((u) => u.roleName === roleName);
}

/**
 * Get a user by employee ID.
 */
export function getUserByEmployeeId(employeeId: string): User | undefined {
  return allTestUsers.find((u) => u.employeeId === employeeId);
}

/**
 * Create a batch of random operators for load testing.
 */
export function createRandomOperators(count: number): User[] {
  const firstNames = ['Thabo', 'Nomsa', 'Sipho', 'Zanele', 'Mandla', 'Pulane', 'Kagiso', 'Lerato'];
  const lastNames = ['Dlamini', 'Ndlovu', 'Mokoena', 'Nkosi', 'Mthembu', 'Zulu', 'Shabalala', 'Motaung'];
  const departments = ['DEPT-PROD', 'DEPT-QA', 'DEPT-LOG', 'DEPT-MAINT', 'DEPT-ADMIN'];

  return Array.from({ length: count }, (_, i) => ({
    id: generateId(),
    email: `operator${i + 1}@yfpoza.com`,
    firstName: firstNames[i % firstNames.length],
    lastName: lastNames[i % lastNames.length],
    displayName: `${firstNames[i % firstNames.length]} ${lastNames[i % lastNames.length]}`,
    employeeId: `EMP${(i + 1).toString().padStart(3, '0')}`,
    roleId: `ROLE-OP-${String(i + 1).padStart(3, '0')}`,
    roleName: 'Operator' as User['roleName'],
    departmentId: departments[i % departments.length],
    departmentName: departments[i % departments.length],
    managerId: `EMP002`,
    isActive: true,
    lastLoginAt: new Date().toISOString(),
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
  }));
}
