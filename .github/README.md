# Photography Honor - Corona SDA Pathfinders

A Blazor Server web application for SDA Pathfinders from Corona SDA church to submit photos demonstrating10 photography composition rules. Built with .NET Aspire for improved observability and service orchestration.

[![Docker Build](https://github.com/glensouza/csdac-pathfinder-25-honor-photography/actions/workflows/docker-build.yml/badge.svg)](https://github.com/glensouza/csdac-pathfinder-25-honor-photography/actions/workflows/docker-build.yml)

## Features

- **.NET Aspire**: Modern cloud-native application with built-in observability, service discovery, and health checks
- **Google Authentication**: Secure login with Google accounts to track submissions
- **Role-Based Access**: Three-tier role system (Pathfinder, Instructor, Admin) for granular permissions
- **User Management**: Admin users can promote/demote users between Pathfinder and Instructor roles
- **Admin Dashboard**: Comprehensive dashboard with statistics, analytics, and quick actions for admins
- **PDF Export**: Generate detailed PDF reports of submissions and pathfinder progress
- **Email Notifications**: Automatic email notifications for grading and new submissions (optional)
- **Educational Content**: Learn about10 essential photography composition rules with descriptions and explanations
- **Photo Submission**: Upload photos for each composition rule with personal descriptions
- **Automatic Name Tracking**: User names are automatically pulled from Google account
- **Gallery View**: Browse all submitted photos with filtering by rule or pathfinder name
- **PostgreSQL Database**: Robust data persistence for submissions
- **Observability**: Built-in telemetry, metrics, and distributed tracing with OpenTelemetry
- **SigNoz Integration**: Advanced telemetry dashboards with SigNoz (optional)
- **Docker Support**: Pre-built images on GitHub Container Registry for easy deployment
- **Home Lab Ready**: Simple deployment to your home lab infrastructure

##10 Composition Rules

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

## Quick Start - Home Lab Deployment 🏠

The easiest way to deploy is using the pre-built Docker image from GitHub Container Registry.

### Option A: One-line Script

```bash
curl -sSL -o deploy-homelab.sh https://raw.githubusercontent.com/glensouza/csdac-pathfinder-25-honor-photography/main/deploy-homelab.sh && bash deploy-homelab.sh
```

Script options:
- `-d <dir>` deployment directory (default: `~/pathfinder-photography`)
- `-p <port>` host port (default: `8080`)
- `--update` pull latest image and recreate containers

### Option B: Manual Compose

```bash
# Create deployment directory
mkdir -p ~/pathfinder-photography
cd ~/pathfinder-photography

# Download compose and env template
curl -o docker-compose.yml https://raw.githubusercontent.com/glensouza/csdac-pathfinder-25-honor-photography/main/docker-compose.yml
curl -o .env https://raw.githubusercontent.com/glensouza/csdac-pathfinder-25-honor-photography/main/.env.example

# Edit .env (set GOOGLE_CLIENT_ID/SECRET, POSTGRES_PASSWORD, optional EMAIL_* settings)

# Start the application
docker compose pull
docker compose up -d
```

Access at: http://your-server:8080

Add Google OAuth redirect URIs:
- `http://your-server:8080/signin-google`
- `http://localhost:8080/signin-google`
- If using HTTPS/reverse proxy: `https://yourdomain.com/signin-google`

**📖 For detailed home lab deployment, see `HOMELAB_DEPLOYMENT.md`**

## Docker Images

Pre-built multi-arch images are available on GitHub Container Registry:

```bash
# Pull latest image
docker pull ghcr.io/glensouza/csdac-pathfinder-25-honor-photography:latest

# Pull specific version
docker pull ghcr.io/glensouza/csdac-pathfinder-25-honor-photography:v1.0.0
```

**Supported Platforms**: `linux/amd64`, `linux/arm64`

## Prerequisites

- **For Home Lab**: Docker and Docker Compose
- **For Development**: .NET9.0 SDK, Docker Desktop
- **For All**: Google OAuth2.0 credentials

## Google OAuth Setup

1. Go to https://console.cloud.google.com/
2. Create a project
3. Enable Google+ API
4. Create OAuth2.0 Client ID (Web)
5. Configure consent screen
6. Authorized redirect URIs (pick the ones you use):
 - Local dev: `https://localhost:5001/signin-google`, `http://localhost:5000/signin-google`
 - Aspire (check port): e.g. `https://localhost:7152/signin-google`
 - Home lab: `http://your-server:8080/signin-google` or your HTTPS domain
7. Add Client ID/Secret to `.env` or `appsettings.Development.json`

## Running with .NET Aspire (Recommended)

.NET Aspire provides complete application orchestration with integrated SigNoz observability.

1. Configure Google OAuth in `appsettings.Development.json`
2. Run the AppHost:
 ```bash
 dotnet run --project PathfinderPhotography.AppHost
 ```
3. The Aspire Dashboard will auto-open showing all services including:
   - PostgreSQL database with PgAdmin
   - Pathfinder Photography web application
   - **SigNoz observability stack** (ClickHouse, OpenTelemetry Collector, Query Service, Frontend UI, Alert Manager)
4. Click the `webapp` endpoint to open the Blazor app
5. Access SigNoz UI at http://localhost:3301 (check Aspire Dashboard for exact URL)

**Benefits**: All services start automatically, connection strings auto-configured, persistent data, built-in observability

## Running with Docker Compose

1. Copy `.env.example` to `.env` and add Google OAuth credentials
2. Start:
 ```bash
 docker compose up -d
 ```
3. App: `http://localhost:8080`

## Email Configuration (Using Gmail)

Use Gmail App Passwords (2FA must be enabled):

```env
EMAIL_SMTP_HOST=smtp.gmail.com
EMAIL_SMTP_PORT=587
EMAIL_SMTP_USERNAME=your-email@gmail.com
EMAIL_SMTP_PASSWORD=your-16-char-app-password
EMAIL_USE_SSL=true
EMAIL_FROM_ADDRESS=your-email@gmail.com
EMAIL_FROM_NAME=Pathfinder Photography
```

Leave `EMAIL_SMTP_HOST` empty to disable email.

## Observability

### Development with Aspire
- **Aspire Dashboard**: Built-in telemetry viewer for logs, traces, and metrics
- **SigNoz Integration**: Full observability stack automatically started with Aspire
  - SigNoz UI: http://localhost:3301
  - Distributed tracing, metrics, and log aggregation
  - No manual configuration required

### Production/Home Lab
- Health: `/health`, `/alive`, `/ready`
- Metrics: `/metrics`
- Optional SigNoz: Enable with `docker compose --profile signoz up -d`
  - SigNoz UI: `http://localhost:3301`

## User Roles

- Pathfinder (0): submit, vote
- Instructor (1): grade + Pathfinder abilities
- Admin (2): dashboard, user management, export, grade

First user to sign in becomes Admin automatically.

## Troubleshooting

- Validate OAuth redirect URIs
- Check logs: `docker compose logs -f`
- Verify ports and firewall
- Ensure uploads volume exists and is writable

## License

See LICENSE file for details.
