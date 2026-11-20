#!/bin/bash
# =============================================================================
# WACAMS PostgreSQL Restore Script
# =============================================================================
# Usage: ./restore.sh <backup_file> [target_database]
# =============================================================================

set -euo pipefail

# Configuration
BACKUP_DIR="/backup/postgresql"

# Database credentials (from environment)
DB_HOST="${POSTGRES_HOST:-postgres}"
DB_PORT="${POSTGRES_PORT:-5432}"
DB_NAME="${1:-wacams}"
DB_USER="${POSTGRES_USER:-wacams}"
DB_PASSWORD="${POSTGRES_PASSWORD}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

log_info() { echo -e "${GREEN}[INFO]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1"; }
log_warn() { echo -e "${YELLOW}[WARN]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $(date '+%Y-%m-%d %H:%M:%S') - $1"; }

# Function to list available backups
list_backups() {
    log_info "Available backups:"
    echo ""
    echo "Daily backups:"
    ls -lh "$BACKUP_DIR/daily"/*.sql.gz 2>/dev/null || echo "  No daily backups found"
    echo ""
    echo "Weekly backups:"
    ls -lh "$BACKUP_DIR/weekly"/*.sql.gz 2>/dev/null || echo "  No weekly backups found"
    echo ""
    echo "Monthly backups:"
    ls -lh "$BACKUP_DIR/monthly"/*.sql.gz 2>/dev/null || echo "  No monthly backups found"
}

# Function to restore from backup
restore_backup() {
    local backup_file="$1"
    local target_db="${2:-$DB_NAME}"
    
    if [ ! -f "$backup_file" ]; then
        log_error "Backup file not found: $backup_file"
        exit 1
    fi
    
    log_info "=========================================="
    log_info "WACAMS Database Restore"
    log_info "=========================================="
    log_info "Backup file: $backup_file"
    log_info "Target database: $target_db"
    log_info "Target host: $DB_HOST:$DB_PORT"
    log_info "=========================================="
    
    # Confirmation prompt
    echo ""
    log_warn "WARNING: This will overwrite the target database!"
    read -p "Are you sure you want to continue? (yes/no): " confirm
    
    if [ "$confirm" != "yes" ]; then
        log_info "Restore cancelled by user"
        exit 0
    fi
    
    log_info "Starting restore process..."
    
    # Drop and recreate database
    log_info "Dropping existing database..."
    PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -c "DROP DATABASE IF EXISTS $target_db;" 2>> "$BACKUP_DIR/restore.log"
    
    log_info "Creating new database..."
    PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -c "CREATE DATABASE $target_db;" 2>> "$BACKUP_DIR/restore.log"
    
    # Restore backup
    log_info "Restoring backup..."
    if pg_restore \
        -h "$DB_HOST" \
        -p "$DB_PORT" \
        -U "$DB_USER" \
        -d "$target_db" \
        --verbose \
        --no-owner \
        --no-privileges \
        "$backup_file" 2>> "$BACKUP_DIR/restore.log"; then
        
        log_info "Restore completed successfully"
        
        # Verify restore
        log_info "Verifying restore..."
        local table_count=$(PGPASSWORD="$DB_PASSWORD" psql -h "$DB_HOST" -p "$DB_PORT" -U "$DB_USER" -d "$target_db" -t -c "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';" | xargs)
        log_info "Tables restored: $table_count"
        
        return 0
    else
        log_error "Restore failed"
        log_error "Check $BACKUP_DIR/restore.log for details"
        return 1
    fi
}

# Function to restore to point in time
restore_pitr() {
    local target_time="$1"
    local target_db="${2:-$DB_NAME}"
    
    log_info "Point-in-time restore to: $target_time"
    log_warn "PITR requires WAL archives and is not implemented in this script"
    log_warn "Please use Oracle Cloud Infrastructure backup restore instead"
    exit 1
}

# Main execution
main() {
    local action="${1:-list}"
    
    case "$action" in
        list)
            list_backups
            ;;
        restore)
            if [ -z "${2:-}" ]; then
                log_error "Backup file not specified"
                echo "Usage: $0 restore <backup_file> [target_database]"
                exit 1
            fi
            restore_backup "$2" "${3:-$DB_NAME}"
            ;;
        pitr)
            if [ -z "${2:-}" ]; then
                log_error "Target time not specified"
                echo "Usage: $0 pitr <timestamp> [target_database]"
                exit 1
            fi
            restore_pitr "$2" "${3:-$DB_NAME}"
            ;;
        *)
            log_error "Unknown action: $action"
            echo "Usage: $0 [list|restore|pitr]"
            echo ""
            echo "Commands:"
            echo "  list                          - List available backups"
            echo "  restore <file> [database]     - Restore from backup file"
            echo "  pitr <timestamp> [database]   - Point-in-time restore"
            exit 1
            ;;
    esac
}

# Run main function
main "$@"
