# Step 3: Install SigNoz

## 📋 Quick Navigation

| [← .NET Runtime](02-install-dotnet.md) | [Home](../DEPLOY.md) | [Next: Application →](03-install-application.md) |
|:---------------------------------------|:--------------------:|--------------------------------------------------:|

## 📑 Deployment Steps Index

- [Prerequisites](00-prerequisites.md)
- [Step 1: Install PostgreSQL](01-install-postgresql.md)
- [Step 2: Install .NET Runtime](02-install-dotnet.md)
- **Step 3: Install SigNoz** ← You are here
- [Step 4: Install Application](03-install-application.md)
- [Step 5: Configure Systemd Service](04-configure-systemd.md)
- [Step 6: Install Nginx Reverse Proxy](06-install-nginx.md)
- [Step 7: Setup Automated Deployments](07-automated-deployments.md)
- [Security & Performance](08-security-performance.md)

---

## Overview

SigNoz provides observability (traces, metrics, logs) for monitoring your application performance and troubleshooting issues. **This is a required component** - the application needs SigNoz telemetry configured to start properly.

**Estimated time**: 20-30 minutes

## Important: Port Conflict with Cockpit

**⚠️ SigNoz cannot be installed on the same server as Cockpit** because both services use port 9090:
- Cockpit (system management UI) uses port 9090
- SigNoz UI also defaults to port 9090 (or 3301 depending on installation method)

**Recommended Solution**: Install SigNoz on a **separate server or LXC container**.

## Option A: Install SigNoz on Separate LXC (Recommended)

If you're using Proxmox or another hypervisor, create a dedicated LXC container for SigNoz.

### Using Proxmox Community Script (Easiest)

Use the Proxmox community script to create a SigNoz LXC:

```bash
# Run this on your Proxmox host (not in an LXC)
bash -c "$(wget -qLO - https://github.com/community-scripts/ProxmoxVE/raw/main/ct/signoz.sh)"
```

Or visit: https://community-scripts.github.io/ProxmoxVE/scripts?id=signoz

The script will:
- Create a new LXC container with optimal settings
- Install SigNoz with all dependencies
- Configure networking automatically

### Manual LXC Setup

If you prefer manual installation on a separate Ubuntu server/LXC:

1. Create a new LXC container or VM with:
   - Ubuntu 22.04 LTS
   - 4GB RAM minimum (8GB recommended)
   - 20GB disk space
   - Static IP address (e.g., `10.10.10.201`)

2. Install SigNoz using Docker (recommended for separate server):
   ```bash
   # Install Docker
   curl -fsSL https://get.docker.com | sh

   # Clone SigNoz repository
   git clone -b main https://github.com/SigNoz/signoz.git
   cd signoz/deploy

   # Start SigNoz with Docker Compose
   docker compose -f docker/clickhouse-setup/docker-compose.yaml up -d
   ```

3. Verify SigNoz is running:
   ```bash
   docker compose -f docker/clickhouse-setup/docker-compose.yaml ps
   ```

### Configure Application to Use Remote SigNoz

After installing SigNoz on a separate server, note the IP address (e.g., `10.10.10.201`). You'll need to configure the application to connect to it.

Add the SigNoz endpoint to your `appsettings.Production.json`:

```json
{
  "OpenTelemetry": {
    "OtlpEndpoint": "http://10.10.10.201:4317"
  }
}
```

And update the systemd service environment variable in [Step 5](04-configure-systemd.md):

```ini
Environment=OTEL_EXPORTER_OTLP_ENDPOINT=http://10.10.10.201:4317
```

Replace `10.10.10.201` with your SigNoz server's IP address.

## Option B: Install SigNoz on Same Server (Port Conflict)

**⚠️ Not recommended** if Cockpit is installed, as both use port 9090.

If you choose this option, you must either:
1. Disable Cockpit: `sudo systemctl disable --now cockpit`
2. Change Cockpit's port (complex)
3. Change SigNoz UI port (complex)

**Native Linux Installation** (if not using Cockpit):

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

For Docker-based installation (Option A):
```bash
# Check status
docker compose -f docker/clickhouse-setup/docker-compose.yaml ps

# View logs
docker compose -f docker/clickhouse-setup/docker-compose.yaml logs -f

# Restart services
docker compose -f docker/clickhouse-setup/docker-compose.yaml restart
```

For native installation (Option B):
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

## Verification Checklist

Before moving to the next step, verify:

- [ ] SigNoz is installed (on separate server or same server)
- [ ] SigNoz UI is accessible at `http://<signoz-ip>:3301`
- [ ] OTLP collector is listening on port 4317
- [ ] You have noted the SigNoz server IP address for configuration

You can verify the collector is running from the application server:
```bash
# Test connectivity to SigNoz OTLP endpoint
nc -zv <signoz-ip> 4317
# OR
telnet <signoz-ip> 4317
```

**Note**: The application will be configured to use SigNoz in Step 5 (Systemd Service), and the Nginx configuration for SigNoz UI will be added in Step 6.

## Troubleshooting

### Installation Script Syntax Error

If you see `syntax error near unexpected token 'newline'` when running the installation script, the download was corrupted or received an HTML error page instead of the script.

```bash
# Check if you got a valid script or an HTML error page
head -5 install-linux.sh

# If you see HTML content like '<html>' or '<!DOCTYPE html>', the download failed
# Delete and re-download with verbose output to see what's happening:
rm install-linux.sh
curl -L https://github.com/SigNoz/signoz/raw/main/deploy/install-linux.sh -o install-linux.sh

# If the above URL doesn't work, check the official SigNoz documentation for the current installation method:
# https://signoz.io/docs/install/linux/
chmod +x install-linux.sh
sudo ./install-linux.sh
```

**Note**: GitHub sometimes returns redirect pages. Using `-L` (follow redirects) instead of `-sL` can help diagnose issues by showing errors.

### SigNoz Services Not Starting

Check the logs for each service:

```bash
sudo journalctl -u signoz-otel-collector -n 100 --no-pager
sudo journalctl -u signoz-query-service -n 100 --no-pager
sudo journalctl -u clickhouse-server -n 100 --no-pager
```

### OTLP Collector Not Listening

Verify the collector is running on port 4317:
```bash
sudo netstat -tlnp | grep 4317
# OR
sudo ss -tlnp | grep 4317
```

If not running, check the collector service status:
```bash
sudo systemctl status signoz-otel-collector
```

---

## Next Steps

SigNoz is now installed and ready! Continue with Step 4 to install the application, which will be configured to use SigNoz in Step 5.

| [← .NET Runtime](02-install-dotnet.md) | [Home](../DEPLOY.md) | [Next: Application →](03-install-application.md) |
|:---------------------------------------|:--------------------:|--------------------------------------------------:|
