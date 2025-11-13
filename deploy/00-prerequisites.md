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
- **Cloudflare account** (if using Cloudflare for DNS and SSL/CDN)
- *(Optional)* **Cloudflare Tunnel** (cloudflared) if already running - see Cloudflare configuration section in [Step 5](05-install-nginx.md)
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
- [ ] (Optional) Email SMTP settings for notifications
- [ ] (Optional) Additional domain names for pgAdmin and SigNoz

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
9. *(Optional)* **SigNoz** - Observability platform
10. *(Optional)* **GitHub Actions Runner** - For automated deployments

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
