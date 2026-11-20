# WACAMS Domain Configuration Checklist

**Version**: 1.0.0  
**Created**: 2026-11-19  
**Phase**: 5D — Production Readiness  
**Status**: READY FOR CONFIGURATION

---

## Overview

This checklist covers all domain, DNS, and network configuration required before WACAMS goes live in production on Oracle Cloud Infrastructure.

---

## 1. Domain Registration

| # | Task | Owner | Status | Notes |
|---|------|-------|--------|-------|
| 1.1 | Register primary domain (e.g., `wacams.company.com`) | IT/DNS Team | ☐ | Via domain registrar |
| 1.2 | Register email domain (e.g., `wacams.company.com`) | IT/DNS Team | ☐ | Can be same as primary |
| 1.3 | Confirm domain ownership with OCI | Cloud Admin | ☐ | Required for Email Delivery |
| 1.4 | Set domain auto-renewal to ON | IT/DNS Team | ☐ | Prevent accidental expiry |

---

## 2. DNS Records

### 2.1 Application Routing

| Record Type | Name | Value | TTL | Purpose |
|-------------|------|-------|-----|---------|
| A | `wacams.company.com` | `<OCI Load Balancer Public IP>` | 300 | Main app access |
| A | `api.wacams.company.com` | `<OCI Load Balancer Public IP>` | 300 | API endpoint |
| CNAME | `www.wacams.company.com` | `wacams.company.com` | 300 | WWW redirect |

### 2.2 Email Delivery

| Record Type | Name | Value | TTL | Purpose |
|-------------|------|-------|-----|---------|
| TXT | `wacams.company.com` | `v=spf1 include:smtp.email.us-ashburn-1.oci.oraclecloud.com ~all` | 3600 | SPF |
| TXT | `wacams._domainkey.wacams.company.com` | *(Provided by OCI)* | 3600 | DKIM |
| TXT | `_dmarc.wacams.company.com` | `v=DMARC1; p=quarantine; rua=mailto:dmarc@company.com` | 3600 | DMARC |

### 2.3 Verification

```bash
# Check A record
dig wacams.company.com +short

# Check SPF
dig wacams.company.com TXT +short

# Check DKIM
dig wacams._domainkey.wacams.company.com TXT +short

# Check DMARC
dig _dmarc.wacams.company.com TXT +short
```

---

## 3. SSL/TLS Certificates

| # | Task | Owner | Status | Notes |
|---|------|-------|--------|-------|
| 3.1 | Provision SSL certificate for `wacams.company.com` | Cloud Admin | ☐ | Via OCI Load Balancer or Let's Encrypt |
| 3.2 | Provision SSL certificate for `api.wacams.company.com` | Cloud Admin | ☐ | Same method |
| 3.3 | Configure auto-renewal for certificates | Cloud Admin | ☐ | Certbot cron or OCI managed |
| 3.4 | Verify HTTPS works on both domains | QA | ☐ | `curl -v https://wacams.company.com` |
| 3.5 | Redirect HTTP → HTTPS | Cloud Admin | ☐ | Nginx config |

---

## 4. OCI Network Configuration

| # | Task | Owner | Status | Notes |
|---|------|-------|--------|-------|
| 4.1 | Create VCN with public + private subnets | Network Admin | ☐ | See `network-topology.md` |
| 4.2 | Configure Internet Gateway | Network Admin | ☐ | For public load balancer |
| 4.3 | Configure NAT Gateway | Network Admin | ☐ | For private subnet outbound |
| 4.4 | Configure security lists (port 443, 80 inbound) | Network Admin | ☐ | Block all other inbound |
| 4.5 | Configure NSG for backend (port 8080 internal only) | Network Admin | ☐ | Not exposed to internet |
| 4.6 | Configure NSG for database (port 5432 internal only) | Network Admin | ☐ | Not exposed to internet |
| 4.7 | Configure NSG for Redis (port 6379 internal only) | Network Admin | ☐ | Not exposed to internet |

---

## 5. Load Balancer

| # | Task | Owner | Status | Notes |
|---|------|-------|--------|-------|
| 5.1 | Create OCI Load Balancer (public) | Cloud Admin | ☐ | Minimum 10 Mbps |
| 5.2 | Add backend set: `wacams-frontend` (port 3000) | Cloud Admin | ☐ | Health check: `/` |
| 5.3 | Add backend set: `wacams-api` (port 8080) | Cloud Admin | ☐ | Health check: `/health` |
| 5.4 | Configure listener: 443 → frontend | Cloud Admin | ☐ | SSL termination |
| 5.5 | Configure listener: 443/api → api backend | Cloud Admin | ☐ | Path-based routing |
| 5.6 | Attach SSL certificates | Cloud Admin | ☐ | From step 3 |
| 5.7 | Test load balancer health | QA | ☐ | All backends healthy |

