# WACAMS SSL/TLS Configuration Guide

## Overview

This document outlines the SSL/TLS configuration for WACAMS deployed on Oracle Cloud Infrastructure.

## Certificate Management

### Option 1: Let's Encrypt (Recommended for Production)

#### Automatic Certificate Renewal

**Docker Compose Service:**
```yaml
certbot:
  image: certbot/dns-oci:latest
  container_name: wacams-certbot
  volumes:
    - ./nginx/ssl:/etc/letsencrypt/live/wacams.yourcompany.com
    - ./certbot:/var/www/certbot
  environment:
    - OCI_CLI_USER=${OCI_USER_ID}
    - OCI_CLI_TENANCY=${OCI_TENANCY_ID}
    - OCI_CLI_FINGERPRINT=${OCI_FINGERPRINT}
    - OCI_CLI_KEY_FILE=/root/.oci/oci_api_key.pem
    - OCI_CLI_REGION=${OCI_REGION}
  command: >
    certonly
    --dns-oci
    --email admin@yourcompany.com
    --domains wacams.yourcompany.com,api.wacams.yourcompany.com
    --agree-tos
    --non-interactive
    --keep-until-expiring
```

**Renewal Cron Job:**
```bash
# Renew certificates twice daily (Let's Encrypt recommends this)
0 */12 * * * docker-compose run --rm certbot renew --quiet && docker-compose exec nginx nginx -s reload
```

#### Manual Certificate Generation
```bash
# Generate certificate
docker-compose run --rm certbot certonly \
  --webroot \
  --webroot-path=/var/www/certbot \
  --email admin@yourcompany.com \
  --agree-tos \
  --domains wacams.yourcompany.com

# Verify certificate
docker-compose run --rm certbot certificates
```

### Option 2: Commercial SSL Certificate (DigiCert, GlobalSign, GoDaddy, etc.)

#### Purchase and Generate CSR
```bash
# Generate private key and CSR
openssl req -new -newkey rsa:2048 -nodes \
  -keyout wacams.key \
  -out wacams.csr \
  -subj "/C=ZA/ST=Gauteng/L=Johannesburg/O=YourCompany/CN=wacams.yourcompany.com"

# Submit CSR to certificate authority (CA)
# Download certificate files after approval
```

#### Install Commercial Certificate
```bash
# Place certificate files in nginx/ssl/
cp your-certificate.crt nginx/ssl/fullchain.pem
cp your-private-key.key nginx/ssl/privkey.pem
cp ca-bundle.crt nginx/ssl/chain.pem

# Set proper permissions
chmod 644 nginx/ssl/fullchain.pem
chmod 600 nginx/ssl/privkey.pem
chmod 644 nginx/ssl/chain.pem

# Reload nginx
docker-compose exec nginx nginx -s reload
```

