-- ============================================================================
-- WACAMS — Workforce Attendance & Corrective Action Management System
-- PostgreSQL Production Schema
-- Generated: 2026-11-12
-- ============================================================================

-- ============================================================================
-- EXTENSIONS
-- ============================================================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
CREATE EXTENSION IF NOT EXISTS "moddatetime";

-- ============================================================================
-- ENUM TYPES
-- ============================================================================

CREATE TYPE gender_enum AS ENUM ('Male', 'Female', 'Other', 'PreferNotToSay');

CREATE TYPE employment_status_enum AS ENUM (
    'Active', 'OnLeave', 'Suspended', 'Terminated', 'Resigned', 'Retired'
);

CREATE TYPE shift_type_enum AS ENUM ('Morning', 'Afternoon', 'Night');

CREATE TYPE attendance_status_enum AS ENUM (
    'Present', 'Late', 'EarlyDeparture', 'Absent', 'AWOL',
    'SickLeave', 'FamilyResponsibilityLeave', 'AnnualLeave',
    'ShiftCancellation', 'MissingClockIn', 'MissingClockOut',
    'UnscheduledClockIn', 'UnscheduledClockOut',
    'WorkshopAttendance', 'Training'
);

CREATE TYPE exception_type_enum AS ENUM (
    'LateArrival', 'EarlyDeparture', 'UnplannedAbsenteeism',
    'AWOL', 'MissingClockIn', 'MissingClockOut',
    'UnscheduledClockIn', 'UnscheduledClockOut', 'ShiftCancellation'
);

CREATE TYPE investigation_status_enum AS ENUM (
    'Pending', 'UnderInvestigation', 'Completed', 'Dismissed', 'Escalated'
);

CREATE TYPE corrective_action_type_enum AS ENUM (
    'Counselling', 'VerbalWarning', 'WrittenWarning',
    'FinalWrittenWarning', 'Dismissal'
);

CREATE TYPE corrective_action_status_enum AS ENUM (
    'Issued', 'Acknowledged', 'Disputed', 'Upheld', 'Overturned', 'Expired'
);

CREATE TYPE escalation_status_enum AS ENUM (
    'Pending', 'Acknowledged', 'ActionTaken', 'Overdue', 'Resolved'
);

CREATE TYPE notification_type_enum AS ENUM (
    'ExceptionDetected', 'InvestigationRequired', 'WarningIssued',
    'EscalationTriggered', 'ActionOverdue', 'WarningAcknowledged',
    'ShiftStart', 'ShiftEnd', 'SystemAlert'
);

CREATE TYPE audit_action_enum AS ENUM ('INSERT', 'UPDATE', 'DELETE');

CREATE TYPE role_permission_enum AS ENUM (
    'view_dashboard', 'manage_employees', 'manage_attendance',
    'investigate_exceptions', 'issue_warnings', 'approve_warnings',
    'manage_escalations', 'view_reports', 'manage_users',
    'manage_departments', 'manage_rules', 'view_audit_logs',
    'manage_system_settings', 'upload_documents'
);

-- ============================================================================
-- 1. DEPARTMENTS
-- ============================================================================

CREATE TABLE departments (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    department_code VARCHAR(20) NOT NULL UNIQUE,
    department_name VARCHAR(200) NOT NULL,
    parent_id       UUID REFERENCES departments(id) ON DELETE SET NULL,
    cost_center     VARCHAR(50),
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by      VARCHAR(50),
    updated_by      VARCHAR(50)
);

-- ============================================================================
-- 2. EMPLOYEES (Employee ID = Primary Key)
-- ============================================================================

CREATE TABLE employees (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    employee_id         VARCHAR(20) NOT NULL UNIQUE,   -- GIS Employee ID (master key)
    display_name        VARCHAR(200) NOT NULL,
    email               VARCHAR(255) UNIQUE,
    gender              gender_enum,
    nationality         VARCHAR(100),
    department_id       UUID NOT NULL REFERENCES departments(id) ON DELETE RESTRICT,
    employment_status   employment_status_enum NOT NULL DEFAULT 'Active',
    hire_date           DATE,
    phone               VARCHAR(20),
    is_active           BOOLEAN NOT NULL DEFAULT TRUE,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by          VARCHAR(50),
    updated_by          VARCHAR(50)
);

-- ============================================================================
-- 3. EMPLOYEE REPORTING HIERARCHY (5 Manager Levels)
-- ============================================================================

CREATE TABLE employee_reporting_hierarchy (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    employee_id         VARCHAR(20) NOT NULL REFERENCES employees(employee_id) ON DELETE CASCADE,
    manager_level_1     VARCHAR(20) REFERENCES employees(employee_id),  -- Direct supervisor
    manager_level_2     VARCHAR(20) REFERENCES employees(employee_id),  -- Team Leader
    manager_level_3     VARCHAR(20) REFERENCES employees(employee_id),  -- Production Manager
    manager_level_4     VARCHAR(20) REFERENCES employees(employee_id),  -- Senior Manager
    manager_level_5     VARCHAR(20) REFERENCES employees(employee_id),  -- Executive
    effective_from      DATE NOT NULL DEFAULT CURRENT_DATE,
    effective_to        DATE,
    is_current          BOOLEAN NOT NULL DEFAULT TRUE,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by          VARCHAR(50),
    updated_by          VARCHAR(50),
    CONSTRAINT uq_hierarchy_current UNIQUE (employee_id, is_current)
);

-- ============================================================================
-- 4. APP ACCOUNTS (Authentication)
-- ============================================================================

