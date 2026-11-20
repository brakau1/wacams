-- RBAC Database Migration Script
-- Creates tables for Role-Based Access Control with fine-grained permissions

-- 1. Create Roles table
CREATE TABLE IF NOT EXISTS "Roles" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(50) NOT NULL UNIQUE,
    "Description" VARCHAR(200),
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "UpdatedAt" TIMESTAMP
);

-- 2. Create Permissions table
CREATE TABLE IF NOT EXISTS "Permissions" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL UNIQUE,
    "Module" VARCHAR(50) NOT NULL,
    "Description" VARCHAR(200),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT NOW()
);

-- 3. Create UserRoles junction table (many-to-many: Employee <-> Role)
CREATE TABLE IF NOT EXISTS "UserRoles" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INTEGER NOT NULL REFERENCES "Employees"("Id") ON DELETE CASCADE,
    "RoleId" INTEGER NOT NULL REFERENCES "Roles"("Id") ON DELETE CASCADE,
    "AssignedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "AssignedBy" VARCHAR(100),
    UNIQUE ("UserId", "RoleId")
);

-- 4. Create RolePermissions junction table (many-to-many: Role <-> Permission)
CREATE TABLE IF NOT EXISTS "RolePermissions" (
    "Id" SERIAL PRIMARY KEY,
    "RoleId" INTEGER NOT NULL REFERENCES "Roles"("Id") ON DELETE CASCADE,
    "PermissionId" INTEGER NOT NULL REFERENCES "Permissions"("Id") ON DELETE CASCADE,
    "AssignedAt" TIMESTAMP NOT NULL DEFAULT NOW(),
    "AssignedBy" VARCHAR(100),
    UNIQUE ("RoleId", "PermissionId")
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS "IX_UserRoles_UserId" ON "UserRoles" ("UserId");
CREATE INDEX IF NOT EXISTS "IX_UserRoles_RoleId" ON "UserRoles" ("RoleId");
CREATE INDEX IF NOT EXISTS "IX_RolePermissions_RoleId" ON "RolePermissions" ("RoleId");
CREATE INDEX IF NOT EXISTS "IX_RolePermissions_PermissionId" ON "RolePermissions" ("PermissionId");
CREATE INDEX IF NOT EXISTS "IX_Permissions_Module" ON "Permissions" ("Module");

-- 5. Seed default roles
INSERT INTO "Roles" ("Name", "Description") VALUES
    ('operator', 'Basic operator - view own records only'),
    ('hr', 'Human Resources - attendance oversight, leave approvals, warning generation'),
    ('supervisor', 'Supervisor - view all data, manage teams, approve actions'),
    ('manager', 'Manager - department-wide access, escalation management'),
    ('admin', 'Administrator - full system access, user management')
ON CONFLICT ("Name") DO NOTHING;

-- 6. Seed permissions (43 total across 7 modules)
INSERT INTO "Permissions" ("Name", "Module", "Description") VALUES
    -- Attendance Module (5)
    ('attendance.view.own', 'Attendance', 'View own attendance records'),
    ('attendance.view.team', 'Attendance', 'View team attendance records'),
    ('attendance.view.department', 'Attendance', 'View department attendance records'),
    ('attendance.view.all', 'Attendance', 'View all attendance records'),
    ('attendance.edit', 'Attendance', 'Edit attendance records'),

    -- Exceptions Module (7)
    ('exceptions.view.own', 'Exceptions', 'View own exceptions'),
    ('exceptions.view.team', 'Exceptions', 'View team exceptions'),
    ('exceptions.view.department', 'Exceptions', 'View department exceptions'),
    ('exceptions.view.all', 'Exceptions', 'View all exceptions'),
    ('exceptions.investigate', 'Exceptions', 'Investigate exceptions'),
    ('exceptions.approve', 'Exceptions', 'Approve exceptions'),
    ('exceptions.reject', 'Exceptions', 'Reject exceptions'),

    -- Warnings Module (7)
    ('warnings.view.own', 'Warnings', 'View own warnings'),
    ('warnings.view.team', 'Warnings', 'View team warnings'),
    ('warnings.view.department', 'Warnings', 'View department warnings'),
    ('warnings.view.all', 'Warnings', 'View all warnings'),
    ('warnings.create', 'Warnings', 'Create warning letters'),
    ('warnings.upload', 'Warnings', 'Upload warning documents'),
    ('warnings.issue', 'Warnings', 'Issue warning letters'),

    -- Escalations Module (5)
    ('escalations.view.own', 'Escalations', 'View own escalations'),
    ('escalations.view.team', 'Escalations', 'View team escalations'),
    ('escalations.view.department', 'Escalations', 'View department escalations'),
    ('escalations.view.all', 'Escalations', 'View all escalations'),
    ('escalations.manage', 'Escalations', 'Manage escalations'),

    -- Employees Module (6)
    ('employees.view.own', 'Employees', 'View own employee record'),
    ('employees.view.all', 'Employees', 'View all employee records'),
    ('employees.create', 'Employees', 'Create employee records'),
    ('employees.edit', 'Employees', 'Edit employee records'),
    ('employees.delete', 'Employees', 'Delete employee records'),
    ('employees.assign.role', 'Employees', 'Assign roles to employees'),

    -- Reports Module (5)
    ('reports.view.own', 'Reports', 'View own reports'),
    ('reports.view.team', 'Reports', 'View team reports'),
    ('reports.view.department', 'Reports', 'View department reports'),
    ('reports.view.all', 'Reports', 'View all reports'),
    ('reports.generate', 'Reports', 'Generate reports'),

    -- System Module (5)
    ('system.view.logs', 'System', 'View system logs'),
    ('system.manage.settings', 'System', 'Manage system settings'),
    ('system.manage.roles', 'System', 'Manage roles'),
    ('system.manage.permissions', 'System', 'Manage permissions'),
    ('system.view.audit', 'System', 'View audit trail')
ON CONFLICT ("Name") DO NOTHING;

-- 7. Seed role-permission mappings
-- Operator: 6 permissions (own records only)
INSERT INTO "RolePermissions" ("RoleId", "PermissionId")
SELECT r."Id", p."Id"
FROM "Roles" r
CROSS JOIN "Permissions" p
WHERE r."Name" = 'operator'
  AND p."Name" IN (
    'attendance.view.own',
    'exceptions.view.own',
    'warnings.view.own',
    'escalations.view.own',
    'employees.view.own',
    'reports.view.own'
  )
ON CONFLICT DO NOTHING;

-- HR: 17 permissions (own + department + employee management + warnings)
INSERT INTO "RolePermissions" ("RoleId", "PermissionId")
SELECT r."Id", p."Id"
FROM "Roles" r
CROSS JOIN "Permissions" p
WHERE r."Name" = 'hr'
  AND p."Name" IN (
    'attendance.view.own', 'attendance.view.department',
    'exceptions.view.own', 'exceptions.view.department', 'exceptions.investigate',
    'warnings.view.own', 'warnings.view.department', 'warnings.create', 'warnings.upload',
    'escalations.view.own', 'escalations.view.department',
    'employees.view.own', 'employees.view.all', 'employees.edit',
    'reports.view.own', 'reports.view.department', 'reports.generate'
  )
ON CONFLICT DO NOTHING;

-- Supervisor: 21 permissions (own + team + approvals)
INSERT INTO "RolePermissions" ("RoleId", "PermissionId")
SELECT r."Id", p."Id"
FROM "Roles" r
CROSS JOIN "Permissions" p
WHERE r."Name" = 'supervisor'
  AND p."Name" IN (
    'attendance.view.own', 'attendance.view.team', 'attendance.edit',
    'exceptions.view.own', 'exceptions.view.team', 'exceptions.investigate', 'exceptions.approve', 'exceptions.reject',
    'warnings.view.own', 'warnings.view.team', 'warnings.create', 'warnings.upload', 'warnings.issue',
    'escalations.view.own', 'escalations.view.team', 'escalations.manage',
    'employees.view.own', 'employees.view.all',
    'reports.view.own', 'reports.view.team', 'reports.generate'
  )
ON CONFLICT DO NOTHING;

-- Manager: 27 permissions (own + team + department)
INSERT INTO "RolePermissions" ("RoleId", "PermissionId")
SELECT r."Id", p."Id"
FROM "Roles" r
CROSS JOIN "Permissions" p
WHERE r."Name" = 'manager'
  AND p."Name" IN (
    'attendance.view.own', 'attendance.view.team', 'attendance.view.department', 'attendance.edit',
    'exceptions.view.own', 'exceptions.view.team', 'exceptions.view.department', 'exceptions.investigate', 'exceptions.approve', 'exceptions.reject',
    'warnings.view.own', 'warnings.view.team', 'warnings.view.department', 'warnings.create', 'warnings.upload', 'warnings.issue',
    'escalations.view.own', 'escalations.view.team', 'escalations.view.department', 'escalations.manage',
    'employees.view.own', 'employees.view.all', 'employees.edit',
    'reports.view.own', 'reports.view.team', 'reports.view.department', 'reports.generate'
  )
ON CONFLICT DO NOTHING;

-- Admin: ALL 40 permissions
INSERT INTO "RolePermissions" ("RoleId", "PermissionId")
SELECT r."Id", p."Id"
FROM "Roles" r
CROSS JOIN "Permissions" p
WHERE r."Name" = 'admin'
ON CONFLICT DO NOTHING;
