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

## Next Steps

SigNoz is now installed and ready! The OTLP collector is running on port 4317. When you configure the systemd service in Step 5, the application will automatically connect to SigNoz using these environment variables:

```ini
Environment=OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
Environment=OTEL_RESOURCE_ATTRIBUTES=service.name=pathfinder-photography
```

## Verification Checklist

Before moving to the next step, verify:

- [ ] SigNoz services are installed and running
- [ ] SigNoz UI is accessible at `http://localhost:3301`
- [ ] OTLP collector is listening on port 4317

You can verify the collector is running:
```bash
sudo netstat -tlnp | grep 4317
# OR
sudo ss -tlnp | grep 4317
```

**Note**: The application will be configured to use SigNoz in Step 5 (Systemd Service), and the Nginx configuration for SigNoz UI will be added in Step 6.

## Troubleshooting

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
