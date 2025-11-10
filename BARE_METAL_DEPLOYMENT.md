# Bare Metal Deployment Guide

This guide provides instructions for deploying the Pathfinder Photography application on a single server without Docker. All components (PostgreSQL, .NET application, and optionally SigNoz) will be installed directly on the host system.

> 💡 **Want Automated Deployments?** After completing the initial setup below, see [GITHUB_RUNNER_SETUP.md](GITHUB_RUNNER_SETUP.md) to configure automatic deployments via GitHub Actions. Every push to the `main` branch will automatically deploy to your server.

## Table of Contents
- [Prerequisites](#prerequisites)
- [System Requirements](#system-requirements)
- [Installation Steps](#installation-steps)
  - [1. Install PostgreSQL](#1-install-postgresql)
  - [2. Install .NET Runtime](#2-install-net-runtime)
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
- Root or sudo access
- Public IP address or domain name (for Google OAuth)
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

#### Configure PostgreSQL

```bash
# Switch to postgres user
sudo -u postgres psql

# Create database and user
CREATE DATABASE pathfinder_photography;
CREATE USER pathfinder WITH PASSWORD 'your_secure_password_here';
GRANT ALL PRIVILEGES ON DATABASE pathfinder_photography TO pathfinder;

# Grant schema permissions
\c pathfinder_photography
GRANT ALL ON SCHEMA public TO pathfinder;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO pathfinder;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO pathfinder;

# Exit PostgreSQL
\q
```

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

### 2. Install .NET Runtime

Install .NET 9.0 Runtime (required for the application):

```bash
# Add Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/22.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Update package list
sudo apt update

# Install .NET 9.0 Runtime and ASP.NET Core Runtime
sudo apt install -y aspnetcore-runtime-9.0

# Verify installation
dotnet --list-runtimes
```

Expected output should include:
```
Microsoft.AspNetCore.App 9.0.x
Microsoft.NETCore.App 9.0.x
```

### 3. Install Application

#### Create Application User

```bash
# Create a system user for running the application
sudo useradd -r -m -s /bin/bash pathfinder
sudo usermod -aG www-data pathfinder
```

#### Download and Deploy Application

Option A: Build from source (if you have .NET SDK):
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

Option B: Use pre-built release (recommended):
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

# Apply migrations (if dotnet SDK is installed)
dotnet ef database update

# If SDK is not installed, migrations will run automatically on first startup
exit
```

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
    server_name your-domain.com www.your-domain.com;

    # Redirect all HTTP to HTTPS
    return 301 https://$server_name$request_uri;
}

# HTTPS server
server {
    listen 443 ssl http2;
    listen [::]:443 ssl http2;
    server_name your-domain.com www.your-domain.com;

    # SSL configuration (will be managed by Certbot)
    # ssl_certificate /etc/letsencrypt/live/your-domain.com/fullchain.pem;
    # ssl_certificate_key /etc/letsencrypt/live/your-domain.com/privkey.pem;
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

```bash
# Install Certbot
sudo apt install -y certbot python3-certbot-nginx

# Obtain and install certificate
sudo certbot --nginx -d your-domain.com -d www.your-domain.com

# Test automatic renewal
sudo certbot renew --dry-run
```

Certbot will automatically:
- Obtain SSL certificate
- Update Nginx configuration
- Set up automatic renewal

#### Configure Firewall

```bash
# Allow SSH (if not already allowed)
sudo ufw allow 22/tcp

# Allow HTTP and HTTPS
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp

# Enable firewall
sudo ufw enable

# Check status
sudo ufw status
```

### 6. Install SigNoz (Optional)

SigNoz provides observability (traces, metrics, logs). Install it only if you need application monitoring.

#### Prerequisites for SigNoz

```bash
# Install Docker (SigNoz components run in containers)
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker $USER

# Install Docker Compose
sudo apt install -y docker-compose-plugin

# Log out and back in for group changes to take effect
```

#### Install SigNoz

```bash
# Create directory for SigNoz
sudo mkdir -p /opt/signoz
cd /opt/signoz

# Clone SigNoz repository
git clone -b main https://github.com/SigNoz/signoz.git
cd signoz/deploy/

# Run installation script
./install.sh
```

The installation will:
- Download and start SigNoz containers
- Configure ClickHouse for data storage
- Set up OTLP collector on port 4317
- Start SigNoz UI on port 3301

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
    server_name signoz.your-domain.com;

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
sudo certbot --nginx -d signoz.your-domain.com
```

#### SigNoz Startup on Boot

SigNoz containers should start automatically with Docker. To ensure this:

```bash
cd /opt/signoz/signoz/deploy/
docker compose ps
# All containers should show "Up" status
```

To stop SigNoz:
```bash
cd /opt/signoz/signoz/deploy/
docker compose down
```

To start SigNoz:
```bash
cd /opt/signoz/signoz/deploy/
docker compose up -d
```

### 7. Setup Automated Deployments (Optional)

For automatic deployments on every code push to `main` branch, you can set up a self-hosted GitHub Actions runner on your server.

**Benefits:**
- ✅ Automatic deployment on every push to `main`
- ✅ Automated build and testing
- ✅ Automatic rollback on deployment failures
- ✅ Backup creation before each deployment
- ✅ Health checks and verification

**Quick Setup:**

```bash
# The runner will be installed and configured following the detailed guide
# See GITHUB_RUNNER_SETUP.md for complete instructions
```

**📚 Full Documentation:** See [GITHUB_RUNNER_SETUP.md](GITHUB_RUNNER_SETUP.md) for:
- Installing and configuring the GitHub Actions runner
- Setting up automatic deployments
- Configuring deployment secrets and variables
- Monitoring and troubleshooting deployments

After setting up the runner, every push to the `main` branch will automatically:
1. Build the application
2. Run tests
3. Create a backup of the current deployment
4. Deploy the new version
5. Apply database migrations
6. Restart the service
7. Verify the deployment
8. Rollback automatically if anything fails

**Manual Deployment Trigger:**
You can also trigger deployments manually from GitHub:
1. Go to your repository on GitHub
2. Click **Actions** → **Deploy to Bare Metal Server**
3. Click **Run workflow** → Select environment → **Run workflow**

## Configuration

### Google OAuth Redirect URIs

Add these URIs to your Google Cloud Console OAuth credentials:
- `https://your-domain.com/signin-google`
- `https://www.your-domain.com/signin-google`

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
dotnet ef database update  # If SDK is installed
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

Check SigNoz containers:
```bash
cd /opt/signoz/signoz/deploy/
docker compose ps
# All containers should be "Up"

# Check collector logs
docker compose logs signoz-otel-collector

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

8. **Keep .NET runtime updated**:
   ```bash
   sudo apt update
   sudo apt install --only-upgrade aspnetcore-runtime-9.0
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

For Docker-based deployment, see [HOMELAB_DEPLOYMENT.md](HOMELAB_DEPLOYMENT.md).  
For development setup, see [SETUP.md](SETUP.md).
