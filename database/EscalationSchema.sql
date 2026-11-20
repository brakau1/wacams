-- WACAMS Escalation Workflow Database Schema
-- This schema supports the multi-level escalation system with 24-hour timeouts

-- ============================================================================
-- Table: escalations
-- Stores active escalation records
-- ============================================================================
CREATE TABLE escalations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    exception_id UUID NOT NULL,
    employee_id VARCHAR(50) NOT NULL,
    employee_name VARCHAR(200),
    corrective_action_id UUID,
    current_level INTEGER NOT NULL DEFAULT 1,
    current_manager_id UUID NOT NULL,
    current_manager_name VARCHAR(200),
    status VARCHAR(50) NOT NULL DEFAULT 'Pending',
    escalation_time TIMESTAMP WITH TIME ZONE NOT NULL,
    next_escalation_time TIMESTAMP WITH TIME ZONE NOT NULL,
    document_uploaded BOOLEAN NOT NULL DEFAULT FALSE,
    document_url TEXT,
    action_notes TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    closed_at TIMESTAMP WITH TIME ZONE,
    resolved_at TIMESTAMP WITH TIME ZONE,
    
    CONSTRAINT chk_escalation_status CHECK (status IN ('Pending', 'Acknowledged', 'ActionTaken', 'Escalated', 'Closed', 'Resolved', 'FinalEscalation')),
    CONSTRAINT chk_escalation_level CHECK (current_level >= 1 AND current_level <= 7)
);

-- Indexes for escalations
CREATE INDEX idx_escalations_exception_id ON escalations(exception_id);
CREATE INDEX idx_escalations_employee_id ON escalations(employee_id);
CREATE INDEX idx_escalations_manager_id ON escalations(current_manager_id);
CREATE INDEX idx_escalations_status ON escalations(status);
CREATE INDEX idx_escalations_next_escalation_time ON escalations(next_escalation_time);
CREATE INDEX idx_escalations_created_at ON escalations(created_at);

-- ============================================================================
-- Table: escalation_history
-- Tracks all state changes and actions for each escalation
-- ============================================================================
CREATE TABLE escalation_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    escalation_id UUID NOT NULL REFERENCES escalations(id) ON DELETE CASCADE,
    from_level INTEGER,
    to_level INTEGER,
    from_manager_id UUID,
    from_manager_name VARCHAR(200),
    to_manager_id UUID,
    to_manager_name VARCHAR(200),
    action VARCHAR(100) NOT NULL,
    comments TEXT,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    CONSTRAINT chk_history_action CHECK (action IN ('Created', 'Acknowledged', 'Escalated', 'ActionTaken', 'Closed', 'Resolved'))
);

-- Indexes for escalation_history
CREATE INDEX idx_escalation_history_escalation_id ON escalation_history(escalation_id);
CREATE INDEX idx_escalation_history_timestamp ON escalation_history(timestamp);
CREATE INDEX idx_escalation_history_action ON escalation_history(action);

-- ============================================================================
-- Table: escalation_audit_logs
-- Comprehensive audit trail for compliance and debugging
-- ============================================================================
CREATE TABLE escalation_audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(100) NOT NULL,
    entity_id VARCHAR(100) NOT NULL,
    user_id VARCHAR(100) NOT NULL,
    details TEXT,
    timestamp TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

-- Indexes for audit logs
CREATE INDEX idx_audit_logs_entity ON escalation_audit_logs(entity_type, entity_id);
CREATE INDEX idx_audit_logs_user_id ON escalation_audit_logs(user_id);
CREATE INDEX idx_audit_logs_timestamp ON escalation_audit_logs(timestamp);
CREATE INDEX idx_audit_logs_action ON escalation_audit_logs(action);