---

## 6. Firewall & WAF

| # | Task | Owner | Status | Notes |
|---|------|-------|--------|-------|
| 6.1 | Enable OCI Web Application Firewall (WAF) | Security Team | ☐ | Protects against L7 attacks |
| 6.2 | Configure WAF rules (SQLi, XSS, etc.) | Security Team | ☐ | Use managed rules |
| 6.3 | Configure rate limiting (100 req/min per IP) | Security Team | ☐ | Prevent abuse |
| 6.4 | Configure IP blocklist for known bad actors | Security Team | ☐ | Optional |
| 6.5 | Verify database port NOT accessible from internet | Security Team | ☐ | Critical |
| 6.6 | Verify Redis port NOT accessible from internet | Security Team | ☐ | Critical |

---

## 7. GIS Integration Network

| # | Task | Owner | Status | Notes |
|---|------|-------|--------|-------|
| 7.1 | Verify connectivity from OCI to `gis.yfpo.com:9105` | Network Admin | ☐ | May require VPN/peering |
| 7.2 | Verify connectivity from OCI to `10.192.240.45` (MES) | Network Admin | ☐ | Factory network |
| 7.3 | Configure fast-connect or VPN if needed | Network Admin | ☐ | For private factory IPs |
| 7.4 | Test GIS API reachability from OCI | QA | ☐ | `curl https://gis.yfpo.com:9105` |

---

## 8. Email Domain Verification

| # | Task | Owner | Status | Notes |
|---|------|-------|--------|-------|
| 8.1 | Create email domain in OCI Email Delivery | Cloud Admin | ☐ | See `SMTP_ONBOARDING.md` |
| 8.2 | Add approved sender address | Cloud Admin | ☐ | `noreply@wacams.company.com` |
| 8.3 | Verify sender email (click verification link) | Cloud Admin | ☐ | Check inbox |
| 8.4 | Generate SMTP credentials | Cloud Admin | ☐ | Save securely |
| 8.5 | Send test email from WACAMS | QA | ☐ | Via API test endpoint |

---

## 9. Monitoring & Alerts

| # | Task | Owner | Status | Notes |
|---|------|-------|--------|-------|
| 9.1 | Configure OCI Monitoring for compute instances | Cloud Admin | ☐ | CPU, memory, disk |
| 9.2 | Configure alerts for high CPU (>80%) | Cloud Admin | ☐ | Email/notification |
| 9.3 | Configure alerts for database connectivity | Cloud Admin | ☐ | Critical |
| 9.4 | Set up uptime checks for `wacams.company.com` | QA/Ops | ☐ | Every 5 min |
| 9.5 | Set up uptime checks for `api.wacams.company.com` | QA/Ops | ☐ | Every 5 min |

---

## 10. Final Go-Live Verification

| # | Check | Command/Method | Pass/Fail |
|---|-------|----------------|-----------|
| 10.1 | DNS resolves correctly | `dig wacams.company.com` | ☐ |
| 10.2 | HTTPS loads frontend | `curl -I https://wacams.company.com` | ☐ |
| 10.3 | HTTPS loads API | `curl -I https://api.wacams.company.com/health` | ☐ |
| 10.4 | HTTP redirects to HTTPS | `curl -I http://wacams.company.com` | ☐ |
| 10.5 | SSL certificate valid | `openssl s_client -connect wacams.company.com:443` | ☐ |
| 10.6 | SPF record present | `dig wacams.company.com TXT` | ☐ |
| 10.7 | DKIM record present | `dig wacams._domainkey.wacams.company.com TXT` | ☐ |
| 10.8 | DMARC record present | `dig _dmarc.wacams.company.com TXT` | ☐ |
| 10.9 | Email sends successfully | API test endpoint | ☐ |
| 10.10 | GIS integration reachable | `curl https://gis.yfpo.com:9105` from OCI | ☐ |
| 10.11 | Database not exposed | `nmap -p 5432 <public-ip>` | ☐ |
| 10.12 | WAF active | Check OCI Console | ☐ |

---

## Sign-Off

| Role | Name | Signature | Date |
|------|------|-----------|------|
| Network Admin | | | |
| Cloud Admin | | | |
| Security Team | | | |
| QA Lead | | | |
| Product Owner | | | |

---

**Document Status**: READY  
**Next Review**: Before production deployment
