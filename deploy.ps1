# =============================================================================
# WACAMS Deployment Script (Windows PowerShell)
# =============================================================================
# Usage: .\deploy.ps1 -Environment [development|staging|production] -Command [build|deploy|rollback|status|logs]

param(
    [Parameter(Mandatory=$false)]
    [ValidateSet('development', 'staging', 'production')]
    [string]$Environment = 'development',
    
    [Parameter(Mandatory=$false)]
    [ValidateSet('build', 'deploy', 'rollback', 'status', 'logs', 'cleanup')]
    [string]$Command = 'deploy'
)

$ErrorActionPreference = 'Stop'

# Configuration
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$BackupDir = Join-Path $ScriptDir 'backups'
$LogDir = Join-Path $ScriptDir 'logs'
$LogFile = Join-Path $LogDir "deploy-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"

# Ensure directories exist
if (-not (Test-Path $LogDir)) {
    New-Item -ItemType Directory -Path $LogDir | Out-Null
}

# Functions
function Write-LogInfo {
    param([string]$Message)
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $logMessage = "[$timestamp] [INFO] $Message"
    Write-Host $logMessage -ForegroundColor Blue
    Add-Content -Path $LogFile -Value $logMessage
}

function Write-LogSuccess {
    param([string]$Message)
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $logMessage = "[$timestamp] [SUCCESS] $Message"
    Write-Host $logMessage -ForegroundColor Green
    Add-Content -Path $LogFile -Value $logMessage
}

function Write-LogWarning {
    param([string]$Message)
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $logMessage = "[$timestamp] [WARNING] $Message"
    Write-Host $logMessage -ForegroundColor Yellow
    Add-Content -Path $LogFile -Value $logMessage
}

function Write-LogError {
    param([string]$Message)
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $logMessage = "[$timestamp] [ERROR] $Message"
    Write-Host $logMessage -ForegroundColor Red
    Add-Content -Path $LogFile -Value $logMessage
}

function Test-Prerequisites {
    Write-LogInfo 'Checking prerequisites...'
    
    # Check Docker
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        Write-LogError 'Docker is not installed. Please install Docker Desktop first.'
        exit 1
    }
    
    # Check Docker Compose
    if (-not (Get-Command docker-compose -ErrorAction SilentlyContinue)) {
        Write-LogError 'Docker Compose is not installed.'
        exit 1
    }
    
    # Check .env file
    $envFile = Join-Path $ScriptDir '.env'
    if (-not (Test-Path $envFile)) {
        Write-LogWarning '.env file not found. Copying from .env.example...'
        Copy-Item -Path '.env.example' -Destination '.env'
        Write-LogWarning 'Please update .env file with your configuration before proceeding.'
        exit 1
    }
    
    Write-LogSuccess 'All prerequisites met'
}

function Backup-Database {
    Write-LogInfo 'Creating database backup...'
    
    if (-not (Test-Path $BackupDir)) {
        New-Item -ItemType Directory -Path $BackupDir | Out-Null
    }
    
    $backupFile = Join-Path $BackupDir "wacam-db-$(Get-Date -Format 'yyyyMMdd-HHmmss').sql"
    
    try {
        docker-compose exec -T postgres pg_dump -U wacam_user wacam_db | Out-File -FilePath $backupFile -Encoding UTF8
        
        Write-LogSuccess "Database backup created: $backupFile"
        
        # Compress backup
        $gzipFile = "$backupFile.gz"
        Compress-Archive -Path $backupFile -DestinationPath $gzipFile -Force
        Remove-Item $backupFile
        
        Write-LogSuccess "Backup compressed: $gzipFile"
    }
    catch {
        Write-LogError "Database backup failed: $_"
        exit 1
    }
}

function Build-Images {
    Write-LogInfo "Building Docker images for $Environment environment..."
    
    try {
        docker-compose build --no-cache
        
        if ($LASTEXITCODE -eq 0) {
            Write-LogSuccess 'Docker images built successfully'
        }
        else {
            Write-LogError 'Failed to build Docker images'
            exit 1
        }
    }
    catch {
        Write-LogError "Build failed: $_"
        exit 1
    }
}

function Invoke-Migrations {
    Write-LogInfo 'Running database migrations...'
    
    # Wait for database to be ready
    Write-LogInfo 'Waiting for database to be ready...'
    $maxAttempts = 30
    $attempt = 0
    
    while ($attempt -lt $maxAttempts) {
        $result = docker-compose exec -T postgres pg_isready -U wacam_user -d wacam_db 2>&1
        if ($LASTEXITCODE -eq 0) {
            break
        }
        Start-Sleep -Seconds 2
        $attempt++
    }
    
    if ($attempt -ge $maxAttempts) {
        Write-LogError 'Database failed to become ready'
        exit 1
    }
    
    # Run migrations
    try {
        docker-compose exec -T backend dotnet WACAMS.Api.dll --migrate
        
        if ($LASTEXITCODE -eq 0) {
            Write-LogSuccess 'Database migrations completed'
        }
        else {
            Write-LogError 'Database migrations failed'
            exit 1
        }
    }
    catch {
        Write-LogError "Migration failed: $_"
        exit 1
    }
}

