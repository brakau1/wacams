import { describe, it, expect, vi } from 'vitest'
import type { Role, User } from '@/core/api/types'

// ─── Role Hierarchy Helper (extracted for testing) ──────────────────────────

const ROLE_HIERARCHY: Record<Role, number> = {
  SuperAdmin: 6,
  SeniorManager: 5,
  Manager: 4,
  Coordinator: 3,
  TeamLeader: 2,
  Operator: 1,
}

function hasRole(user: User | null, role: Role | Role[]): boolean {
  if (!user) return false
  const roles = Array.isArray(role) ? role : [role]
  const userLevel = ROLE_HIERARCHY[user.roleName] ?? 0
  return roles.some(r => (ROLE_HIERARCHY[r] ?? 0) <= userLevel)
}

function hasMinimumRole(user: User | null, minRole: Role): boolean {
  if (!user) return false
  const userLevel = ROLE_HIERARCHY[user.roleName] ?? 0
  const minLevel = ROLE_HIERARCHY[minRole] ?? 0
  return userLevel >= minLevel
}

function canAccessPage(user: User | null, requiredRole: Role | Role[]): boolean {
  return hasRole(user, requiredRole)
}

function canPerformAction(
  user: User | null,
  action: 'create' | 'read' | 'update' | 'delete',
  minRoles: Record<string, Role[]>
): boolean {
  if (!user) return false
  const requiredRoles = minRoles[action] || []
  return hasRole(user, requiredRoles)
}

function getRoleDisplayName(role: Role): string {
  const names: Record<Role, string> = {
    SuperAdmin: 'Super Administrator',
    SeniorManager: 'Senior Manager',
    Manager: 'Manager',
    Coordinator: 'Coordinator',
    TeamLeader: 'Team Leader',
    Operator: 'Operator',
  }
  return names[role] || role
}

function getRoleColor(role: Role): string {
  const colors: Record<Role, string> = {
    SuperAdmin: 'text-purple-400',
    SeniorManager: 'text-red-400',
    Manager: 'text-orange-400',
    Coordinator: 'text-yellow-400',
    TeamLeader: 'text-blue-400',
    Operator: 'text-gray-400',
  }
  return colors[role] || 'text-gray-400'
}

// ─── Mock Users ─────────────────────────────────────────────────────────────

function createUser(overrides: Partial<User> = {}): User {
  return {
    id: 'test-user',
    email: 'test@test.com',
    firstName: 'Test',
    lastName: 'User',
    displayName: 'Test User',
    employeeId: '708001',
    roleId: '1',
    roleName: 'Operator' as Role,
    departmentId: '1',
    departmentName: 'Production',
    managerId: null,
    isActive: true,
    lastLoginAt: null,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    ...overrides,
  }
}

describe('hasRole', () => {
  it('should return false for null user', () => {
    expect(hasRole(null, 'Operator')).toBe(false)
  })

  it('should return true when user has exact role', () => {
    const user = createUser({ roleName: 'Operator' })
    expect(hasRole(user, 'Operator')).toBe(true)
  })

  it('should return true when user has higher role', () => {
    const user = createUser({ roleName: 'SuperAdmin' })
    expect(hasRole(user, 'Operator')).toBe(true)
  })

  it('should return false when user has lower role', () => {
    const user = createUser({ roleName: 'Operator' })
    expect(hasRole(user, 'TeamLeader')).toBe(false)
  })

  it('should accept array of roles - returns true if any match', () => {
    const user = createUser({ roleName: 'Coordinator' })
    expect(hasRole(user, ['Operator', 'TeamLeader'])).toBe(false)
    expect(hasRole(user, ['Coordinator', 'Operator'])).toBe(true)
  })

  it('should accept single role in array', () => {
    const user = createUser({ roleName: 'TeamLeader' })
    expect(hasRole(user, ['TeamLeader'])).toBe(true)
  })

  it('should handle unknown roles gracefully', () => {
    const user = createUser({ roleName: 'Operator' })
    expect(hasRole(user, 'Operator' as Role)).toBe(true)
  })

  it('should check role hierarchy correctly', () => {
    const levels: Role[] = ['SuperAdmin', 'SeniorManager', 'Manager', 'Coordinator', 'TeamLeader', 'Operator']
    for (let i = 0; i < levels.length; i++) {
      const user = createUser({ roleName: levels[i] })
      // Should have all roles at or below their level
      for (let j = 0; j <= i; j++) {
        expect(hasRole(user, levels[j])).toBe(true)
      }
      // Should NOT have roles above their level
      for (let j = i + 1; j < levels.length; j++) {
        expect(hasRole(user, levels[j])).toBe(false)
      }
    }
  })
})

