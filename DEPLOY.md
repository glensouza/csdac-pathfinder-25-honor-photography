# Production Deployment Guide

Welcome to the Pathfinder Photography application deployment wizard! This guide will walk you through deploying the application on Ubuntu 22.04 LTS (physical server, VM, or cloud instance).

## 🚀 Quick Navigation

| Step | Description | Status |
|------|-------------|--------|
| [**Prerequisites**](deploy/00-prerequisites.md) | System requirements and prerequisites | ⬜ Start here |
| [**Step 1: PostgreSQL**](deploy/01-install-postgresql.md) | Install and configure PostgreSQL 16 | ⬜ |
| [**Step 2: .NET Runtime**](deploy/02-install-dotnet.md) | Install .NET 9.0 SDK and runtime | ⬜ |
| [**Step 3: SigNoz**](deploy/05-install-signoz.md) | Install observability platform (required) | ⬜ |
| [**Step 4: Application**](deploy/03-install-application.md) | Deploy the application | ⬜ |
| [**Step 5: Systemd Service**](deploy/04-configure-systemd.md) | Configure service and autostart | ⬜ |
| [**Step 6: Nginx**](deploy/06-install-nginx.md) | Install and configure reverse proxy | ⬜ |
| [**Step 7: Automation**](deploy/07-automated-deployments.md) | Setup GitHub Actions deployments | ⬜ |
| [**Security & Performance**](deploy/08-security-performance.md) | Best practices and optimization | ⬜ |

## 📋 What You'll Need

Before starting, gather the following:
- Ubuntu 22.04 LTS server (physical, VM, or cloud instance)
- Root or sudo access
- Domain name (e.g., `photohonor.coronasda.church`)
- Google OAuth credentials ([setup guide](SETUP.md#google-oauth20-setup))
- Secure PostgreSQL password (generate with `openssl rand -base64 32`)

## 🎯 Deployment Options

### Manual Deployment
Follow each step in order using the links above. This is recommended for first-time deployments to understand the process.

### Automated Deployment
After completing the initial manual setup, you can configure automated deployments via GitHub Actions. See [Step 7](deploy/07-automated-deployments.md) for details.

## 📖 Additional Resources

- **[Deployment Checklist](DEPLOYMENT_CHECKLIST.md)** - Verification checklist for each step
- **[Local Setup](SETUP.md)** - For development environment setup

## 💡 Helpful Tips

- Use the navigation links at the bottom of each page to move between steps
- Each page has a complete index at the top for quick navigation
- Mark your progress in the table above as you complete each step
- Save important passwords and configuration values securely

## 🆘 Getting Help

If you encounter issues:
1. Check the troubleshooting sections in each step
2. Review the [Deployment Checklist](DEPLOYMENT_CHECKLIST.md)
3. Open an issue in the GitHub repository

---

**Ready to begin?** Start with [Prerequisites →](deploy/00-prerequisites.md)