-- ============================================================================
-- Table: escalation_levels
-- Configuration for escalation levels and managers
-- ============================================================================
CREATE TABLE escalation_levels (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    level INTEGER NOT NULL UNIQUE,
    manager_id UUID NOT NULL,
    manager_name VARCHAR(200) NOT NULL,
    escalation_after_hours INTEGER NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    
    CONSTRAINT chk_level_range CHECK (level >= 1 AND level <= 7),
    CONSTRAINT chk_hours_positive CHECK (escalation_after_hours > 0)
);

-- Index for escalation levels
CREATE INDEX idx_escalation_levels_level ON escalation_levels(level);
CREATE INDEX idx_escalation_levels_active ON escalation_levels(is_active);

-- ============================================================================
-- Insert default escalation levels
-- ============================================================================
INSERT INTO escalation_levels (level, manager_id, manager_name, escalation_after_hours) VALUES
(1, '00000000-0000-0000-0000-000000000001', 'Manager Level 1', 24),
(2, '00000000-0000-0000-0000-000000000002', 'Manager Level 2', 48),
(3, '00000000-0000-0000-0000-000000000003', 'Manager Level 3', 72),
(4, '00000000-0000-0000-0000-000000000004', 'Manager Level 4', 96),
(5, '00000000-0000-0000-0000-000000000005', 'Manager Level 5', 120),
(6, '00000000-0000-0000-0000-000000000006', 'HR Manager', 144),
(7, '00000000-0000-0000-0000-000000000007', 'Administrator', 168);

-- ============================================================================
-- Views for reporting and monitoring
-- ============================================================================

-- View: Active escalations with days remaining
CREATE VIEW v_active_escalations AS
SELECT 
    e.id,
    e.exception_id,
    e.employee_id,
    e.employee_name,
    e.current_level,
    e.current_manager_id,
    e.current_manager_name,
    e.status,
    e.escalation_time,
    e.next_escalation_time,
    e.document_uploaded,
    e.created_at,
    e.updated_at,
    EXTRACT(EPOCH FROM (e.next_escalation_time - NOW())) / 3600 AS hours_remaining,
    CASE 
        WHEN e.next_escalation_time <= NOW() THEN 'OVERDUE'
        WHEN EXTRACT(EPOCH FROM (e.next_escalation_time - NOW())) / 3600 < 6 THEN 'URGENT'
        ELSE 'ACTIVE'
    END AS urgency_status
FROM escalations e
WHERE e.status IN ('Pending', 'Acknowledged', 'ActionTaken')
ORDER BY e.next_escalation_time ASC;

-- View: Escalation statistics by manager
CREATE VIEW v_manager_escalation_stats AS
SELECT 
    current_manager_id,
    current_manager_name,
    COUNT(*) AS total_escalations,
    COUNT(*) FILTER (WHERE status = 'Pending') AS pending_count,
    COUNT(*) FILTER (WHERE status = 'Acknowledged') AS acknowledged_count,
    COUNT(*) FILTER (WHERE status = 'ActionTaken') AS action_taken_count,
    COUNT(*) FILTER (WHERE status = 'Closed') AS closed_count,
    COUNT(*) FILTER (WHERE status = 'Resolved') AS resolved_count,
    AVG(EXTRACT(EPOCH FROM (closed_at - created_at)) / 3600) FILTER (WHERE closed_at IS NOT NULL) AS avg_resolution_hours
FROM escalations
GROUP BY current_manager_id, current_manager_name;

-- View: Escalation history with full details
CREATE VIEW v_escalation_history_full AS
SELECT 
    h.id,
    h.escalation_id,
    e.exception_id,
    e.employee_id,
    e.employee_name,
    h.from_level,
    h.to_level,
    h.from_manager_id,
    h.from_manager_name,
    h.to_manager_id,
    h.to_manager_name,
    h.action,
    h.comments,
    h.timestamp,
    el.escalation_after_hours
FROM escalation_history h
JOIN escalations e ON h.escalation_id = e.id
LEFT JOIN escalation_levels el ON h.to_level = el.level
ORDER BY h.timestamp DESC;

