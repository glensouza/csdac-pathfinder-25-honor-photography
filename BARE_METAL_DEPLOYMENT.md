# Bare Metal / VM Deployment Guide

This guide provides instructions for deploying the Pathfinder Photography application on a single server or virtual machine without Docker. All components (PostgreSQL, .NET application, and optionally SigNoz) will be installed directly on the host system.

> 💡 **Want Automated Deployments?** After completing the initial setup below, see [section 7](#7-setup-automated-deployments-optional) to configure automatic deployments via GitHub Actions. Every push to the `main` branch will automatically deploy to your server.

## Table of Contents
- [Prerequisites](#prerequisites)
- [System Requirements](#system-requirements)
- [Installation Steps](#installation-steps)
  - [1. Install PostgreSQL](#1-install-postgresql)
  - [2. Install .NET Runtime and SDK](#2-install-net-runtime-and-sdk)
  - [2.1. Install Git (Required for Building from Source)](#21-install-git-required-for-building-from-source)
  - [3. Install Application](#3-install-application)
  - [4. Configure Systemd Service](#4-configure-systemd-service)
  - [5. Install Nginx Reverse Proxy](#5-install-nginx-reverse-proxy)
  - [6. Install SigNoz (Optional)](#6-install-signoz-optional)
  - [7. Setup Automated Deployments (Optional)](#7-setup-automated-deployments-optional)
- [Configuration](#configuration)
- [Maintenance](#maintenance)
- [Troubleshooting](#troubleshooting)

## Prerequisites

- Ubuntu 22.04 LTS or later (or equivalent Debian-based distribution)
- Physical server, virtual machine (VM), or cloud instance
- Root or sudo access
- Domain name configured (example: `photohonor.coronasda.church`)
- Cloudflare account (if using Cloudflare for DNS and SSL/CDN)
- (Optional) Cloudflare Tunnel (cloudflared) if already running - see Cloudflare configuration section
- Google OAuth credentials (see [SETUP.md](SETUP.md#google-oauth20-setup))

## System Requirements

### Minimum
- CPU: 2 cores
- RAM: 4 GB
- Disk: 20 GB free space
- Network: 1 Gbps

### Recommended
- CPU: 4 cores
- RAM: 8 GB
- Disk: 50 GB free space (more if storing many photos)
- Network: 1 Gbps

## Installation Steps

### 1. Install PostgreSQL

PostgreSQL 16 is the recommended version.

```bash
# Update system packages
sudo apt update && sudo apt upgrade -y

# Install PostgreSQL
sudo apt install -y postgresql-16 postgresql-contrib-16

# Start and enable PostgreSQL
sudo systemctl start postgresql
sudo systemctl enable postgresql

# Verify installation
sudo systemctl status postgresql
```

#### Install Cockpit (System Management Interface)

Cockpit provides a web-based management interface for your server, making it easier to monitor system resources, manage services, and perform administrative tasks.

```bash
# Install Cockpit
sudo apt install -y cockpit

# Start and enable Cockpit
sudo systemctl start cockpit
sudo systemctl enable cockpit

# Enable root login (comment out root from disallowed users)
sudo sed -i 's/^root$/#root/' /etc/cockpit/disallowed-users

# Verify installation
sudo systemctl status cockpit
```

**Note**: By default, Cockpit disables root login for security. The configuration above enables root access by commenting out the root user from `/etc/cockpit/disallowed-users`. If you prefer to keep root disabled, skip the `sed` command and create a separate admin user instead.

Cockpit will be accessible at `https://your-server-ip:9090`. To access it securely, you can configure Nginx as a reverse proxy or use SSH tunneling:

```bash
# SSH tunnel to access Cockpit securely
ssh -L 9090:localhost:9090 user@your-server-ip
# Then access http://localhost:9090 from your local browser
```

**Troubleshooting: Cockpit Updates Page Error**

If you encounter "Cannot refresh cache whilst offline" error on the Cockpit updates page, this is a known PackageKit issue on some systems where it requires a network interface with a gateway. Workaround:

```bash
# Create a dummy network interface with gateway
sudo nmcli con add type dummy con-name fake ifname fake0 ip4 1.2.3.4/24 gw4 1.2.3.1
```

Alternatively, you can manage updates via command line instead:
```bash
sudo apt update && sudo apt upgrade
```

#### Install PGAdmin 4 (PostgreSQL Management Tool)

PGAdmin 4 provides a web-based interface for managing PostgreSQL databases.

```bash
# Add PGAdmin repository GPG key
curl -fsSL https://www.pgadmin.org/static/packages_pgadmin_org.pub | sudo gpg --dearmor -o /usr/share/keyrings/packages-pgadmin-org.gpg

# Add PGAdmin repository
sudo sh -c 'echo "deb [signed-by=/usr/share/keyrings/packages-pgadmin-org.gpg] https://ftp.postgresql.org/pub/pgadmin/pgadmin4/apt/$(lsb_release -cs) pgadmin4 main" > /etc/apt/sources.list.d/pgadmin4.list'

# Update package list
sudo apt update

# Install PGAdmin 4 (web mode only)
sudo apt install -y pgadmin4-web

# Configure PGAdmin 4
sudo /usr/pgadmin4/bin/setup-web.sh
```

During setup, you'll be prompted to:
1. Create an initial PGAdmin user email and password
2. Configure the web server (select option to use Apache or standalone mode)

PGAdmin 4 will be accessible at `http://your-server-ip/pgadmin4`. To access it securely:

**Option A: Configure Nginx reverse proxy (recommended for production)**

Add to your Nginx configuration:

```bash
sudo nano /etc/nginx/sites-available/pgadmin4
```

Add the following content:

```nginx
server {
    listen 80;
    server_name pgadmin.photohonor.coronasda.church;

    location / {
        proxy_pass http://localhost:80/pgadmin4/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        proxy_cache_bypass $http_upgrade;
    }
}
```

Enable the site and secure with SSL:

```bash
sudo ln -s /etc/nginx/sites-available/pgadmin4 /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
sudo certbot --nginx -d pgadmin.photohonor.coronasda.church
```

**Option B: SSH tunnel (for temporary access)**

```bash
# SSH tunnel to access PGAdmin 4 securely
ssh -L 8080:localhost:80 user@your-server-ip
# Then access http://localhost:8080/pgadmin4 from your local browser
```

**Security Note**: PGAdmin 4 is a powerful database management tool. Ensure it's properly secured with strong passwords and only accessible by authorized administrators. Consider restricting access via firewall rules or VPN.

#### Update Login Message (Optional)

To display service URLs in the login message, update the `/etc/profile.d/00_lxc-details.sh` file (or create it if it doesn't exist):

```bash
sudo nano /etc/profile.d/00_lxc-details.sh
```

Add the following content to display service information on login:

```bash
echo -e ""
echo -e "Pathfinder Photography Server"
echo -e "    🏠   Hostname: $(hostname)"
echo -e "    💡   IP Address: $(hostname -I | awk '{print $1}')"
echo -e ""
echo -e "Available Services:"
echo -e "    🖥️   Cockpit (System Management):"
echo -e "        - Local: https://10.10.10.200:9090"
echo -e "        - Public: https://photohonor.coronasda.church (via Cloudflare Tunnel)"
echo -e "    🗄️   PGAdmin 4 (Database Management):"
echo -e "        - Local: http://10.10.10.200/pgadmin4"
echo -e "        - Public: https://pgadmin.photohonor.coronasda.church"
echo -e "    📊   SigNoz (Observability):"
echo -e "        - Local: http://10.10.10.200:3301"
echo -e "        - Public: https://signoz.photohonor.coronasda.church"
echo -e "    🌐   Pathfinder Photography App:"
echo -e "        - Local: http://10.10.10.200:5000"
echo -e "        - Public: https://photohonor.coronasda.church"
echo -e ""
```

**Note**: 
- Replace `10.10.10.200` with your actual local network IP address
- Comment out or remove the SigNoz section if you don't install SigNoz (Section 6)
- Local URLs use HTTP and specific ports; public URLs use HTTPS via Cloudflare Tunnel

Make the script executable:

```bash
sudo chmod +x /etc/profile.d/00_lxc-details.sh
```

The login message will display on your next SSH login, showing quick access URLs for all services.

#### Configure PostgreSQL

**Security Note**: Generate a strong random password instead of using a weak one:

```bash
# Generate a secure random password (save this - you'll need it later)
openssl rand -base64 32

# Switch to postgres user
sudo -u postgres psql
```

Create the database and user with your generated password:

```sql
# Create database and user
CREATE DATABASE pathfinder_photography;
CREATE USER pathfinder WITH PASSWORD 'paste_your_generated_password_here';
GRANT ALL PRIVILEGES ON DATABASE pathfinder_photography TO pathfinder;

# Grant schema permissions
\c pathfinder_photography
GRANT ALL ON SCHEMA public TO pathfinder;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO pathfinder;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO pathfinder;

# Exit PostgreSQL
\q
```

**Important**: Save the generated password securely - you'll need it for the application configuration.

#### Secure PostgreSQL (Production)

Edit PostgreSQL configuration to only listen on localhost (if app is on same server):

```bash
sudo nano /etc/postgresql/16/main/postgresql.conf
```

Ensure this line exists:
```
listen_addresses = 'localhost'
```

Edit `pg_hba.conf` for authentication:
```bash
sudo nano /etc/postgresql/16/main/pg_hba.conf
```

Add this line for the pathfinder user:
```
local   pathfinder_photography    pathfinder                            scram-sha-256
```

Restart PostgreSQL:
```bash
sudo systemctl restart postgresql
```

### 2. Install .NET Runtime and SDK

Install .NET 9.0 Runtime (required for running the application) and .NET SDK (required for building and database migrations):

```bash
# Add Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Update package list
sudo apt update

# Install .NET 9.0 SDK (includes runtime and ASP.NET Core)
sudo apt install -y dotnet-sdk-9.0

# Verify installation
dotnet --version
dotnet --list-runtimes
dotnet --list-sdks
```

Expected output from `dotnet --list-runtimes` should include:
```
Microsoft.AspNetCore.App 9.0.x
Microsoft.NETCore.App 9.0.x
```

Expected output from `dotnet --list-sdks` should include:
```
9.0.xxx [/usr/share/dotnet/sdk]
```

**Note**: The .NET SDK is required for:
- Building the application from source (`dotnet publish`)
- Running Entity Framework migrations (`dotnet ef database update`)
- Development and debugging on the server

If you're only deploying pre-built releases and don't need to run migrations manually, you can install just the runtime:

```bash
# Alternative: Runtime only (minimal installation)
sudo apt install -y aspnetcore-runtime-9.0
```

However, installing the SDK is **recommended** for production servers to enable:
- Quick hotfixes and patches
- Database schema updates
- Troubleshooting and diagnostics

### 2.1. Install Git (Required for Building from Source)

If you plan to build the application from source (recommended), install Git:

```bash
# Install Git
sudo apt update
sudo apt install -y git

# Verify installation
git --version
```

**Expected output:**
```
git version 2.x.x
```

**Note**: Git is only required if you're building from source (Option A in Section 3). If you're deploying pre-built binaries (Option B), you can skip this step.

### 3. Install Application

#### Create Application User

```bash
# Create a system user for running the application
sudo useradd -r -m -s /bin/bash pathfinder
sudo usermod -aG www-data pathfinder
```

#### Download and Deploy Application

Option A: Build from source (recommended with .NET SDK installed):
```bash
# Clone repository
cd /tmp
git clone https://github.com/glensouza/csdac-pathfinder-25-honor-photography.git
cd csdac-pathfinder-25-honor-photography

# Build application
dotnet publish -c Release -o /opt/pathfinder-photography

# Set ownership
sudo chown -R pathfinder:pathfinder /opt/pathfinder-photography
```

Option B: Use pre-built release:
```bash
# Create application directory
sudo mkdir -p /opt/pathfinder-photography
cd /opt/pathfinder-photography

# Download latest release (replace URL with actual release)
sudo wget https://github.com/glensouza/csdac-pathfinder-25-honor-photography/releases/download/vX.X.X/pathfinder-photography.tar.gz

# Extract
sudo tar -xzf pathfinder-photography.tar.gz
sudo rm pathfinder-photography.tar.gz

# Set ownership
sudo chown -R pathfinder:pathfinder /opt/pathfinder-photography
```

#### Create Uploads Directory

```bash
# Create directory for uploaded photos
sudo mkdir -p /opt/pathfinder-photography/wwwroot/uploads
sudo chown -R pathfinder:pathfinder /opt/pathfinder-photography/wwwroot/uploads
sudo chmod 755 /opt/pathfinder-photography/wwwroot/uploads
```

#### Configure Application

Create production configuration file:

```bash
sudo nano /opt/pathfinder-photography/appsettings.Production.json
```

Add the following content:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "pathfinder-photography": "Host=localhost;Database=pathfinder_photography;Username=pathfinder;Password=your_secure_password_here",
    "DefaultConnection": "Host=localhost;Database=pathfinder_photography;Username=pathfinder;Password=your_secure_password_here"
  },
  "Authentication": {
    "Google": {
      "ClientId": "your_google_client_id_here.apps.googleusercontent.com",
      "ClientSecret": "your_google_client_secret_here"
    }
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-16-character-app-password",
    "UseSsl": "true",
    "FromAddress": "your-email@gmail.com",
    "FromName": "Pathfinder Photography"
  }
}
```

**Important**: Set proper permissions:
```bash
sudo chmod 600 /opt/pathfinder-photography/appsettings.Production.json
sudo chown pathfinder:pathfinder /opt/pathfinder-photography/appsettings.Production.json
```

#### Apply Database Migrations

```bash
# Switch to pathfinder user
sudo -u pathfinder bash
cd /opt/pathfinder-photography

# Set environment
export ASPNETCORE_ENVIRONMENT=Production

# Apply migrations
dotnet ef database update

# Exit pathfinder user
exit
```

**Note**: Database migrations will also run automatically on first application startup if not applied manually.

### 4. Configure Systemd Service

Create a systemd service file to manage the application:

```bash
sudo nano /etc/systemd/system/pathfinder-photography.service
```

Add the following content:

```ini
[Unit]
Description=Pathfinder Photography Application
After=network.target postgresql.service
Wants=postgresql.service

[Service]
Type=notify
User=pathfinder
Group=pathfinder
WorkingDirectory=/opt/pathfinder-photography
ExecStart=/usr/bin/dotnet /opt/pathfinder-photography/PathfinderPhotography.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=pathfinder-photography
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://localhost:5000
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

# Security settings
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
ReadWritePaths=/opt/pathfinder-photography/wwwroot/uploads

# Resource limits
LimitNOFILE=65536
TasksMax=4096

[Install]
WantedBy=multi-user.target
```

**Note**: If you're using SigNoz (see section 6), you'll need to add these environment variables to the service file:
```ini
Environment=OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
Environment=OTEL_RESOURCE_ATTRIBUTES=service.name=pathfinder-photography
```

Enable and start the service:

```bash
# Reload systemd to recognize new service
sudo systemctl daemon-reload

# Enable service to start on boot
sudo systemctl enable pathfinder-photography

# Start the service
sudo systemctl start pathfinder-photography

# Check status
sudo systemctl status pathfinder-photography

# View logs
sudo journalctl -u pathfinder-photography -f
```

### 5. Install Nginx Reverse Proxy

Install Nginx to serve the application over HTTP/HTTPS:

```bash
# Install Nginx
sudo apt install -y nginx

# Create Nginx configuration
sudo nano /etc/nginx/sites-available/pathfinder-photography
```

Add the following configuration:

```nginx
# HTTP server - redirects to HTTPS
server {
    listen 80;
    listen [::]:80;
    server_name photohonor.coronasda.church www.photohonor.coronasda.church;

    # Redirect all HTTP to HTTPS
    return 301 https://$server_name$request_uri;
}

# HTTPS server
server {
    listen 443 ssl http2;
    listen [::]:443 ssl http2;
    server_name photohonor.coronasda.church www.photohonor.coronasda.church;

    # SSL configuration (will be managed by Certbot or Cloudflare)
    # If using Cloudflare, SSL certificates are managed by Cloudflare
    # If using Let's Encrypt directly:
    # ssl_certificate /etc/letsencrypt/live/photohonor.coronasda.church/fullchain.pem;
    # ssl_certificate_key /etc/letsencrypt/live/photohonor.coronasda.church/privkey.pem;
    # include /etc/letsencrypt/options-ssl-nginx.conf;
    # ssl_dhparam /etc/letsencrypt/ssl-dhparams.pem;

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

    # Optional: serve static files directly (better performance)
    location /uploads/ {
        alias /opt/pathfinder-photography/wwwroot/uploads/;
        expires 1y;
        access_log off;
    }

    # Access and error logs
    access_log /var/log/nginx/pathfinder-photography-access.log;
    error_log /var/log/nginx/pathfinder-photography-error.log;
}
```

Enable the site and reload Nginx:

```bash
# Enable site
sudo ln -s /etc/nginx/sites-available/pathfinder-photography /etc/nginx/sites-enabled/

# Test Nginx configuration
sudo nginx -t

# Reload Nginx
sudo systemctl reload nginx
```

#### Install SSL Certificate with Let's Encrypt

**Note**: If you're using Cloudflare for DNS and SSL management (recommended for `photohonor.coronasda.church`), Cloudflare can handle SSL certificates automatically. In that case, you can skip this section and configure Cloudflare SSL settings in your Cloudflare dashboard.

**If NOT using Cloudflare SSL** (using Let's Encrypt directly):

```bash
# Install Certbot
sudo apt install -y certbot python3-certbot-nginx

# Obtain and install certificate
sudo certbot --nginx -d photohonor.coronasda.church -d www.photohonor.coronasda.church

# Test automatic renewal
sudo certbot renew --dry-run
```

Certbot will automatically:
- Obtain SSL certificate
- Update Nginx configuration
- Set up automatic renewal

#### Configure Cloudflare (Optional)

If you're using Cloudflare for DNS management (recommended for `photohonor.coronasda.church`):

**Option A: Using Cloudflare Tunnel (cloudflared) - If Already Running**

If you already have a cloudflared container running (Cloudflare Tunnel), you only need to add this service to your existing tunnel configuration:

1. **Add the service to your cloudflared configuration:**
   - Edit your cloudflared config file (typically in your container or `/etc/cloudflared/config.yml`)
   - Add the following ingress rule:
   ```yaml
   ingress:
     - hostname: photohonor.coronasda.church
       service: http://localhost:5000
     - hostname: pgadmin.photohonor.coronasda.church
       service: http://localhost:80/pgadmin4
     - hostname: signoz.photohonor.coronasda.church  # Optional: if using SigNoz for observability
       service: http://localhost:3301
     # ... your other services ...
     - service: http_status:404  # catch-all rule
   ```

2. **Restart your cloudflared container** to apply the changes

3. **Configure DNS in Cloudflare Dashboard:**
   - The DNS records should already be created automatically by cloudflared
   - If not, add CNAME records pointing to your tunnel subdomain
   - Proxy status should be "DNS only" (gray cloud) when using Cloudflare Tunnel

**Benefits of Cloudflare Tunnel:**
- ✅ No need to expose ports publicly
- ✅ Automatic SSL/TLS certificates
- ✅ DDoS protection
- ✅ No need for port forwarding or firewall configuration
- ✅ Access from anywhere without VPN

**Option B: Direct Connection (Standard Cloudflare Proxy)**

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

**3. Configure Cloudflare SSL Certificate (Optional):**
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
- Update Nginx configuration to use these certificates instead of Let's Encrypt

**4. Enable Cloudflare Features (Optional):**
- Under Speed → Optimization: Enable Auto Minify (JS, CSS, HTML)
- Under Security → Settings: Set Security Level to Medium
- Under Firewall: Configure rules as needed

**Benefits of Using Cloudflare:**
- ✅ Free SSL/TLS certificates
- ✅ DDoS protection
- ✅ CDN for faster content delivery
- ✅ Automatic HTTPS rewrites
- ✅ Web Application Firewall (WAF)

#### Configure Firewall

**Important**: Configure SSH access first to avoid being locked out:

```bash
# CRITICAL: Allow SSH first to prevent lockout
sudo ufw allow 22/tcp comment 'SSH access'

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

### 6. Install SigNoz (Optional)

SigNoz provides observability (traces, metrics, logs). Install it only if you need application monitoring.

**Native Linux Installation** (no Docker required):

**Prerequisites:**
- Ubuntu 22.04 LTS or later
- 4GB RAM minimum (8GB recommended)
- 20GB disk space

**Install SigNoz:**

```bash
# Create installation directory
sudo mkdir -p /opt/signoz
cd /opt/signoz

# Download the installation script
curl -sL https://github.com/SigNoz/signoz/raw/main/deploy/install-linux.sh -o install-linux.sh

# Make it executable
chmod +x install-linux.sh

# Run the installation
sudo ./install-linux.sh
```

The script will:
- Install all required dependencies (ClickHouse, OTEL Collector, Query Service)
- Set up systemd services for automatic startup
- Configure OTLP collector on port 4317
- Start SigNoz UI on port 3301

**Manage SigNoz Services:**

```bash
# Check status of all SigNoz services
sudo systemctl status signoz-otel-collector
sudo systemctl status signoz-query-service
sudo systemctl status clickhouse-server

# Start/Stop/Restart services
sudo systemctl start signoz-otel-collector
sudo systemctl stop signoz-otel-collector
sudo systemctl restart signoz-otel-collector

# Enable services to start on boot (should be done by installer)
sudo systemctl enable signoz-otel-collector
sudo systemctl enable signoz-query-service
sudo systemctl enable clickhouse-server
```

**Benefits of Native Installation:**
- ✅ No Docker overhead
- ✅ Better performance on bare metal
- ✅ Simpler service management via systemd
- ✅ Lower resource consumption
- ✅ Easier troubleshooting with standard Linux tools

#### Configure Application to Use SigNoz

Update the systemd service file to include SigNoz configuration:

```bash
sudo nano /etc/systemd/system/pathfinder-photography.service
```

Add these environment variables to the `[Service]` section:

```ini
Environment=OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
Environment=OTEL_RESOURCE_ATTRIBUTES=service.name=pathfinder-photography
```

Restart the application:

```bash
sudo systemctl daemon-reload
sudo systemctl restart pathfinder-photography
```

#### Access SigNoz UI

SigNoz UI is available at `http://your-server-ip:3301`. To expose it securely, create an Nginx configuration:

```bash
sudo nano /etc/nginx/sites-available/signoz
```

Add:

```nginx
server {
    listen 80;
    server_name signoz.photohonor.coronasda.church;

    location / {
        proxy_pass http://localhost:3301;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection 'upgrade';
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

Enable and secure with SSL:

```bash
sudo ln -s /etc/nginx/sites-available/signoz /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
sudo certbot --nginx -d signoz.photohonor.coronasda.church
```

### 7. Setup Automated Deployments (Optional)

For automatic deployments on every code push to `main` branch, you can set up a self-hosted GitHub Actions runner on your server.

**Benefits:**
- ✅ Automatic deployment on every push to `main`
- ✅ Automated build and testing
- ✅ Automatic rollback on deployment failures
- ✅ Backup creation before each deployment
- ✅ Health checks and verification

#### Overview

The self-hosted GitHub runner will:
- Run on your deployment server
- Listen for workflow events from your GitHub repository
- Execute the deployment workflow automatically when code is pushed to `main` branch
- Deploy the .NET application to `/opt/pathfinder-photography`
- Manage service restarts and health checks
- Perform automatic rollbacks on deployment failures

**Deployment Workflow**: The automation is defined in `.github/workflows/deploy-bare-metal.yml` in the repository. This workflow handles building, testing, deploying, and verifying the application with built-in security features.

#### Prerequisites for Runner Setup

- Application already installed following steps 1-5 above
- GitHub repository admin access
- At least 2GB free disk space for runner

#### 7.1. Create Runner User

Create a dedicated user for running the GitHub Actions runner:

```bash
# Create user with no login shell for security
sudo useradd -r -m -s /bin/bash github-runner

# Add to necessary groups (NOT sudo - privileges granted via sudoers file only)
sudo usermod -aG pathfinder github-runner
sudo usermod -aG www-data github-runner
```

**Security Note**: The `github-runner` user is intentionally NOT added to the `sudo` group. All required privileges are explicitly granted through the sudoers file in step 7.2, following the principle of least privilege.

#### 7.2. Configure Passwordless Sudo

Create a dedicated sudoers configuration file for the runner user:

```bash
# As root, create the sudoers file
sudo visudo -f /etc/sudoers.d/github-runner
```

Add the following content to `/etc/sudoers.d/github-runner`:

```sudoers
# GitHub Actions Runner - Passwordless sudo configuration
# Security: This grants ONLY the minimum privileges needed for deployment
# The runner cannot execute arbitrary commands or obtain a root shell

# System service management - pathfinder-photography service only
github-runner ALL=(ALL) NOPASSWD: /usr/bin/systemctl start pathfinder-photography
github-runner ALL=(ALL) NOPASSWD: /usr/bin/systemctl stop pathfinder-photography
github-runner ALL=(ALL) NOPASSWD: /usr/bin/systemctl restart pathfinder-photography
github-runner ALL=(ALL) NOPASSWD: /usr/bin/systemctl reload pathfinder-photography
github-runner ALL=(ALL) NOPASSWD: /usr/bin/systemctl status pathfinder-photography
github-runner ALL=(ALL) NOPASSWD: /usr/bin/systemctl is-active pathfinder-photography

# Nginx management - reload and test only (no stop/start for security)
github-runner ALL=(ALL) NOPASSWD: /usr/bin/systemctl reload nginx
github-runner ALL=(ALL) NOPASSWD: /usr/bin/systemctl status nginx
github-runner ALL=(ALL) NOPASSWD: /usr/sbin/nginx -t

# Directory creation - restricted to specific paths
github-runner ALL=(ALL) NOPASSWD: /usr/bin/mkdir -p /opt/pathfinder-photography
github-runner ALL=(ALL) NOPASSWD: /usr/bin/mkdir -p /opt/backups/pathfinder-photography/deployments
github-runner ALL=(ALL) NOPASSWD: /usr/bin/mkdir -p /opt/backups/pathfinder-photography/uploads

# Backup operations - highly restricted paths
github-runner ALL=(ALL) NOPASSWD: /usr/bin/tar -czf /opt/backups/pathfinder-photography/deployments/backup_[0-9]*.tar.gz -C /opt/pathfinder-photography .
github-runner ALL=(ALL) NOPASSWD: /usr/bin/tar -czf /opt/backups/pathfinder-photography/deployments/backup_[0-9]*.tar.gz -C /opt/pathfinder-photography *

# Deployment extraction - only from current directory to deployment dir
github-runner ALL=(ALL) NOPASSWD: /usr/bin/tar -xzf pathfinder-photography-[0-9a-f]*.tar.gz -C /opt/pathfinder-photography

# File ownership - restricted to deployment paths only
github-runner ALL=(ALL) NOPASSWD: /usr/bin/chown -R pathfinder\:pathfinder /opt/pathfinder-photography
github-runner ALL=(ALL) NOPASSWD: /usr/bin/chown pathfinder\:pathfinder /opt/backups/pathfinder-photography/deployments/backup_[0-9]*.tar.gz

# File permissions - specific modes only for security
github-runner ALL=(ALL) NOPASSWD: /usr/bin/chmod -R 755 /opt/pathfinder-photography
github-runner ALL=(ALL) NOPASSWD: /usr/bin/chmod 755 /opt/pathfinder-photography/wwwroot/uploads

# Rsync for preserving uploads
github-runner ALL=(ALL) NOPASSWD: /usr/bin/rsync -av /opt/backups/pathfinder-photography/uploads/ /opt/pathfinder-photography/wwwroot/uploads/

# Log viewing - restricted to pathfinder-photography service only
github-runner ALL=(ALL) NOPASSWD: /usr/bin/journalctl -u pathfinder-photography *

# Backup cleanup - restricted to backup directory with date pattern
github-runner ALL=(ALL) NOPASSWD: /usr/bin/find /opt/backups/pathfinder-photography/deployments -name backup_[0-9]*.tar.gz -type f -printf *
github-runner ALL=(ALL) NOPASSWD: /usr/bin/rm -f /opt/backups/pathfinder-photography/deployments/backup_[0-9]*.tar.gz
```

**Security Note**: This configuration follows the principle of least privilege:
- ✅ Only specific commands are allowed - no wildcard command execution
- ✅ Paths are restricted to deployment directories only
- ✅ File patterns use `[0-9]*` for timestamps and `[0-9a-f]*` for SHA hashes to prevent path traversal
- ✅ No ability to obtain a root shell or execute arbitrary commands
- ✅ Nginx can only be reloaded (not stopped), preventing service disruption
- ✅ All operations are scoped to the pathfinder-photography application only

Validate the sudoers configuration:

```bash
# Check syntax
sudo visudo -c -f /etc/sudoers.d/github-runner

# Set proper permissions
sudo chmod 0440 /etc/sudoers.d/github-runner
```

#### 7.3. Download and Configure GitHub Runner

Switch to the runner user:
```bash
sudo -u github-runner bash
cd ~
```

Create runner directory:
```bash
mkdir actions-runner && cd actions-runner
```

Download the latest runner package:
```bash
# Get the latest runner version automatically (requires jq)
sudo apt install -y jq
RUNNER_VERSION=$(curl -s https://api.github.com/repos/actions/runner/releases/latest | jq -r '.tag_name' | sed 's/^v//')

# Or manually set version (check https://github.com/actions/runner/releases for latest)
# RUNNER_VERSION="2.311.0"

echo "Installing GitHub Actions Runner version: $RUNNER_VERSION"

# Download runner
curl -o actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz -L https://github.com/actions/runner/releases/download/v${RUNNER_VERSION}/actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz

# Optional: Verify the download with checksum
# Get the expected checksum from: https://github.com/actions/runner/releases/latest
# echo "EXPECTED_SHA256  actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz" | shasum -a 256 -c

# Extract
tar xzf ./actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz
```

#### 7.4. Get Repository Token

You need to get a registration token from GitHub:

**Option A: Using GitHub Web UI (Recommended)**

1. Go to your repository on GitHub
2. Click **Settings** → **Actions** → **Runners**
3. Click **New self-hosted runner**
4. Select **Linux** as the operating system
5. Copy the token from the configuration command

**Option B: Using GitHub CLI**

```bash
# Install GitHub CLI if not already installed
sudo apt install gh

# Authenticate
gh auth login

# Get registration token
gh api -X POST repos/{owner}/{repo}/actions/runners/registration-token | jq -r .token
```

#### 7.5. Configure the Runner

Run the configuration script with the token from step 7.4:

```bash
./config.sh --url https://github.com/glensouza/csdac-pathfinder-25-honor-photography --token YOUR_REGISTRATION_TOKEN --name production-server --labels self-hosted,linux,bare-metal,production --work _work
```

Parameters explained:
- `--url`: Your repository URL
- `--token`: Registration token from GitHub
- `--name`: A descriptive name for your runner (e.g., "production-server")
- `--labels`: Labels to identify this runner (used in workflow: `runs-on: self-hosted`)
- `--work`: Working directory for the runner

When prompted:
- Runner group: Press Enter (default)
- Runner name: Press Enter (or provide custom name)
- Runner labels: Press Enter (or add custom labels)
- Work folder: Press Enter (default: `_work`)

#### 7.6. Create Systemd Service for Runner

Exit back to your regular user:
```bash
exit  # Exit github-runner user
```

Create systemd service file:
```bash
sudo nano /etc/systemd/system/github-runner.service
```

Add the following content:

```ini
[Unit]
Description=GitHub Actions Runner
After=network.target

[Service]
Type=simple
User=github-runner
Group=github-runner
WorkingDirectory=/home/github-runner/actions-runner
ExecStart=/home/github-runner/actions-runner/run.sh
Restart=always
RestartSec=10
KillMode=process
KillSignal=SIGTERM
TimeoutStopSec=5min

# Environment variables
Environment="RUNNER_ALLOW_RUNASROOT=0"
Environment="DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1"

# Security settings
# Note: NoNewPrivileges is not set here because the runner needs to use sudo
# for deployment operations (systemctl, chown, chmod, etc.). The sudoers file
# restricts which commands can be run with sudo (see step 7.2).
PrivateTmp=true

# Resource limits
LimitNOFILE=65536
TasksMax=4096

[Install]
WantedBy=multi-user.target
```

#### 7.7. Start the Runner Service

```bash
# Reload systemd
sudo systemctl daemon-reload

# Enable service to start on boot
sudo systemctl enable github-runner

# Start the service
sudo systemctl start github-runner

# Check status
sudo systemctl status github-runner
```

Verify the runner is online:
- Go to your GitHub repository
- Navigate to **Settings** → **Actions** → **Runners**
- You should see your runner listed with a green "Idle" status

#### 7.8. Create Backup Directory Structure

```bash
# Create backup directories
sudo mkdir -p /opt/backups/pathfinder-photography/deployments
sudo mkdir -p /opt/backups/pathfinder-photography/uploads

# Set ownership to github-runner so backups can be created without sudo
sudo chown -R github-runner:github-runner /opt/backups/pathfinder-photography

# Set permissions
sudo chmod -R 755 /opt/backups/pathfinder-photography
```

**Important**: The backup directories must be owned by the `github-runner` user so that the deployment workflow can create backups without requiring sudo privileges.

#### 7.9. Configure GitHub Secrets

Configure the following secrets in your GitHub repository:

1. Go to **Settings** → **Secrets and variables** → **Actions**
2. Click **New repository secret**
3. Add the following secrets (if needed for your deployment):

| Secret Name | Description | Example |
|-------------|-------------|---------|
| `DEPLOY_SSH_KEY` | SSH key for deployment (if using remote deployment) | Private SSH key |
| `DB_PASSWORD` | Database password (if needed for migrations) | Strong password |

#### 7.10. Configure GitHub Environment Variables

Configure environment variables in GitHub repository:

1. Go to **Settings** → **Environments**
2. Create environment named `production`
3. Add environment variables:

| Variable Name | Description | Example |
|---------------|-------------|---------|
| `APP_URL` | Application URL | `https://pathfinder.yourdomain.com` |
| `DEPLOY_SERVER` | Server hostname or IP | `pathfinder-prod-01` |

#### 7.11. Workflow Customization

The deployment workflow is located at `.github/workflows/deploy-bare-metal.yml`.

Key customization points:

```yaml
env:
  DOTNET_VERSION: '9.0.x'          # .NET version
  PUBLISH_DIR: './publish'          # Build output directory
  DEPLOY_DIR: '/opt/pathfinder-photography'  # Deployment directory
  SERVICE_NAME: 'pathfinder-photography'     # Systemd service name
```

To modify deployment behavior:
1. Edit `.github/workflows/deploy-bare-metal.yml`
2. Commit and push changes
3. The workflow will automatically use the new configuration

#### Runner Security Considerations

**Isolation**: The runner runs as a dedicated user (`github-runner`) with limited sudo privileges.

**Sudo Access**: Only specific commands are allowed via sudoers configuration. Never grant full sudo access.

**Network**: Consider using firewall rules to restrict runner network access:

```bash
# Allow only necessary outbound connections
sudo ufw allow out to any port 443 proto tcp comment 'GitHub HTTPS'
sudo ufw allow out to any port 80 proto tcp comment 'HTTP for packages'
```

**Repository Security**

**Branch Protection**: Enable branch protection for `main` branch:
1. Go to **Settings** → **Branches**
2. Add rule for `main` branch
3. Enable:
   - Require pull request reviews before merging
   - Require status checks to pass before merging
   - Require branches to be up to date before merging

**Workflow Permissions**: Limit workflow permissions:
1. Go to **Settings** → **Actions** → **General**
2. Under "Workflow permissions", select "Read repository contents and packages permissions"
3. Enable "Allow GitHub Actions to create and approve pull requests" only if needed

**Secret Management**: Never commit secrets to the repository. Always use GitHub Secrets or environment files with proper permissions.

**Audit Logging**: Enable audit logging for the runner:

```bash
# Create log directory
sudo mkdir -p /var/log/github-runner
sudo chown github-runner:github-runner /var/log/github-runner

# Modify systemd service to include logging
sudo nano /etc/systemd/system/github-runner.service
```

Add to `[Service]` section:
```ini
StandardOutput=append:/var/log/github-runner/runner.log
StandardError=append:/var/log/github-runner/runner-error.log
```

Reload and restart:
```bash
sudo systemctl daemon-reload
sudo systemctl restart github-runner
```

#### Runner Monitoring and Maintenance

**Monitor Runner Status**

Check runner status in real-time:

```bash
# Check service status
sudo systemctl status github-runner

# View logs
sudo journalctl -u github-runner -f

# View runner-specific logs
tail -f /var/log/github-runner/runner.log
```

Check runner status on GitHub:
- Go to **Settings** → **Actions** → **Runners**
- Runner should show "Idle" (green) when waiting for jobs
- Runner shows "Active" (yellow) when executing a job

**Update Runner**

GitHub occasionally releases runner updates:

```bash
# Stop the runner
sudo systemctl stop github-runner

# Switch to runner user
sudo -u github-runner bash
cd ~/actions-runner

# Download new version (check GitHub releases for latest version)
RUNNER_VERSION="2.312.0"  # Update this
curl -o actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz -L https://github.com/actions/runner/releases/download/v${RUNNER_VERSION}/actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz

# Extract (this will update binaries)
tar xzf ./actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz

# Exit back to regular user
exit

# Restart the runner
sudo systemctl start github-runner
```

**Cleanup Old Deployments**

Old deployment backups should be cleaned up periodically:

```bash
# Manual cleanup (keeps last 10 backups)
sudo find /opt/backups/pathfinder-photography/deployments -name "backup_*.tar.gz" -type f | sort -r | tail -n +11 | xargs -r sudo rm

# Automated cleanup (add to crontab)
sudo crontab -e
```

Add:
```cron
# Clean old deployment backups weekly (keeps last 10)
0 3 * * 0 find /opt/backups/pathfinder-photography/deployments -name "backup_*.tar.gz" -type f | sort -r | tail -n +11 | xargs -r rm
```

**Monitor Disk Space**

Runner work directory can grow over time:

```bash
# Check disk usage
du -sh /home/github-runner/actions-runner/_work

# Clean old workflow artifacts (done automatically by runner)
# But you can manually clean if needed:
sudo -u github-runner bash
cd ~/actions-runner/_work
rm -rf */  # Warning: only do this when no jobs are running
exit
```

**Health Checks**

Create a monitoring script:

```bash
sudo nano /opt/scripts/check-runner-health.sh
```

Add:
```bash
#!/bin/bash

# Check if runner service is running
if ! systemctl is-active --quiet github-runner; then
    echo "ERROR: GitHub runner service is not running"
    systemctl start github-runner
    exit 1
fi

# Check if runner is registered on GitHub
RUNNER_STATUS=$(curl -s -H "Authorization: token YOUR_GITHUB_PAT" \
    "https://api.github.com/repos/glensouza/csdac-pathfinder-25-honor-photography/actions/runners" \
    | jq -r '.runners[] | select(.name=="production-server") | .status')

if [ "$RUNNER_STATUS" != "online" ]; then
    echo "WARNING: Runner is not online on GitHub"
    echo "Current status: $RUNNER_STATUS"
fi

echo "GitHub runner is healthy"
```

Make executable and schedule:
```bash
sudo chmod +x /opt/scripts/check-runner-health.sh
sudo crontab -e
```

Add:
```cron
# Check runner health every 5 minutes
*/5 * * * * /opt/scripts/check-runner-health.sh >> /var/log/runner-health.log 2>&1
```

#### Runner Troubleshooting

**Runner Not Starting**

Check service status:
```bash
sudo systemctl status github-runner
sudo journalctl -u github-runner -n 50
```

Common issues:
1. **Permission denied**: Check file ownership
   ```bash
   sudo chown -R github-runner:github-runner /home/github-runner/actions-runner
   ```

2. **Token expired**: Re-register runner
   ```bash
   sudo systemctl stop github-runner
   sudo -u github-runner bash
   cd ~/actions-runner
   ./config.sh remove --token YOUR_REMOVAL_TOKEN
   ./config.sh --url https://github.com/glensouza/csdac-pathfinder-25-honor-photography --token NEW_TOKEN
   exit
   sudo systemctl start github-runner
   ```

**Deployment Failures**

Check deployment logs:
```bash
# View workflow logs on GitHub
# Go to Actions → Select failed workflow → View logs

# Check application logs on server
sudo journalctl -u pathfinder-photography -n 100
```

Common deployment issues:

1. **Insufficient disk space**:
   ```bash
   df -h
   # Clean up if needed
   sudo apt clean
   docker system prune -af  # If Docker is installed
   ```

2. **Permission errors during deployment**:
   ```bash
   # Fix permissions
   sudo chown -R pathfinder:pathfinder /opt/pathfinder-photography
   sudo chmod -R 755 /opt/pathfinder-photography
   ```

3. **Service won't start after deployment**:
   ```bash
   # Check service logs
   sudo journalctl -u pathfinder-photography -n 100
   
   # Check configuration
   sudo -u pathfinder dotnet /opt/pathfinder-photography/PathfinderPhotography.dll --urls http://localhost:5000
   ```

**Runner Shows Offline on GitHub**

Restart runner service:
```bash
sudo systemctl restart github-runner
sudo systemctl status github-runner
```

Check network connectivity:
```bash
# Test GitHub API access
curl -I https://api.github.com

# Test runner connectivity
sudo -u github-runner bash
cd ~/actions-runner
./run.sh  # Run in foreground to see errors
# Press Ctrl+C to stop, then exit
exit
```

Re-register if needed:
```bash
sudo systemctl stop github-runner
sudo -u github-runner bash
cd ~/actions-runner

# Remove old registration
./config.sh remove

# Get new token from GitHub and re-register
./config.sh --url https://github.com/glensouza/csdac-pathfinder-25-honor-photography --token NEW_TOKEN

exit
sudo systemctl start github-runner
```

**Workflow Not Triggering**

Check workflow file syntax:
```bash
# Install act for local testing (optional)
curl https://raw.githubusercontent.com/nektos/act/master/install.sh | sudo bash

# Test workflow locally
cd /path/to/repository
act -l  # List workflows
```

Verify trigger configuration:
- Check `.github/workflows/deploy-bare-metal.yml`
- Ensure `on.push.branches` includes your branch
- Verify no `paths-ignore` is blocking execution

Check repository settings:
- Go to **Settings** → **Actions** → **General**
- Ensure "Allow all actions and reusable workflows" is selected
- Check if workflows are enabled for the repository

**Database Migration Failures**

Manual migration:
```bash
sudo -u pathfinder bash
cd /opt/pathfinder-photography
export ASPNETCORE_ENVIRONMENT=Production

# Check pending migrations
dotnet ef migrations list

# Apply migrations
dotnet ef database update

exit
```

Rollback migration:
```bash
sudo -u pathfinder bash
cd /opt/pathfinder-photography
export ASPNETCORE_ENVIRONMENT=Production

# Rollback to specific migration
dotnet ef database update PreviousMigrationName

exit
```

#### Advanced Runner Configuration

**Multiple Runners**

For high availability or staging/production separation:

1. **Create separate runner for staging:**
   ```bash
   # On staging server
   sudo useradd -r -m -s /bin/bash github-runner-staging
   # Follow installation steps with label: staging
   ```

2. **Update workflow to use specific runner:**
   ```yaml
   deploy-staging:
     runs-on: [self-hosted, staging]
   
   deploy-production:
     runs-on: [self-hosted, production]
   ```

**Custom Deployment Scripts**

Create custom deployment scripts in `/opt/scripts/`:

```bash
sudo mkdir -p /opt/scripts
sudo nano /opt/scripts/pre-deploy.sh
```

Example pre-deployment script:
```bash
#!/bin/bash
# Pre-deployment health checks
echo "Running pre-deployment checks..."

# Check database connectivity
if ! pg_isready -h localhost -U pathfinder; then
    echo "Database is not ready"
    exit 1
fi

# Check disk space (require at least 1GB free)
FREE_SPACE=$(df /opt | tail -1 | awk '{print $4}')
if [ $FREE_SPACE -lt 1048576 ]; then
    echo "Insufficient disk space"
    exit 1
fi

echo "Pre-deployment checks passed"
```

Reference in workflow:
```yaml
- name: Pre-deployment checks
  run: sudo /opt/scripts/pre-deploy.sh
```

#### Automated Deployment Best Practices

1. **Regular Updates**: Keep the runner software updated
2. **Monitor Resources**: Set up monitoring for CPU, memory, and disk
3. **Backup Strategy**: Maintain deployment backups and database backups
4. **Security Patches**: Apply system security updates regularly
5. **Access Control**: Limit who can trigger manual deployments
6. **Logging**: Maintain deployment logs for audit trail
7. **Testing**: Test deployments in staging before production
8. **Documentation**: Keep deployment procedures documented

#### Automated Deployment Summary

After setting up the runner, every push to the `main` branch will automatically:
1. Build the application
2. Run tests
3. **Verify artifact integrity** with SHA-256 checksum
4. Create a backup of the current deployment
5. Deploy the new version
6. **Set proper file ownership and permissions** for security
7. Apply database migrations
8. Restart the service
9. Verify the deployment with health checks
10. Rollback automatically if anything fails

**Security Features in Automated Deployment:**
- ✅ Artifact integrity verification prevents corrupted deployments
- ✅ Automatic backup before deployment enables safe rollback
- ✅ File ownership set to `pathfinder:pathfinder` after extraction
- ✅ Upload directory permissions set to 755 for proper access control
- ✅ Health check verification ensures application is responding
- ✅ Automatic rollback restores from backup on any failure
- ✅ All operations use sudo with restricted privileges from sudoers file

**Manual Deployment Trigger:**
You can also trigger deployments manually from GitHub:
1. Go to your repository on GitHub
2. Click **Actions** → **Deploy to Bare Metal Server**
3. Click **Run workflow** → Select environment → **Run workflow**

**Quick Reference for Runner Management:**

```bash
# Runner management
sudo systemctl start github-runner
sudo systemctl stop github-runner
sudo systemctl restart github-runner
sudo systemctl status github-runner
sudo journalctl -u github-runner -f

# View runner logs
tail -f /var/log/github-runner/runner.log
tail -f /var/log/github-runner/runner-error.log

# Check runner on GitHub
# Settings → Actions → Runners

# Manual deployment
# Go to repository → Actions → Deploy to Bare Metal Server → Run workflow

# Check backups
ls -lh /opt/backups/pathfinder-photography/deployments/

# Test deployment locally
sudo -u github-runner bash
cd ~/actions-runner
./run.sh  # Run in foreground
```

## Configuration

### Google OAuth Redirect URIs

Add these URIs to your Google Cloud Console OAuth credentials:
- `https://photohonor.coronasda.church/signin-google`
- `https://www.photohonor.coronasda.church/signin-google`

### Email Configuration

Email is optional but recommended for notifications. Configure in `appsettings.Production.json`:

```json
"Email": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "SmtpUsername": "your-email@gmail.com",
  "SmtpPassword": "your-16-character-app-password",
  "UseSsl": "true",
  "FromAddress": "your-email@gmail.com",
  "FromName": "Pathfinder Photography"
}
```

To disable email, set `SmtpHost` to empty string or remove the Email section.

### First Admin User

The first user to authenticate becomes Admin automatically. To promote additional admins:

```bash
sudo -u postgres psql pathfinder_photography
UPDATE "Users" SET "Role" = 2 WHERE "Email" = 'admin@example.com';
SELECT "Name", "Email", "Role" FROM "Users";
\q
```

Roles:
- 0 = Pathfinder
- 1 = Instructor  
- 2 = Admin

## Maintenance

### Update Application

```bash
# Stop the application
sudo systemctl stop pathfinder-photography

# Backup current version
sudo cp -r /opt/pathfinder-photography /opt/pathfinder-photography.backup

# Download and deploy new version
cd /opt/pathfinder-photography
# ... download and extract new version ...

# Apply migrations if needed
sudo -u pathfinder bash
cd /opt/pathfinder-photography
export ASPNETCORE_ENVIRONMENT=Production
dotnet ef database update
exit

# Start the application
sudo systemctl start pathfinder-photography
sudo systemctl status pathfinder-photography
```

### Backup Database

Create automated backup script:

```bash
sudo nano /opt/backups/backup-pathfinder-db.sh
```

Add:

```bash
#!/bin/bash
BACKUP_DIR="/opt/backups/pathfinder"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
mkdir -p $BACKUP_DIR

# Backup database
sudo -u postgres pg_dump pathfinder_photography | gzip > $BACKUP_DIR/db_backup_$TIMESTAMP.sql.gz

# Backup uploaded photos
tar -czf $BACKUP_DIR/uploads_backup_$TIMESTAMP.tar.gz /opt/pathfinder-photography/wwwroot/uploads/

# Keep only last 7 days of backups
find $BACKUP_DIR -name "db_backup_*.sql.gz" -mtime +7 -delete
find $BACKUP_DIR -name "uploads_backup_*.tar.gz" -mtime +7 -delete

echo "Backup completed: $TIMESTAMP"
```

Make executable and schedule:

```bash
sudo chmod +x /opt/backups/backup-pathfinder-db.sh

# Add to crontab (daily at 2 AM)
sudo crontab -e
# Add this line:
0 2 * * * /opt/backups/backup-pathfinder-db.sh >> /var/log/pathfinder-backup.log 2>&1
```

### Restore Database

```bash
# Stop the application
sudo systemctl stop pathfinder-photography

# Restore database
gunzip -c /opt/backups/pathfinder/db_backup_YYYYMMDD_HHMMSS.sql.gz | sudo -u postgres psql pathfinder_photography

# Restore uploads
sudo tar -xzf /opt/backups/pathfinder/uploads_backup_YYYYMMDD_HHMMSS.tar.gz -C /

# Start the application
sudo systemctl start pathfinder-photography
```

### Monitor Application

```bash
# View real-time logs
sudo journalctl -u pathfinder-photography -f

# View last 100 lines
sudo journalctl -u pathfinder-photography -n 100

# View logs for specific date
sudo journalctl -u pathfinder-photography --since "2024-01-01" --until "2024-01-02"

# Check application status
sudo systemctl status pathfinder-photography

# Check resource usage
htop  # or 'top'
```

### Log Rotation

Application logs are managed by systemd/journald. Configure retention:

```bash
sudo nano /etc/systemd/journald.conf
```

Set:
```ini
SystemMaxUse=500M
MaxRetentionSec=7day
```

Restart journald:
```bash
sudo systemctl restart systemd-journald
```

## Troubleshooting

### Application Won't Start

Check logs:
```bash
sudo journalctl -u pathfinder-photography -n 50
```

Common issues:
1. **Database connection failed**: Verify PostgreSQL is running and credentials are correct
2. **Port already in use**: Check if another service is using port 5000
3. **Permission denied**: Ensure pathfinder user has access to application files

### Database Connection Errors

```bash
# Verify PostgreSQL is running
sudo systemctl status postgresql

# Test database connection
sudo -u pathfinder psql -h localhost -U pathfinder -d pathfinder_photography

# Check pg_hba.conf if connection refused
sudo nano /etc/postgresql/16/main/pg_hba.conf
```

### High Memory Usage

```bash
# Check memory usage
free -h

# Optimize PostgreSQL memory settings
sudo nano /etc/postgresql/16/main/postgresql.conf

# Adjust these based on available RAM:
shared_buffers = 256MB          # 25% of RAM
effective_cache_size = 1GB      # 50-75% of RAM
work_mem = 16MB
maintenance_work_mem = 128MB

# Restart PostgreSQL
sudo systemctl restart postgresql
```

### SSL Certificate Issues

```bash
# Renew certificate manually
sudo certbot renew

# Check certificate status
sudo certbot certificates

# Test automatic renewal
sudo certbot renew --dry-run
```

### Photo Upload Failures

Check disk space:
```bash
df -h
```

Check directory permissions:
```bash
ls -la /opt/pathfinder-photography/wwwroot/uploads/
# Should be owned by pathfinder:pathfinder with 755 permissions
```

Increase upload size limit in Nginx if needed:
```bash
sudo nano /etc/nginx/sites-available/pathfinder-photography
# Adjust: client_max_body_size 20M;
sudo systemctl reload nginx
```

### SigNoz Not Receiving Telemetry

Check SigNoz services:
```bash
# Check all SigNoz services status
sudo systemctl status signoz-otel-collector
sudo systemctl status signoz-query-service
sudo systemctl status clickhouse-server

# Check collector logs
sudo journalctl -u signoz-otel-collector -n 50

# Verify endpoint in application service
sudo systemctl cat pathfinder-photography | grep OTEL_EXPORTER
```

### Performance Issues

Check system resources:
```bash
# CPU and memory
htop

# Disk I/O
iostat -x 1

# Network
iftop
```

Optimize .NET application:
```bash
# Add to systemd service file
Environment=DOTNET_gcServer=1
Environment=DOTNET_GCDynamicAdaptationMode=1

sudo systemctl daemon-reload
sudo systemctl restart pathfinder-photography
```

## Security Best Practices

1. **Keep system updated**:
   ```bash
   sudo apt update && sudo apt upgrade -y
   ```

2. **Use strong passwords** for database and application

3. **Enable firewall** and only open necessary ports

4. **Regular backups** - automate database and uploads backups

5. **Monitor logs** for suspicious activity:
   ```bash
   sudo journalctl -u pathfinder-photography | grep -i error
   ```

6. **Use HTTPS only** - redirect all HTTP to HTTPS

7. **Limit SSH access**:
   ```bash
   # Edit SSH config
   sudo nano /etc/ssh/sshd_config
   # Set: PermitRootLogin no
   # Set: PasswordAuthentication no  (use SSH keys)
   sudo systemctl restart sshd
   ```

8. **Keep .NET SDK and runtime updated**:
   ```bash
   sudo apt update
   sudo apt install --only-upgrade dotnet-sdk-9.0
   ```

## Performance Tuning

### PostgreSQL Optimization

For better performance with moderate workload:

```bash
sudo nano /etc/postgresql/16/main/postgresql.conf
```

Recommended settings for 8GB RAM server:
```ini
# Memory
shared_buffers = 2GB
effective_cache_size = 6GB
maintenance_work_mem = 512MB
work_mem = 32MB

# Checkpoint and WAL
checkpoint_completion_target = 0.9
wal_buffers = 16MB
min_wal_size = 1GB
max_wal_size = 4GB

# Query planner
random_page_cost = 1.1
effective_io_concurrency = 200

# Logging
log_min_duration_statement = 1000  # Log slow queries (>1s)
```

Restart PostgreSQL:
```bash
sudo systemctl restart postgresql
```

### Nginx Optimization

```bash
sudo nano /etc/nginx/nginx.conf
```

Add/modify in `http` block:
```nginx
# Connection handling
keepalive_timeout 65;
keepalive_requests 100;

# Compression
gzip on;
gzip_vary on;
gzip_min_length 1024;
gzip_types text/plain text/css text/xml text/javascript application/javascript application/json;

# File caching
open_file_cache max=2000 inactive=20s;
open_file_cache_valid 60s;
open_file_cache_min_uses 2;
```

## Support and Resources

- Application Repository: https://github.com/glensouza/csdac-pathfinder-25-honor-photography
- .NET Documentation: https://docs.microsoft.com/dotnet/
- PostgreSQL Documentation: https://www.postgresql.org/docs/
- Nginx Documentation: https://nginx.org/en/docs/
- SigNoz Documentation: https://signoz.io/docs/

## Deployment Checklist

Use this checklist when deploying the Pathfinder Photography application on bare metal or virtual machine.

### Pre-Deployment

#### Required Information
- [ ] Server/VM IP address or domain name
- [ ] Google OAuth Client ID
- [ ] Google OAuth Client Secret
- [ ] PostgreSQL password (secure, random - generated with `openssl rand -base64 32`)
- [ ] (Optional) Email SMTP settings for notifications
- [ ] SSL certificate domain (if using Let's Encrypt)

#### Prerequisites Verified
- [ ] Ubuntu 22.04 LTS or later installed (physical server, VM, or cloud instance)
- [ ] Root or sudo access available
- [ ] At least 20GB free disk space (50GB+ recommended)
- [ ] Server/VM has network connectivity
- [ ] Public IP or domain name configured
- [ ] Firewall rules planned (ports 22, 80, 443)
- [ ] Internet connection available

### Google OAuth Configuration

- [ ] Created Google Cloud project
- [ ] Enabled Google+ API
- [ ] Created OAuth 2.0 credentials
- [ ] Configured OAuth consent screen
- [ ] Added authorized redirect URIs:
  - [ ] `https://photohonor.coronasda.church/signin-google`
  - [ ] `https://www.photohonor.coronasda.church/signin-google` (if using www)
- [ ] Saved Client ID and Client Secret securely

### Step 1: PostgreSQL Installation

- [ ] Installed PostgreSQL 16: `sudo apt install postgresql-16`
- [ ] PostgreSQL service started and enabled
- [ ] Installed Cockpit: `sudo apt install cockpit`
- [ ] Cockpit service started and enabled
- [ ] Enabled root login in Cockpit (commented out root in `/etc/cockpit/disallowed-users`)
- [ ] Cockpit accessible at `https://your-server-ip:9090`
- [ ] Installed PGAdmin 4: `sudo apt install pgadmin4-web`
- [ ] Configured PGAdmin 4 with setup-web.sh
- [ ] PGAdmin 4 accessible (either via Nginx proxy or SSH tunnel)
- [ ] (Optional) Updated login message in `/etc/profile.d/00_lxc-details.sh` with service URLs
- [ ] Created database: `pathfinder_photography`
- [ ] Created user: `pathfinder` with strong password
- [ ] Granted all privileges to pathfinder user
- [ ] Configured `postgresql.conf` to listen on localhost only
- [ ] Updated `pg_hba.conf` with scram-sha-256 authentication
- [ ] Restarted PostgreSQL service
- [ ] Verified connection: `psql -h localhost -U pathfinder -d pathfinder_photography`

### Step 2: .NET SDK and Runtime Installation

- [ ] Added Microsoft package repository
- [ ] Installed .NET SDK 9.0: `sudo apt install dotnet-sdk-9.0`
- [ ] Verified SDK installation: `dotnet --list-sdks`
- [ ] Verified runtime installation: `dotnet --list-runtimes`
- [ ] Confirmed Microsoft.AspNetCore.App 9.0.x is listed
- [ ] Confirmed .NET SDK 9.0.x is listed

### Step 3: Application Installation

- [ ] Created pathfinder system user
- [ ] Added pathfinder to www-data group
- [ ] Created application directory: `/opt/pathfinder-photography`
- [ ] Downloaded/built application files
- [ ] Created uploads directory: `/opt/pathfinder-photography/wwwroot/uploads`
- [ ] Set ownership: `chown -R pathfinder:pathfinder /opt/pathfinder-photography`
- [ ] Created `appsettings.Production.json` with:
  - [ ] PostgreSQL connection string
  - [ ] Google OAuth credentials
  - [ ] (Optional) Email SMTP settings
- [ ] Set file permissions: `chmod 600 appsettings.Production.json`
- [ ] Applied database migrations (if SDK installed) or verified they run on startup

### Step 4: Systemd Service Configuration

- [ ] Created service file: `/etc/systemd/system/pathfinder-photography.service`
- [ ] Set User=pathfinder, Group=pathfinder
- [ ] Set WorkingDirectory=/opt/pathfinder-photography
- [ ] Configured environment variables (ASPNETCORE_ENVIRONMENT=Production)
- [ ] Set security settings (NoNewPrivileges, PrivateTmp, etc.)
- [ ] Added ReadWritePaths for uploads directory
- [ ] (Optional) Added SigNoz environment variables if using observability
- [ ] Reloaded systemd: `systemctl daemon-reload`
- [ ] Enabled service: `systemctl enable pathfinder-photography`
- [ ] Started service: `systemctl start pathfinder-photography`
- [ ] Verified service is running: `systemctl status pathfinder-photography`
- [ ] Checked logs for errors: `journalctl -u pathfinder-photography -n 50`

### Step 5: Nginx Configuration

- [ ] Installed Nginx
- [ ] Created site configuration: `/etc/nginx/sites-available/pathfinder-photography`
- [ ] Configured HTTP server (port 80)
- [ ] Configured HTTPS server (port 443)
- [ ] Set proxy headers (X-Forwarded-For, X-Forwarded-Proto, etc.)
- [ ] Set client_max_body_size to 10M (for photo uploads)
- [ ] Added security headers (X-Frame-Options, X-Content-Type-Options, etc.)
- [ ] Enabled site: `ln -s sites-available/pathfinder-photography sites-enabled/`
- [ ] Tested configuration: `nginx -t`
- [ ] Reloaded Nginx: `systemctl reload nginx`
- [ ] Installed Certbot: `apt install certbot python3-certbot-nginx`
- [ ] Obtained SSL certificate: `certbot --nginx -d photohonor.coronasda.church` (or using Cloudflare SSL/Tunnel)
- [ ] Verified auto-renewal: `certbot renew --dry-run`
- [ ] (Optional) If using Cloudflare Tunnel: Added service to cloudflared configuration and restarted container

### Step 6: Firewall Configuration

- [ ] Allowed SSH (CRITICAL - before enabling firewall): `ufw allow 22/tcp`
- [ ] Allowed HTTP: `ufw allow 80/tcp`
- [ ] Allowed HTTPS: `ufw allow 443/tcp`
- [ ] Set default policies: `ufw default deny incoming`
- [ ] Set default policies: `ufw default allow outgoing`
- [ ] Enabled firewall: `ufw enable`
- [ ] Verified firewall status: `ufw status verbose`

### Step 7: Automated Deployments (Optional)

#### GitHub Runner Setup
- [ ] Created github-runner user (NOT in sudo group)
- [ ] Added github-runner to pathfinder and www-data groups
- [ ] Created sudoers file: `/etc/sudoers.d/github-runner`
- [ ] Configured passwordless sudo with restricted commands
- [ ] Validated sudoers syntax: `visudo -c`
- [ ] Set sudoers file permissions: `chmod 0440`
- [ ] Downloaded GitHub Actions runner
- [ ] Extracted runner to `/home/github-runner/actions-runner`
- [ ] Obtained registration token from GitHub
- [ ] Configured runner with repository URL and token
- [ ] Set runner labels: `self-hosted,linux,bare-metal,production`
- [ ] Created systemd service: `/etc/systemd/system/github-runner.service`
- [ ] Enabled and started runner service
- [ ] Verified runner shows as "Idle" on GitHub
- [ ] Created backup directories: `/opt/backups/pathfinder-photography/deployments`
- [ ] Set ownership of backup directories to github-runner
- [ ] Configured GitHub repository secrets (if needed)
- [ ] Configured GitHub environment variables

#### Workflow Verification
- [ ] Verified workflow file exists: `.github/workflows/deploy-bare-metal.yml`
- [ ] Made test commit to trigger deployment
- [ ] Verified workflow runs successfully
- [ ] Checked deployment creates backups
- [ ] Verified file ownership set correctly
- [ ] Confirmed health checks pass
- [ ] Tested rollback on failure (optional)

### Post-Deployment Verification

#### Services Running
- [ ] PostgreSQL service active: `systemctl is-active postgresql`
- [ ] Application service active: `systemctl is-active pathfinder-photography`
- [ ] Nginx service active: `systemctl is-active nginx`
- [ ] (Optional) GitHub runner active: `systemctl is-active github-runner`
- [ ] No errors in application logs: `journalctl -u pathfinder-photography -n 100`

#### Application Access
- [ ] Can access http://localhost:5000 from server
- [ ] Can access https://photohonor.coronasda.church from browser
- [ ] Home page loads correctly
- [ ] Can see all 10 composition rules
- [ ] SSL certificate is valid (no browser warnings)

#### Google Authentication
- [ ] "Sign in with Google" button appears
- [ ] Clicking redirects to Google OAuth
- [ ] After authentication, redirected back to app
- [ ] Signed in with correct user name
- [ ] User session persists across requests

#### User Roles
- [ ] First signed-in user automatically has Admin role
- [ ] Admin can access `/admin/users` page
- [ ] Admin can promote users to Instructor
- [ ] Admin can delete unauthorized users
- [ ] Verified ELO ratings recalculate when users are deleted
- [ ] (Optional) Promoted additional admin via SQL if needed

#### Photo Upload
- [ ] Navigated to Submit page
- [ ] Selected a composition rule
- [ ] Uploaded a test photo (<10MB)
- [ ] Added description
- [ ] Submitted successfully
- [ ] Photo appears in gallery
- [ ] Photo file exists in `/opt/pathfinder-photography/wwwroot/uploads/`
- [ ] Uploads persist after service restart

#### Database
- [ ] Database is accessible:
  ```bash
  sudo -u pathfinder psql -h localhost -U pathfinder -d pathfinder_photography -c "SELECT COUNT(*) FROM \"PhotoSubmissions\";"
  ```
- [ ] Data persists after service restart
- [ ] Migrations applied successfully

#### Health Endpoints
- [ ] `/health` endpoint responds
- [ ] `/alive` endpoint responds
- [ ] `/ready` endpoint responds
- [ ] `/metrics` endpoint responds (if enabled)

### Optional Configuration

#### SigNoz Observability (Optional)
- [ ] Installed SigNoz natively using install-linux.sh script
- [ ] SigNoz services running: `systemctl status signoz-otel-collector signoz-query-service clickhouse-server`
- [ ] Services enabled to start on boot
- [ ] Updated application systemd service with OTEL variables
- [ ] Restarted application service
- [ ] SigNoz UI accessible: `http://your-server-ip:3301`
- [ ] Created Nginx reverse proxy for SigNoz UI
- [ ] Obtained SSL certificate for SigNoz domain
- [ ] Application sending telemetry to SigNoz

#### Email Notifications
- [ ] Configured SMTP settings in `appsettings.Production.json`
- [ ] Tested email by submitting and grading a photo
- [ ] Verified email delivery

### Security Hardening

- [ ] Changed default PostgreSQL password to strong random password
- [ ] Firewall configured with default-deny policy
- [ ] Using strong passwords for all services
- [ ] Secrets not in version control
- [ ] SSL/TLS enabled and working
- [ ] `appsettings.Production.json` has 600 permissions
- [ ] Application runs as non-root user (pathfinder)
- [ ] Systemd security settings enabled (NoNewPrivileges, PrivateTmp)
- [ ] PostgreSQL only listens on localhost
- [ ] Nginx security headers configured
- [ ] SSH key-based authentication enabled (optional)
- [ ] Root login disabled (optional)

### Backup Strategy

- [ ] Created backup script: `/opt/backups/backup-pathfinder-db.sh`
- [ ] Script backs up database
- [ ] Script backs up uploaded photos
- [ ] Script keeps last 7 days of backups
- [ ] Made script executable: `chmod +x`
- [ ] Scheduled backup script in crontab (daily at 2 AM)
- [ ] Test backup created successfully
- [ ] Test restore successful
- [ ] Backup location has sufficient space
- [ ] (Optional) Backups copied to remote location

### Monitoring Setup

- [ ] Configured log rotation in journald
- [ ] Set SystemMaxUse and MaxRetentionSec
- [ ] (Optional) Set up external monitoring/alerting
- [ ] (Optional) Configured health check script
- [ ] (Optional) Scheduled health check in crontab
- [ ] Know where to find logs: `journalctl -u pathfinder-photography`

### Troubleshooting Completed

If issues occurred, verify resolved:

- [ ] Service logs checked: `journalctl -u pathfinder-photography`
- [ ] Database connection working
- [ ] Google OAuth redirect URI matches exactly
- [ ] File permissions correct for uploads
- [ ] Nginx configuration valid: `nginx -t`
- [ ] SSL certificate valid and not expired
- [ ] Firewall not blocking required ports
- [ ] Disk space available: `df -h`

### Performance Tuning

- [ ] PostgreSQL tuned for available RAM
- [ ] Nginx gzip compression enabled
- [ ] Nginx file caching configured
- [ ] Application resource limits set in systemd
- [ ] Performance acceptable under expected load
- [ ] Response times measured and documented

### Documentation

- [ ] Read complete BARE_METAL_DEPLOYMENT.md
- [ ] Read SETUP.md (for development setup if needed)
- [ ] Bookmarked useful commands
- [ ] Know where to find logs
- [ ] Documented server-specific configuration
- [ ] Documented custom modifications (if any)

### Maintenance Plan

- [ ] Update schedule determined (monthly recommended)
- [ ] Backup schedule set (daily recommended)
- [ ] Monitoring in place
- [ ] Disaster recovery plan created
- [ ] Contact information for support documented
- [ ] Security update policy established

### Final Verification

- [ ] Application accessible from all intended devices
- [ ] Multiple users can sign in
- [ ] Photos upload and display correctly
- [ ] Gallery filtering works
- [ ] Performance acceptable
- [ ] No errors in logs
- [ ] SSL certificate valid
- [ ] Automated deployments working (if configured)
- [ ] Backups working
- [ ] Ready for users

### Production Readiness

#### For Church/Organization Use
- [ ] Announced to pathfinders
- [ ] Instructions provided to users
- [ ] Support contact available
- [ ] Deadline for submissions set
- [ ] Storage capacity verified for expected photos

#### Performance Baseline
- [ ] Noted current resource usage (CPU, RAM, disk)
- [ ] Response time acceptable (<2s for page loads)
- [ ] Can handle expected concurrent users
- [ ] Database queries performing well

## Sign-Off

- **Deployed by**: ________________
- **Date**: ________________
- **Server/VM**: ________________
- **Domain**: ________________
- **Application Version**: ________________
- **Deployment Type**: ☐ Physical Server ☐ Virtual Machine ☐ Cloud Instance
- **Status**: ☐ Development ☐ Staging ☐ Production

## Deployment Notes

```
Add any deployment-specific notes, customizations, or issues encountered:




```

---

**Next Steps After Deployment:**
1. Monitor logs for first 24-48 hours: `journalctl -u pathfinder-photography -f`
2. Test with small group before full rollout
3. Ensure backup is working: check `/opt/backups/pathfinder-photography/`
4. Share access information with pathfinders
5. Set submission deadline
6. Plan for photo review and grading

**Support Resources:**
- This deployment guide (BARE_METAL_DEPLOYMENT.md)
- Application repository: https://github.com/glensouza/csdac-pathfinder-25-honor-photography
- GitHub Issues: Report problems
- Server logs: `journalctl -u pathfinder-photography -f`

## Quick Command Reference

```bash
# Application management
sudo systemctl start pathfinder-photography
sudo systemctl stop pathfinder-photography
sudo systemctl restart pathfinder-photography
sudo systemctl status pathfinder-photography
sudo journalctl -u pathfinder-photography -f

# Database management
sudo -u postgres psql pathfinder_photography
sudo systemctl restart postgresql

# Nginx management
sudo nginx -t
sudo systemctl reload nginx
sudo systemctl restart nginx

# View logs
sudo journalctl -u pathfinder-photography -n 100
sudo tail -f /var/log/nginx/pathfinder-photography-access.log
sudo tail -f /var/log/nginx/pathfinder-photography-error.log

# SigNoz management (if installed)
cd /opt/signoz/signoz/deploy/
docker compose up -d
docker compose down
docker compose logs -f

# Backups
/opt/backups/backup-pathfinder-db.sh
```

---

**Note**: This deployment guide is for installing directly on a server or VM. For local development setup, see [SETUP.md](SETUP.md).
