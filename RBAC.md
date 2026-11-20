# WACAMS RBAC System Documentation

## Overview

WACAMS implements a **Role-Based Access Control (RBAC)** system with fine-grained permissions. The system uses a many-to-many relationship between Roles and Permissions, allowing flexible access control across 7 modules.

---

## Architecture

### Database Schema

```
Employees (Users)
    ↓ (many-to-many)
UserRoles (junction table)
    ↓
Roles (5 roles)
    ↓ (many-to-many)
RolePermissions (junction table)
    ↓
Permissions (40 permissions across 7 modules)
```

### Tables

| Table | Purpose |
|-------|---------|
| `Roles` | Defines system roles (operator, hr, supervisor, manager, admin) |
| `Permissions` | Defines granular permissions (e.g., `attendance.view.team`) |
| `UserRoles` | Maps employees to roles (many-to-many) |
| `RolePermissions` | Maps roles to permissions (many-to-many) |

---

## Role Hierarchy

| Role | Level | Description |
|------|-------|-------------|
| **Operator** | 1 | Basic access - view own records only |
| **HR** | 2 | Human Resources - attendance oversight, warnings, employee management |
| **Supervisor** | 3 | Team management - approve exceptions, issue warnings |
| **Manager** | 4 | Department-wide access - escalation management |
| **Admin** | 5 | Full system access - user management, all permissions |

**Note:** Admin role automatically bypasses all permission checks.

---

## Permission Matrix

### Module 1: Attendance (5 permissions)

| Permission | Operator | HR | Supervisor | Manager | Admin |
|------------|:--------:|:--:|:----------:|:-------:|:-----:|
| `attendance.view.own` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `attendance.view.team` | | | ✓ | ✓ | ✓ |
| `attendance.view.department` | | ✓ | | ✓ | ✓ |
| `attendance.view.all` | | | | | ✓ |
| `attendance.edit` | | | ✓ | ✓ | ✓ |

### Module 2: Exceptions (7 permissions)

| Permission | Operator | HR | Supervisor | Manager | Admin |
|------------|:--------:|:--:|:----------:|:-------:|:-----:|
| `exceptions.view.own` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `exceptions.view.team` | | | ✓ | ✓ | ✓ |
| `exceptions.view.department` | | ✓ | | ✓ | ✓ |
| `exceptions.view.all` | | | | | ✓ |
| `exceptions.investigate` | | ✓ | ✓ | ✓ | ✓ |
| `exceptions.approve` | | | ✓ | ✓ | ✓ |
| `exceptions.reject` | | | ✓ | ✓ | ✓ |

### Module 3: Warnings (7 permissions)

| Permission | Operator | HR | Supervisor | Manager | Admin |
|------------|:--------:|:--:|:----------:|:-------:|:-----:|
| `warnings.view.own` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `warnings.view.team` | | | ✓ | ✓ | ✓ |
| `warnings.view.department` | | ✓ | | ✓ | ✓ |
| `warnings.view.all` | | | | | ✓ |
| `warnings.create` | | ✓ | ✓ | ✓ | ✓ |
| `warnings.upload` | | ✓ | ✓ | ✓ | ✓ |
| `warnings.issue` | | | ✓ | ✓ | ✓ |

### Module 4: Escalations (5 permissions)

| Permission | Operator | HR | Supervisor | Manager | Admin |
|------------|:--------:|:--:|:----------:|:-------:|:-----:|
| `escalations.view.own` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `escalations.view.team` | | | ✓ | ✓ | ✓ |
| `escalations.view.department` | | ✓ | | ✓ | ✓ |
| `escalations.view.all` | | | | | ✓ |
| `escalations.manage` | | | ✓ | ✓ | ✓ |

### Module 5: Employees (6 permissions)

| Permission | Operator | HR | Supervisor | Manager | Admin |
|------------|:--------:|:--:|:----------:|:-------:|:-----:|
| `employees.view.own` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `employees.view.all` | | ✓ | ✓ | ✓ | ✓ |
| `employees.create` | | | | | ✓ |
| `employees.edit` | | ✓ | | ✓ | ✓ |
| `employees.delete` | | | | | ✓ |
| `employees.assign.role` | | | | | ✓ |

### Module 6: Reports (5 permissions)

| Permission | Operator | HR | Supervisor | Manager | Admin |
|------------|:--------:|:--:|:----------:|:-------:|:-----:|
| `reports.view.own` | ✓ | ✓ | ✓ | ✓ | ✓ |
| `reports.view.team` | | | ✓ | ✓ | ✓ |
| `reports.view.department` | | ✓ | | ✓ | ✓ |
| `reports.view.all` | | | | | ✓ |
| `reports.generate` | | ✓ | ✓ | ✓ | ✓ |