-- ============================================================================
-- Functions and Triggers
-- ============================================================================

-- Function: Auto-update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = NOW();
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Trigger: Auto-update escalations.updated_at
CREATE TRIGGER update_escalations_updated_at
    BEFORE UPDATE ON escalations
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Trigger: Auto-update escalation_levels.updated_at
CREATE TRIGGER update_escalation_levels_updated_at
    BEFORE UPDATE ON escalation_levels
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- Function: Check and escalate overdue escalations
CREATE OR REPLACE FUNCTION process_overdue_escalations()
RETURNS INTEGER AS $$
DECLARE
    overdue_count INTEGER := 0;
    escalation_record RECORD;
BEGIN
    -- Find all overdue escalations
    FOR escalation_record IN
        SELECT id, current_level, next_escalation_time
        FROM escalations
        WHERE status IN ('Pending', 'Acknowledged', 'ActionTaken')
        AND next_escalation_time <= NOW()
        AND document_uploaded = FALSE
        AND current_level < 7
    LOOP
        -- Update escalation level
        UPDATE escalations
        SET 
            current_level = escalation_record.current_level + 1,
            status = 'Escalated',
            next_escalation_time = NOW() + (
                SELECT escalation_after_hours * INTERVAL '1 hour'
                FROM escalation_levels
                WHERE level = escalation_record.current_level + 1
            ),
            updated_at = NOW()
        WHERE id = escalation_record.id;
        
        -- Add history record
        INSERT INTO escalation_history (
            escalation_id,
            from_level,
            to_level,
            action,
            comments,
            timestamp
        ) VALUES (
            escalation_record.id,
            escalation_record.current_level,
            escalation_record.current_level + 1,
            'Escalated',
            'Automatically escalated due to timeout',
            NOW()
        );
        
        overdue_count := overdue_count + 1;
    END LOOP;
    
    RETURN overdue_count;
END;
$$ language 'plpgsql';

-- ============================================================================
-- Permissions and Security
-- ============================================================================

-- Create role for escalation management
DO $$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = 'escalation_manager') THEN
        CREATE ROLE escalation_manager;
    END IF;
END
$$;

-- Grant permissions
GRANT SELECT, INSERT, UPDATE ON escalations TO escalation_manager;
GRANT SELECT, INSERT ON escalation_history TO escalation_manager;
GRANT SELECT ON escalation_levels TO escalation_manager;
GRANT SELECT ON v_active_escalations TO escalation_manager;
GRANT SELECT ON v_manager_escalation_stats TO escalation_manager;
GRANT SELECT ON v_escalation_history_full TO escalation_manager;

-- ============================================================================
-- Comments for documentation
-- ============================================================================

COMMENT ON TABLE escalations IS 'Stores active escalation records with current level and manager assignment';
COMMENT ON TABLE escalation_history IS 'Tracks all state changes and actions for audit trail';
COMMENT ON TABLE escalation_audit_logs IS 'Comprehensive audit trail for compliance';
COMMENT ON TABLE escalation_levels IS 'Configuration for escalation levels and timeout rules';

COMMENT ON COLUMN escalations.status IS 'Current status: Pending, Acknowledged, ActionTaken, Escalated, Closed, Resolved, FinalEscalation';
COMMENT ON COLUMN escalations.next_escalation_time IS 'Deadline for manager action before auto-escalation';
COMMENT ON COLUMN escalations.document_uploaded IS 'Flag indicating if corrective action document was uploaded';

COMMENT ON VIEW v_active_escalations IS 'Active escalations with urgency status and time remaining';
COMMENT ON VIEW v_manager_escalation_stats IS 'Statistics per manager for performance monitoring';
COMMENT ON VIEW v_escalation_history_full IS 'Complete escalation history with employee and manager details';

COMMENT ON FUNCTION process_overdue_escalations() IS 'Automatically escalates overdue items to next level';
