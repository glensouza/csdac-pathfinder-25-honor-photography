# Step 2: Install .NET Runtime and SDK

## 📋 Quick Navigation

| [← PostgreSQL](01-install-postgresql.md) | [Home](../DEPLOY.md) | [Next: Install SigNoz →](05-install-signoz.md) |
|:-----------------------------------------|:--------------------:|------------------------------------------------:|

## 📑 Deployment Steps Index

- [Prerequisites](00-prerequisites.md)
- [Step 1: Install PostgreSQL](01-install-postgresql.md)
- **Step 2: Install .NET Runtime** ← You are here
- [Step 3: Install SigNoz](05-install-signoz.md)
- [Step 4: Install Application](03-install-application.md)
- [Step 5: Configure Systemd Service](04-configure-systemd.md)
- [Step 6: Install Nginx Reverse Proxy](06-install-nginx.md)
- [Step 7: Setup Automated Deployments](07-automated-deployments.md)
- [Security & Performance](08-security-performance.md)

---

## Overview

In this step, you'll install:
- .NET 9.0 SDK (includes runtime and ASP.NET Core)
- Entity Framework Core tools
- Git (for building from source)

**Estimated time**: 10-15 minutes

## Install .NET 9.0 SDK

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

### Expected Output

Expected output from `dotnet --list-runtimes` should include:
```
Microsoft.AspNetCore.App 9.0.x
Microsoft.NETCore.App 9.0.x
```

Expected output from `dotnet --list-sdks` should include:
```
9.0.xxx [/usr/share/dotnet/sdk]
```

### SDK vs Runtime Only

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

## Install Entity Framework Core Tools

The EF Core tools are required for running database migrations. Install them globally:

```bash
# Install EF Core tools globally
dotnet tool install --global dotnet-ef

# Verify installation
dotnet ef --version
```

Expected output:
```
Entity Framework Core .NET Command-line Tools
9.x.x
```

### Configure PATH for EF Tools

**Note**: If you get a warning about the tools not being on PATH, add the .NET tools directory to your PATH:

```bash
# Add to PATH (add this to ~/.bashrc for persistence)
export PATH="$PATH:$HOME/.dotnet/tools"

# Or for the pathfinder user (recommended)
sudo -u pathfinder bash -c 'echo "export PATH=\"\$PATH:\$HOME/.dotnet/tools\"" >> ~/.bashrc'

# Verify it works
dotnet ef --version
```

### Troubleshooting EF Tools

If you see "Could not execute because the specified command or file was not found" when running `dotnet ef`:
- The EF tools are not installed globally - run `dotnet tool install --global dotnet-ef`
- The tools path is not in PATH - add `~/.dotnet/tools` to your PATH environment variable
- Try updating the tools: `dotnet tool update --global dotnet-ef`

## Install Git (Required for Building from Source)

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

**Note**: Git is only required if you're building from source (Option A in [Step 3](03-install-application.md)). If you're deploying pre-built binaries (Option B), you can skip this step.

## Verification Checklist

Before moving to the next step, verify:

- [ ] .NET 9.0 SDK is installed
- [ ] `dotnet --version` shows version 9.0.x
- [ ] `dotnet --list-runtimes` shows ASP.NET Core and .NET Core runtimes
- [ ] Entity Framework tools are installed (`dotnet ef --version` works)
- [ ] Git is installed (if building from source)

---

## Next Step

.NET is now installed! Continue with installing SigNoz observability platform.

| [← PostgreSQL](01-install-postgresql.md) | [Home](../DEPLOY.md) | [Next: Install SigNoz →](05-install-signoz.md) |
|:-----------------------------------------|:--------------------:|------------------------------------------------:|