**Benefits**:
- 1-2 year validity (vs 90 days for Let's Encrypt)
- Extended Validation (EV) certificates available
- Wildcard certificates (*.yourcompany.com)
- Higher trust level for enterprise clients

### Option 3: Oracle Cloud Certificate Manager

#### Import Certificate
```bash
# Upload certificate to OCI Certificate Manager
oci certs-mgmt certificate create-by-importing \
  --compartment-id <compartment-ocid> \
  --name "wacams-ssl-cert" \
  --certificate-pem file:///path/to/cert.pem \
  --private-key-pem file:///path/to/key.pem \
  --chain-pem file:///path/to/chain.pem
```

#### Load Balancer Integration
```bash
# Associate certificate with Load Balancer
oci lb certificate create \
  --load-balancer-id <lb-ocid> \
  --name "wacams-ssl" \
  --public-certificate file:///path/to/cert.pem \
  --private-key file:///path/to/key.pem \
  --ca-certificate file:///path/to/chain.pem
```

### Option 3: Self-Signed (Development Only)

```bash
# Generate self-signed certificate
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
  -keyout nginx/ssl/privkey.pem \
  -out nginx/ssl/fullchain.pem \
  -subj "/C=ZA/ST=Gauteng/L=Johannesburg/O=YourCompany/CN=wacams.local"
```

## Nginx SSL Configuration

### Strong SSL Settings
```nginx
# SSL Protocols (TLS 1.2 and 1.3 only)
ssl_protocols TLSv1.2 TLSv1.3;

# Strong Cipher Suites
ssl_ciphers ECDHE-ECDSA-AES128-GCM-SHA256:ECDHE-RSA-AES128-GCM-SHA256:ECDHE-ECDSA-AES256-GCM-SHA384:ECDHE-RSA-AES256-GCM-SHA384:ECDHE-ECDSA-CHACHA20-POLY1305:ECDHE-RSA-CHACHA20-POLY1305:DHE-RSA-AES128-GCM-SHA256:DHE-RSA-AES256-GCM-SHA384;

# Prefer Server Ciphers
ssl_prefer_server_ciphers off;

# SSL Session Settings
ssl_session_cache shared:SSL:10m;
ssl_session_timeout 1d;
ssl_session_tickets off;

# OCSP Stapling
ssl_stapling on;
ssl_stapling_verify on;
resolver 8.8.8.8 8.8.4.4 valid=300s;
resolver_timeout 5s;

# SSL Buffer Size
ssl_buffer_size 16k;
```

### HTTP to HTTPS Redirect
```nginx
server {
    listen 80;
    listen [::]:80;
    server_name wacams.yourcompany.com;

    # Let's Encrypt Challenge
    location /.well-known/acme-challenge/ {
        root /var/www/certbot;
    }

    # Redirect all other traffic to HTTPS
    location / {
        return 301 https://$server_name$request_uri;
    }
}
```

### HSTS Configuration
```nginx
# HTTP Strict Transport Security
add_header Strict-Transport-Security "max-age=63072000; includeSubDomains; preload" always;
```

## Security Headers

### Complete Security Headers Configuration
```nginx
# X-Frame-Options (Clickjacking Protection)
add_header X-Frame-Options "SAMEORIGIN" always;

# X-Content-Type-Options (MIME Sniffing Protection)
add_header X-Content-Type-Options "nosniff" always;

# X-XSS-Protection (XSS Protection)
add_header X-XSS-Protection "1; mode=block" always;

# Referrer-Policy
add_header Referrer-Policy "strict-origin-when-cross-origin" always;

# Content-Security-Policy
add_header Content-Security-Policy "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' data:; connect-src 'self' https://api.wacams.yourcompany.com;" always;

# Permissions-Policy
add_header Permissions-Policy "geolocation=(), microphone=(), camera=()" always;
```

## Certificate Monitoring

### Expiry Monitoring Script
```bash
#!/bin/bash
# check-cert-expiry.sh

DOMAIN="wacams.yourcompany.com"
WARNING_DAYS=30

# Get certificate expiry date
EXPIRY_DATE=$(echo | openssl s_client -servername $DOMAIN -connect $DOMAIN:443 2>/dev/null | openssl x509 -noout -enddate | cut -d= -f2)

# Convert to timestamp
EXPIRY_TIMESTAMP=$(date -d "$EXPIRY_DATE" +%s)
CURRENT_TIMESTAMP=$(date +%s)

# Calculate days until expiry
DAYS_LEFT=$(( ($EXPIRY_TIMESTAMP - $CURRENT_TIMESTAMP) / 86400 ))

if [ $DAYS_LEFT -le 0 ]; then
    echo "CRITICAL: Certificate has expired!"
    exit 2
elif [ $DAYS_LEFT -le $WARNING_DAYS ]; then
    echo "WARNING: Certificate expires in $DAYS_LEFT days"
    exit 1
else
    echo "OK: Certificate valid for $DAYS_LEFT days"
    exit 0
fi
```

### Prometheus Monitoring
```yaml
# Add to prometheus.yml
- job_name: 'ssl-certificates'
  metrics_path: /probe
  params:
    module: [http_ssl]
  static_configs:
    - targets:
      - https://wacams.yourcompany.com
  relabel_configs:
    - source_labels: [__address__]
      target_label: __param_target
    - source_labels: [__param_target]
      target_label: instance
    - target_label: __address__
      replacement: blackbox-exporter:9115
```

## Load Balancer SSL Configuration

### OCI Load Balancer SSL Policy
```bash
# Create SSL cipher suite
oci lb ssl-cipher-suite create \
  --load-balancer-id <lb-ocid> \
  --name "wacams-ssl-policy" \
  --ciphers TLS_AES_128_GCM_SHA256 TLS_AES_256_GCM_SHA384 ECDHE-RSA-AES128-GCM-SHA256 ECDHE-RSA-AES256-GCM-SHA384

# Create HTTPS listener
oci lb listener create \
  --load-balancer-id <lb-ocid> \
  --name "https-listener" \
  --port 443 \
  --protocol "HTTP" \
  --default-backend-set-name "wacams-backend-set" \
  --ssl-cipher-suite-name "wacams-ssl-policy" \
  --ssl-configuration '{"certificate-name":"wacams-ssl","verify-depth":1,"protocols":["TLSv1.2","TLSv1.3"]}'
```

## Certificate Rotation Automation

### Automated Rotation Script
```bash
#!/bin/bash
# rotate-cert.sh

set -e

DOMAIN="wacams.yourcompany.com"
EMAIL="admin@yourcompany.com"

echo "Starting certificate rotation for $DOMAIN"

# Renew certificate
docker-compose run --rm certbot renew --cert-name $DOMAIN

# Check if certificate was renewed
if [ $? -eq 0 ]; then
    echo "Certificate renewed successfully"
    
    # Reload Nginx
    docker-compose exec nginx nginx -s reload
    
    # Update Load Balancer certificate (if using OCI LB)
    # oci lb certificate update ...
    
    # Send notification
    echo "Certificate rotated successfully" | mail -s "WACAMS SSL Certificate Rotated" $EMAIL
else
    echo "Certificate renewal failed"
    echo "Certificate renewal failed for $DOMAIN" | mail -s "URGENT: WACAMS SSL Certificate Rotation Failed" $EMAIL
    exit 1
fi
```

## Testing SSL Configuration

### SSL Labs Test
```bash
# Test SSL configuration
curl -s https://www.ssllabs.com/ssltest/analyze.html?d=wacams.yourcompany.com
```

### Manual Testing
```bash
# Test TLS 1.2
openssl s_client -connect wacams.yourcompany.com:443 -tls1_2

# Test TLS 1.3
openssl s_client -connect wacams.yourcompany.com:443 -tls1_3

# Verify certificate chain
openssl s_client -connect wacams.yourcompany.com:443 -showcerts

# Check certificate details
openssl s_client -connect wacams.yourcompany.com:443 | openssl x509 -noout -text
```

### Security Scanning
```bash
# Test for vulnerabilities
nmap --script ssl-enum-ciphers -p 443 wacams.yourcompany.com

# Test for specific vulnerabilities
nmap --script ssl-heartbleed -p 443 wacams.yourcompany.com
nmap --script ssl-poodle -p 443 wacams.yourcompany.com
```

## Troubleshooting

### Common Issues

#### Issue: Certificate Not Trusted
```bash
# Check certificate chain
openssl s_client -connect wacams.yourcompany.com:443 -showcerts

# Verify CA bundle is complete
cat fullchain.pem | grep "BEGIN CERTIFICATE" | wc -l
# Should show 2-3 certificates (leaf + intermediate + optional root)
```

#### Issue: Mixed Content Warnings
```bash
# Find HTTP resources
curl -s https://wacams.yourcompany.com | grep -o 'http://[^"]*'

# Update application to use HTTPS or protocol-relative URLs
```

#### Issue: HSTS Preload Issues
```bash
# Verify HSTS header
curl -I https://wacams.yourcompany.com | grep Strict-Transport

# Check preload eligibility at https://hstspreload.org/
```

## Best Practices

1. **Use TLS 1.3** when possible (faster, more secure)
2. **Enable HSTS** with long max-age (63072000 seconds = 2 years)
3. **Enable OCSP Stapling** for faster SSL handshakes
4. **Use Strong Ciphers** only (AEAD ciphers preferred)
5. **Implement HSTS Preload** for maximum security
6. **Monitor Certificate Expiry** with automated alerts
7. **Automate Renewal** to prevent expiration
8. **Regular Security Audits** with tools like SSL Labs
9. **Backup Certificates** securely before rotation
10. **Test in Staging** before production deployment

## Compliance

### PCI DSS Requirements
- TLS 1.2 or higher required
- Strong cipher suites only
- Valid certificates from trusted CA
- Regular vulnerability scans

### POPIA Considerations
- Encrypt all personal data in transit
- Maintain certificate audit trail
- Document encryption policies
- Regular security assessments

---

**Document Version**: 1.0  
**Last Updated**: 2026-11-17  
**Next Review**: 2027-02-17