### Module 7: System (5 permissions)

| Permission | Operator | HR | Supervisor | Manager | Admin |
|------------|:--------:|:--:|:----------:|:-------:|:-----:|
| `system.view.logs` | | | | | ✓ |
| `system.manage.settings` | | | | | ✓ |
| `system.manage.roles` | | | | | ✓ |
| `system.manage.permissions` | | | | | ✓ |
| `system.view.audit` | | | | | ✓ |

**Total:** 40 permissions across 7 modules

---

## Usage in Controllers

### Option 1: Using `[RequirePermission]` Attribute (Recommended)

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AttendanceController : ControllerBase
{
    [HttpGet("team")]
    [RequirePermission("attendance.view.team")]
    public IActionResult GetTeamAttendance()
    {
        // Only users with attendance.view.team permission can access
        return Ok(...);
    }

    [HttpPost]
    [RequirePermission("attendance.edit")]
    public IActionResult UpdateAttendance([FromBody] AttendanceDto dto)
    {
        // Only users with attendance.edit permission can access
        return Ok(...);
    }

    [HttpGet("department")]
    [RequirePermission("attendance.view.department", "attendance.view.all")]
    public IActionResult GetDepartmentAttendance()
    {
        // Users with EITHER permission can access
        return Ok(...);
    }
}
```

### Option 2: Using Policy-Based Authorization

```csharp
[Authorize(Policy = "Attendance.ViewTeam")]
public IActionResult GetTeamAttendance()
{
    return Ok(...);
}
```

### Option 3: Using Legacy Role Hierarchy

```csharp
[RoleRequirement("Supervisor")]
public IActionResult ApproveException()
{
    // Supervisor, Manager, and Admin can access
    return Ok(...);
}
```

---

## JWT Token Structure

When a user logs in, the JWT token includes:

```json
{
  "employeeId": "708513",
  "role": "supervisor",
  "permissions": [
    "attendance.view.own",
    "attendance.view.team",
    "attendance.edit",
    "exceptions.view.own",
    "exceptions.view.team",
    "exceptions.investigate",
    "exceptions.approve",
    "exceptions.reject",
    ...
  ],
  "exp": 1731571200
}
```

---

## How It Works

### 1. Login Flow

```
User Login
    ↓
AuthService validates credentials
    ↓
PermissionService loads user permissions from DB
    ↓
TokenGenerator creates JWT with permissions claims
    ↓
JWT returned to client
```

### 2. Request Authorization Flow

```
API Request with JWT
    ↓
JWT Bearer Middleware validates token
    ↓
AuthorizationMiddleware checks:
  - Role requirement (if [RoleRequirement] attribute)
  - Permission requirement (if [RequirePermission] attribute)
    ↓
If authorized → Controller action executes
If forbidden → 403 Forbidden response
```

---

## Database Migration

### Run Migration Script

```bash
psql -U postgres -d wacams_db -f Api/Data/Migrations/001_AddRBAC.sql
```

### Seed Data

The migration script automatically seeds:
- 5 roles
- 40 permissions
- Role-permission mappings

### Verify Seeding

```sql
-- Check roles
SELECT * FROM "Roles";

-- Check permissions
SELECT * FROM "Permissions" ORDER BY "Module", "Name";

-- Check role-permission mappings
SELECT r."Name" AS Role, COUNT(rp."PermissionId") AS PermissionCount
FROM "Roles" r
LEFT JOIN "RolePermissions" rp ON r."Id" = rp."RoleId"
GROUP BY r."Name"
ORDER BY PermissionCount DESC;
```

Expected output:
```
Role       | PermissionCount
-----------|----------------
admin      | 40
manager    | 27
supervisor | 21
hr         | 17
operator   | 6
```

---

## Assigning Roles to Users

### Via SQL

```sql
-- Assign supervisor role to employee ID 5
INSERT INTO "UserRoles" ("UserId", "RoleId", "AssignedBy")
SELECT e."Id", r."Id", 'admin'
FROM "Employees" e
CROSS JOIN "Roles" r
WHERE e."EmployeeId" = '708513'
  AND r."Name" = 'supervisor';
```

### Via API (Future Enhancement)

```http
POST /api/admin/users/708513/roles
Authorization: Bearer <admin-token>
Content-Type: application/json

