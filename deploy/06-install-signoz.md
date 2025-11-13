# Step 6: Install SigNoz (Optional)

## 📋 Quick Navigation

| [← Nginx](05-install-nginx.md) | [Home](../DEPLOY.md) | [Next: Automated Deployments →](07-automated-deployments.md) |
|:-------------------------------|:--------------------:|--------------------------------------------------------------:|

## 📑 Deployment Steps Index

- [Prerequisites](00-prerequisites.md)
- [Step 1: Install PostgreSQL](01-install-postgresql.md)
- [Step 2: Install .NET Runtime](02-install-dotnet.md)
- [Step 3: Install Application](03-install-application.md)
- [Step 4: Configure Systemd Service](04-configure-systemd.md)
- [Step 5: Install Nginx Reverse Proxy](05-install-nginx.md)
- **Step 6: Install SigNoz** ← You are here
- [Step 7: Setup Automated Deployments *(Optional)*](07-automated-deployments.md)
- [Security & Performance](08-security-performance.md)

---

## Overview

SigNoz provides observability (traces, metrics, logs). Install it only if you need application monitoring.

This step is **optional**. Skip to [Step 7](07-automated-deployments.md) if you don't need observability.

**Estimated time**: 20-30 minutes

## Prerequisites

- Ubuntu 22.04 LTS or later
- 4GB RAM minimum (8GB recommended)
- 20GB disk space

## Install SigNoz

**Native Linux Installation** (no Docker required):

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

## Manage SigNoz Services

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

## Configure Application to Use SigNoz

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

## Access SigNoz UI

SigNoz UI is available at `http://your-server-ip:3301`. To expose it securely, create an Nginx configuration:

```bash
sudo nano /etc/nginx/sites-available/signoz
```

Add:

```nginx
server {
    listen 80;
    server_name photohonorsignoz.coronasda.church;

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

Enable and test:

```bash
sudo ln -s /etc/nginx/sites-available/signoz /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

If using Let's Encrypt, secure with SSL:

```bash
sudo certbot --nginx -d photohonorsignoz.coronasda.church
```

If using Cloudflare, add the subdomain to your Cloudflare Tunnel configuration or DNS records as described in [Step 5](05-install-nginx.md).

## Verification Checklist

Before moving to the next step, verify:

- [ ] SigNoz services are installed and running
- [ ] SigNoz UI is accessible at `http://localhost:3301`
- [ ] Application service is configured with OTEL environment variables
- [ ] Application is sending telemetry to SigNoz
- [ ] Nginx is configured for SigNoz subdomain
- [ ] SigNoz is accessible via your domain (e.g., `https://photohonorsignoz.coronasda.church`)

## Troubleshooting

### SigNoz Services Not Starting

Check the logs for each service:

```bash
sudo journalctl -u signoz-otel-collector -n 100 --no-pager
sudo journalctl -u signoz-query-service -n 100 --no-pager
sudo journalctl -u clickhouse-server -n 100 --no-pager
```

### No Telemetry Data

1. Verify application environment variables are set correctly
2. Check application logs for OTEL-related errors
3. Verify SigNoz collector is running on port 4317:
   ```bash
   sudo netstat -tlnp | grep 4317
   ```

---

## Next Steps

SigNoz is now installed and configured! Continue with [Step 7: Automated Deployments](07-automated-deployments.md) if you want to set up GitHub Actions for automated deployments, or skip to [Security & Performance](08-security-performance.md).

| [← Nginx](05-install-nginx.md) | [Home](../DEPLOY.md) | [Next: Automated Deployments →](07-automated-deployments.md) |
|:-------------------------------|:--------------------:|--------------------------------------------------------------:|