function Deploy-Application {
    Write-LogInfo "Starting deployment for $Environment environment..."
    
    # Check prerequisites
    Test-Prerequisites
    
    # Backup database
    Backup-Database
    
    # Build images
    Build-Images
    
    # Stop existing containers
    Write-LogInfo 'Stopping existing containers...'
    docker-compose down
    
    # Start new containers
    Write-LogInfo 'Starting new containers...'
    docker-compose up -d
    
    # Wait for services to be healthy
    Write-LogInfo 'Waiting for services to start...'
    Start-Sleep -Seconds 15
    
    # Run migrations
    Invoke-Migrations
    
    # Health check
    Write-LogInfo 'Performing health check...'
    
    try {
        $backendHealth = Invoke-WebRequest -Uri 'http://localhost:8080/api/health' -UseBasicParsing -TimeoutSec 10
        if ($backendHealth.StatusCode -eq 200) {
            Write-LogSuccess 'Backend health check passed'
        }
        else {
            Write-LogError 'Backend health check failed'
            exit 1
        }
    }
    catch {
        Write-LogError "Backend health check failed: $_"
        exit 1
    }
    
    try {
        $frontendHealth = Invoke-WebRequest -Uri 'http://localhost:80' -UseBasicParsing -TimeoutSec 10
        if ($frontendHealth.StatusCode -eq 200) {
            Write-LogSuccess 'Frontend health check passed'
        }
        else {
            Write-LogError 'Frontend health check failed'
            exit 1
        }
    }
    catch {
        Write-LogError "Frontend health check failed: $_"
        exit 1
    }
    
    Write-LogSuccess 'Deployment completed successfully!'
    Write-LogInfo 'Application is available at:'
    Write-LogInfo '  Frontend: http://localhost:80'
    Write-LogInfo '  Backend API: http://localhost:8080'
    Write-LogInfo '  Adminer (DB): http://localhost:8081'
}

function Invoke-Rollback {
    Write-LogWarning 'Rolling back to previous version...'
    
    # Stop current containers
    docker-compose down
    
    # Find latest backup
    if (-not (Test-Path $BackupDir)) {
        Write-LogError 'No backup directory found'
        exit 1
    }
    
    $latestBackup = Get-ChildItem -Path $BackupDir -Filter '*.gz' | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    
    if (-not $latestBackup) {
        Write-LogError 'No backup found for rollback'
        exit 1
    }
    
    Write-LogInfo "Using backup: $($latestBackup.FullName)"
    
    # Restore database
    Write-LogInfo 'Restoring database from backup...'
    
    try {
        # Decompress and restore
        $tempFile = Join-Path $BackupDir 'temp-restore.sql'
        Expand-Archive -Path $latestBackup.FullName -DestinationPath $BackupDir -Force
        $sqlFile = Get-ChildItem -Path $BackupDir -Filter '*.sql' | Select-Object -First 1
        
        Get-Content $sqlFile.FullName | docker-compose exec -T postgres psql -U wacam_user wacam_db
        
        Remove-Item $sqlFile.FullName -Force
        
        # Start containers with previous images
        docker-compose up -d
        
        Write-LogSuccess 'Rollback completed'
    }
    catch {
        Write-LogError "Rollback failed: $_"
        exit 1
    }
}

function Show-Status {
    Write-LogInfo 'Checking service status...'
    docker-compose ps
    
    Write-Host ''
    Write-LogInfo 'Container logs (last 20 lines):'
    docker-compose logs --tail=20
}

function Show-Logs {
    Write-LogInfo 'Showing logs (press Ctrl+C to exit)...'
    docker-compose logs -f
}

function Invoke-Cleanup {
    Write-LogWarning 'Cleaning up old resources...'
    
    # Remove unused Docker images
    docker image prune -f
    
    # Remove old backups (keep last 10)
    if (Test-Path $BackupDir) {
        $backups = Get-ChildItem -Path $BackupDir -Filter '*.gz' | Sort-Object LastWriteTime -Descending
        if ($backups.Count -gt 10) {
            $backups | Select-Object -Skip 10 | Remove-Item -Force
            Write-LogSuccess 'Old backups cleaned up'
        }
    }
    
    Write-LogSuccess 'Cleanup completed'
}

# Main execution
switch ($Command) {
    'build' {
        Test-Prerequisites
        Build-Images
    }
    'deploy' {
        Deploy-Application
    }
    'rollback' {
        Invoke-Rollback
    }
    'status' {
        Show-Status
    }
    'logs' {
        Show-Logs
    }
    'cleanup' {
        Invoke-Cleanup
    }
    default {
        Write-Host 'Usage: .\deploy.ps1 -Environment [development|staging|production] -Command [build|deploy|rollback|status|logs|cleanup]'
        exit 1
    }
}
