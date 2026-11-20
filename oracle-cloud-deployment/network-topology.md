# WACAMS Oracle Cloud Network Topology

## Overview

This document describes the network architecture for WACAMS deployed on Oracle Cloud Infrastructure (OCI).

## Network Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    Oracle Cloud Infrastructure (OCI)                     │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │                    Virtual Cloud Network (VCN)                     │  │
│  │                    CIDR: 10.0.0.0/16                               │  │
│  │                                                                    │  │
│  │  ┌─────────────────────────────────────────────────────────────┐  │  │
│  │  │              Public Subnet (10.0.1.0/24)                     │  │  │
│  │  │                                                               │  │  │
│  │  │  ┌──────────────────────────────────────────────────────┐   │  │  │
│  │  │  │  Load Balancer                                        │   │  │  │
│  │  │  │  - Public IP: 129.xxx.xxx.xxx                         │   │  │  │
│  │  │  │  - SSL Termination                                    │   │  │  │
│  │  │  │  - Health Checks                                      │   │  │  │
│  │  │  └──────────────────────────────────────────────────────┘   │  │  │
│  │  │                                                               │  │  │
│  │  │  ┌──────────────────────────────────────────────────────┐   │  │  │
│  │  │  │  Bastion Host (Optional)                              │   │  │  │
│  │  │  │  - SSH Access                                         │   │  │  │
│  │  │  │  - Management Interface                               │   │  │  │
│  │  │  └──────────────────────────────────────────────────────┘   │  │  │
│  │  └─────────────────────────────────────────────────────────────┘  │  │
│  │                                                                    │  │
│  │  ┌─────────────────────────────────────────────────────────────┐  │  │
│  │  │              Private Subnet (10.0.2.0/24)                    │  │  │
│  │  │                                                               │  │  │
│  │  │  ┌──────────────────────────────────────────────────────┐   │  │  │
│  │  │  │  Compute Instance (VM)                                │   │  │  │
│  │  │  │  - Private IP: 10.0.2.10                              │   │  │  │
│  │  │  │  - Docker Host                                        │   │  │  │
│  │  │  │  - No Public IP                                       │   │  │  │
│  │  │  │                                                        │   │  │  │
│  │  │  │  ┌────────────────────────────────────────────────┐  │   │  │  │
│  │  │  │  │  Docker Containers                             │  │   │  │  │
│  │  │  │  │  - Nginx (Port 80, 443)                        │  │   │  │  │
│  │  │  │  │  - Backend API (Port 8080)                     │  │   │  │  │
│  │  │  │  │  - Frontend (Port 3000)                        │  │   │  │  │
│  │  │  │  │  - PostgreSQL (Port 5432)                      │  │   │  │  │
│  │  │  │  │  - Redis (Port 6379)                           │  │   │  │  │
│  │  │  │  │  - Prometheus (Port 9090)                      │  │   │  │  │
│  │  │  │  │  - Grafana (Port 3001)                         │  │   │  │  │
│  │  │  │  └────────────────────────────────────────────────┘  │   │  │  │
│  │  │  └──────────────────────────────────────────────────────┘   │  │  │
│  │  └─────────────────────────────────────────────────────────────┘  │  │
│  │                                                                    │  │
│  │  ┌─────────────────────────────────────────────────────────────┐  │  │
│  │  │              Database Subnet (10.0.3.0/24)                   │  │  │
│  │  │                                                               │  │  │
│  │  │  ┌──────────────────────────────────────────────────────┐   │  │  │
│  │  │  │  PostgreSQL (Standalone or RAC)                       │   │  │  │
│  │  │  │  - Private IP: 10.0.3.10                              │   │  │  │
│  │  │  │  - Port: 5432                                         │   │  │  │
│  │  │  │  - Encrypted Storage                                  │   │  │  │
│  │  │  └──────────────────────────────────────────────────────┘   │  │  │
│  │  └─────────────────────────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │              Internet Gateway                                      │  │
│  │              - Public Subnet Access                                │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │              NAT Gateway                                           │  │
│  │              - Private Subnet Outbound Access                      │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                                                         │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │              Service Gateway                                       │  │
│  │              - OCI Services Access (Object Storage, etc.)          │  │
│  └───────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘

