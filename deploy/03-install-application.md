# Step 4: Install Application

## 📋 Quick Navigation

| [← Install SigNoz](05-install-signoz.md) | [Home](../DEPLOY.md) | [Next: Systemd Service →](04-configure-systemd.md) |
|:----------------------------------------|:--------------------:|----------------------------------------------------:|

## 📑 Deployment Steps Index

- [Prerequisites](00-prerequisites.md)
- [Step 1: Install PostgreSQL](01-install-postgresql.md)
- [Step 2: Install .NET Runtime](02-install-dotnet.md)
- [Step 3: Install SigNoz](05-install-signoz.md)
- **Step 4: Install Application** ← You are here
- [Step 5: Configure Systemd Service](04-configure-systemd.md)
- [Step 6: Install Nginx Reverse Proxy](06-install-nginx.md)
- [Step 7: Setup Automated Deployments](07-automated-deployments.md)
- [Security & Performance](08-security-performance.md)

---

## Overview

In this step, you'll:
- Create a dedicated application user
- Deploy the application (build from source or use pre-built release)
- Configure the application settings
- Apply database migrations

**Estimated time**: 20-30 minutes

## Create Application User

```bash
# Create a system user for running the application
sudo useradd -r -m -s /bin/bash pathfinder
sudo usermod -aG www-data pathfinder
```

## Download and Deploy Application

Choose one of the following options:

### Option A: Build from Source (Recommended)

This option is recommended if you have the .NET SDK installed (from [Step 2](02-install-dotnet.md)).

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

### Option B: Use Pre-built Release

This option is suitable if you only installed the runtime.

Notes:
- Many tarballs include a top-level folder (e.g., `repo-main/`). Extracting normally will create that folder under the target directory. To avoid that and place the application files directly into `/opt/pathfinder-photography`, use `--strip-components=1` and `-C`.
- Verify the archive contents first with `tar -tzf` if you want to inspect before extracting.

```bash
# Create application directory
sudo mkdir -p /opt/pathfinder-photography
cd /opt/pathfinder-photography

# Download latest release (replace URL with actual release)
sudo wget https://github.com/glensouza/csdac-pathfinder-25-honor-photography/archive/refs/tags/main.tar.gz

# Extract
sudo tar -xzf main.tar.gz --strip-components=1 -C /opt/pathfinder-photography --no-same-owner
sudo rm main.tar.gz

# Set ownership
sudo chown -R pathfinder:pathfinder /opt/pathfinder-photography
```

## Configure Application

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

**Important Configuration Notes**:

- Replace `your_secure_password_here` with the PostgreSQL password you created in [Step 1](01-install-postgresql.md)
- Replace `your_google_client_id_here` and `your_google_client_secret_here` with your Google OAuth credentials (see [SETUP.md](../SETUP.md#google-oauth20-setup))
- **Email configuration is required** - Configure SMTP settings for notifications
- For Gmail SMTP, you'll need to generate an App Password (not your regular Gmail password)
- **SigNoz was installed in Step 3** and will be configured in the systemd service (Step 5)

### Set Proper Permissions

**Security**: Protect your configuration file with restrictive permissions:

```bash
sudo chmod 600 /opt/pathfinder-photography/appsettings.Production.json
sudo chown pathfinder:pathfinder /opt/pathfinder-photography/appsettings.Production.json
```

This ensures only the `pathfinder` user can read the configuration file containing sensitive credentials.

## Apply Database Migrations

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

**Note**: Database migrations will also run automatically on first application startup if not applied manually. However, applying them now helps catch any configuration issues early.

### Troubleshooting Database Migrations

If you encounter errors during migrations:

1. **Connection errors**: Verify the connection string in `appsettings.Production.json` matches your PostgreSQL configuration
2. **Permission errors**: Ensure the `pathfinder` database user has the correct permissions (see [Step 1](01-install-postgresql.md))
3. **EF tools not found**: Verify `dotnet ef` is installed and in PATH (see [Step 2](02-install-dotnet.md))

## Verification Checklist

Before moving to the next step, verify:

- [ ] Application user `pathfinder` is created
- [ ] Application files are deployed to `/opt/pathfinder-photography`
- [ ] Files are owned by `pathfinder:pathfinder`
- [ ] `appsettings.Production.json` is created with correct settings
- [ ] Configuration file has 600 permissions (owner read/write only)
- [ ] Database migrations completed successfully
- [ ] You've secured your PostgreSQL password, Google OAuth credentials, and other sensitive configuration

---

## Next Step

The application is now installed and configured! Continue with setting up the systemd service to manage the application lifecycle.

| [← Install SigNoz](05-install-signoz.md) | [Home](../DEPLOY.md) | [Next: Systemd Service →](04-configure-systemd.md) |
|:---------------------------------------|:--------------------:|----------------------------------------------------:|
