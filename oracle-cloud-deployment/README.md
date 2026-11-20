# WACAMS Oracle Cloud Deployment Package

**日期**: 2026-11-17  
**状态**: 生产就绪  
**目标环境**: Oracle Cloud Infrastructure (OCI)

---

## 目录结构

```
oracle-cloud-deployment/
├── docker-compose.yml                    # 生产级容器编排
├── docker-compose.override.yml           # 开发环境覆盖
├── nginx/
│   ├── nginx.conf                        # Nginx 主配置
│   ├── conf.d/
│   │   ├── upstream.conf                 # 上游服务器配置
│   │   ├── ssl.conf                      # SSL 配置
│   │   ├── security.conf                 # 安全头配置
│   │   └── rate-limiting.conf            # 限流配置
│   └── ssl/
│       └── README.md                     # SSL 证书说明
├── postgres/
│   ├── postgresql.conf                   # PostgreSQL 优化配置
│   ├── pg_hba.conf                       # 访问控制
│   └── init-scripts/
│       ├── 01-schema.sql                 # 数据库 schema
│       ├── 02-indexes.sql                # 索引优化
│       └── 03-seed.sql                   # 初始数据
├── monitoring/
│   ├── prometheus/
│   │   ├── prometheus.yml                # Prometheus 配置
│   │   └── alert-rules.yml               # 告警规则
│   ├── grafana/
│   │   ├── dashboards/
│   │   │   ├── wacams-overview.json      # 总览仪表板
│   │   │   ├── database.json             # 数据库仪表板
│   │   │   └── api-performance.json      # API 性能仪表板
│   │   └── provisioning/
│   │       ├── dashboards.yml
│   │       └── datasources.yml
│   └── alertmanager/
│       └── alertmanager.yml              # 告警管理配置
├── backup/
│   ├── backup.sh                         # 备份脚本
│   ├── restore.sh                        # 恢复脚本
│   ├── backup-policy.json                # 备份策略
│   └── README.md                         # 备份说明
├── network/
│   ├── oci-network-topology.md           # 网络拓扑文档
│   ├── security-lists.md                 # 安全列表配置
│   └── load-balancer.md                  # 负载均衡器配置
├── disaster-recovery/
│   ├── dr-plan.md                        # 灾难恢复计划
│   ├── rpo-rto.md                        # RPO/RTO 定义
│   └── failover-procedures.md            # 故障切换程序
├── .env.production                       # 生产环境变量模板
├── .env.staging                          # 预发布环境变量模板
├── deployment.sh                         # 自动化部署脚本
└── README.md                             # 本文件
```

---

## 快速开始

### 1. 准备 Oracle Cloud 环境

```bash
# 安装 OCI CLI
bash -c "$(curl -L https://raw.githubusercontent.com/oracle/oci-cli/master/scripts/install/install.sh)"

# 配置 OCI CLI
oci setup config

# 创建 Compartment
oci iam compartment create --name WACAMS --description "WACAMS Production Environment"
```

### 2. 部署基础设施

```bash
# 克隆部署包
git clone <repository-url>
cd wacams/oracle-cloud-deployment

# 配置环境变量
cp .env.production .env
nano .env  # 编辑配置

# 运行部署前检查
./deployment.sh pre-check

# 部署
./deployment.sh deploy
```

### 3. 验证部署

```bash
# 检查服务状态
docker-compose ps

# 健康检查
curl -k https://your-domain.com/health

# 查看日志
docker-compose logs -f
```

---

## 架构概览

