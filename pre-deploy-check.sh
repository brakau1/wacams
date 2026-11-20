#!/bin/bash

# =============================================================================
# WACAMS 部署前检查脚本
# =============================================================================
# 用途: 在部署前验证所有依赖和配置是否正确
# 用法: ./pre-deploy-check.sh [environment]

set -euo pipefail

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

ENVIRONMENT="${1:-development}"
ERRORS=0
WARNINGS=0

log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[✓]${NC} $1"; }
log_warning() { echo -e "${YELLOW}[!]${NC} $1"; WARNINGS=$((WARNINGS + 1)); }
log_error() { echo -e "${RED}[✗]${NC} $1"; ERRORS=$((ERRORS + 1)); }

echo "=========================================="
echo "WACAMS 部署前检查 - $ENVIRONMENT"
echo "=========================================="
echo ""

# 1. 检查 Docker
log_info "检查 Docker..."
if command -v docker &> /dev/null; then
    DOCKER_VERSION=$(docker --version | cut -d' ' -f3 | sed 's/,//')
    log_success "Docker 已安装 (版本: $DOCKER_VERSION)"
else
    log_error "Docker 未安装"
fi

# 2. 检查 Docker Compose
log_info "检查 Docker Compose..."
if command -v docker-compose &> /dev/null; then
    COMPOSE_VERSION=$(docker-compose --version | cut -d' ' -f3 | sed 's/,//')
    log_success "Docker Compose 已安装 (版本: $COMPOSE_VERSION)"
else
    log_error "Docker Compose 未安装"
fi

# 3. 检查 .env 文件
log_info "检查环境配置..."
if [ -f ".env" ]; then
    log_success ".env 文件存在"
    
    # 检查关键环境变量
    if grep -q "DB_PASSWORD" .env && ! grep -q "DB_PASSWORD=ChangeThis" .env; then
        log_success "数据库密码已配置"
    else
        log_error "数据库密码未配置或使用默认值"
    fi
    
    if grep -q "JWT_KEY" .env && ! grep -q "JWT_KEY=Generate-A" .env; then
        log_success "JWT 密钥已配置"
    else
        log_error "JWT 密钥未配置或使用默认值"
    fi
    
    if grep -q "GIS__Username" .env && ! grep -q "GIS__Username=sntseke1" .env; then
        log_success "GIS 集成已配置"
    else
        log_warning "GIS 集成使用默认凭据"
    fi
else
    log_error ".env 文件不存在"
fi

# 4. 检查 Docker 磁盘空间
log_info "检查 Docker 磁盘空间..."
DOCKER_DISK=$(docker system df --format '{{.Size}}' | head -1)
log_success "Docker 磁盘使用: $DOCKER_DISK"

# 5. 检查端口可用性
log_info "检查端口可用性..."
for port in 80 8080 8081 5432; do
    if ! netstat -tuln 2>/dev/null | grep -q ":$port "; then
        log_success "端口 $port 可用"
    else
        log_warning "端口 $port 已被占用"
    fi
done

# 6. 检查数据库备份目录
log_info "检查备份目录..."
if [ -d "backups" ]; then
    BACKUP_COUNT=$(ls -1 backups/*.gz 2>/dev/null | wc -l)
    log_success "备份目录存在 ($BACKUP_COUNT 个备份)"
else
    log_warning "备份目录不存在"
fi

# 7. 检查日志目录
log_info "检查日志目录..."
if [ -d "logs" ]; then
    log_success "日志目录存在"
else
    log_warning "日志目录不存在"
fi

# 8. 检查数据库迁移文件
log_info "检查数据库迁移..."
if [ -f "database/schema.sql" ]; then
    log_success "数据库 schema 文件存在"
else
    log_error "数据库 schema 文件缺失"
fi

# 9. 检查 Docker Compose 配置
log_info "检查 Docker Compose 配置..."
if [ -f "docker-compose.yml" ]; then
    if docker-compose config > /dev/null 2>&1; then
        log_success "docker-compose.yml 配置有效"
    else
        log_error "docker-compose.yml 配置无效"
    fi
else
    log_error "docker-compose.yml 文件不存在"
fi

# 10. 检查 Dockerfile
log_info "检查 Dockerfile..."
if [ -f "Dockerfile.backend" ] && [ -f "Dockerfile.frontend" ]; then
    log_success "Dockerfile 文件存在"
else
    log_error "Dockerfile 文件缺失"
fi

# 11. 检查 .gitignore
log_info "检查 .gitignore..."
if [ -f ".gitignore" ]; then
    if grep -q ".env" .gitignore; then
        log_success ".gitignore 包含 .env"
    else
        log_warning ".gitignore 未包含 .env"
    fi
else
    log_warning ".gitignore 文件不存在"
fi

# 12. 检查内存
log_info "检查系统内存..."
TOTAL_MEM=$(free -h 2>/dev/null | awk '/^Mem:/{print $2}' || echo "Unknown")
log_success "系统总内存: $TOTAL_MEM"

# 13. 检查 CPU
log_info "检查 CPU..."
CPU_CORES=$(nproc 2>/dev/null || echo "Unknown")
log_success "CPU 核心数: $CPU_CORES"

# 14. 检查磁盘空间
log_info "检查磁盘空间..."
DISK_SPACE=$(df -h . | awk 'NR==2{print $4}' || echo "Unknown")
log_success "可用磁盘空间: $DISK_SPACE"

# 15. 检查网络连通性
log_info "检查网络连通性..."
if ping -c 1 docker.io > /dev/null 2>&1; then
    log_success "Docker Hub 可访问"
else
    log_warning "Docker Hub 不可访问"
fi

# 总结
echo ""
echo "=========================================="
echo "检查完成"
echo "=========================================="
echo ""

if [ $ERRORS -eq 0 ]; then
    log_success "所有关键检查通过"
else
    log_error "发现 $ERRORS 个错误"
fi

if [ $WARNINGS -gt 0 ]; then
    log_warning "发现 $WARNINGS 个警告"
fi

echo ""

if [ $ERRORS -gt 0 ]; then
    log_error "请修复上述错误后再进行部署"
    exit 1
else
    log_success "可以安全进行部署"
    exit 0
fi
