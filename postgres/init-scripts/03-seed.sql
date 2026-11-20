-- WACAMS Initial Seed Data
-- PostgreSQL 16+

-- Insert default roles
INSERT INTO roles (id, name, description, permissions) VALUES
    ('superadmin', 'Super Administrator', 'Full system access with all permissions', 
     '{"all": true}'::jsonb),
    ('admin', 'Administrator', 'Administrative access to most features',
     '{"users.read": true, "users.write": true, "users.delete": true, "departments.read": true, "departments.write": true, "attendance.read": true, "attendance.write": true, "warnings.read": true, "warnings.write": true, "warnings.issue": true, "reports.read": true, "reports.export": true, "settings.read": true, "settings.write": true}'::jsonb),
    ('supervisor', 'Supervisor', 'Supervisory access to team management',
     '{"users.read": true, "attendance.read": true, "attendance.write": true, "warnings.read": true, "warnings.write": true, "warnings.issue": true, "reports.read": true, "reports.export": true}'::jsonb),
    ('team_leader', 'Team Leader', 'Team leader access to attendance and warnings',
     '{"attendance.read": true, "attendance.write": true, "warnings.read": true, "warnings.write": true}'::jsonb),
    ('operator', 'Operator', 'Basic operator access',
     '{"attendance.read": true}'::jsonb)
ON CONFLICT (id) DO NOTHING;

-- Insert default departments
INSERT INTO departments (id, name, code, manager_id) VALUES
    ('d1111111-1111-1111-1111-111111111111', 'Production', 'PROD', NULL),
    ('d2222222-2222-2222-2222-222222222222', 'Quality', 'QA', NULL),
    ('d3333333-3333-3333-3333-333333333333', 'Maintenance', 'MAINT', NULL),
    ('d4444444-4444-4444-4444-444444444444', 'Logistics', 'LOG', NULL),
    ('d5555555-5555-5555-5555-555555555555', 'Administration', 'ADMIN', NULL)
ON CONFLICT (id) DO NOTHING;

-- Insert SuperAdmin user
-- Password hash generated via BCrypt (cost factor 12)
-- Default credentials: admin@wacams.local / Admin@2026
-- IMPORTANT: Change this password immediately after first login!
INSERT INTO users (id, email, password_hash, first_name, last_name, employee_id, role_id, department_id, is_active) VALUES
    ('a0000000-0000-0000-0000-000000000001', 
     'admin@wacams.local', 
     '$2b$12$bb4AZQoBDp7qSEOQiFhwzOcuQkqo2qD8w8PPF/xYnNMNO6vAtvsEW',
     'System', 
     'Administrator', 
     'ADMIN001', 
     'superadmin', 
     'd5555555-5555-5555-5555-555555555555', 
     true)
ON CONFLICT (id) DO NOTHING;

-- GIS integration settings: configured via WACAMS UI after deployment
-- Navigate to Settings > GIS Integration to enter credentials
-- Credentials are encrypted with AES-256 before storage
-- DO NOT seed GIS passwords in SQL — use the application UI instead
INSERT INTO gis_integration_settings (id, gis_base_url, gis_username, gis_password_encrypted, sync_interval_minutes, is_enabled) VALUES
    ('g0000000-0000-0000-0000-000000000001',
     'https://gis.yfpo.com:9105',
     '',
     '',
     60,
     false)
ON CONFLICT (id) DO NOTHING;

-- Insert sample employees (can be removed in production)
INSERT INTO employees (id, employee_id, first_name, last_name, email, department_id, manager_id, is_active, hire_date) VALUES
    ('e0000000-0000-0000-0000-000000000001', 'EMP001', 'Jeremiah', 'Bezuidenhout', 'jeremiah.bezuidenhout@company.com', 'd1111111-1111-1111-1111-111111111111', NULL, true, '2020-01-15'),
    ('e0000000-0000-0000-0000-000000000002', 'EMP002', 'George', 'Ntseke', 'george.ntseke@company.com', 'd1111111-1111-1111-1111-111111111111', 'EMP001', true, '2021-03-10'),
    ('e0000000-0000-0000-0000-000000000003', 'EMP003', 'Botlhale', 'Rakau', 'botlhale.rakau@company.com', 'd1111111-1111-1111-1111-111111111111', 'EMP001', true, '2021-06-20')
ON CONFLICT (id) DO NOTHING;

-- Create audit log for initial setup
INSERT INTO audit_logs (user_id, action, entity_type, entity_id, new_values, ip_address, user_agent) VALUES
    ('a0000000-0000-0000-0000-000000000001', 
     'SYSTEM_INIT', 
     'database', 
     NULL, 
     '{"version": "1.0.0", "initialized_at": "' || NOW()::text || '"}'::jsonb,
     '127.0.0.1',
     'PostgreSQL Init Script');

-- Update departments with manager references
UPDATE departments SET manager_id = 'EMP001' WHERE id = 'd1111111-1111-1111-1111-111111111111';

-- Log completion
DO $$
BEGIN
    RAISE NOTICE 'WACAMS database initialization completed successfully';
    RAISE NOTICE 'Default roles created: superadmin, admin, supervisor, team_leader, operator';
    RAISE NOTICE 'Default departments created: Production, Quality, Maintenance, Logistics, Administration';
    RAISE NOTICE 'SuperAdmin user created: admin@wacams.local (CHANGE PASSWORD IMMEDIATELY)';
    RAISE NOTICE 'Sample employees created for testing';
END $$;
