#!/bin/bash

# =============================================================================
# WACAMS Database Backup Script
# =============================================================================
# Usage: ./backup-db.sh [backup|restore] [filename]

set -euo pipefail

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Configuration
DB_CONTAINER="wacam-postgres"
BACKUP_DIR="./backups"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="$BACKUP_DIR/wacam_backup_$TIMESTAMP.sql"

# Functions
backup_database() {
    log_info "Starting database backup..."
    
    # Create backup directory if it doesn't exist
    mkdir -p "$BACKUP_DIR"
    
    # Check if database container is running
    if ! docker-compose ps | grep -q "$DB_CONTAINER"; then
        log_error "Database container is not running. Start it first with: docker-compose up -d postgres"
        exit 1
    fi
    
    # Perform backup
    docker-compose exec -T $DB_CONTAINER pg_dump -U wacam_user wacam_db > "$BACKUP_FILE"
    
    if [ $? -eq 0 ]; then
        log_success "Backup created: $BACKUP_FILE"
        
        # Compress backup
        gzip "$BACKUP_FILE"
        log_success "Backup compressed: ${BACKUP_FILE}.gz"
        
        # Show file size
        SIZE=$(du -h "${BACKUP_FILE}.gz" | cut -f1)
        log_info "Backup size: $SIZE"
    else
        log_error "Backup failed"
        exit 1
    fi
}

restore_database() {
    local backup_file="$1"
    
    if [ -z "$backup_file" ]; then
        # Find most recent backup
        backup_file=$(ls -t "$BACKUP_DIR"/*.sql.gz 2>/dev/null | head -n1)
        if [ -z "$backup_file" ]; then
            log_error "No backup files found in $BACKUP_DIR"
            exit 1
        fi
        log_info "Using most recent backup: $backup_file"
    fi
    
    if [ ! -f "$backup_file" ]; then
        log_error "Backup file not found: $backup_file"
        exit 1
    fi
    
    log_info "Starting database restore from: $backup_file"
    
    # Confirm restore
    echo -e "${YELLOW}WARNING: This will overwrite the current database. Continue? (y/N)${NC}"
    read -r response
    if [[ ! "$response" =~ ^[Yy]$ ]]; then
        log_info "Restore cancelled"
        exit 0
    fi
    
    # Check if database container is running
    if ! docker-compose ps | grep -q "$DB_CONTAINER"; then
        log_error "Database container is not running"
        exit 1
    fi
    
    # Decompress if needed
    if [[ "$backup_file" == *.gz ]]; then
        gunzip -c "$backup_file" | docker-compose exec -T $DB_CONTAINER psql -U wacam_user -d wacam_db
    else
        docker-compose exec -T $DB_CONTAINER psql -U wacam_user -d wacam_db < "$backup_file"
    fi
    
    if [ $? -eq 0 ]; then
        log_success "Database restored successfully"
    else
        log_error "Restore failed"
        exit 1
    fi
}

list_backups() {
    log_info "Available backups:"
    
    if [ ! -d "$BACKUP_DIR" ] || [ -z "$(ls -A "$BACKUP_DIR" 2>/dev/null)" ]; then
        log_info "No backups found in $BACKUP_DIR"
        exit 0
    fi
    
    ls -lh "$BACKUP_DIR"/*.sql.gz 2>/dev/null || ls -lh "$BACKUP_DIR"/*.sql 2>/dev/null
}

# Main execution
case "${1:-backup}" in
    backup)
        backup_database
        ;;
    restore)
        restore_database "$2"
        ;;
    list)
        list_backups
        ;;
    *)
        echo "Usage: $0 [backup|restore|list] [filename]"
        exit 1
        ;;
esac
