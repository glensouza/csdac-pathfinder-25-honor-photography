# Prerequisites and System Requirements

## 📋 Quick Navigation

| [Home](../DEPLOY.md) | [Next: PostgreSQL →](01-install-postgresql.md) |
|:---------------------|------------------------------------------------:|

## 📑 Deployment Steps Index

- **Prerequisites** ← You are here
- [Step 1: Install PostgreSQL](01-install-postgresql.md)
- [Step 2: Install .NET Runtime](02-install-dotnet.md)
- [Step 3: Install Application](03-install-application.md)
- [Step 4: Configure Systemd Service](04-configure-systemd.md)
- [Step 5: Install SigNoz](05-install-signoz.md)
- [Step 6: Install Nginx Reverse Proxy](06-install-nginx.md)
- [Step 7: Setup Automated Deployments](07-automated-deployments.md)
- [Security & Performance](08-security-performance.md)

---

## Prerequisites

Before beginning the deployment, ensure you have the following:

- **Ubuntu 22.04 LTS or later** (or equivalent Debian-based distribution)
- **Physical server, virtual machine (VM), or cloud instance**
- **Root or sudo access** to the server
- **Domain name** configured (example: `photohonor.coronasda.church`)
- **Cloudflare account** (required for DNS and SSL/CDN)
- **Cloudflare Tunnel** (cloudflared) - see Cloudflare configuration section in [Step 6](06-install-nginx.md)
- **Google OAuth credentials** - see [SETUP.md](../SETUP.md#google-oauth20-setup) for setup instructions

## System Requirements

### Minimum Requirements

- **CPU**: 2 cores
- **RAM**: 4 GB
- **Disk**: 20 GB free space
- **Network**: 1 Gbps

### Recommended Requirements

- **CPU**: 4 cores
- **RAM**: 8 GB
- **Disk**: 50 GB free space (more if storing many photos)
- **Network**: 1 Gbps

## Creating Ubuntu LXC Container on Proxmox

If you're using Proxmox VE, you can easily create an Ubuntu LXC container using the Proxmox VE Helper-Scripts community project.

### Using Proxmox VE Helper-Scripts

The easiest way to create an Ubuntu LXC container on Proxmox is to use the community-maintained helper scripts:

1. **Access your Proxmox host** via SSH or the Proxmox web shell

2. **Run the Ubuntu LXC creation script**:
   ```bash
   bash -c "$(wget -qLO - https://github.com/community-scripts/ProxmoxVE/raw/main/ct/ubuntu.sh)"
   ```

3. **Follow the interactive prompts** to configure:
   - Container ID (default: next available)
   - Hostname (e.g., `pathfinder-photography`)
   - Disk Size (minimum 20GB, recommend 50GB)
   - CPU Cores (minimum 2, recommend 4)
   - RAM (minimum 4096MB, recommend 8192MB)
   - Bridge (usually vmbr0)
   - IP Address (DHCP or static)
   - Gateway IP
   - DNS server
   - Root password

4. **Wait for the container to be created** - the script will automatically download and configure Ubuntu 22.04 LTS

5. **Start the container** and access it:
   ```bash
   pct start <container-id>
   pct enter <container-id>
   ```

### Manual LXC Container Creation

Alternatively, you can create an Ubuntu LXC container manually through the Proxmox web interface:

1. Navigate to your Proxmox node in the web interface
2. Click **Create CT** (Create Container)
3. Configure the container:
   - **General**: Container ID, Hostname, Password
   - **Template**: Select Ubuntu 22.04 template (download if needed)
   - **Disks**: Root disk size (minimum 20GB, recommend 50GB)
   - **CPU**: Cores (minimum 2, recommend 4)
   - **Memory**: RAM (minimum 4096MB, recommend 8192MB)
   - **Network**: Bridge (vmbr0), IP configuration
   - **DNS**: Use host settings or specify custom DNS
4. Click **Finish** to create the container
5. Start the container and open a console

### Important Proxmox LXC Notes

- **Privileged vs Unprivileged**: For simplicity, use a privileged container. Unprivileged containers require additional configuration for file permissions.
- **Nesting**: If you plan to run Docker containers inside the LXC (not typically needed for this application), enable "Nesting" in Container Options.
- **Features**: Enable "Nesting" and "FUSE" if you need advanced filesystem features.
- **Start at boot**: Enable "Start at boot" in the container options for automatic startup.

### After Container Creation

Once your Ubuntu LXC container is created and running:

1. **Update the system**:
   ```bash
   apt update && apt upgrade -y
   ```

2. **Verify Ubuntu version**:
   ```bash
   lsb_release -a
   # Should show Ubuntu 22.04 LTS or later
   ```

3. **Proceed to Step 1** of the deployment guide to install PostgreSQL

### Additional Resources

- **Proxmox VE Helper-Scripts**: [https://community-scripts.github.io/ProxmoxVE/](https://community-scripts.github.io/ProxmoxVE/)
- **Ubuntu LXC Script**: [https://community-scripts.github.io/ProxmoxVE/scripts?id=ubuntu](https://community-scripts.github.io/ProxmoxVE/scripts?id=ubuntu&category=Operating+Systems)
- **Proxmox LXC Documentation**: [https://pve.proxmox.com/wiki/Linux_Container](https://pve.proxmox.com/wiki/Linux_Container)

## Before You Start

### Generate Secure Passwords

You'll need a strong password for PostgreSQL. Generate it now and save it securely:

```bash
# Generate a secure random password
openssl rand -base64 32
```

**Important**: Save this password - you'll need it in Step 1 and Step 3.

### Gather Your Information

Create a checklist of the information you'll need:

- [ ] Server IP address or hostname
- [ ] Domain name for the application
- [ ] Google OAuth Client ID
- [ ] Google OAuth Client Secret
- [ ] PostgreSQL password (generated above)
- [ ] Email SMTP settings for notifications
- [ ] Additional domain names for pgAdmin and SigNoz

### Review the Deployment Checklist

For a comprehensive verification checklist, see [DEPLOYMENT_CHECKLIST.md](../DEPLOYMENT_CHECKLIST.md).

## What You'll Install

This deployment will install the following components:

1. **PostgreSQL 16** - Database server
2. **Cockpit** - Web-based system management interface
3. **PGAdmin 4** - PostgreSQL management tool
4. **.NET 9.0 SDK and Runtime** - Required to run the application
5. **Git** - Required for building from source
6. **Pathfinder Photography Application** - The main application
7. **Systemd Service** - Manages application lifecycle
8. **Nginx** - Reverse proxy and web server
9. **SigNoz** - Observability platform
10. **GitHub Actions Runner** - For automated deployments

## Estimated Time

- **Manual deployment**: 2-4 hours (first time)
- **With automation setup**: Initial setup + 10 minutes per future deployment

## Security Considerations

This guide follows security best practices including:

- Services run as non-root users
- PostgreSQL listens only on localhost
- Restrictive file permissions on configuration files
- SSL/TLS for all web services
- Firewall with default-deny policy

For more details, see [Security & Performance](08-security-performance.md).

---

## Next Step

Ready to begin? Start with installing PostgreSQL.

| [Home](../DEPLOY.md) | [Next: PostgreSQL →](01-install-postgresql.md) |
|:---------------------|------------------------------------------------:|
