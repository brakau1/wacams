-- WACAMS Additional Performance Indexes
-- PostgreSQL 16+

-- Composite indexes for common query patterns
CREATE INDEX IF NOT EXISTS idx_attendance_employee_date_status 
    ON attendance_records(employee_id, date DESC, status);

CREATE INDEX IF NOT EXISTS idx_warnings_employee_status_date 
    ON warnings(employee_id, status, issued_at DESC);

CREATE INDEX IF NOT EXISTS idx_employees_department_active 
    ON employees(department_id, is_active);

CREATE INDEX IF NOT EXISTS idx_users_employee_id 
    ON users(employee_id);

CREATE INDEX IF NOT EXISTS idx_users_department_active 
    ON users(department_id, is_active);

-- Date range indexes for reporting
CREATE INDEX IF NOT EXISTS idx_attendance_date_range 
    ON attendance_records(date DESC) INCLUDE (employee_id, status, late_minutes);

CREATE INDEX IF NOT EXISTS idx_warnings_date_range 
    ON warnings(issued_at DESC) INCLUDE (employee_id, warning_type, status);

CREATE INDEX IF NOT EXISTS idx_audit_logs_date_range 
    ON audit_logs(created_at DESC) INCLUDE (user_id, action, entity_type);

-- GIS sync optimization
CREATE INDEX IF NOT EXISTS idx_gis_settings_enabled 
    ON gis_integration_settings(is_enabled) WHERE is_enabled = true;

-- Partial indexes for active records only
CREATE INDEX IF NOT EXISTS idx_active_employees 
    ON employees(id, employee_id, first_name, last_name) 
    WHERE is_active = true;

CREATE INDEX IF NOT EXISTS idx_active_users 
    ON users(id, email, employee_id, role_id) 
    WHERE is_active = true;

-- JSONB indexes for flexible queries
CREATE INDEX IF NOT EXISTS idx_roles_permissions_gin 
    ON roles USING GIN (permissions);

CREATE INDEX IF NOT EXISTS idx_audit_old_values_gin 
    ON audit_logs USING GIN (old_values);

CREATE INDEX IF NOT EXISTS idx_audit_new_values_gin 
    ON audit_logs USING GIN (new_values);

-- Full-text search indexes (if needed for future features)
CREATE INDEX IF NOT EXISTS idx_employees_name_search 
    ON employees USING GIN (to_tsvector('english', first_name || ' ' || last_name));

CREATE INDEX IF NOT EXISTS idx_users_name_search 
    ON users USING GIN (to_tsvector('english', first_name || ' ' || last_name));

-- Unique constraints enforcement
CREATE UNIQUE INDEX IF NOT EXISTS idx_attendance_unique_employee_date 
    ON attendance_records(employee_id, date);

-- Foreign key relationship indexes
CREATE INDEX IF NOT EXISTS idx_employees_manager 
    ON employees(manager_id) WHERE manager_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_departments_parent 
    ON departments(parent_department_id) WHERE parent_department_id IS NOT NULL;

-- Covering indexes for common SELECT patterns
CREATE INDEX IF NOT EXISTS idx_attendance_covering 
    ON attendance_records(employee_id, date DESC) 
    INCLUDE (clock_in_time, clock_out_time, status, late_minutes, early_minutes, total_hours);

CREATE INDEX IF NOT EXISTS idx_warnings_covering 
    ON warnings(employee_id, issued_at DESC) 
    INCLUDE (warning_type, reason, status, issued_by, acknowledged_at);