CREATE TABLE app_accounts (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    employee_id         VARCHAR(20) NOT NULL UNIQUE REFERENCES employees(employee_id) ON DELETE CASCADE,
    username            VARCHAR(100) NOT NULL UNIQUE,
    password_hash       VARCHAR(500) NOT NULL,
    email               VARCHAR(255) NOT NULL,
    is_active           BOOLEAN NOT NULL DEFAULT TRUE,
    email_verified      BOOLEAN NOT NULL DEFAULT FALSE,
    last_login_at       TIMESTAMPTZ,
    failed_login_count  INTEGER NOT NULL DEFAULT 0,
    locked_until        TIMESTAMPTZ,
    password_changed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    must_change_password BOOLEAN NOT NULL DEFAULT FALSE,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================================
-- 5. ROLES
-- ============================================================================

CREATE TABLE roles (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    role_name       VARCHAR(50) NOT NULL UNIQUE,
    display_name    VARCHAR(100) NOT NULL,
    description     TEXT,
    permissions     role_permission_enum[] NOT NULL DEFAULT '{}',
    is_system_role  BOOLEAN NOT NULL DEFAULT FALSE,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Account-Role mapping (many-to-many)
CREATE TABLE account_roles (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    account_id      UUID NOT NULL REFERENCES app_accounts(id) ON DELETE CASCADE,
    role_id         UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    granted_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    granted_by      VARCHAR(50),
    CONSTRAINT uq_account_role UNIQUE (account_id, role_id)
);

-- ============================================================================
-- 6. ATTENDANCE RULES (Configurable)
-- ============================================================================

CREATE TABLE attendance_rules (
    id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    rule_name               VARCHAR(100) NOT NULL,
    shift_type              shift_type_enum NOT NULL,
    expected_clock_in       TIME NOT NULL,
    expected_clock_out      TIME NOT NULL,
    late_grace_minutes      INTEGER NOT NULL DEFAULT 0,
    early_grace_minutes     INTEGER NOT NULL DEFAULT 0,
    late_threshold_minutes  INTEGER NOT NULL DEFAULT 30,
    half_day_threshold_min  INTEGER NOT NULL DEFAULT 120,
    is_active               BOOLEAN NOT NULL DEFAULT TRUE,
    effective_from          DATE NOT NULL,
    effective_to            DATE,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by              VARCHAR(50),
    updated_by              VARCHAR(50)
);

-- ============================================================================
-- 7. ATTENDANCE RECORDS
-- ============================================================================

CREATE TABLE attendance_records (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    employee_id         VARCHAR(20) NOT NULL REFERENCES employees(employee_id) ON DELETE RESTRICT,
    attendance_date     DATE NOT NULL,
    shift_type          shift_type_enum NOT NULL,
    expected_clock_in   TIME NOT NULL,
    expected_clock_out  TIME NOT NULL,
    actual_clock_in     TIME,
    actual_clock_out    TIME,
    status              attendance_status_enum NOT NULL DEFAULT 'Present',
    minutes_late        INTEGER NOT NULL DEFAULT 0,
    minutes_early       INTEGER NOT NULL DEFAULT 0,
    lost_minutes        INTEGER NOT NULL DEFAULT 0,
    is_manual_entry     BOOLEAN NOT NULL DEFAULT FALSE,
    source              VARCHAR(50) NOT NULL DEFAULT 'GIS_iWork',
    gis_raw_data        JSONB,
    notes               TEXT,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at          TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by          VARCHAR(50),
    updated_by          VARCHAR(50),
    CONSTRAINT uq_attendance_emp_date_shift UNIQUE (employee_id, attendance_date, shift_type)
);

-- ============================================================================
-- 8. ATTENDANCE EXCEPTIONS
-- ============================================================================

CREATE TABLE attendance_exceptions (
    id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    employee_id             VARCHAR(20) NOT NULL REFERENCES employees(employee_id) ON DELETE RESTRICT,
    attendance_record_id    UUID REFERENCES attendance_records(id) ON DELETE SET NULL,
    exception_type          exception_type_enum NOT NULL,
    exception_date          DATE NOT NULL,
    shift_type              shift_type_enum,
    minutes_lost            INTEGER NOT NULL DEFAULT 0,
    investigation_status    investigation_status_enum NOT NULL DEFAULT 'Pending',
    investigated_by         VARCHAR(20) REFERENCES employees(employee_id),
    investigated_at         TIMESTAMPTZ,
    investigation_notes     TEXT,
    investigation_outcome   TEXT,
    is_disciplinary_ready   BOOLEAN NOT NULL DEFAULT FALSE,
    detected_at             TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by              VARCHAR(50),
    updated_by              VARCHAR(50)
);

-- ============================================================================
-- 9. EXCEPTION REASONS
-- ============================================================================

CREATE TABLE exception_reasons (
    id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    exception_id            UUID NOT NULL REFERENCES attendance_exceptions(id) ON DELETE CASCADE,
    reason_category         VARCHAR(100) NOT NULL,
    reason_detail           TEXT NOT NULL,
    supporting_evidence     TEXT,
    submitted_by            VARCHAR(20) REFERENCES employees(employee_id),
    submitted_at            TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    is_accepted             BOOLEAN,
    accepted_by             VARCHAR(50),
    accepted_at             TIMESTAMPTZ,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================================
-- 10. DISCIPLINARY RULES
-- ============================================================================

CREATE TABLE disciplinary_rules (
    id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    offence_type            exception_type_enum NOT NULL,
    occurrence_count        INTEGER NOT NULL,
    discipline_level        corrective_action_type_enum NOT NULL,
    description             TEXT,
    is_active               BOOLEAN NOT NULL DEFAULT TRUE,
    effective_from          DATE NOT NULL DEFAULT '2022-08-01',
    effective_to            DATE,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by              VARCHAR(50),
    updated_by              VARCHAR(50),
    CONSTRAINT uq_discipline_rule UNIQUE (offence_type, occurrence_count)
);

-- ============================================================================
-- 11. CORRECTIVE ACTIONS
-- ============================================================================

CREATE TABLE corrective_actions (
    id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    employee_id             VARCHAR(20) NOT NULL REFERENCES employees(employee_id) ON DELETE RESTRICT,
    exception_id            UUID REFERENCES attendance_exceptions(id) ON DELETE SET NULL,
    action_type             corrective_action_type_enum NOT NULL,
    status                  corrective_action_status_enum NOT NULL DEFAULT 'Issued',
    offence_type            exception_type_enum NOT NULL,
    occurrence_number       INTEGER NOT NULL,
    issued_by               VARCHAR(20) NOT NULL REFERENCES employees(employee_id),
    issued_to               VARCHAR(20) NOT NULL REFERENCES employees(employee_id),
    issued_at               TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    due_date                TIMESTAMPTZ,
    acknowledged_at         TIMESTAMPTZ,
    acknowledged_by         VARCHAR(20) REFERENCES employees(employee_id),
    dispute_reason          TEXT,
    dispute_submitted_at    TIMESTAMPTZ,
    resolution_notes        TEXT,
    resolved_at             TIMESTAMPTZ,
    resolved_by             VARCHAR(20) REFERENCES employees(employee_id),
    expiry_date             DATE,
    is_active               BOOLEAN NOT NULL DEFAULT TRUE,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by              VARCHAR(50),
    updated_by              VARCHAR(50)
);

-- ============================================================================
-- 12. WARNING DOCUMENTS (Signed Forms)
-- ============================================================================

CREATE TABLE warning_documents (
    id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    corrective_action_id    UUID NOT NULL REFERENCES corrective_actions(id) ON DELETE CASCADE,
    document_type           VARCHAR(50) NOT NULL,  -- 'WarningLetter', 'HearingNotice', 'OutcomeLetter'
    file_url                VARCHAR(1000) NOT NULL,
    file_storage_key        VARCHAR(500),          -- Azure Blob Storage key
    file_size_bytes         BIGINT,
    mime_type               VARCHAR(100),
    generated_at            TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    signed_by_employee      VARCHAR(20) REFERENCES employees(employee_id),
    employee_signed_at      TIMESTAMPTZ,
    signed_by_witness       VARCHAR(20) REFERENCES employees(employee_id),
    witness_signed_at       TIMESTAMPTZ,
    signed_by_issuer        VARCHAR(20) REFERENCES employees(employee_id),
    issuer_signed_at        TIMESTAMPTZ,
    is_fully_signed         BOOLEAN NOT NULL DEFAULT FALSE,
    signature_hash          VARCHAR(256),          -- SHA-256 hash for integrity
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================================
-- 13. ESCALATION LOGS
-- ============================================================================

CREATE TABLE escalation_logs (
    id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    corrective_action_id    UUID REFERENCES corrective_actions(id) ON DELETE SET NULL,
    exception_id            UUID REFERENCES attendance_exceptions(id) ON DELETE SET NULL,
    employee_id             VARCHAR(20) NOT NULL REFERENCES employees(employee_id) ON DELETE RESTRICT,
    escalation_level        INTEGER NOT NULL CHECK (escalation_level BETWEEN 1 AND 5),
    from_manager_id         VARCHAR(20) NOT NULL REFERENCES employees(employee_id),
    to_manager_id           VARCHAR(20) NOT NULL REFERENCES employees(employee_id),
    status                  escalation_status_enum NOT NULL DEFAULT 'Pending',
    reason                  TEXT NOT NULL,
    action_required         TEXT NOT NULL,
    action_deadline         TIMESTAMPTZ NOT NULL,
    acknowledged_at         TIMESTAMPTZ,
    acknowledged_by         VARCHAR(20) REFERENCES employees(employee_id),
    action_taken_at         TIMESTAMPTZ,
    action_taken_by         VARCHAR(20) REFERENCES employees(employee_id),
    action_notes            TEXT,
    is_overdue              BOOLEAN NOT NULL DEFAULT FALSE,
    escalated_at            TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    resolved_at             TIMESTAMPTZ,
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at              TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================================
-- 14. NOTIFICATIONS
-- ============================================================================

CREATE TABLE notifications (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    recipient_id        VARCHAR(20) NOT NULL REFERENCES employees(employee_id) ON DELETE CASCADE,
    sender_id           VARCHAR(20) REFERENCES employees(employee_id),
    notification_type   notification_type_enum NOT NULL,
    title               VARCHAR(500) NOT NULL,
    message             TEXT NOT NULL,
    reference_type      VARCHAR(50),       -- 'exception', 'corrective_action', 'escalation'
    reference_id        UUID,
    is_read             BOOLEAN NOT NULL DEFAULT FALSE,
    read_at             TIMESTAMPTZ,
    priority            VARCHAR(10) NOT NULL DEFAULT 'Normal' CHECK (priority IN ('Low', 'Normal', 'High', 'Urgent')),
    expires_at          TIMESTAMPTZ,
    created_at          TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================================
-- 15. AUDIT LOGS
-- ============================================================================

CREATE TABLE audit_logs (
    id                  UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    table_name          VARCHAR(100) NOT NULL,
    record_id           VARCHAR(100) NOT NULL,
    action              audit_action_enum NOT NULL,
    old_values          JSONB,
    new_values          JSONB,
    changed_columns     TEXT[],
    performed_by        VARCHAR(50) NOT NULL,
    performed_by_id     VARCHAR(20),     -- Employee ID if applicable
    ip_address          VARCHAR(45),
    user_agent          TEXT,
    performed_at        TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ============================================================================
-- 16. DEPARTMENT METRICS
-- ============================================================================

CREATE TABLE department_metrics (
    id                      UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    department_id           UUID NOT NULL REFERENCES departments(id) ON DELETE CASCADE,
    metric_date             DATE NOT NULL,
    shift_type              shift_type_enum,
    total_employees         INTEGER NOT NULL DEFAULT 0,
    present_count           INTEGER NOT NULL DEFAULT 0,
    late_count              INTEGER NOT NULL DEFAULT 0,
    early_departure_count   INTEGER NOT NULL DEFAULT 0,
    absent_count            INTEGER NOT NULL DEFAULT 0,
    awol_count              INTEGER NOT NULL DEFAULT 0,
    leave_count             INTEGER NOT NULL DEFAULT 0,
    attendance_percentage   DECIMAL(5,2) NOT NULL DEFAULT 0,
    total_lost_minutes      INTEGER NOT NULL DEFAULT 0,
    total_lost_hours        DECIMAL(10,2) NOT NULL DEFAULT 0,
    exceptions_count        INTEGER NOT NULL DEFAULT 0,
    warnings_issued         INTEGER NOT NULL DEFAULT 0,
    escalations_count       INTEGER NOT NULL DEFAULT 0,
    calculated_at           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_at              TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_dept_metrics UNIQUE (department_id, metric_date, shift_type)
);

-- ============================================================================
-- INDEXES
-- ============================================================================

-- Employees
CREATE INDEX idx_employees_department ON employees(department_id);
CREATE INDEX idx_employees_status ON employees(employment_status);
CREATE INDEX idx_employees_name ON employees(display_name);
CREATE INDEX idx_employees_active ON employees(is_active) WHERE is_active = TRUE;

-- Reporting Hierarchy
CREATE INDEX idx_hierarchy_employee ON employee_reporting_hierarchy(employee_id);
CREATE INDEX idx_hierarchy_manager1 ON employee_reporting_hierarchy(manager_level_1);
CREATE INDEX idx_hierarchy_manager2 ON employee_reporting_hierarchy(manager_level_2);
CREATE INDEX idx_hierarchy_current ON employee_reporting_hierarchy(employee_id) WHERE is_current = TRUE;

-- App Accounts
CREATE INDEX idx_accounts_username ON app_accounts(username);
CREATE INDEX idx_accounts_employee ON app_accounts(employee_id);
CREATE INDEX idx_accounts_active ON app_accounts(is_active) WHERE is_active = TRUE;

-- Account Roles
CREATE INDEX idx_account_roles_account ON account_roles(account_id);
CREATE INDEX idx_account_roles_role ON account_roles(role_id);

-- Attendance Records
CREATE INDEX idx_attendance_employee ON attendance_records(employee_id);
CREATE INDEX idx_attendance_date ON attendance_records(attendance_date);
CREATE INDEX idx_attendance_status ON attendance_records(status);
CREATE INDEX idx_attendance_emp_date ON attendance_records(employee_id, attendance_date);
CREATE INDEX idx_attendance_date_range ON attendance_records(attendance_date, shift_type);
CREATE INDEX idx_attendance_source ON attendance_records(source);

-- Attendance Exceptions
CREATE INDEX idx_exceptions_employee ON attendance_exceptions(employee_id);
CREATE INDEX idx_exceptions_date ON attendance_exceptions(exception_date);
CREATE INDEX idx_exceptions_type ON attendance_exceptions(exception_type);
CREATE INDEX idx_exceptions_status ON attendance_exceptions(investigation_status);
CREATE INDEX idx_exceptions_emp_date ON attendance_exceptions(employee_id, exception_date);
CREATE INDEX idx_exceptions_pending ON attendance_exceptions(investigation_status) WHERE investigation_status = 'Pending';
CREATE INDEX idx_exceptions_ready ON attendance_exceptions(is_disciplinary_ready) WHERE is_disciplinary_ready = TRUE;

-- Exception Reasons
CREATE INDEX idx_exception_reasons_exception ON exception_reasons(exception_id);

-- Corrective Actions
CREATE INDEX idx_actions_employee ON corrective_actions(employee_id);
CREATE INDEX idx_actions_type ON corrective_actions(action_type);
CREATE INDEX idx_actions_status ON corrective_actions(status);
CREATE INDEX idx_actions_issued_to ON corrective_actions(issued_to);
CREATE INDEX idx_actions_issued_by ON corrective_actions(issued_by);
CREATE INDEX idx_actions_active ON corrective_actions(is_active) WHERE is_active = TRUE;
CREATE INDEX idx_actions_expiry ON corrective_actions(expiry_date) WHERE is_active = TRUE;

-- Warning Documents
CREATE INDEX idx_documents_action ON warning_documents(corrective_action_id);
CREATE INDEX idx_documents_storage ON warning_documents(file_storage_key);

-- Escalation Logs
CREATE INDEX idx_escalations_employee ON escalation_logs(employee_id);
CREATE INDEX idx_escalations_level ON escalation_logs(escalation_level);
CREATE INDEX idx_escalations_status ON escalation_logs(status);
CREATE INDEX idx_escalations_to_mgr ON escalation_logs(to_manager_id);
CREATE INDEX idx_escalations_deadline ON escalation_logs(action_deadline);
CREATE INDEX idx_escalations_overdue ON escalation_logs(is_overdue) WHERE is_overdue = TRUE;
CREATE INDEX idx_escalations_pending ON escalation_logs(status) WHERE status = 'Pending';

-- Notifications
CREATE INDEX idx_notifications_recipient ON notifications(recipient_id);
CREATE INDEX idx_notifications_type ON notifications(notification_type);
CREATE INDEX idx_notifications_unread ON notifications(recipient_id, is_read) WHERE is_read = FALSE;
CREATE INDEX idx_notifications_created ON notifications(created_at DESC);
CREATE INDEX idx_notifications_ref ON notifications(reference_type, reference_id);

-- Audit Logs
CREATE INDEX idx_audit_table ON audit_logs(table_name);
CREATE INDEX idx_audit_record ON audit_logs(table_name, record_id);
CREATE INDEX idx_audit_performed_by ON audit_logs(performed_by);
CREATE INDEX idx_audit_performed_at ON audit_logs(performed_at DESC);
CREATE INDEX idx_audit_employee ON audit_logs(performed_by_id);

-- Department Metrics
CREATE INDEX idx_dept_metrics_dept ON department_metrics(department_id);
CREATE INDEX idx_dept_metrics_date ON department_metrics(metric_date);
CREATE INDEX idx_dept_metrics_range ON department_metrics(department_id, metric_date);

-- Attendance Rules
CREATE INDEX idx_att_rules_shift ON attendance_rules(shift_type);
CREATE INDEX idx_att_rules_active ON attendance_rules(is_active) WHERE is_active = TRUE;

-- Disciplinary Rules
CREATE INDEX idx_disc_rules_type ON disciplinary_rules(offence_type);
CREATE INDEX idx_disc_rules_active ON disciplinary_rules(is_active) WHERE is_active = TRUE;

-- ============================================================================
-- FOREIGN KEY INDEXES (for join performance)
-- ============================================================================

CREATE INDEX idx_attendance_record_emp_fk ON attendance_records(employee_id);
CREATE INDEX idx_exceptions_record_fk ON attendance_exceptions(attendance_record_id);
CREATE INDEX idx_exceptions_emp_fk ON attendance_exceptions(employee_id);
CREATE INDEX idx_actions_emp_fk ON corrective_actions(employee_id);
CREATE INDEX idx_actions_exception_fk ON corrective_actions(exception_id);
CREATE INDEX idx_escalations_action_fk ON escalation_logs(corrective_action_id);
CREATE INDEX idx_documents_action_fk ON warning_documents(corrective_action_id);

-- ============================================================================
-- AUDIT TRIGGER FUNCTION
-- ============================================================================

CREATE OR REPLACE FUNCTION fn_audit_trigger()
RETURNS TRIGGER AS $$
DECLARE
    v_old_values JSONB;
    v_new_values JSONB;
    v_changed_cols TEXT[];
    v_actor VARCHAR(50);
    v_employee_id VARCHAR(20);
    v_ip VARCHAR(45);
    v_ua TEXT;
BEGIN
    -- Get session variables set by the application
    v_actor := current_setting('app.current_user', TRUE);
    v_employee_id := current_setting('app.employee_id', TRUE);
    v_ip := current_setting('app.ip_address', TRUE);
    v_ua := current_setting('app.user_agent', TRUE);

    IF v_actor IS NULL OR v_actor = '' THEN
        v_actor := 'SYSTEM';
    END IF;

    IF TG_OP = 'DELETE' THEN
        v_old_values := to_jsonb(OLD);
        v_new_values := NULL;
        v_changed_cols := ARRAY(SELECT jsonb_object_keys(v_old_values));
    ELSIF TG_OP = 'INSERT' THEN
        v_old_values := NULL;
        v_new_values := to_jsonb(NEW);
        v_changed_cols := ARRAY(SELECT jsonb_object_keys(v_new_values));
    ELSIF TG_OP = 'UPDATE' THEN
        v_old_values := to_jsonb(OLD);
        v_new_values := to_jsonb(NEW);
        -- Track only changed columns
        v_changed_cols := ARRAY(
            SELECT key
            FROM jsonb_each(v_new_values) AS n(key, new_val)
            JOIN jsonb_each(v_old_values) AS o(key, old_val) USING (key)
            WHERE n.new_val IS DISTINCT FROM o.old_val
        );
        -- Skip if no actual changes
        IF array_length(v_changed_cols, 1) IS NULL THEN
            RETURN NEW;
        END IF;
    END IF;

    INSERT INTO audit_logs (
        table_name, record_id, action,
        old_values, new_values, changed_columns,
        performed_by, performed_by_id,
        ip_address, user_agent, performed_at
    ) VALUES (
        TG_TABLE_NAME,
        COALESCE(
            CASE TG_OP
                WHEN 'DELETE' THEN OLD.id::TEXT
                WHEN 'INSERT' THEN NEW.id::TEXT
                WHEN 'UPDATE' THEN NEW.id::TEXT
            END,
            'UNKNOWN'
        ),
        CASE TG_OP
            WHEN 'INSERT' THEN 'INSERT'::audit_action_enum
            WHEN 'UPDATE' THEN 'UPDATE'::audit_action_enum
            WHEN 'DELETE' THEN 'DELETE'::audit_action_enum
        END,
        v_old_values,
        v_new_values,
        v_changed_cols,
        v_actor,
        v_employee_id,
        v_ip,
        v_ua,
        NOW()
    );

    IF TG_OP = 'DELETE' THEN
        RETURN OLD;
    ELSE
        RETURN NEW;
    END IF;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- APPLY AUDIT TRIGGERS TO ALL TABLES
-- ============================================================================

CREATE TRIGGER trg_audit_employees
    AFTER INSERT OR UPDATE OR DELETE ON employees
    FOR EACH ROW EXECUTE FUNCTION fn_audit_trigger();

CREATE TRIGGER trg_audit_employee_hierarchy
    AFTER INSERT OR UPDATE OR DELETE ON employee_reporting_hierarchy
    FOR EACH ROW EXECUTE FUNCTION fn_audit_trigger();

CREATE TRIGGER trg_audit_app_accounts
    AFTER INSERT OR UPDATE OR DELETE ON app_accounts
    FOR EACH ROW EXECUTE FUNCTION fn_audit_trigger();

CREATE TRIGGER trg_audit_attendance_records
    AFTER INSERT OR UPDATE OR DELETE ON attendance_records
    FOR EACH ROW EXECUTE FUNCTION fn_audit_trigger();

CREATE TRIGGER trg_audit_attendance_exceptions
    AFTER INSERT OR UPDATE OR DELETE ON attendance_exceptions
    FOR EACH ROW EXECUTE FUNCTION fn_audit_trigger();

CREATE TRIGGER trg_audit_exception_reasons
    AFTER INSERT OR UPDATE OR DELETE ON exception_reasons
    FOR EACH ROW EXECUTE FUNCTION fn_audit_trigger();

CREATE TRIGGER trg_audit_corrective_actions
    AFTER INSERT OR UPDATE OR DELETE ON corrective_actions
    FOR EACH ROW EXECUTE FUNCTION fn_audit_trigger();

CREATE TRIGGER trg_audit_warning_documents
    AFTER INSERT OR UPDATE OR DELETE ON warning_documents
    FOR EACH ROW EXECUTE FUNCTION fn_audit_trigger();

CREATE TRIGGER trg_audit_escalation_logs
    AFTER INSERT OR UPDATE OR DELETE ON escalation_logs
    FOR EACH ROW EXECUTE FUNCTION fn_audit_trigger();

CREATE TRIGGER trg_audit_notifications
    AFTER INSERT OR UPDATE OR DELETE ON notifications
    FOR EACH ROW EXECUTE FUNCTION fn_audit_trigger();

CREATE TRIGGER trg_audit_departments
    AFTER INSERT OR UPDATE OR DELETE ON departments
    FOR EACH ROW EXECUTE FUNCTION fn_audit_trigger();

CREATE TRIGGER trg_audit_roles
    AFTER INSERT OR UPDATE OR DELETE ON roles
    FOR EACH ROW EXECUTE FUNCTION fn_audit_trigger();

CREATE TRIGGER trg_audit_attendance_rules
    AFTER INSERT OR UPDATE OR DELETE ON attendance_rules
    FOR EACH ROW EXECUTE FUNCTION fn_audit_trigger();

CREATE TRIGGER trg_audit_disciplinary_rules
    AFTER INSERT OR UPDATE OR DELETE ON disciplinary_rules
    FOR EACH ROW EXECUTE FUNCTION fn_audit_trigger();

-- ============================================================================
-- UPDATED_AT AUTO-TRIGGER
-- ============================================================================

CREATE OR REPLACE FUNCTION fn_set_updated_at()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_updated_employees BEFORE UPDATE ON employees FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();
CREATE TRIGGER trg_updated_hierarchy BEFORE UPDATE ON employee_reporting_hierarchy FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();
CREATE TRIGGER trg_updated_accounts BEFORE UPDATE ON app_accounts FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();
CREATE TRIGGER trg_updated_attendance BEFORE UPDATE ON attendance_records FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();
CREATE TRIGGER trg_updated_exceptions BEFORE UPDATE ON attendance_exceptions FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();
CREATE TRIGGER trg_updated_actions BEFORE UPDATE ON corrective_actions FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();
CREATE TRIGGER trg_updated_documents BEFORE UPDATE ON warning_documents FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();
CREATE TRIGGER trg_updated_escalations BEFORE UPDATE ON escalation_logs FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();
CREATE TRIGGER trg_updated_departments BEFORE UPDATE ON departments FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();
CREATE TRIGGER trg_updated_roles BEFORE UPDATE ON roles FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();
CREATE TRIGGER trg_updated_att_rules BEFORE UPDATE ON attendance_rules FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();
CREATE TRIGGER trg_updated_disc_rules BEFORE UPDATE ON disciplinary_rules FOR EACH ROW EXECUTE FUNCTION fn_set_updated_at();

-- ============================================================================
-- SEED DATA: DEFAULT ROLES
-- ============================================================================

INSERT INTO roles (role_name, display_name, description, permissions, is_system_role) VALUES
('Administrator', 'System Administrator', 'Full system access',
    ARRAY['view_dashboard','manage_employees','manage_attendance','investigate_exceptions',
          'issue_warnings','approve_warnings','manage_escalations','view_reports',
          'manage_users','manage_departments','manage_rules','view_audit_logs',
          'manage_system_settings','upload_documents'], TRUE),
('ProductionManager', 'Production Manager', 'Manage production department attendance and warnings',
    ARRAY['view_dashboard','manage_employees','manage_attendance','investigate_exceptions',
          'issue_warnings','approve_warnings','manage_escalations','view_reports',
          'upload_documents'], TRUE),
('TeamLeader', 'Team Leader', 'Record attendance, investigate exceptions, issue initial warnings',
    ARRAY['view_dashboard','manage_attendance','investigate_exceptions',
          'issue_warnings','view_reports','upload_documents'], TRUE),
('Supervisor', 'Supervisor', 'View team attendance, investigate exceptions',
    ARRAY['view_dashboard','manage_attendance','investigate_exceptions','view_reports'], TRUE),
('QualityTech', 'Quality Technician', 'View quality-related attendance data',
    ARRAY['view_dashboard','view_reports'], TRUE),
('Operator', 'Operator', 'Basic employee access',
    ARRAY['view_dashboard'], TRUE),
('Viewer', 'Read-Only Viewer', 'Dashboard and reports only',
    ARRAY['view_dashboard','view_reports'], TRUE);

-- ============================================================================
-- SEED DATA: DEPARTMENTS
-- ============================================================================

INSERT INTO departments (department_code, department_name, cost_center) VALUES
('PROD', 'Production', 'CC-1000'),
('LOG', 'Logistics', 'CC-2000'),
('QA', 'Quality', 'CC-3000'),
('ENG', 'Engineering', 'CC-4000'),
('OFFICE', 'Office / Admin', 'CC-5000');

-- ============================================================================
-- SEED DATA: ATTENDANCE RULES
-- ============================================================================

INSERT INTO attendance_rules (rule_name, shift_type, expected_clock_in, expected_clock_out, late_grace_minutes, early_grace_minutes, late_threshold_minutes, half_day_threshold_min, effective_from) VALUES
('Morning Shift', 'Morning', '06:00', '14:00', 5, 5, 30, 120, '2022-08-01'),
('Afternoon Shift', 'Afternoon', '14:00', '22:00', 5, 5, 30, 120, '2022-08-01'),
('Night Shift', 'Night', '22:00', '06:00', 5, 5, 30, 120, '2022-08-01');

-- ============================================================================
-- SEED DATA: DISCIPLINARY RULES (YFPO Code of Conduct)
-- ============================================================================

-- Late / Early Departure
INSERT INTO disciplinary_rules (offence_type, occurrence_count, discipline_level, description) VALUES
('LateArrival', 1, 'VerbalWarning', '1st occurrence — Verbal Warning'),
('LateArrival', 2, 'WrittenWarning', '2nd occurrence — Written Warning'),
('LateArrival', 3, 'FinalWrittenWarning', '3rd occurrence — Final Written Warning'),
('LateArrival', 4, 'Dismissal', '4th occurrence — Dismissal'),
('EarlyDeparture', 1, 'VerbalWarning', '1st occurrence — Verbal Warning'),
('EarlyDeparture', 2, 'WrittenWarning', '2nd occurrence — Written Warning'),
('EarlyDeparture', 3, 'FinalWrittenWarning', '3rd occurrence — Final Written Warning'),
('EarlyDeparture', 4, 'Dismissal', '4th occurrence — Dismissal');

-- AWOL (1-2 days)
INSERT INTO disciplinary_rules (offence_type, occurrence_count, discipline_level, description) VALUES
('AWOL', 1, 'WrittenWarning', 'AWOL 1-2 days, 1st — Written Warning'),
('AWOL', 2, 'FinalWrittenWarning', 'AWOL 1-2 days, 2nd — Final Written Warning'),
('AWOL', 3, 'Dismissal', 'AWOL 1-2 days, 3rd — Dismissal');

-- Unplanned Absenteeism
INSERT INTO disciplinary_rules (offence_type, occurrence_count, discipline_level, description) VALUES
('UnplannedAbsenteeism', 1, 'VerbalWarning', '1st occurrence — Verbal Warning'),
('UnplannedAbsenteeism', 2, 'WrittenWarning', '2nd occurrence — Written Warning'),
('UnplannedAbsenteeism', 3, 'FinalWrittenWarning', '3rd occurrence — Final Written Warning'),
('UnplannedAbsenteeism', 4, 'Dismissal', '4th occurrence — Dismissal');

-- Missing Clock In/Out
INSERT INTO disciplinary_rules (offence_type, occurrence_count, discipline_level, description) VALUES
('MissingClockIn', 1, 'VerbalWarning', '1st occurrence — Verbal Warning'),
('MissingClockIn', 2, 'WrittenWarning', '2nd occurrence — Written Warning'),
('MissingClockIn', 3, 'FinalWrittenWarning', '3rd occurrence — Final Written Warning'),
('MissingClockOut', 1, 'VerbalWarning', '1st occurrence — Verbal Warning'),
('MissingClockOut', 2, 'WrittenWarning', '2nd occurrence — Written Warning'),
('MissingClockOut', 3, 'FinalWrittenWarning', '3rd occurrence — Final Written Warning');

-- Unscheduled Clock In/Out
INSERT INTO disciplinary_rules (offence_type, occurrence_count, discipline_level, description) VALUES
('UnscheduledClockIn', 1, 'VerbalWarning', '1st occurrence — Verbal Warning'),
('UnscheduledClockIn', 2, 'WrittenWarning', '2nd occurrence — Written Warning'),
('UnscheduledClockIn', 3, 'FinalWrittenWarning', '3rd occurrence — Final Written Warning'),
('UnscheduledClockOut', 1, 'VerbalWarning', '1st occurrence — Verbal Warning'),
('UnscheduledClockOut', 2, 'WrittenWarning', '2nd occurrence — Written Warning'),
('UnscheduledClockOut', 3, 'FinalWrittenWarning', '3rd occurrence — Final Written Warning');

-- Shift Cancellation
INSERT INTO disciplinary_rules (offence_type, occurrence_count, discipline_level, description) VALUES
('ShiftCancellation', 1, 'WrittenWarning', '1st occurrence — Written Warning'),
('ShiftCancellation', 2, 'FinalWrittenWarning', '2nd occurrence — Final Written Warning'),
('ShiftCancellation', 3, 'Dismissal', '3rd occurrence — Dismissal');

-- ============================================================================
-- UTILITY FUNCTIONS
-- ============================================================================

-- Calculate attendance percentage for a department on a given date
CREATE OR REPLACE FUNCTION fn_calculate_department_attendance(
    p_department_id UUID,
    p_date DATE
)
RETURNS DECIMAL(5,2) AS $$
DECLARE
    v_total INTEGER;
    v_present INTEGER;
    v_result DECIMAL(5,2);
BEGIN
    SELECT COUNT(DISTINCT e.employee_id)
    INTO v_total
    FROM employees e
    WHERE e.department_id = p_department_id
      AND e.employment_status = 'Active'
      AND e.is_active = TRUE;

    IF v_total = 0 THEN
        RETURN 0;
    END IF;

    SELECT COUNT(DISTINCT ar.employee_id)
    INTO v_present
    FROM attendance_records ar
    WHERE ar.attendance_date = p_date
      AND ar.status IN ('Present', 'Late', 'EarlyDeparture')
      AND ar.employee_id IN (
          SELECT e2.employee_id FROM employees e2
          WHERE e2.department_id = p_department_id
            AND e2.employment_status = 'Active'
      );

    v_result := (v_present::DECIMAL / v_total::DECIMAL) * 100;
    RETURN ROUND(v_result, 2);
END;
$$ LANGUAGE plpgsql STABLE;

-- Calculate lost production hours for a department on a given date
CREATE OR REPLACE FUNCTION fn_calculate_department_lost_hours(
    p_department_id UUID,
    p_date DATE
)
RETURNS DECIMAL(10,2) AS $$
DECLARE
    v_total_minutes INTEGER;
BEGIN
    SELECT COALESCE(SUM(ar.lost_minutes), 0)
    INTO v_total_minutes
    FROM attendance_records ar
    WHERE ar.attendance_date = p_date
      AND ar.employee_id IN (
          SELECT e.employee_id FROM employees e
          WHERE e.department_id = p_department_id
            AND e.employment_status = 'Active'
      );

    RETURN ROUND(v_total_minutes::DECIMAL / 60.0, 2);
END;
$$ LANGUAGE plpgsql STABLE;

-- Get next disciplinary level for an employee based on offence history
CREATE OR REPLACE FUNCTION fn_get_next_discipline_level(
    p_employee_id VARCHAR(20),
    p_offence_type exception_type_enum
)
RETURNS corrective_action_type_enum AS $$
DECLARE
    v_occurrence_count INTEGER;
    v_result corrective_action_type_enum;
BEGIN
    SELECT COUNT(*) + 1
    INTO v_occurrence_count
    FROM corrective_actions ca
    WHERE ca.issued_to = p_employee_id
      AND ca.offence_type = p_offence_type
      AND ca.is_active = TRUE
      AND ca.status IN ('Issued', 'Acknowledged', 'Upheld');

    SELECT dr.discipline_level
    INTO v_result
    FROM disciplinary_rules dr
    WHERE dr.offence_type = p_offence_type
      AND dr.occurrence_count = v_occurrence_count
      AND dr.is_active = TRUE;

    RETURN COALESCE(v_result, 'FinalWrittenWarning'::corrective_action_type_enum);
END;
$$ LANGUAGE plpgsql STABLE;

-- Check if investigation is completed for an exception (gate for disciplinary action)
CREATE OR REPLACE FUNCTION fn_can_issue_disciplinary(
    p_exception_id UUID
)
RETURNS BOOLEAN AS $$
DECLARE
    v_status investigation_status_enum;
    v_ready BOOLEAN;
BEGIN
    SELECT investigation_status, is_disciplinary_ready
    INTO v_status, v_ready
    FROM attendance_exceptions
    WHERE id = p_exception_id;

    RETURN (v_status = 'Completed' AND v_ready = TRUE);
END;
$$ LANGUAGE plpgsql STABLE;

-- ============================================================================
-- VIEWS
-- ============================================================================

-- Employee attendance summary view
CREATE OR REPLACE VIEW v_employee_attendance_summary AS
SELECT
    e.employee_id,
    e.display_name,
    e.email,
    d.department_name,
    e.employment_status,
    COUNT(ar.id) AS total_records,
    SUM(CASE WHEN ar.status = 'Present' THEN 1 ELSE 0 END) AS present_days,
    SUM(CASE WHEN ar.status = 'Late' THEN 1 ELSE 0 END) AS late_days,
    SUM(CASE WHEN ar.status = 'EarlyDeparture' THEN 1 ELSE 0 END) AS early_days,
    SUM(CASE WHEN ar.status = 'Absent' THEN 1 ELSE 0 END) AS absent_days,
    SUM(CASE WHEN ar.status = 'AWOL' THEN 1 ELSE 0 END) AS awol_days,
    SUM(ar.minutes_late) AS total_minutes_late,
    SUM(ar.minutes_early) AS total_minutes_early,
    SUM(ar.lost_minutes) AS total_lost_minutes,
    ROUND(SUM(ar.lost_minutes)::DECIMAL / 60.0, 2) AS total_lost_hours
FROM employees e
JOIN departments d ON e.department_id = d.id
LEFT JOIN attendance_records ar ON e.employee_id = ar.employee_id
WHERE e.is_active = TRUE
GROUP BY e.employee_id, e.display_name, e.email, d.department_name, e.employment_status;

-- Current reporting hierarchy view
CREATE OR REPLACE VIEW v_current_hierarchy AS
SELECT
    e.employee_id,
    e.display_name AS employee_name,
    d.department_name,
    h.manager_level_1,
    m1.display_name AS manager_level_1_name,
    h.manager_level_2,
    m2.display_name AS manager_level_2_name,
    h.manager_level_3,
    m3.display_name AS manager_level_3_name,
    h.manager_level_4,
    m4.display_name AS manager_level_4_name,
    h.manager_level_5,
    m5.display_name AS manager_level_5_name
FROM employees e
JOIN departments d ON e.department_id = d.id
JOIN employee_reporting_hierarchy h ON e.employee_id = h.employee_id AND h.is_current = TRUE
LEFT JOIN employees m1 ON h.manager_level_1 = m1.employee_id
LEFT JOIN employees m2 ON h.manager_level_2 = m2.employee_id
LEFT JOIN employees m3 ON h.manager_level_3 = m3.employee_id
LEFT JOIN employees m4 ON h.manager_level_4 = m4.employee_id
LEFT JOIN employees m5 ON h.manager_level_5 = m5.employee_id
WHERE e.is_active = TRUE;

-- Exception investigation status view
CREATE OR REPLACE VIEW v_exception_investigation_status AS
SELECT
    ae.id AS exception_id,
    ae.employee_id,
    e.display_name AS employee_name,
    ae.exception_type,
    ae.exception_date,
    ae.minutes_lost,
    ae.investigation_status,
    ae.is_disciplinary_ready,
    ae.detected_at,
    ae.investigated_by,
    inv.display_name AS investigator_name,
    h.manager_level_1 AS current_manager,
    mgr.display_name AS manager_name,
    COALESCE(
        (SELECT COUNT(*) FROM corrective_actions ca
         WHERE ca.issued_to = ae.employee_id
           AND ca.offence_type = ae.exception_type
           AND ca.is_active = TRUE),
        0
    ) AS prior_occurrences
FROM attendance_exceptions ae
JOIN employees e ON ae.employee_id = e.employee_id
LEFT JOIN employees inv ON ae.investigated_by = inv.employee_id
LEFT JOIN employee_reporting_hierarchy h ON ae.employee_id = h.employee_id AND h.is_current = TRUE
LEFT JOIN employees mgr ON h.manager_level_1 = mgr.employee_id;

-- Escalation status view with overdue detection
CREATE OR REPLACE VIEW v_escalation_status AS
SELECT
    el.id AS escalation_id,
    el.employee_id,
    e.display_name AS employee_name,
    el.escalation_level,
    fm.display_name AS from_manager_name,
    tm.display_name AS to_manager_name,
    el.status,
    el.reason,
    el.action_required,
    el.action_deadline,
    el.is_overdue,
    el.escalated_at,
    el.action_taken_at,
    CASE
        WHEN el.status = 'Pending' AND el.action_deadline < NOW() THEN TRUE
        ELSE el.is_overdue
    END AS is_actually_overdue,
    EXTRACT(EPOCH FROM (el.action_deadline - NOW())) / 3600 AS hours_remaining
FROM escalation_logs el
JOIN employees e ON el.employee_id = e.employee_id
JOIN employees fm ON el.from_manager_id = fm.employee_id
JOIN employees tm ON el.to_manager_id = tm.employee_id;

-- Department daily metrics view
CREATE OR REPLACE VIEW v_department_daily_metrics AS
SELECT
    d.id AS department_id,
    d.department_code,
    d.department_name,
    dm.metric_date,
    dm.shift_type,
    dm.total_employees,
    dm.present_count,
    dm.late_count,
    dm.early_departure_count,
    dm.absent_count,
    dm.awol_count,
    dm.leave_count,
    dm.attendance_percentage,
    dm.total_lost_minutes,
    dm.total_lost_hours,
    dm.exceptions_count,
    dm.warnings_issued,
    dm.escalations_count
FROM departments d
LEFT JOIN department_metrics dm ON d.id = dm.department_id
WHERE d.is_active = TRUE;

-- ============================================================================
-- PARTITIONING (for large-scale attendance data)
-- ============================================================================
-- Note: For production with millions of records, consider partitioning
-- attendance_records by month:
--
-- CREATE TABLE attendance_records_partitioned (
--     ... same columns ...
-- ) PARTITION BY RANGE (attendance_date);
--
-- CREATE TABLE attendance_records_2026_01 PARTITION OF attendance_records_partitioned
--     FOR VALUES FROM ('2026-01-01') TO ('2026-02-01');
-- ...

-- ============================================================================
-- ROW LEVEL SECURITY (optional, for multi-tenant scenarios)
-- ============================================================================

-- Enable RLS on audit_logs (read-only for non-admins)
ALTER TABLE audit_logs ENABLE ROW LEVEL SECURITY;

CREATE POLICY audit_logs_admin_access ON audit_logs
    FOR ALL
    USING (
        EXISTS (
            SELECT 1 FROM account_roles ar
            JOIN roles r ON ar.role_id = r.id
            JOIN app_accounts aa ON ar.account_id = aa.id
            WHERE aa.employee_id = current_setting('app.employee_id', TRUE)
              AND r.role_name = 'Administrator'
        )
    );

CREATE POLICY audit_logs_self_access ON audit_logs
    FOR SELECT
    USING (performed_by_id = current_setting('app.employee_id', TRUE));

-- ============================================================================
-- FINAL: Schema version tracking
-- ============================================================================

CREATE TABLE schema_versions (
    version         VARCHAR(20) PRIMARY KEY,
    applied_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    description     TEXT,
    applied_by      VARCHAR(50)
);

INSERT INTO schema_versions (version, description, applied_by) VALUES
('1.0.0', 'Initial schema — WACAMS production database', 'SYSTEM');

-- ============================================================================
-- END OF SCHEMA
-- ============================================================================