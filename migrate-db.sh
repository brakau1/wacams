#!/bin/bash

# =============================================================================
# WACAMS Database Migration Script
# =============================================================================
# Usage: ./migrate-db.sh [command]
# Commands: migrate, rollback, status, seed

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
BACKEND_CONTAINER="wacam-backend"
DB_CONTAINER="wacam-postgres"

# Functions
run_migrations() {
    log_info "Running database migrations..."
    
    # Check if backend container is running
    if ! docker-compose ps | grep -q "$BACKEND_CONTAINER"; then
        log_error "Backend container is not running. Start it first with: docker-compose up -d backend"
        exit 1
    fi
    
    # Run migrations using dotnet ef
    docker-compose exec -T $BACKEND_CONTAINER dotnet ef database update --project Infrastructure/WACAMS.Infrastructure.csproj --startup-project Api/WACAMS.Api.csproj
    
    if [ $? -eq 0 ]; then
        log_success "Migrations completed successfully"
    else
        log_error "Migration failed"
        exit 1
    fi
}

rollback_migration() {
    log_info "Rolling back last migration..."
    
    docker-compose exec -T $BACKEND_CONTAINER dotnet ef database update --project Infrastructure/WACAMS.Infrastructure.csproj --startup-project Api/WACAMS.Api.csproj MigrationBeforeLast
    
    if [ $? -eq 0 ]; then
        log_success "Rollback completed successfully"
    else
        log_error "Rollback failed"
        exit 1
    fi
}

show_migration_status() {
    log_info "Checking migration status..."
    
    docker-compose exec -T $BACKEND_CONTAINER dotnet ef migrations list --project Infrastructure/WACAMS.Infrastructure.csproj --startup-project Api/WACAMS.Api.csproj
    
    if [ $? -eq 0 ]; then
        log_success "Migration status retrieved"
    else
        log_error "Failed to get migration status"
        exit 1
    fi
}

seed_database() {
    log_info "Seeding database with initial data..."
    
    # Check if seed data file exists
    if [ ! -f "database/seed-data.sql" ]; then
        log_error "Seed data file not found: database/seed-data.sql"
        exit 1
    fi
    
    # Execute seed data
    docker-compose exec -T $DB_CONTAINER psql -U wacam_user -d wacam_db < database/seed-data.sql
    
    if [ $? -eq 0 ]; then
        log_success "Database seeded successfully"
    else
        log_error "Seeding failed"
        exit 1
    fi
}

# Main execution
case "${1:-migrate}" in
    migrate)
        run_migrations
        ;;
    rollback)
        rollback_migration
        ;;
    status)
        show_migration_status
        ;;
    seed)
        seed_database
        ;;
    *)
        echo "Usage: $0 [migrate|rollback|status|seed]"
        exit 1
        ;;
esac