External Access:
┌─────────────────────────────────────────────────────────────────────────┐
│  Internet Users                                                         │
│  - HTTPS (443) → Load Balancer → Nginx → Backend/Frontend              │
│  - SSH (22) → Bastion Host → Compute Instance (Optional)               │
└─────────────────────────────────────────────────────────────────────────┘
```

## Security Lists Configuration

### Public Subnet Security List

**Ingress Rules:**
| Source | Protocol | Port | Description |
|--------|----------|------|-------------|
| 0.0.0.0/0 | TCP | 443 | HTTPS Traffic |
| 0.0.0.0/0 | TCP | 80 | HTTP (Redirect to HTTPS) |
| 10.0.0.0/16 | TCP | 22 | SSH from VCN |
| 10.0.2.0/24 | TCP | All | From Private Subnet |

**Egress Rules:**
| Destination | Protocol | Port | Description |
|-------------|----------|------|-------------|
| 0.0.0.0/0 | TCP | 443 | HTTPS Outbound |
| 0.0.0.0/0 | TCP | 80 | HTTP Outbound |
| 10.0.0.0/16 | TCP | All | To VCN |

### Private Subnet Security List

**Ingress Rules:**
| Source | Protocol | Port | Description |
|--------|----------|------|-------------|
| 10.0.1.0/24 | TCP | 80, 443 | From Load Balancer |
| 10.0.1.0/24 | TCP | 22 | SSH from Bastion |
| 10.0.2.0/24 | TCP | All | Internal Communication |
| 10.0.3.0/24 | TCP | 5432 | From Database Subnet |

**Egress Rules:**
| Destination | Protocol | Port | Description |
|-------------|----------|------|-------------|
| 0.0.0.0/0 | TCP | 443 | HTTPS Outbound (via NAT) |
| 0.0.0.0/0 | TCP | 80 | HTTP Outbound (via NAT) |
| 10.0.0.0/16 | TCP | All | To VCN |

### Database Subnet Security List

**Ingress Rules:**
| Source | Protocol | Port | Description |
|--------|----------|------|-------------|
| 10.0.2.0/24 | TCP | 5432 | From Private Subnet Only |
| 10.0.3.0/24 | TCP | 5432 | Internal DB Communication |

**Egress Rules:**
| Destination | Protocol | Port | Description |
|-------------|----------|------|-------------|
| 10.0.2.0/24 | TCP | All | To Private Subnet |
| 10.0.3.0/24 | TCP | All | Internal Communication |

## Network Security Groups (NSGs)

### NSG: Load Balancer
**Members:** Load Balancer instances
**Ingress:**
- Allow HTTPS (443) from Internet
- Allow HTTP (80) from Internet

**Egress:**
- Allow HTTP/HTTPS to Compute Instance NSG

### NSG: Compute Instance
**Members:** Docker host VM
**Ingress:**
- Allow HTTP/HTTPS from Load Balancer NSG
- Allow SSH from Bastion NSG
- Allow PostgreSQL (5432) from Database NSG
- Allow Redis (6379) from self

**Egress:**
- Allow HTTPS to Internet (via NAT Gateway)
- Allow PostgreSQL to Database NSG
- Allow all to self

### NSG: Database
**Members:** PostgreSQL instances
**Ingress:**
- Allow PostgreSQL (5432) from Compute NSG only

**Egress:**
- Allow PostgreSQL to Compute NSG (replication)
- Allow all to self

## Load Balancer Configuration

### Listener Configuration
```
Listener: HTTPS-443
- Protocol: HTTPS
- Port: 443
- SSL Certificate: OCID of certificate
- Backend Set: wacams-backend-set

Listener: HTTP-80
- Protocol: HTTP
- Port: 80
- Redirect to: HTTPS-443
```

### Backend Set Configuration
```
Backend Set: wacams-backend-set
- Policy: LEAST_CONNECTIONS
- Health Check:
  - Protocol: HTTP
  - Port: 80
  - Path: /health
  - Interval: 10s
  - Timeout: 3s
  - Retries: 3

Backends:
- 10.0.2.10:80 (Compute Instance)
```

## DNS Configuration

### Public DNS (if using custom domain)
```
A Record: wacams.yourcompany.com → Load Balancer Public IP
CNAME: api.wacams.yourcompany.com → wacams.yourcompany.com
```

### Private DNS (within VCN)
```
A Record: wacams-app → 10.0.2.10
A Record: wacams-db → 10.0.3.10
```

## Network Monitoring

### Flow Logs
- Enable VCN Flow Logs for all subnets
- Retention: 30 days
- Destination: Object Storage

### Network Monitoring Metrics
- Load Balancer: Request count, latency, error rate
- Network: Bytes in/out, packet drops
- DNS: Query count, latency

## Disaster Recovery Network

### Cross-Region Setup
```
Primary Region: af-johannesburg-1
- VCN CIDR: 10.0.0.0/16

DR Region: af-johannesburg-1 (different AD) or eu-frankfurt-1
- VCN CIDR: 10.1.0.0/16 (non-overlapping)
- Peering: Remote Peering Connection (RPC)
```

### Failover DNS
```
Primary: wacams.yourcompany.com → Primary LB IP
Secondary: dr.wacams.yourcompany.com → DR LB IP
TTL: 60 seconds (for fast failover)
```

## Access Control

### Bastion Host Access
```bash
# SSH to Bastion
ssh -i ~/.ssh/oci_key.pem opc@bastion-public-ip

# SSH to Compute via Bastion
ssh -i ~/.ssh/oci_key.pem -J opc@bastion-public-ip opc@10.0.2.10

# Port forwarding to PostgreSQL
ssh -i ~/.ssh/oci_key.pem -L 5432:10.0.3.10:5432 opc@bastion-public-ip
```

### VPN Access (Optional)
```
Site-to-Site VPN:
- Customer Premises Equipment (CPE): Your office router
- OCI CPE: Public IP of office router
- IPSec Connection: Encrypted tunnel
- Routes: 10.0.0.0/16 → Office network
```

## Cost Optimization

### Network Costs
- Data Transfer: First 10 TB/month free (outbound)
- Load Balancer: ~$15/month (flexible)
- NAT Gateway: ~$15/month + data processing
- Service Gateway: Free

### Recommendations
- Use Service Gateway for OCI services (Object Storage, etc.)
- Minimize cross-region traffic
- Use CDN for static assets (reduces load balancer traffic)
- Compress responses (gzip)

## Security Best Practices

1. **Network Segmentation**: Separate public, private, and database subnets
2. **Least Privilege**: Only open required ports
3. **Encryption**: TLS 1.3 for all external traffic
4. **Monitoring**: Enable all logging and monitoring
5. **Regular Audits**: Review security lists monthly
6. **Bastion Host**: Use for all administrative access
7. **Private Endpoints**: Use private IPs for all internal communication
8. **WAF**: Consider Web Application Firewall for additional protection

## Implementation Steps

1. Create VCN with specified CIDR blocks
2. Create subnets (public, private, database)
3. Configure security lists and NSGs
4. Deploy Load Balancer
5. Configure Internet Gateway, NAT Gateway, Service Gateway
6. Set up route tables
7. Deploy compute instances
8. Configure DNS records
9. Test connectivity and security rules
10. Enable monitoring and logging
