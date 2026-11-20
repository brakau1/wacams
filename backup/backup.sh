#!/bin/bash
# =============================================================================
# WACAMS PostgreSQL Backup Script
# =============================================================================
# Usage: ./backup.sh [full|incremental|schema]
# =============================================================================

set -euo pipefail

# Configuration
BACKUP_DIR="/backup/postgresql"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
RETENTION_DAYS=30
RETENTION_WEEKS=12
RETENTION_MONTHS=12

# Database credentials (from environment)
DB_HOST="${POSTGRES_HOST:-postgres}"
DB_PORT="${POSTGRES_PORT:-5432}"
DB_NAME="${POSTGRES_DB:-wacams}"
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

# Create backup directory structure
mkdir -p "$BACKUP_DIR/daily"
mkdir -p "$BACKUP_DIR/weekly"
mkdir -p "$BACKUP_DIR/monthly"

# Function to perform full backup
full_backup() {
    local backup_file="$BACKUP_DIR/daily/wacams_full_${TIMESTAMP}.sql.gz"
    
    log_info "Starting full database backup..."
    log_info "Database: $DB_NAME @ $DB_HOST:$DB_PORT"
    log_info "Backup file: $backup_file"
    
    # Perform backup with compression
    if PGPASSWORD="$DB_PASSWORD" pg_dump \
        -h "$DB_HOST" \
        -p "$DB_PORT" \
        -U "$DB_USER" \
        -d "$DB_NAME" \
        --format=custom \
        --compress=9 \
        --verbose \
        --file="$backup_file" 2>> "$BACKUP_DIR/backup.log"; then
        
        local size=$(du -h "$backup_file" | cut -f1)
        log_info "Full backup completed successfully"
        log_info "Backup size: $size"
        
        # Verify backup integrity
        if pg_restore --list "$backup_file" > /dev/null 2>&1; then
            log_info "Backup verification: PASSED"
        else
            log_error "Backup verification: FAILED"
            return 1
        fi
        
        return 0
    else
        log_error "Full backup failed"
        return 1
    fi
}

# Function to perform schema-only backup
schema_backup() {
    local backup_file="$BACKUP_DIR/daily/wacams_schema_${TIMESTAMP}.sql"
    
    log_info "Starting schema-only backup..."
    
    if PGPASSWORD="$DB_PASSWORD" pg_dump \
        -h "$DB_HOST" \
        -p "$DB_PORT" \
        -U "$DB_USER" \
        -d "$DB_NAME" \
        --schema-only \
        --file="$backup_file" 2>> "$BACKUP_DIR/backup.log"; then
        
        log_info "Schema backup completed: $backup_file"
        return 0
    else
        log_error "Schema backup failed"
        return 1
    fi
}

# Function to cleanup old backups
cleanup_old_backups() {
    log_info "Cleaning up old backups..."
    
    # Remove daily backups older than RETENTION_DAYS
    local deleted_count=$(find "$BACKUP_DIR/daily" -name "wacams_full_*.sql.gz" -mtime +$RETENTION_DAYS -delete -print | wc -l)
    log_info "Deleted $deleted_count daily backups older than $RETENTION_DAYS days"
    
    # Remove weekly backups older than RETENTION_WEEKS
    deleted_count=$(find "$BACKUP_DIR/weekly" -name "wacams_full_*.sql.gz" -mtime +$((RETENTION_WEEKS * 7)) -delete -print | wc -l)
    log_info "Deleted $deleted_count weekly backups older than $RETENTION_WEEKS weeks"
    
    # Remove monthly backups older than RETENTION_MONTHS
    deleted_count=$(find "$BACKUP_DIR/monthly" -name "wacams_full_*.sql.gz" -mtime +$((RETENTION_MONTHS * 30)) -delete -print | wc -l)
    log_info "Deleted $deleted_count monthly backups older than $RETENTION_MONTHS months"
}

# Function to move backup to weekly/monthly
rotate_backups() {
    local day_of_week=$(date +%u)
    local day_of_month=$(date +%d)
    local latest_backup=$(ls -t "$BACKUP_DIR/daily"/wacams_full_*.sql.gz 2>/dev/null | head -n1)
    
    if [ -z "$latest_backup" ]; then
        log_warn "No backup found for rotation"
        return 0
    fi
    
    # Weekly backup (Sunday)
    if [ "$day_of_week" -eq 7 ]; then
        local weekly_backup="$BACKUP_DIR/weekly/$(basename "$latest_backup")"
        cp "$latest_backup" "$weekly_backup"
        log_info "Created weekly backup: $weekly_backup"
    fi
    
    # Monthly backup (1st of month)
    if [ "$day_of_month" -eq 1 ]; then
        local monthly_backup="$BACKUP_DIR/monthly/$(basename "$latest_backup")"
        cp "$latest_backup" "$monthly_backup"
        log_info "Created monthly backup: $monthly_backup"
    fi
}

# Main execution
main() {
    local backup_type="${1:-full}"
    
    log_info "=========================================="
    log_info "WACAMS Database Backup"
    log_info "Backup type: $backup_type"
    log_info "=========================================="
    
    case "$backup_type" in
        full)
            full_backup
            rotate_backups
            cleanup_old_backups
            ;;
        schema)
            schema_backup
            ;;
        *)
            log_error "Unknown backup type: $backup_type"
            echo "Usage: $0 [full|schema]"
            exit 1
            ;;
    esac
    
    log_info "=========================================="
    log_info "Backup process completed"
    log_info "=========================================="
}

# Run main function
main "$@"
