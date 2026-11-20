# WACAMS 部署指南

本指南涵盖 WACAMS（Workforce Attendance & Corrective Action Management System）的完整部署流程。

## 目录

- [系统要求](#系统要求)
- [环境配置](#环境配置)
- [部署步骤](#部署步骤)
- [健康检查](#健康检查)
- [监控与日志](#监控与日志)
- [故障排除](#故障排除)
- [回滚流程](#回滚流程)

## 系统要求

### 最低硬件要求

| 组件 | 开发环境 | 生产环境 |
|------|---------|---------|
| CPU | 2 核 | 4 核+ |
| 内存 | 4 GB | 8 GB+ |
| 存储 | 20 GB | 50 GB+ |
| 网络 | 10 Mbps | 100 Mbps+ |

### 软件依赖

- **Docker**: 20.10+
- **Docker Compose**: 2.0+
- **操作系统**: Linux (Ubuntu 20.04+), Windows Server 2019+, macOS 12+

## 环境配置

### 1. 克隆代码库

```bash
git clone https://github.com/your-org/wacams.git
cd wacams
```

### 2. 配置环境变量

复制环境变量模板并编辑：

```bash
cp .env.example .env
```

编辑 `.env` 文件，配置以下关键参数：

#### 数据库配置
```bash
DB_HOST=postgres
DB_PORT=5432
DB_NAME=wacams_db
DB_USER=wacams_user
DB_PASSWORD=<生成强密码>
```

#### JWT 认证配置
```bash
JWT_SECRET=<生成至少32字符的随机密钥>
JWT_ISSUER=wacams
JWT_AUDIENCE=wacams-app
JWT_EXPIRATION_MINUTES=480
```

#### GIS 集成配置
```bash
GIS_BASE_URL=https://gis.yfpo.com:9105
GIS_USERNAME=<your_username>
GIS_PASSWORD=<your_password>
GIS_SYNC_INTERVAL_MINUTES=60
```

#### 邮件服务配置
```bash
SMTP_HOST=smtp.yourcompany.com
SMTP_PORT=587
SMTP_USERNAME=<your_email>
SMTP_PASSWORD=<your_password>
SMTP_FROM=noreply@yourcompany.com
```

### 3. 生成安全密钥

使用以下命令生成 JWT 密钥：

```bash
# Linux/macOS
openssl rand -base64 32

# Windows PowerShell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }) -as [byte[]])
```

## 部署步骤

### 快速部署（推荐）

#### Linux/macOS

```bash
chmod +x deploy.sh
./deploy.sh
```

#### Windows

```powershell
.\deploy.ps1
```

### 手动部署

#### 1. 启动基础设施

```bash
# 启动 PostgreSQL 数据库
docker-compose up -d postgres

# 等待数据库就绪
sleep 10

# 验证数据库连接
docker-compose exec postgres pg_isready -U wacams_user
```

#### 2. 构建并启动应用

```bash
# 构建 Docker 镜像
docker-compose build

# 启动所有服务
docker-compose up -d

# 查看服务状态
docker-compose ps
```

#### 3. 运行数据库迁移

```bash
# Linux/macOS
chmod +x migrate-db.sh
./migrate-db.sh migrate

# Windows
docker-compose exec backend dotnet ef database update --project Infrastructure --startup-project Api
```

#### 4. 初始化种子数据（可选）

```bash
# Linux/macOS
./migrate-db.sh seed

# Windows
docker-compose exec backend dotnet run --project Api -- --seed
```

### 验证部署

```bash
# 检查后端健康状态
curl http://localhost:5000/health

# 检查前端可访问性
curl http://localhost:3000

# 查看日志
docker-compose logs -f backend
docker-compose logs -f frontend
```

## 健康检查

### 端点说明

| 端点 | 描述 | 预期响应 |
|------|------|---------|
| `/health` | 综合健康检查 | 200 OK |
| `/health/db` | 数据库连接检查 | 200 OK / 503 Service Unavailable |
| `/health/api` | API 服务检查 | 200 OK / 503 Service Unavailable |
| `/health/ready` | 应用就绪检查 | 200 OK / 503 Service Unavailable |

### 使用健康检查

```bash
# 综合健康检查
curl http://localhost:5000/health

# 详细健康信息
curl http://localhost:5000/health | jq

# 数据库连接检查
curl http://localhost:5000/health/db
```

### Docker 健康检查

Docker Compose 配置了自动健康检查：

```yaml
healthcheck:
  test: ["CMD", "curl", "-f", "http://localhost:5000/health"]
  interval: 30s
  timeout: 10s
  retries: 3
  start_period: 40s
```

## 监控与日志

### 日志位置

- **后端日志**: `docker-compose logs backend`
- **前端日志**: `docker-compose logs frontend`
- **数据库日志**: `docker-compose logs postgres`

### 日志级别配置

在 `.env` 文件中配置：

```bash
LOG_LEVEL=Information  # 可选: Trace, Debug, Information, Warning, Error, Critical
```

### 结构化日志

后端使用 Serilog 输出结构化 JSON 日志：

```json
{
  "Timestamp": "2026-11-17T13:40:00.000Z",
  "Level": "Information",
  "Message": "User login successful",
  "Properties": {
    "UserId": "user123",
    "IpAddress": "192.168.1.1",
    "UserAgent": "Mozilla/5.0..."
  }
}
```

### 性能监控

启用 Application Insights（可选）：

```bash
APPLICATION_INSIGHTS_KEY=<your_instrumentation_key>
```

## 故障排除

### 常见问题

#### 1. 数据库连接失败

**症状**: 后端启动失败，日志显示 "Cannot connect to database"

**解决方案**:
```bash
# 检查数据库容器状态
docker-compose ps postgres

# 查看数据库日志
docker-compose logs postgres

# 重启数据库
docker-compose restart postgres

# 验证连接
docker-compose exec postgres psql -U wacams_user -d wacams_db -c "SELECT 1"
```

#### 2. 端口冲突

**症状**: "Port 5000 already in use"

**解决方案**:
```bash
# 查找占用端口的进程
# Linux/macOS
lsof -i :5000

# Windows
netstat -ano | findstr :5000

# 修改 docker-compose.yml 中的端口映射
ports:
  - "5001:5000"  # 改为其他可用端口
```

#### 3. 迁移失败

**症状**: "Migration failed" 错误

**解决方案**:
```bash
# 检查迁移状态
./migrate-db.sh status

# 回滚到上一个版本
./migrate-db.sh rollback

# 重新应用迁移
./migrate-db.sh migrate
```

#### 4. GIS 集成失败

**症状**: 员工数据同步失败

**解决方案**:
```bash
# 验证 GIS 连接
curl -k https://gis.yfpo.com:9105/api/health

# 检查 GIS 配置
docker-compose exec backend env | grep GIS

# 手动触发同步
curl -X POST http://localhost:5000/api/gis/sync \
  -H "Authorization: Bearer <token>"
```

#### 5. 内存不足

**症状**: 容器被 OOM killed

**解决方案**:
```bash
# 增加 Docker 内存限制
# 编辑 docker-compose.yml
services:
  backend:
    deploy:
      resources:
        limits:
          memory: 2G
        reservations:
          memory: 1G

# 重启服务
docker-compose restart backend
```

### 诊断命令

```bash
# 查看所有服务状态
docker-compose ps

# 查看实时日志
docker-compose logs -f

# 进入容器调试
docker-compose exec backend sh

# 检查资源使用
docker stats

# 清理未使用的资源
docker system prune -a
```

## 回滚流程

### 应用回滚

```bash
# 1. 停止当前服务
docker-compose down

# 2. 切换到上一个稳定版本
git checkout v1.0.0  # 替换为目标版本

# 3. 重新构建并启动
docker-compose build
docker-compose up -d

# 4. 验证服务状态
curl http://localhost:5000/health
```

### 数据库回滚

```bash
# 1. 恢复数据库备份
./backup-db.sh restore backups/wacams_backup_20261117_134000.sql.gz

# 2. 回滚迁移
./migrate-db.sh rollback

# 3. 验证数据完整性
docker-compose exec postgres psql -U wacams_user -d wacams_db -c "SELECT COUNT(*) FROM employees"
```

### 完整回滚脚本

```bash
#!/bin/bash
# rollback.sh

VERSION=$1

if [ -z "$VERSION" ]; then
  echo "Usage: ./rollback.sh <version>"
  exit 1
fi

echo "Rolling back to version $VERSION..."

# 停止服务
docker-compose down

# 切换代码版本
git checkout $VERSION

# 恢复数据库
LATEST_BACKUP=$(ls -t backups/*.sql.gz | head -1)
./backup-db.sh restore $LATEST_BACKUP

# 重建并启动
docker-compose build
docker-compose up -d

# 验证
sleep 10
curl http://localhost:5000/health

echo "Rollback complete!"
```

## 生产环境优化

### 1. 启用 HTTPS

在 `docker-compose.yml` 中配置 Nginx 反向代理：

```yaml
services:
  nginx:
    image: nginx:alpine
    ports:
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf
      - ./ssl:/etc/nginx/ssl
    depends_on:
      - backend
      - frontend
```

### 2. 配置自动备份

创建 cron 任务：

```bash
# 每天凌晨 2 点备份数据库
0 2 * * * /path/to/wacams/backup-db.sh backup
```

### 3. 启用日志轮转

在 `docker-compose.yml` 中配置：

```yaml
services:
  backend:
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"
```

### 4. 性能调优

```bash
# 增加数据库连接池
DB_POOL_SIZE=50

# 启用 Redis 缓存
REDIS_HOST=redis
REDIS_PORT=6379

# 配置 CDN（前端静态资源）
CDN_URL=https://cdn.yourcompany.com
```

## 安全加固

### 1. 更新默认密码

```bash
# 生成新密码
openssl rand -base64 16

# 更新 .env 文件中的密码
DB_PASSWORD=<new_password>
JWT_SECRET=<new_secret>
```

### 2. 限制数据库访问

```bash
# 仅允许内部网络访问
# docker-compose.yml
services:
  postgres:
    ports:
      - "127.0.0.1:5432:5432"  # 仅本地访问
```

### 3. 启用防火墙规则

```bash
# 仅开放必要端口
sudo ufw allow 443/tcp  # HTTPS
sudo ufw allow 22/tcp   # SSH
sudo ufw enable
```

## 升级指南

### 从 v1.x 升级到 v2.x

```bash
# 1. 备份当前数据
./backup-db.sh backup

# 2. 拉取新版本
git pull origin main

# 3. 更新环境变量
cp .env.example .env.new
# 手动合并 .env 和 .env.new

# 4. 运行迁移
./migrate-db.sh migrate

# 5. 重建并重启
docker-compose build
docker-compose up -d

# 6. 验证
curl http://localhost:5000/health
```

## 联系支持

- **技术支持**: support@yourcompany.com
- **文档**: https://docs.yourcompany.com/wacams
- **问题反馈**: https://github.com/your-org/wacams/issues

---

**最后更新**: 2026-11-17  
**版本**: 1.0.0