```
┌─────────────────────────────────────────────────────────────┐
│                    Oracle Cloud Infrastructure              │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │              Load Balancer (OCI LB)                     │ │
│  │              - SSL Termination                          │ │
│  │              - Health Checks                            │ │
│  └────────────────────┬───────────────────────────────────┘ │
│                       │                                      │
│  ┌────────────────────▼───────────────────────────────────┐ │
│  │              Compute Instance (VM)                      │ │
│  │                                                         │ │
│  │  ┌──────────────────────────────────────────────────┐  │ │
│  │  │              Docker Compose Stack                 │  │ │
│  │  │                                                   │  │ │
│  │  │  ┌─────────┐  ┌─────────┐  ┌─────────┐          │  │ │
│  │  │  │  Nginx  │  │ Backend │  │Frontend │          │  │ │
│  │  │  │  (443)  │→ │ (.NET)  │  │ (React) │          │  │ │
│  │  │  └─────────┘  │  (5000) │  │  (80)   │          │  │ │
│  │  │               └────┬────┘  └─────────┘          │  │ │
│  │  │                    │                             │  │ │
│  │  │               ┌────▼────┐                        │  │ │
│  │  │               │Postgres │                        │  │ │
│  │  │               │  (5432) │                        │  │ │
│  │  │               └─────────┘                        │  │ │
│  │  │                                                   │  │ │
│  │  │  ┌─────────┐  ┌─────────┐  ┌─────────┐          │  │ │
│  │  │  │Prometheus│  │Grafana  │  │AlertMgr │          │  │ │
│  │  │  │  (9090) │  │  (3000) │  │  (9093) │          │  │ │
│  │  │  └─────────┘  └─────────┘  └─────────┘          │  │ │
│  │  └──────────────────────────────────────────────────┘  │ │
│  └─────────────────────────────────────────────────────────┘ │
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │              Block Volume (Backup Storage)              │ │
│  │              - Daily Snapshots                          │ │
│  │              - Cross-Region Replication                 │ │
│  └────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

---

## 资源规格

### 生产环境

| 组件 | OCI 资源 | 规格 | 数量 |
|------|---------|------|------|
| **计算** | VM.Standard3.Flex | 4 OCPU, 16 GB RAM | 1 |
| **存储** | Block Volume | 500 GB (SSD) | 1 |
| **数据库** | PostgreSQL 16 | 优化配置 | 1 |
| **备份** | Object Storage | 1 TB | 1 |
| **网络** | VCN + LB | 100 Mbps | 1 |

### 预发布环境

| 组件 | OCI 资源 | 规格 | 数量 |
|------|---------|------|------|
| **计算** | VM.Standard3.Flex | 2 OCPU, 8 GB RAM | 1 |
| **存储** | Block Volume | 200 GB (SSD) | 1 |
| **数据库** | PostgreSQL 16 | 标准配置 | 1 |

---

## 成本估算

### 月度成本（生产环境）

| 资源 | 规格 | 月成本 (USD) |
|------|------|-------------|
| Compute (VM) | 4 OCPU, 16 GB | $165 |
| Block Volume | 500 GB SSD | $50 |
| Object Storage | 1 TB | $25 |
| Load Balancer | 100 Mbps | $35 |
| Network Transfer | 1 TB | $0 (Free Tier) |
| **总计** | | **$275/月** |

### 成本优化建议

1. **使用 Always Free 资源**
   - 2 AMD Compute VMs (1 OCPU, 1 GB each)
   - 4 ARM Compute VMs (24 GB RAM total)
   - 200 GB Block Volume total

2. **预留实例**
   - 1 年预留：节省 40%
   - 3 年预留：节省 60%

3. **自动缩放**
   - 非工作时间降低实例规格
   - 预计节省：30%

---

## 安全加固

### 网络安全

- ✅ VCN 隔离
- ✅ 安全列表（Stateful Firewall）
- ✅ NSG（Network Security Groups）
- ✅ 私网 subnet（数据库）
- ✅ 公网 subnet（负载均衡器）

### 应用安全

- ✅ SSL/TLS 加密（Let's Encrypt）
- ✅ HTTP/2 支持
- ✅ 安全头（HSTS, CSP, X-Frame-Options）
- ✅ 速率限制
- ✅ WAF（Web Application Firewall）

### 数据安全

- ✅ 数据库加密（at rest）
- ✅ 传输加密（TLS 1.3）
- ✅ 自动备份（每日）
- ✅ 跨区域复制
- ✅ 访问审计

---

## 监控与告警

### 监控指标

- **基础设施**: CPU, 内存, 磁盘, 网络
- **应用**: 请求延迟, 错误率, 吞吐量
- **数据库**: 连接数, 查询性能, 死锁
- **业务**: 活跃用户, 异常检测, 升级状态

### 告警阈值

| 指标 | 警告 | 严重 | 紧急 |
|------|------|------|------|
| CPU 使用率 | > 70% | > 85% | > 95% |
| 内存使用率 | > 75% | > 90% | > 95% |
| 磁盘使用率 | > 80% | > 90% | > 95% |
| API 延迟 | > 500ms | > 1s | > 2s |
| 错误率 | > 1% | > 5% | > 10% |
| 数据库连接 | > 80 | > 90 | > 95 |

---

## 灾难恢复

### RPO/RTO 目标

| 场景 | RPO | RTO |
|------|-----|-----|
| 应用故障 | 0 | 5 分钟 |
| 数据库故障 | 1 小时 | 30 分钟 |
| 区域故障 | 24 小时 | 4 小时 |
| 数据丢失 | 0 | 1 小时 |

### 备份策略

- **数据库**: 每日全量 + 每小时增量
- **配置文件**: Git 版本控制
- **应用数据**: 每日快照
- **跨区域**: 实时复制

---

## 维护窗口

### 计划维护

- **时间**: 每周日 02:00 - 04:00 (SAST)
- **通知**: 提前 48 小时
- **影响**: 服务降级（非停机）

### 紧急维护

- **通知**: 即时通知
- **影响**: 可能停机
- **恢复**: < 30 分钟

---

## 联系支持

- **技术支持**: support@yourcompany.com
- **紧急联系**: +27 XX XXX XXXX
- **文档**: https://docs.yourcompany.com/wacams

---

**版本**: 1.0.0  
**最后更新**: 2026-11-17  
**状态**: 生产就绪 ✅
