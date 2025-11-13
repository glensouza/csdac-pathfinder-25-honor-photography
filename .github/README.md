# Photography Honor - Corona SDA Pathfinders

A Blazor Server web application for SDA Pathfinders from Corona SDA church to submit photos demonstrating 10 photography composition rules. Built with .NET 9.0 and .NET Aspire for improved observability and service orchestration.

[![Deploy to Bare Metal Server](https://github.com/glensouza/csdac-pathfinder-25-honor-photography/actions/workflows/deploy-bare-metal.yml/badge.svg)](https://github.com/glensouza/csdac-pathfinder-25-honor-photography/actions/workflows/deploy-bare-metal.yml)

## Features

- **.NET Aspire**: Modern cloud-native application with built-in observability, service discovery, and health checks
- **Google Authentication**: Secure login with Google accounts to track submissions
- **Role-Based Access**: Three-tier role system (Pathfinder, Instructor, Admin) for granular permissions
- **User Management**: Admin users can promote/demote users between Pathfinder and Instructor roles
- **Admin Dashboard**: Comprehensive dashboard with statistics, analytics, and quick actions for admins
- **PDF Export**: Generate detailed PDF reports of submissions and pathfinder progress
- **Email Notifications**: Automatic email notifications for grading and new submissions (optional)
- **Educational Content**: Learn about 10 essential photography composition rules with descriptions and explanations
- **Photo Submission**: Upload photos for each composition rule with personal descriptions
- **Automatic Name Tracking**: User names are automatically pulled from Google account
- **Gallery View**: Browse all submitted photos with filtering by rule or pathfinder name
- **PostgreSQL Database**: Robust data persistence for submissions
- **Observability**: Built-in telemetry, metrics, and distributed tracing with OpenTelemetry
- **SigNoz Integration**: Advanced telemetry dashboards with SigNoz (optional)

## 10 Composition Rules

1. Rule of Thirds
2. Leading Lines
3. Framing Natural
4. Fill the Frame
5. Symmetry & Asymmetry
6. Patterns & Repetition
7. Golden Ratio
8. Diagonals
9. Center Dominant Eye
10. Picture to Ground

## Quick Start

### Local Development (Recommended)

For local development, use **.NET Aspire** which provides complete application orchestration with integrated observability:

```bash
dotnet run --project PathfinderPhotography.AppHost
```

The Aspire Dashboard will auto-open showing all services including PostgreSQL, the web application, and SigNoz observability stack. See **[SETUP.md](../SETUP.md)** for detailed local development instructions.

### Production Deployment

For production, deploy to **bare metal Ubuntu server or VM**. All components (PostgreSQL, .NET application, Nginx) are installed directly on the host system. See **[BARE_METAL_DEPLOYMENT.md](../BARE_METAL_DEPLOYMENT.md)** for comprehensive production deployment instructions.

## Prerequisites

- **For Development**: .NET 9.0 SDK
- **For Production**: Ubuntu 22.04 LTS server/VM
- **For All**: Google OAuth 2.0 credentials (see [SETUP.md](../SETUP.md#google-oauth20-setup))

## User Roles

- **Pathfinder (0)**: Submit photos, vote on submissions
- **Instructor (1)**: Grade submissions + all Pathfinder abilities
- **Admin (2)**: Dashboard, user management, PDF export, grading + all other abilities

First user to sign in automatically becomes Admin.

## Documentation

### Setup & Deployment
- **[SETUP.md](../SETUP.md)** - Complete local development setup guide
  - .NET Aspire orchestration (recommended for local development)
  - Local .NET option
  - Google OAuth configuration
  - Email notifications setup
  - Database migrations
  - Observability with SigNoz

- **[BARE_METAL_DEPLOYMENT.md](../BARE_METAL_DEPLOYMENT.md)** - Production deployment on Ubuntu server/VM
  - PostgreSQL installation and security
  - .NET runtime installation
  - Application deployment
  - Nginx reverse proxy with SSL
  - Automated deployments via GitHub Actions
  - Security hardening
  - Backup strategies
  - Monitoring and maintenance

- **[DEPLOYMENT_CHECKLIST.md](../DEPLOYMENT_CHECKLIST.md)** - Deployment verification checklist
  - Pre-deployment requirements
  - Step-by-step installation verification
  - Post-deployment testing
  - Security hardening checklist
  - Backup verification

### Application Guides
- **[GRADING_SYSTEM.md](../GRADING_SYSTEM.md)** - Role-based grading system
  - User roles and permissions
  - Grading workflow for instructors
  - Photo resubmission for pathfinders
  - Admin user management

- **[CODE_SUMMARY.md](../CODE_SUMMARY.md)** - Technical implementation overview
  - Architecture and project structure
  - ELO rating system for photo comparison
  - Database schema
  - Key features and technologies
  - API endpoints

### Contributing
- **[CONTRIBUTING.md](../CONTRIBUTING.md)** - Contribution guidelines
  - How to contribute
  - Development workflow
  - Pull request process
  - Code style guidelines

- **[CODE_OF_CONDUCT.md](../CODE_OF_CONDUCT.md)** - Community standards
  - Expected behavior
  - Enforcement policies
  - Reporting guidelines

### Security
- **[SECURITY.md](SECURITY.md)** - Security policy
  - Reporting vulnerabilities
  - Security update process
  - Supported versions

## Quick Commands

### Development with Aspire
```bash
# Start all services with Aspire
dotnet run --project PathfinderPhotography.AppHost

# The Aspire Dashboard will open automatically
# Click the 'webapp' endpoint to access the application
```

### Production (Ubuntu Server)
```bash
# Application management
sudo systemctl start pathfinder-photography
sudo systemctl stop pathfinder-photography
sudo systemctl restart pathfinder-photography
sudo systemctl status pathfinder-photography

# View logs
sudo journalctl -u pathfinder-photography -f

# Database backup
sudo -u postgres pg_dump pathfinder_photography > backup.sql
```

See [BARE_METAL_DEPLOYMENT.md](../BARE_METAL_DEPLOYMENT.md) for complete production deployment commands.

## Support

- **Documentation**: See links above for comprehensive guides
- **Issues**: Report bugs or request features via [GitHub Issues](https://github.com/glensouza/csdac-pathfinder-25-honor-photography/issues)
- **Security**: Report vulnerabilities privately via [SECURITY.md](SECURITY.md)

## License

See [LICENSE](../LICENSE) file for details.

---

**CSDAC Pathfinders 2025** 🎉
