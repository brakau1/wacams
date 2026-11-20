# WACAMS SMTP Email Onboarding Guide

**Version**: 1.0.0  
**Created**: 2026-11-19  
**Phase**: 5D — Production Readiness  
**Status**: READY FOR CONFIGURATION

---

## Table of Contents

1. [Overview](#1-overview)
2. [Oracle Cloud Email Delivery](#2-oracle-cloud-email-delivery)
3. [Configuration Steps](#3-configuration-steps)
4. [Environment Variables](#4-environment-variables)
5. [Testing Email Delivery](#5-testing-email-delivery)
6. [Troubleshooting](#6-troubleshooting)
7. [Alternative SMTP Providers](#7-alternative-smtp-providers)

---

## 1. Overview

WACAMS uses SMTP for sending email notifications including:
- Warning letters
- Attendance exceptions
- Escalation alerts
- Corrective action forms
- Password reset links
- Shift notifications

The system supports any SMTP provider, with Oracle Cloud Email Delivery recommended for Oracle Cloud deployments.

---

## 2. Oracle Cloud Email Delivery

### Prerequisites

| Requirement | Details |
|-------------|---------|
| OCI Tenancy | Active Oracle Cloud Infrastructure tenancy |
| Compartment | Dedicated compartment for email resources |
| Approved Sender | Verified email address or domain |
| SMTP Credentials | Generated SMTP credentials for authenticated sending |

### Pricing

- **Free Tier**: 3,000 emails/month (100 emails/day)
- **Paid**: $0.12 per 1,000 emails after free tier
- **No setup fees**

---

## 3. Configuration Steps

### Step 1: Create Email Domain

1. Navigate to **Email Delivery** → **Email Domains** in OCI Console
2. Click **Create Email Domain**
3. Enter domain name (e.g., `wacams.yourcompany.com`)
4. Select compartment
5. Click **Create**

### Step 2: Configure DNS Records

After creating the domain, OCI provides DNS records to add:

```dns
; SPF Record (allow Oracle Cloud to send)
wacams.yourcompany.com. IN TXT "v=spf1 include:smtp.email.us-ashburn-1.oci.oraclecloud.com ~all"

; DKIM Record (provided by OCI)
wacams._domainkey.wacams.yourcompany.com. IN TXT "v=DKIM1; k=rsa; p=MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8A..."

; DMARC Record (recommended)
_dmarc.wacams.yourcompany.com. IN TXT "v=DMARC1; p=quarantine; rua=mailto:dmarc@yourcompany.com"
```

**Wait 24-48 hours for DNS propagation.**

### Step 3: Create Approved Sender

1. Navigate to **Email Delivery** → **Approved Senders**
2. Click **Create Approved Sender**
3. Enter email address (e.g., `noreply@wacams.yourcompany.com`)
4. Click **Create**
5. Verify the email address (check inbox for verification link)

### Step 4: Generate SMTP Credentials

1. Navigate to **Identity** → **Users**
2. Select your user (or create a dedicated `wacams-smtp` user)
3. Click **Resources** → **SMTP Credentials**
4. Click **Generate SMTP Credentials**
5. Enter description: `WACAMS SMTP Credentials`
6. Click **Generate Credentials**
7. **IMPORTANT**: Copy and save both:
   - **Username**: e.g., `ocid1.user.oc1..aaaaaaa.../wacams-smtp@yourcompany.com`
   - **Password**: Auto-generated password (e.g., `OQKp...`)

### Step 5: Get SMTP Endpoint

Oracle Cloud Email Delivery endpoints:

| Region | Endpoint | Port |
|--------|----------|------|
| US Ashburn | `smtp.email.us-ashburn-1.oci.oraclecloud.com` | 587 (TLS) |
| US Phoenix | `smtp.email.us-phoenix-1.oci.oraclecloud.com` | 587 (TLS) |
| EU Frankfurt | `smtp.email.eu-frankfurt-1.oci.oraclecloud.com` | 587 (TLS) |
| UK London | `smtp.email.uk-london-1.oci.oraclecloud.com` | 587 (TLS) |

---

## 4. Environment Variables

Update `.env` file with SMTP configuration:

```bash
# SMTP Configuration
SMTP_HOST=smtp.email.us-ashburn-1.oci.oraclecloud.com
SMTP_PORT=587
SMTP_USERNAME=ocid1.user.oc1..aaaaaaa.../wacams-smtp@yourcompany.com
SMTP_PASSWORD=OQKp...
SMTP_FROM_EMAIL=noreply@wacams.yourcompany.com
SMTP_FROM_NAME=WACAMS System
SMTP_USE_TLS=true

# Email Settings
EMAIL_MAX_RETRIES=3
EMAIL_RETRY_DELAY_SECONDS=60
EMAIL_BATCH_SIZE=50
EMAIL_BATCH_DELAY_SECONDS=5
```

### Docker Compose Integration

The `docker-compose.yml` already includes SMTP configuration:

```yaml
services:
  backend:
    environment:
      - SMTP_HOST=${SMTP_HOST}
      - SMTP_PORT=${SMTP_PORT}
      - SMTP_USERNAME=${SMTP_USERNAME}
      - SMTP_PASSWORD=${SMTP_PASSWORD}
      - SMTP_FROM_EMAIL=${SMTP_FROM_EMAIL}
      - SMTP_FROM_NAME=${SMTP_FROM_NAME}
      - SMTP_USE_TLS=${SMTP_USE_TLS}
```

---

## 5. Testing Email Delivery

### Test 1: Send Test Email via API

```bash
curl -X POST http://localhost:8080/api/notifications/test-email \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "to": "your.email@company.com",
    "subject": "WACAMS SMTP Test",
    "body": "This is a test email from WACAMS."
  }'
```

Expected response:
```json
{
  "success": true,
  "messageId": "abc123...",
  "timestamp": "2026-11-19T12:00:00Z"
}
```

### Test 2: Trigger Real Notification

1. Log in to WACAMS as HR Manager
2. Create a test warning for an employee
3. Approve the warning
4. Check that the employee receives the warning email

### Test 3: Check Email Logs

```bash
# View backend logs
docker logs wacams-backend | grep "Email sent"

# Check SMTP connection
docker logs wacams-backend | grep "SMTP"
```

---

## 6. Troubleshooting

### Issue: "Authentication failed"

**Cause**: Incorrect SMTP credentials or username format.

**Solution**:
- Verify username format: `ocid1.user.oc1..aaaaaaa.../username@domain.com`
- Regenerate SMTP credentials if needed
- Ensure user has permission to send emails

### Issue: "Connection timeout"

**Cause**: Firewall blocking port 587 or incorrect endpoint.

**Solution**:
- Verify endpoint matches your OCI region
- Check firewall rules allow outbound traffic on port 587
- Test connectivity: `telnet smtp.email.us-ashburn-1.oci.oraclecloud.com 587`

### Issue: "Sender not approved"

**Cause**: From email address not in approved senders list.

**Solution**:
- Add email to **Approved Senders** in OCI Console
- Verify the email address (check inbox for verification link)
- Wait for DNS propagation (up to 24 hours)

### Issue: "Emails going to spam"

**Cause**: Missing or incorrect DNS records.

**Solution**:
- Verify SPF record is configured correctly
- Check DKIM signature is valid
- Add DMARC record for better deliverability
- Use a consistent From address

### Issue: "Rate limit exceeded"

**Cause**: Exceeding Oracle Cloud's sending limits.

**Solution**:
- Free tier: 100 emails/day, 3,000 emails/month
- Request quota increase in OCI Console
- Implement email batching in application (already configured)

---

## 7. Alternative SMTP Providers

If not using Oracle Cloud Email Delivery, WACAMS supports any SMTP provider:

### Microsoft 365 / Exchange Online

```bash
SMTP_HOST=smtp.office365.com
SMTP_PORT=587
SMTP_USERNAME=noreply@yourcompany.com
SMTP_PASSWORD=your-m365-password
SMTP_USE_TLS=true
SMTP_FROM_EMAIL=noreply@yourcompany.com
SMTP_FROM_NAME=WACAMS System
```

**Requirements**:
- Microsoft 365 business account with Exchange Online
- SMTP AUTH must be enabled for the sender account
- Admin center: Settings → Org settings → Services → SMTP AUTH
- May require "App Password" if MFA is enabled
- Rate limit: 10,000 emails/day (varies by license)

**Troubleshooting**:
- Error "Authentication failed": Check if SMTP AUTH is enabled
- Error "Client not authenticated": May need to use OAuth2 instead of basic auth
- Contact IT admin if SMTP AUTH is disabled at org level

### SendGrid

```bash
SMTP_HOST=smtp.sendgrid.net
SMTP_PORT=587
SMTP_USERNAME=apikey
SMTP_PASSWORD=SG.xxxxxxxx
SMTP_USE_TLS=true
```

**Pricing**: Free tier (100 emails/day), paid from $19.95/month

### AWS SES (Simple Email Service)

```bash
SMTP_HOST=email-smtp.us-east-1.amazonaws.com
SMTP_PORT=587
SMTP_USERNAME=AKIA...
SMTP_PASSWORD=xxxxxxxx
SMTP_USE_TLS=true
```

**Pricing**: $0.10 per 1,000 emails, free tier (62,000 emails/month from EC2)

### Gmail (for testing only)

```bash
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=your.email@gmail.com
SMTP_PASSWORD=your-app-password
SMTP_USE_TLS=true
```

**Note**: Gmail requires "App Password" (not regular password) and has strict rate limits (500 emails/day).

### Custom SMTP Server

For on-premises Exchange or custom SMTP servers:

```bash
SMTP_HOST=mail.yourcompany.com
SMTP_PORT=587
SMTP_USERNAME=wacams@yourcompany.com
SMTP_PASSWORD=your-password
SMTP_USE_TLS=true
```

**Requirements**:
- SMTP server must support TLS 1.2+
- Valid credentials with send permissions
- Firewall rules allowing outbound SMTP traffic
- May require relay configuration on SMTP server

---

## Checklist

Before going live, verify:

- [ ] Email domain created in OCI
- [ ] DNS records (SPF, DKIM, DMARC) configured
- [ ] Approved sender email verified
- [ ] SMTP credentials generated and saved securely
- [ ] Environment variables updated in `.env`
- [ ] Test email sent successfully via API
- [ ] Real notification triggered and received
- [ ] Email logs checked for errors
- [ ] Deliverability tested (not in spam)
- [ ] Rate limits understood and monitored

---

## Support

For SMTP configuration issues:
- Oracle Cloud Documentation: https://docs.oracle.com/en-us/iaas/Content/Email/home.htm
- WACAMS GitHub Issues: https://github.com/yourcompany/wacams/issues

---

**Document Status**: READY  
**Next Review**: After production deployment
