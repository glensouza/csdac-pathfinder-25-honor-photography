# Step 5: Configure Systemd Service

## 📋 Quick Navigation

| [← Install Application](03-install-application.md) | [Home](../DEPLOY.md) | [Next: Nginx →](06-install-nginx.md) |
|:---------------------------------------------------|:--------------------:|--------------------------------------:|

## 📑 Deployment Steps Index

- [Prerequisites](00-prerequisites.md)
- [Step 1: Install PostgreSQL](01-install-postgresql.md)
- [Step 2: Install .NET Runtime](02-install-dotnet.md)
- [Step 3: Install SigNoz](05-install-signoz.md)
- [Step 4: Install Application](03-install-application.md)
- **Step 5: Configure Systemd Service** ← You are here
- [Step 6: Install Nginx Reverse Proxy](06-install-nginx.md)
- [Step 7: Setup Automated Deployments](07-automated-deployments.md)
- [Security & Performance](08-security-performance.md)

---

## Overview

In this step, you'll:
- Create a systemd service to manage the application
- Configure the service for automatic startup
- Start the application and verify it's running

**Estimated time**: 10-15 minutes

## Create Systemd Service File

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
Environment=OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317
Environment=OTEL_RESOURCE_ATTRIBUTES=service.name=pathfinder-photography

# Security settings
NoNewPrivileges=true
PrivateTmp=true
ProtectSystem=strict
ProtectHome=true
StateDirectory=pathfinder-keys

# Resource limits
LimitNOFILE=65536
TasksMax=4096

[Install]
WantedBy=multi-user.target
```

### Service Configuration Explained

**Unit Section**:
- `After=network.target postgresql.service` - Start after network and PostgreSQL are available
- `Wants=postgresql.service` - Prefer to start PostgreSQL first, but don't fail if it's not available

**Service Section**:
- `Type=notify` - Application notifies systemd when it's ready
- `User=pathfinder` - Run as the pathfinder user (non-root)
- `Restart=always` - Automatically restart if the application crashes
- `StateDirectory=pathfinder-keys` - Creates `/var/lib/pathfinder-keys` for Data Protection keys

**Security Settings**:
- `NoNewPrivileges=true` - Prevent privilege escalation
- `ProtectSystem=strict` - Filesystem is read-only except for explicitly allowed paths
- `ProtectHome=true` - Hide home directories from the service

**SigNoz Integration**:
- `OTEL_EXPORTER_OTLP_ENDPOINT` - OpenTelemetry collector endpoint (required, configured in Step 3)
- `OTEL_RESOURCE_ATTRIBUTES` - Service name for telemetry identification

### Important: Data Protection Keys

**Note**: The `StateDirectory=pathfinder-keys` directive automatically creates `/var/lib/pathfinder-keys` with proper ownership (pathfinder:pathfinder) and permissions. This directory is used for ASP.NET Core Data Protection keys, which are required for authentication cookies to persist across application restarts. With `ProtectSystem=strict`, the filesystem is read-only except for explicitly allowed paths, so `StateDirectory` is essential.

## Enable and Start the Service

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

## Verify the Service is Running

To verify the service is running correctly, check:

```bash
# Verify service is active
sudo systemctl status pathfinder-photography

# Check that it's listening on port 5000
sudo netstat -tlnp | grep :5000
# OR
sudo ss -tlnp | grep :5000

# View recent logs for any errors
sudo journalctl -u pathfinder-photography -n 50 --no-pager
```

## Important Note about Direct Access

At this point in the deployment, the application is running on `http://localhost:5000`, but you **will not** be able to access it directly via a web browser at `http://your-server-ip:5000`. This is **normal and expected** behavior because:

1. The application is configured with `ASPNETCORE_URLS=http://localhost:5000`, which binds only to localhost (not all network interfaces)
2. The application has `UseHttpsRedirection()` enabled, which automatically redirects all HTTP requests to HTTPS
3. Without a valid SSL certificate configured, the HTTPS redirect will fail

The application is **designed to work through a reverse proxy** (Nginx), which will be set up in the [next step](05-install-nginx.md). After completing Nginx configuration, you'll be able to access the application through your domain name with proper SSL/TLS.

## Useful Service Management Commands

```bash
# Stop the service
sudo systemctl stop pathfinder-photography

# Restart the service
sudo systemctl restart pathfinder-photography

# View service status
sudo systemctl status pathfinder-photography

# View logs (live tail)
sudo journalctl -u pathfinder-photography -f

# View last 100 log lines
sudo journalctl -u pathfinder-photography -n 100 --no-pager

# View logs from today only
sudo journalctl -u pathfinder-photography --since today

# Disable automatic startup
sudo systemctl disable pathfinder-photography
```

## Troubleshooting

### Service Fails to Start

Check the logs for error messages:
```bash
sudo journalctl -u pathfinder-photography -n 100 --no-pager
```

Common issues:
- **Database connection errors**: Verify PostgreSQL is running and credentials are correct
- **Port already in use**: Another service might be using port 5000
- **Permission errors**: Check file ownership in `/opt/pathfinder-photography`

### Application Crashes Immediately

1. Verify the DLL path is correct:
   ```bash
   ls -l /opt/pathfinder-photography/PathfinderPhotography.dll
   ```

2. Check configuration file:
   ```bash
   sudo -u pathfinder cat /opt/pathfinder-photography/appsettings.Production.json
   ```

3. Try running manually to see detailed errors:
   ```bash
   sudo -u pathfinder bash
   cd /opt/pathfinder-photography
   export ASPNETCORE_ENVIRONMENT=Production
   dotnet PathfinderPhotography.dll
   # Press Ctrl+C to stop
   exit
   ```

## Verification Checklist

Before moving to the next step, verify:

- [ ] Systemd service file is created at `/etc/systemd/system/pathfinder-photography.service`
- [ ] Service is enabled to start on boot
- [ ] Service is currently running (`systemctl status` shows "active (running)")
- [ ] Application is listening on port 5000 (localhost only)
- [ ] No errors in the logs
- [ ] Data Protection keys directory exists at `/var/lib/pathfinder-keys`

---

## Next Step

The application service is now running! Continue with installing and configuring Nginx as a reverse proxy to make the application accessible via your domain.

| [← Install Application](03-install-application.md) | [Home](../DEPLOY.md) | [Next: Nginx →](06-install-nginx.md) |
|:---------------------------------------------------|:--------------------:|--------------------------------------:|