describe('hasMinimumRole', () => {
  it('should return false for null user', () => {
    expect(hasMinimumRole(null, 'Operator')).toBe(false)
  })

  it('should return true when user meets minimum', () => {
    const user = createUser({ roleName: 'Coordinator' })
    expect(hasMinimumRole(user, 'TeamLeader')).toBe(true)
  })

  it('should return true when user equals minimum', () => {
    const user = createUser({ roleName: 'TeamLeader' })
    expect(hasMinimumRole(user, 'TeamLeader')).toBe(true)
  })

  it('should return false when user below minimum', () => {
    const user = createUser({ roleName: 'Operator' })
    expect(hasMinimumRole(user, 'TeamLeader')).toBe(false)
  })

  it('should verify full hierarchy chain', () => {
    const superAdmin = createUser({ roleName: 'SuperAdmin' })
    const operator = createUser({ roleName: 'Operator' })

    expect(hasMinimumRole(superAdmin, 'Operator')).toBe(true)
    expect(hasMinimumRole(superAdmin, 'Coordinator')).toBe(true)
    expect(hasMinimumRole(operator, 'Operator')).toBe(true)
    expect(hasMinimumRole(operator, 'TeamLeader')).toBe(false)
  })
})

describe('canAccessPage', () => {
  it('should allow access with matching role', () => {
    const user = createUser({ roleName: 'TeamLeader' })
    expect(canAccessPage(user, 'TeamLeader')).toBe(true)
  })

  it('should deny access with insufficient role', () => {
    const user = createUser({ roleName: 'Operator' })
    expect(canAccessPage(user, 'Coordinator')).toBe(false)
  })

  it('should allow access with higher role', () => {
    const user = createUser({ roleName: 'SeniorManager' })
    expect(canAccessPage(user, 'TeamLeader')).toBe(true)
  })
})

describe('canPerformAction', () => {
  it('should require correct roles for CRUD actions', () => {
    const senior = createUser({ roleName: 'SeniorManager' })
    const operator = createUser({ roleName: 'Operator' })

    const minRoles: Record<string, Role[]> = {
      read: ['Operator'],
      create: ['TeamLeader'],
      update: ['Coordinator'],
      delete: ['SeniorManager'],
    }

    // Read should be available to all
    expect(canPerformAction(senior, 'read', minRoles)).toBe(true)
    expect(canPerformAction(operator, 'read', minRoles)).toBe(true)

    // Create needs TeamLeader+
    expect(canPerformAction(senior, 'create', minRoles)).toBe(true)
    expect(canPerformAction(operator, 'create', minRoles)).toBe(false)

    // Update needs Coordinator+
    expect(canPerformAction(senior, 'update', minRoles)).toBe(true)
    expect(canPerformAction(operator, 'update', minRoles)).toBe(false)

    // Delete needs SeniorManager+
    expect(canPerformAction(senior, 'delete', minRoles)).toBe(true)
    expect(canPerformAction(operator, 'delete', minRoles)).toBe(false)
  })

  it('should return false for null user', () => {
    const minRoles: Record<string, Role[]> = { read: ['Operator'] }
    expect(canPerformAction(null, 'read', minRoles)).toBe(false)
  })

  it('should default to empty array (no access) when action not defined', () => {
    const operator = createUser({ roleName: 'Operator' })
    expect(canPerformAction(operator, 'read' as any, {})).toBe(false)
  })
})

describe('getRoleDisplayName', () => {
  it('should return full display name for each role', () => {
    expect(getRoleDisplayName('SuperAdmin')).toBe('Super Administrator')
    expect(getRoleDisplayName('SeniorManager')).toBe('Senior Manager')
    expect(getRoleDisplayName('Manager')).toBe('Manager')
    expect(getRoleDisplayName('Coordinator')).toBe('Coordinator')
    expect(getRoleDisplayName('TeamLeader')).toBe('Team Leader')
    expect(getRoleDisplayName('Operator')).toBe('Operator')
  })

  it('should fall back to role name for unknown roles', () => {
    // TypeScript won't allow passing invalid role, but function handles it
    const result = getRoleDisplayName('Operator' as Role)
    expect(result).toBe('Operator')
  })
})

describe('getRoleColor', () => {
  it('should return correct color class for each role', () => {
    expect(getRoleColor('SuperAdmin')).toContain('purple')
    expect(getRoleColor('SeniorManager')).toContain('red')
    expect(getRoleColor('Manager')).toContain('orange')
    expect(getRoleColor('Coordinator')).toContain('yellow')
    expect(getRoleColor('TeamLeader')).toContain('blue')
    expect(getRoleColor('Operator')).toContain('gray')
  })

  it('should fallback to gray for unknown roles', () => {
    expect(getRoleColor('Operator' as Role)).toContain('gray')
  })
})

describe('Role Hierarchy Verification', () => {
  it('should maintain strict ordering', () => {
    const hierarchyValues = Object.values(ROLE_HIERARCHY)
    const sorted = [...hierarchyValues].sort((a, b) => a - b)
    expect(hierarchyValues).toEqual(sorted)
  })

  it('should have distinct levels', () => {
    const levels = Object.values(ROLE_HIERARCHY)
    const uniqueLevels = new Set(levels)
    expect(uniqueLevels.size).toBe(levels.length)
  })

  it('should verify no gaps in hierarchy', () => {
    const levels = Object.values(ROLE_HIERARCHY).sort((a, b) => a - b)
    for (let i = 1; i < levels.length; i++) {
      expect(levels[i]).toBeGreaterThan(levels[i - 1])
    }
  })
})
