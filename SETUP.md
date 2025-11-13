# Setup Guide - Pathfinder Photography Honor Application

This guide will walk you through setting up and running the Pathfinder Photography Honor application for local development.

## Quick Start Options

Choose your preferred local development setup:
- **Option 1: .NET Aspire** - Recommended for development with integrated observability
- **Option 2: Local .NET** - Direct .NET development without containers

## Quick Start (Aspire - Recommended for Local Development)

.NET Aspire provides service orchestration, automatic service discovery, and integrated OpenTelemetry with SigNoz.

### Prerequisites
- .NET 9.0 SDK
- Google OAuth credentials (see below)

### Steps
1. Configure Google OAuth (below) and ensure PostgreSQL is not already bound to required ports.
2. Run the AppHost:
   ```bash
   dotnet run --project PathfinderPhotography.AppHost
   ```
3. The Aspire Dashboard will auto-open (or check console output for its URL).
4. Click the `webapp` endpoint in the dashboard to open the Blazor Server application.
5. Access SigNoz UI at the URL shown in the Aspire Dashboard (typically http://localhost:3301).
6. Dashboard gives access to logs, traces, metrics, PostgreSQL utility tooling, and SigNoz observability.

### Aspire Benefits
- Automatic connection string injection (`ConnectionStrings__DefaultConnection`)
- Centralized logs, traces, and metrics (OpenTelemetry)
- Built-in health checks and resource overview
- **SigNoz integration**: All SigNoz containers (ClickHouse, OpenTelemetry Collector, Query Service, Frontend, and Alert Manager) are automatically started and configured
- **Automatic telemetry export**: Application automatically sends traces, metrics, and logs to SigNoz

## Google OAuth2.0 Setup

### Step1: Create a Google Cloud Project
1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Click "Select a project" > "NEW PROJECT"
3. Name it (e.g., "Pathfinder Photography") and click "CREATE"

### Step2: Enable Google+ API
1. "APIs & Services" > "Library"
2. Search "Google+ API" and enable it

### Step3: Configure OAuth Consent Screen
1. "APIs & Services" > "OAuth consent screen"
2. User type: External
3. Fill required fields (App name, support email, developer email)
4. Save & Continue through scopes/test users unless needed
5. Finish

### Step 4: Create OAuth 2.0 Credentials
1. "APIs & Services" > "Credentials" > "CREATE CREDENTIALS" > "OAuth client ID"
2. Application type: Web application
3. Authorized redirect URIs:
   - Local dev HTTPS: `https://localhost:5001/signin-google`
   - Local dev HTTP: `http://localhost:5000/signin-google`
   - Docker local: `http://localhost:8080/signin-google`
   - Aspire (check port): e.g. `https://localhost:7152/signin-google`
   - Production: `https://your-domain.com/signin-google`
4. Create and copy Client ID & Secret

### Step 5: Configure Application

**For local development** (`appsettings.Development.json`):
```json
{
  "Authentication": {
    "Google": {
      "ClientId": "your_client_id_here.apps.googleusercontent.com",
      "ClientSecret": "your_client_secret_here"
    }
  }
}
```

**For production deployment**, see [DEPLOY.md](DEPLOY.md) for configuration instructions ([Step 3: Install Application](deploy/03-install-application.md)).

## Email Notifications (Optional)

Used to notify instructors/admins of new submissions and pathfinders of grading results.

### Gmail App Password
1. Enable 2-Step Verification on your Google Account.
2. Create an App Password (select Mail, custom name e.g. "Pathfinder Photography").
3. Copy the 16-character password.

### Configure

**For local development** (`appsettings.Development.json`):
```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-16-char-app-password",
    "UseSsl": "true",
    "FromAddress": "your-email@gmail.com",
    "FromName": "Pathfinder Photography"
  }
}
```

**For production deployment**, see [DEPLOY.md](DEPLOY.md) for secure configuration ([Step 3: Install Application](deploy/03-install-application.md)).

### Disable Email
Leave `SmtpHost` blank or omit the Email section entirely.

### Test
1. Submit a photo (instructor/admin should receive notification).
2. Grade a photo (pathfinder should receive email).
3. Check logs if issues arise.

## Local Development Setup (Without Aspire)

### Prerequisites
- .NET 9.0 SDK
- PostgreSQL 16 or later
- Google OAuth credentials

### Steps
1. Install PostgreSQL (remember password).
2. Create Database:
   ```bash
   psql -U postgres
   CREATE DATABASE pathfinder_photography;
   \q
   ```
3. Configure Connection String in `appsettings.Development.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Host=localhost;Database=pathfinder_photography;Username=postgres;Password=your_password"
     },
     "Authentication": {
       "Google": {
         "ClientId": "your_client_id_here",
         "ClientSecret": "your_client_secret_here"
       }
     }
   }
   ```
4. Apply Migrations:
   ```bash
   dotnet ef database update
   ```
5. Run Application:
   ```bash
   dotnet run
   ```
6. Access: https://localhost:5001 or http://localhost:5000

## Creating New Migrations
After model changes:
```bash
dotnet ef migrations add MeaningfulName
```
Apply:
```bash
dotnet ef database update
```
Target project flag if needed:
```bash
dotnet ef migrations add MeaningfulName --project PathfinderPhotography.csproj
```

## Production Deployment

For production deployment on Ubuntu servers or VMs, see the wizard-style deployment guide [DEPLOY.md](DEPLOY.md) which includes:
- [Prerequisites](deploy/00-prerequisites.md) - System requirements and preparation
- [Step 1: PostgreSQL](deploy/01-install-postgresql.md) - Database installation and security
- [Step 2: .NET Runtime](deploy/02-install-dotnet.md) - .NET SDK installation
- [Step 3: Application](deploy/03-install-application.md) - Application deployment
- [Step 4: Systemd Service](deploy/04-configure-systemd.md) - Service configuration
- [Step 5: SigNoz](deploy/05-install-signoz.md) - Observability platform
- [Step 6: Nginx](deploy/06-install-nginx.md) - Reverse proxy with SSL
- [Step 7: Automated Deployments](deploy/07-automated-deployments.md) - GitHub Actions automation
- [Security & Performance](deploy/08-security-performance.md) - Security hardening and optimization

### Important Notes
- Use HTTPS for Google OAuth in production
- Keep PostgreSQL credentials secure
- Back up database regularly (photos are stored in database)
- Follow the deployment checklist in [DEPLOYMENT_CHECKLIST.md](DEPLOYMENT_CHECKLIST.md)

## Observability & Health

### Aspire / OpenTelemetry with SigNoz
When running via AppHost, all SigNoz observability components are automatically started and configured:
- **SigNoz UI**: Access at http://localhost:3301 (check Aspire Dashboard for exact URL)
- **Automatic telemetry collection**: Traces, logs, and metrics are automatically sent to SigNoz
- **ClickHouse database**: Stores all telemetry data with persistent volumes
- **No manual configuration needed**: Connection strings and secrets are automatically injected

Use the SigNoz UI to inspect:
- Distributed request traces across services
- Application and infrastructure metrics
- Centralized log aggregation with correlation to traces
- Custom dashboards and alerts
- EF Core database query performance
- .NET runtime metrics

You can also use the built-in Aspire Dashboard for basic telemetry viewing.

### Health & Metrics Endpoints
- Liveness: `/alive`
- Readiness: `/ready`
- Health: `/health`
- Metrics (Prometheus): `/metrics`

## PDF Export
Admin PDF export features (requires Admin role):
- Endpoint: `/admin/export`
- Generate: All submissions report, per-Pathfinder progress report, filtered by composition rule.
- Uses QuestPDF; images embedded.

## SigNoz Observability (Integrated with Aspire)

SigNoz is now fully integrated with .NET Aspire and starts automatically when you run the AppHost.

### Running with Aspire (Recommended)
```bash
dotnet run --project PathfinderPhotography.AppHost
```
This automatically starts:
- PostgreSQL database
- Pathfinder Photography web application
- SigNoz ClickHouse database
- SigNoz OpenTelemetry Collector
- SigNoz Query Service
- SigNoz Frontend UI
- SigNoz Alert Manager

Access SigNoz UI at http://localhost:3301 (check Aspire Dashboard for exact URL).

### Benefits of Aspire Integration
- **Zero manual configuration**: All connection strings and secrets are automatically configured
- **One-command startup**: All services start together with `dotnet run`
- **Persistent data**: SigNoz data is preserved across restarts
- **Automatic dependency management**: Services start in the correct order
- **Development-focused**: Optimized for local development and debugging

## User Roles & Admin Management
Roles:
- 0 Pathfinder (default)
- 1 Instructor
- 2 Admin

First authenticated user becomes Admin automatically. Subsequent Admins must be promoted via direct DB update.

### Admin Features
- Navigate to `/admin/users` to manage users
- Promote users to Instructor role
- Demote users back to Pathfinder role
- Delete users who shouldn't have access (removes user, all submissions, and votes; recalculates ELO ratings for affected photos)
- When a user is deleted, ELO ratings are recalculated for affected photos based on the remaining votes

### Promote User to Admin (Database)
```bash
psql -U postgres pathfinder_photography
UPDATE "Users" SET "Role" = 2 WHERE "Email" = 'email@example.com';
SELECT "Name","Email","Role" FROM "Users";
```

## Environment Variables Summary

**For local .NET development** (`appsettings.Development.json`):
- `ConnectionStrings__DefaultConnection`
- `Authentication__Google__ClientId`
- `Authentication__Google__ClientSecret`
- `ASPNETCORE_URLS`
- `ASPNETCORE_ENVIRONMENT`
- `Email` section (optional)

## Troubleshooting

### "Invalid redirect_uri"
Match protocol, port, and path exactly in Google Console.

### Database Connection Issues
Check PostgreSQL service status, credentials, and network connectivity.

### Photos Not Uploading
Check file size (<10MB), disk space, database connectivity, and application logs for errors.

### Email Failures
Confirm App Password usage; inspect logs for SMTP errors; verify port 587 is accessible.

## Database Management

### Admin Users
See User Roles above. Only first user auto-admin; others manual SQL updates.

### Backup Database

### Backup Database

**For local PostgreSQL:**
```bash
pg_dump -U postgres pathfinder_photography > backup.sql
```

### Restore Database

**For local PostgreSQL:**
```bash
psql -U postgres pathfinder_photography < backup.sql
```

### Inspect Tables

**For local PostgreSQL:**
```bash
psql -U postgres pathfinder_photography
\dt
SELECT * FROM "PhotoSubmissions" LIMIT 5;
```

## Quick Links
- README overview: `./.github/README.md`
- Production deployment: [DEPLOY.md](DEPLOY.md)
- SigNoz details: `signoz/README.md`

## Support
For issues:
- Review logs / dashboard
- Check README & this guide
- Open GitHub issue if needed

---
CSDAC Pathfinders 2025
