#!/bin/bash

# =============================================================================
# WACAMS Deployment Script (Linux/Mac)
# =============================================================================
# Usage: ./deploy.sh [environment] [command]
# Environments: development, staging, production
# Commands: build, deploy, rollback, status, logs

set -euo pipefail

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENVIRONMENT="${1:-development}"
COMMAND="${2:-deploy}"
COMPOSE_FILE="docker-compose.yml"
BACKUP_DIR="./backups"
LOG_FILE="./logs/deploy-$(date +%Y%m%d-%H%M%S).log"

# Functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1" | tee -a "$LOG_FILE"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1" | tee -a "$LOG_FILE"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1" | tee -a "$LOG_FILE"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1" | tee -a "$LOG_FILE"
}

check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check Docker
    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed. Please install Docker first."
        exit 1
    fi
    
    # Check Docker Compose
    if ! command -v docker-compose &> /dev/null; then
        log_error "Docker Compose is not installed. Please install Docker Compose first."
        exit 1
    fi
    
    # Check .env file
    if [ ! -f ".env" ]; then
        log_warning ".env file not found. Copying from .env.example..."
        cp .env.example .env
        log_warning "Please update .env file with your configuration before proceeding."
        exit 1
    fi
    
    log_success "All prerequisites met"
}

backup_database() {
    log_info "Creating database backup..."
    
    mkdir -p "$BACKUP_DIR"
    BACKUP_FILE="$BACKUP_DIR/wacam-db-$(date +%Y%m%d-%H%M%S).sql"
    
    docker-compose exec -T postgres pg_dump -U wacam_user wacam_db > "$BACKUP_FILE"
    
    if [ $? -eq 0 ]; then
        log_success "Database backup created: $BACKUP_FILE"
        # Compress backup
        gzip "$BACKUP_FILE"
        log_success "Backup compressed: ${BACKUP_FILE}.gz"
    else
        log_error "Database backup failed"
        exit 1
    fi
}

build_images() {
    log_info "Building Docker images for $ENVIRONMENT environment..."
    
    docker-compose -f "$COMPOSE_FILE" build --no-cache
    
    if [ $? -eq 0 ]; then
        log_success "Docker images built successfully"
    else
        log_error "Failed to build Docker images"
        exit 1
    fi
}

run_migrations() {
    log_info "Running database migrations..."
    
    # Wait for database to be ready
    log_info "Waiting for database to be ready..."
    until docker-compose exec -T postgres pg_isready -U wacam_user -d wacam_db > /dev/null 2>&1; do
        sleep 2
    done
    
    # Run migrations
    docker-compose exec -T backend dotnet WACAMS.Api.dll --migrate
    
    if [ $? -eq 0 ]; then
        log_success "Database migrations completed"
    else
        log_error "Database migrations failed"
        exit 1
    fi
}

deploy() {
    log_info "Starting deployment for $ENVIRONMENT environment..."
    
    # Create logs directory
    mkdir -p logs
    
    # Check prerequisites
    check_prerequisites
    
    # Backup database
    backup_database
    
    # Build images
    build_images
    
    # Stop existing containers
    log_info "Stopping existing containers..."
    docker-compose down
    
    # Start new containers
    log_info "Starting new containers..."
    docker-compose up -d
    
    # Wait for services to be healthy
    log_info "Waiting for services to start..."
    sleep 10
    
    # Run migrations
    run_migrations
    
    # Health check
    log_info "Performing health check..."
    if curl -f http://localhost:8080/api/health > /dev/null 2>&1; then
        log_success "Backend health check passed"
    else
        log_error "Backend health check failed"
        exit 1
    fi
    
    if curl -f http://localhost:80 > /dev/null 2>&1; then
        log_success "Frontend health check passed"
    else
        log_error "Frontend health check failed"
        exit 1
    fi
    
    log_success "Deployment completed successfully!"
    log_info "Application is available at:"
    log_info "  Frontend: http://localhost:80"
    log_info "  Backend API: http://localhost:8080"
    log_info "  Adminer (DB): http://localhost:8081"
}

rollback() {
    log_warning "Rolling back to previous version..."
    
    # Stop current containers
    docker-compose down
    
    # Find latest backup
    LATEST_BACKUP=$(ls -t "$BACKUP_DIR"/*.gz 2>/dev/null | head -n1)
    
    if [ -z "$LATEST_BACKUP" ]; then
        log_error "No backup found for rollback"
        exit 1
    fi
    
    log_info "Using backup: $LATEST_BACKUP"
    
    # Restore database
    log_info "Restoring database from backup..."
    gunzip -c "$LATEST_BACKUP" | docker-compose exec -T postgres psql -U wacam_user wacam_db
    
    # Start containers with previous images
    docker-compose up -d
    
    log_success "Rollback completed"
}

status() {
    log_info "Checking service status..."
    docker-compose ps
    
    echo ""
    log_info "Container logs (last 20 lines):"
    docker-compose logs --tail=20
}

logs() {
    log_info "Showing logs (press Ctrl+C to exit)..."
    docker-compose logs -f
}

cleanup() {
    log_warning "Cleaning up old resources..."
    
    # Remove unused Docker images
    docker image prune -f
    
    # Remove old backups (keep last 10)
    if [ -d "$BACKUP_DIR" ]; then
        cd "$BACKUP_DIR"
        ls -t | tail -n +11 | xargs -r rm --
        cd "$SCRIPT_DIR"
        log_success "Old backups cleaned up"
    fi
    
    log_success "Cleanup completed"
}

# Main execution
mkdir -p logs

case "$COMMAND" in
    build)
        check_prerequisites
        build_images
        ;;
    deploy)
        deploy
        ;;
    rollback)
        rollback
        ;;
    status)
        status
        ;;
    logs)
        logs
        ;;
    cleanup)
        cleanup
        ;;
    *)
        echo "Usage: $0 [environment] [command]"
        echo "Environments: development, staging, production"
        echo "Commands: build, deploy, rollback, status, logs, cleanup"
        exit 1
        ;;
esac