{
  "roles": ["supervisor", "hr"]
}
```

---

## Adding New Permissions

### Step 1: Define Permission Constant

In `Api/Models/Permission.cs`:

```csharp
public static class Permissions
{
    public static class NewModule
    {
        public const string ViewOwn = "newmodule.view.own";
        public const string Edit = "newmodule.edit";
    }
}
```

### Step 2: Add to Migration Script

```sql
INSERT INTO "Permissions" ("Name", "Module", "Description") VALUES
    ('newmodule.view.own', 'NewModule', 'View own records'),
    ('newmodule.edit', 'NewModule', 'Edit records');
```

### Step 3: Assign to Roles

```sql
INSERT INTO "RolePermissions" ("RoleId", "PermissionId")
SELECT r."Id", p."Id"
FROM "Roles" r
CROSS JOIN "Permissions" p
WHERE r."Name" IN ('admin', 'manager')
  AND p."Name" IN ('newmodule.view.own', 'newmodule.edit');
```

### Step 4: Use in Controllers

```csharp
[RequirePermission("newmodule.view.own")]
public IActionResult GetRecords() { ... }
```

---

## Security Considerations

### ✅ Best Practices

1. **Admin Bypass**: Admin role automatically bypasses all permission checks
2. **JWT Permissions**: Permissions are embedded in JWT tokens (refresh on login)
3. **Database-Driven**: Role-permission mappings stored in DB (easy to modify)
4. **Audit Trail**: All role assignments logged with `AssignedBy` and `AssignedAt`

### ⚠️ Important Notes

1. **Token Refresh**: When permissions change, users must re-login to get updated JWT
2. **Role Hierarchy**: Legacy `[RoleRequirement]` uses hierarchy (Admin > Manager > Supervisor > HR > Operator)
3. **Permission Granularity**: Use most specific permission possible (e.g., `attendance.view.team` not `attendance.view.all`)

---

## Troubleshooting

### User Gets 403 Forbidden

**Check:**
1. User has the required role assigned in `UserRoles` table
2. Role has the required permission in `RolePermissions` table
3. User re-logged in after role/permission changes (JWT needs refresh)

```sql
-- Check user roles
SELECT e."EmployeeId", r."Name" AS Role
FROM "Employees" e
JOIN "UserRoles" ur ON e."Id" = ur."UserId"
JOIN "Roles" r ON ur."RoleId" = r."Id"
WHERE e."EmployeeId" = '708513';

-- Check role permissions
SELECT r."Name" AS Role, p."Name" AS Permission
FROM "Roles" r
JOIN "RolePermissions" rp ON r."Id" = rp."RoleId"
JOIN "Permissions" p ON rp."PermissionId" = p."Id"
WHERE r."Name" = 'supervisor'
ORDER BY p."Module", p."Name";
```

### Permission Not Working After Update

**Solution:** User must re-login to get updated JWT token with new permissions.

---

## Files Created

| File | Purpose |
|------|---------|
| `Api/Models/RolePermission.cs` | Junction model for Role-Permission relationship |
| `Api/Data/SeedData/PermissionSeeder.cs` | Seeds permissions, roles, and mappings |
| `Api/Filters/RequirePermissionAttribute.cs` | Attribute for permission-based authorization |
| `Api/Policies/AuthorizationPolicies.cs` | Policy-based authorization definitions |
| `Api/Services/PermissionService.cs` | Service to query user permissions |
| `Api/Data/Migrations/001_AddRBAC.sql` | Database migration script |

## Files Modified

| File | Changes |
|------|---------|
| `Api/Data/WacamDbContext.cs` | Added `Roles`, `Permissions`, `UserRoles`, `RolePermissions` DbSets and configurations |
| `Api/Utils/TokenGenerator.cs` | Added `permissions` parameter to `GenerateTokens()` and `GetPermissionsFromToken()` |
| `Api/Services/AuthService.cs` | Injected `PermissionService`, loads permissions on login |
| `Api/Middleware/AuthorizationMiddleware.cs` | Added permission-based authorization checks |
| `Api/Program.cs` | Registered `PermissionService`, authorization policies, and RBAC seeder |

---

## Summary

The RBAC system provides:
- ✅ **40 granular permissions** across 7 modules
- ✅ **5 predefined roles** with progressive access levels
- ✅ **Flexible role-permission mappings** (database-driven)
- ✅ **Multiple authorization methods** (attributes, policies, role hierarchy)
- ✅ **JWT-embedded permissions** (no DB lookup on each request)
- ✅ **Admin bypass** (full access without explicit permissions)

**Next Steps:**
1. Run migration script to create tables and seed data
2. Assign roles to existing employees
3. Apply `[RequirePermission]` attributes to controller endpoints
4. Test authorization with different user roles
