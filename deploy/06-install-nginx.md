# Step 6: Install Nginx Reverse Proxy

## 📋 Quick Navigation

| [← Systemd Service](04-configure-systemd.md) | [Home](../DEPLOY.md) | [Next: Automated Deployments →](07-automated-deployments.md) |
|:---------------------------------------------|:--------------------:|--------------------------------------------------------------:|

## 📑 Deployment Steps Index

- [Prerequisites](00-prerequisites.md)
- [Step 1: Install PostgreSQL](01-install-postgresql.md)
- [Step 2: Install .NET Runtime](02-install-dotnet.md)
- [Step 3: Install SigNoz](05-install-signoz.md)
- [Step 4: Install Application](03-install-application.md)
- [Step 5: Configure Systemd Service](04-configure-systemd.md)
- **Step 6: Install Nginx Reverse Proxy** ← You are here
- [Step 7: Setup Automated Deployments](07-automated-deployments.md)
- [Security & Performance](08-security-performance.md)

---

## Overview

In this step, you'll:
- Install Nginx as a reverse proxy
- Configure Nginx for the main application, PGAdmin, and SigNoz
- Configure SSL/TLS with Cloudflare
- Set up DNS and firewall

**Estimated time**: 30-45 minutes

## Install Nginx

Install Nginx to serve the application over HTTP/HTTPS:

```bash
# Install Nginx
sudo apt install -y nginx

# Create Nginx configuration
sudo nano /etc/nginx/sites-available/pathfinder-photography
```

## Create Nginx Configuration

**Initial configuration (HTTP only):**

Since you're using Cloudflare for SSL/TLS, start with a simple HTTP configuration. Cloudflare will handle the SSL termination at their edge.

Add the following configuration for all three services (main app, PGAdmin, and SigNoz):

```nginx
# Main application server
server {
    listen 80;
    listen [::]:80;
    server_name photohonor.coronasda.church www.photohonor.coronasda.church;

    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;

    # Client body size (for photo uploads)
    client_max_body_size 10M;

    # Proxy to .NET application
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Real-IP $remote_addr;
        
        # Timeouts
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }

    # Access and error logs
    access_log /var/log/nginx/pathfinder-photography-access.log;
    error_log /var/log/nginx/pathfinder-photography-error.log;
}

# PGAdmin subdomain server (using single-level subdomain to avoid certificate issues)
server {
    listen 80;
    listen [::]:80;
    server_name photohonorpgadmin.coronasda.church;

    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;

    # Proxy to Apache on port 8080
    location / {
        proxy_pass http://localhost:8080/pgadmin4/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_set_header X-Script-Name /pgadmin4;
        proxy_cache_bypass $http_upgrade;
        
        # Timeouts
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }

    # Access and error logs
    access_log /var/log/nginx/pgadmin-access.log;
    error_log /var/log/nginx/pgadmin-error.log;
}

# SigNoz subdomain server (observability platform)
server {
    listen 80;
    listen [::]:80;
    server_name photohonorsignoz.coronasda.church;

    # Security headers
    add_header X-Frame-Options "SAMEORIGIN" always;
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
    add_header Referrer-Policy "strict-origin-when-cross-origin" always;

    # Proxy to SigNoz on separate server port 8080
    location / {
        proxy_pass http://<SIGNOZ_SERVER_IP>:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        # Timeouts
        proxy_connect_timeout 60s;
        proxy_send_timeout 60s;
        proxy_read_timeout 60s;
    }

    # Access and error logs
    access_log /var/log/nginx/signoz-access.log;
    error_log /var/log/nginx/signoz-error.log;
}
```

**Note**: Replace `<SIGNOZ_SERVER_IP>` with the IP address of your separate SigNoz server. Using single-level subdomains (e.g., `photohonorpgadmin`, `photohonorsignoz`) instead of multi-level subdomains (e.g., `pgadmin.photohonor`, `signoz.photohonor`) avoids SSL/TLS certificate issues with wildcard certificates.

## Enable and Test Nginx

Enable the site and test:

```bash
# Enable site
sudo ln -s /etc/nginx/sites-available/pathfinder-photography /etc/nginx/sites-enabled/

# Test Nginx configuration
sudo nginx -t

# Start Nginx
sudo systemctl start nginx
sudo systemctl enable nginx

# Verify it's running
sudo systemctl status nginx
```

## Configure SSL/TLS with Cloudflare

Cloudflare is the required method for SSL/TLS in this deployment. Cloudflare handles SSL/TLS at their edge (between users and Cloudflare), and you don't need to configure SSL certificates on your server.

**For most users, "Flexible" mode is sufficient** and works with the HTTP-only Nginx configuration above.

### Cloudflare SSL/TLS Settings

1. **Go to SSL/TLS → Overview in Cloudflare Dashboard**
2. **Set encryption mode:**
   - **Flexible**: Cloudflare ↔ User (HTTPS), Your Server ↔ Cloudflare (HTTP) - Recommended, works with HTTP-only Nginx config above
   - **Full**: Cloudflare ↔ User (HTTPS), Your Server ↔ Cloudflare (HTTPS with self-signed cert)
   - **Full (strict)**: Requires valid certificate on your server (Let's Encrypt or Cloudflare Origin Certificate)

**Recommended: Use "Flexible" mode** - it works with the HTTP-only Nginx configuration above.

## Configure Cloudflare DNS

Cloudflare for DNS management is required for `photohonor.coronasda.church`. Choose one of the following options:

### Option A: Using Cloudflare Tunnel (cloudflared)

If you're using Cloudflare Tunnel, you need to configure your tunnel to route traffic to this server's Nginx web server.

**Important**: Nginx listens on port 80 (HTTP) on this server. If your cloudflared service runs on a different machine/VM, configure the service URLs to point to this server's IP address.

**If SigNoz is on a separate server**, you may need to configure separate ingress rules or tunnels for the SigNoz subdomain. Consult your Cloudflare Tunnel documentation for routing to multiple backends.

**1. Add the service to your cloudflared configuration:**

Edit your cloudflared config file (typically `/etc/cloudflared/config.yml` or in your container) and add the following ingress rules:

**If cloudflared is on the SAME server:**
```yaml
ingress:
  - hostname: photohonor.coronasda.church
    service: http://localhost:80
  - hostname: photohonorpgadmin.coronasda.church  # Note: Single-level subdomain to avoid certificate issues
    service: http://localhost:80
  - hostname: photohonorsignoz.coronasda.church  # SigNoz observability platform
    service: http://localhost:80
  # ... your other services ...
  - service: http_status:404  # catch-all rule
```

**If cloudflared is on a DIFFERENT server/VM:**
```yaml
ingress:
  - hostname: photohonor.coronasda.church
    service: http://<YOUR_SERVER_IP>:80
  - hostname: photohonorpgadmin.coronasda.church  # Note: Single-level subdomain to avoid certificate issues
    service: http://<YOUR_SERVER_IP>:80
  - hostname: photohonorsignoz.coronasda.church  # SigNoz observability platform
    service: http://<YOUR_SERVER_IP>:80
  # ... your other services ...
  - service: http_status:404  # catch-all rule
```

Replace `<YOUR_SERVER_IP>` with the IP address of this Pathfinder Photography server.

**Important**: Using single-level subdomains (e.g., `photohonorpgadmin.coronasda.church`) instead of multi-level subdomains (e.g., `pgadmin.photohonor.coronasda.church`) avoids SSL/TLS certificate issues with wildcard certificates.

**Note**: All services are routed through Nginx on port 80. Nginx handles the routing to the appropriate backend services:
- Main application runs on port 5000 (proxied by Nginx)
- pgAdmin runs on port 8080 (proxied by Nginx)
- SigNoz runs on port 8080 on separate server (proxied by Nginx - see [Step 3](05-install-signoz.md))

**Security Note**: PGAdmin provides database management capabilities. Ensure strong passwords are configured and consider additional authentication layers if exposing publicly.

**2. Restart your cloudflared service** to apply the changes

**3. Configure DNS in Cloudflare Dashboard:**
- The DNS records should already be created automatically by cloudflared
- If not, add CNAME records pointing to your tunnel subdomain
- Proxy status should be "DNS only" (gray cloud) when using Cloudflare Tunnel

**Benefits of Cloudflare Tunnel:**
- ✅ No need to expose ports publicly
- ✅ Automatic SSL/TLS certificates
- ✅ DDoS protection
- ✅ No need for port forwarding or firewall configuration
- ✅ Access from anywhere without VPN

### Option B: Direct Connection (Standard Cloudflare Proxy)

If you're NOT using Cloudflare Tunnel, use the standard DNS proxy configuration:

**1. Add DNS Records in Cloudflare Dashboard:**

```
Type: A
Name: photohonor (or @)
Content: Your-Server-IP
Proxy status: Proxied (orange cloud)

Type: A
Name: www
Content: Your-Server-IP
Proxy status: Proxied (orange cloud)

Type: CNAME
Name: pgadmin
Content: photohonor.coronasda.church
Proxy status: Proxied (orange cloud)

Type: CNAME
Name: signoz (if using SigNoz)
Content: photohonor.coronasda.church
Proxy status: Proxied (orange cloud)
```

**2. Configure SSL/TLS Settings:**
- Go to SSL/TLS → Overview
- Set encryption mode to **Full (strict)** or **Full**
- This ensures end-to-end encryption between Cloudflare and your server

**3. Configure Cloudflare SSL Certificate (for Full/Strict mode):**
- Go to SSL/TLS → Origin Server
- Create Origin Certificate
- Copy the certificate and private key
- Save to your server:
  ```bash
  sudo mkdir -p /etc/ssl/cloudflare
  sudo nano /etc/ssl/cloudflare/cert.pem    # Paste certificate
  sudo nano /etc/ssl/cloudflare/key.pem     # Paste private key
  sudo chmod 600 /etc/ssl/cloudflare/*.pem
  ```
- Update Nginx configuration to use these certificates

**4. Enable Cloudflare Features:**
- Under Speed → Optimization: Enable Auto Minify (JS, CSS, HTML)
- Under Security → Settings: Set Security Level to Medium
- Under Firewall: Configure rules as needed

**Benefits of Cloudflare:**
- ✅ Free SSL/TLS certificates
- ✅ DDoS protection
- ✅ CDN for faster content delivery
- ✅ Automatic HTTPS rewrites
- ✅ Web Application Firewall (WAF)

## Configure Firewall

If using Cloudflare Tunnel, your firewall rules will depend on the tunnel configuration. Generally, you would allow Cloudflare IPs and the tunnel port.

If not using Cloudflare Tunnel, configure your firewall to allow HTTP/HTTPS traffic and block everything else by default:

```bash
# CRITICAL: Allow SSH first to prevent lockout
sudo ufw allow 22/tcp

# Allow HTTP and HTTPS
sudo ufw allow 80/tcp comment 'HTTP'
sudo ufw allow 443/tcp comment 'HTTPS'

# Set default policies
sudo ufw default deny incoming
sudo ufw default allow outgoing

# Enable firewall (confirm when prompted)
sudo ufw enable

# Check status
sudo ufw status verbose
```

**Security Note**: The firewall is configured with a default-deny policy for incoming connections, allowing only SSH, HTTP, and HTTPS. This follows the principle of least privilege.

## Verification Checklist

Before moving to the next step, verify:

- [ ] Nginx is installed and running
- [ ] Nginx configuration is created and enabled
- [ ] Nginx configuration test passes (`sudo nginx -t`)
- [ ] Application is accessible via your domain (e.g., `https://photohonor.coronasda.church`)
- [ ] PGAdmin is accessible via subdomain (e.g., `https://photohonorpgadmin.coronasda.church`)
- [ ] SigNoz is accessible via subdomain (e.g., `https://photohonorsignoz.coronasda.church`)
- [ ] Cloudflare SSL/TLS is configured (if using Cloudflare)
- [ ] DNS records are configured and propagated
- [ ] Firewall is configured and enabled
- [ ] All services are accessible with HTTPS (via Cloudflare)

## Troubleshooting

### Application Not Accessible

1. Check Nginx is running:
   ```bash
   sudo systemctl status nginx
   ```

2. Check Nginx logs:
   ```bash
   sudo tail -f /var/log/nginx/pathfinder-photography-error.log
   ```

3. Verify application is running:
   ```bash
   sudo systemctl status pathfinder-photography
   curl http://localhost:5000
   ```

### DNS Not Resolving

1. Check DNS propagation:
   ```bash
   dig photohonor.coronasda.church
   nslookup photohonor.coronasda.church
   ```

2. Verify Cloudflare DNS records are correct
3. Wait for DNS propagation (can take up to 24 hours, usually much faster)

### SSL Certificate Errors

1. Verify Cloudflare SSL mode is set correctly
2. Check if using Cloudflare Tunnel - DNS should be "DNS only" (gray cloud)
3. Clear browser cache and try again

---

## Next Steps

Nginx is now configured and all services (main app, PGAdmin, and SigNoz) should be accessible via your domains! 

Continue with [Step 7: Automated Deployments](07-automated-deployments.md) to set up GitHub Actions for automated deployments.

| [← SigNoz](05-install-signoz.md) | [Home](../DEPLOY.md) | [Next: Automated Deployments →](07-automated-deployments.md) |
|:----------------------------------|:--------------------:|--------------------------------------------------------------:|