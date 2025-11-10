# Setup Guide - Pathfinder Photography Honor Application

This guide will walk you through setting up and running the Pathfinder Photography Honor application for local development.

## Quick Start Options

Choose your preferred local development setup:
- **Option 1: Docker Compose** - Easiest, no local .NET SDK required
- **Option 2: .NET Aspire** - Best for development with integrated observability
- **Option 3: Local .NET** - Direct .NET development without containers

## Quick Start (Docker Compose)

### Prerequisites
- Docker Desktop installed
- Google OAuth credentials (see below)

### Steps

1. **Clone the repository**
   ```bash
   git clone https://github.com/glensouza/csdac-pathfinder-25-honor-photography.git
   cd csdac-pathfinder-25-honor-photography
   ```
2. **Configure Google OAuth** (see detailed instructions below)
3. **Create environment file**
   ```bash
   cp .env.example .env
   # Edit .env and add your Google OAuth credentials
   ```
4. **Start the application**
   ```bash
   docker-compose up -d
   ```
5. **Access the application**
   - Open your browser to http://localhost:8080
6. **View logs** (optional)
   ```bash
   docker-compose logs -f pathfinder-photography
   ```
7. **Stop the application**
   ```bash
   docker-compose down
   ```

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

**For Docker Compose** (`.env` file):
```env
GOOGLE_CLIENT_ID=your_client_id_here.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your_client_secret_here
```

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

**For production deployment**, see [BARE_METAL_DEPLOYMENT.md](BARE_METAL_DEPLOYMENT.md) for configuration instructions.

## Email Notifications (Optional)

Used to notify instructors/admins of new submissions and pathfinders of grading results.

### Gmail App Password
1. Enable 2-Step Verification on your Google Account.
2. Create an App Password (select Mail, custom name e.g. "Pathfinder Photography").
3. Copy the 16-character password.

### Configure

**For Docker Compose** (add to `.env`):
```env
EMAIL_SMTP_HOST=smtp.gmail.com
EMAIL_SMTP_PORT=587
EMAIL_SMTP_USERNAME=your-email@gmail.com
EMAIL_SMTP_PASSWORD=your-16-char-app-password
EMAIL_USE_SSL=true
EMAIL_FROM_ADDRESS=your-email@gmail.com
EMAIL_FROM_NAME=Pathfinder Photography
```
Restart containers: `docker-compose restart`

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

**For production deployment**, see [BARE_METAL_DEPLOYMENT.md](BARE_METAL_DEPLOYMENT.md) for secure configuration.

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

For production deployment on Ubuntu servers or VMs, see the comprehensive [BARE_METAL_DEPLOYMENT.md](BARE_METAL_DEPLOYMENT.md) guide which includes:
- PostgreSQL installation and security
- .NET runtime installation
- Application deployment
- Nginx reverse proxy with SSL
- Automated deployments via GitHub Actions
- Security hardening
- Backup strategies
- Monitoring and maintenance

### Important Notes
- Use HTTPS for Google OAuth in production
- Keep PostgreSQL credentials secure
- Back up database and uploads regularly
- Follow the deployment checklist in BARE_METAL_DEPLOYMENT.md

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

### Running SigNoz Separately (Alternative)
If you prefer to run SigNoz separately without Aspire:
```bash
docker-compose --profile signoz up -d
```
SigNoz UI will be available at `http://localhost:3301`.

This is useful if you:
- Want to run SigNoz independently of the main application
- Are debugging the application without Aspire
- Need more control over individual SigNoz components

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

**For Docker Compose** (`.env` file):
- `GOOGLE_CLIENT_ID`
- `GOOGLE_CLIENT_SECRET`
- `POSTGRES_PASSWORD`
- `EMAIL_SMTP_HOST`, `EMAIL_SMTP_PORT`, `EMAIL_SMTP_USERNAME`, `EMAIL_SMTP_PASSWORD`, `EMAIL_USE_SSL`, `EMAIL_FROM_ADDRESS`, `EMAIL_FROM_NAME` (optional)

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
Check PostgreSQL service status, credentials, network connectivity, and for Docker setups, container network linkage.

### Photos Not Uploading
Validate `wwwroot/uploads` exists & writable; check file size (<10MB) & disk space. For Docker, ensure volume mounts are correct.

### Email Failures
Confirm App Password usage; inspect logs for SMTP errors; verify port 587 is accessible.

## Database Management

### Admin Users
See User Roles above. Only first user auto-admin; others manual SQL updates.

### Backup Database

**For Docker Compose:**
```bash
docker exec -t pathfinder-postgres pg_dump -U postgres pathfinder_photography > backup.sql
```

**For local PostgreSQL:**
```bash
pg_dump -U postgres pathfinder_photography > backup.sql
```

### Restore Database

**For Docker Compose:**
```bash
cat backup.sql | docker exec -i pathfinder-postgres psql -U postgres pathfinder_photography
```

**For local PostgreSQL:**
```bash
psql -U postgres pathfinder_photography < backup.sql
```

### Inspect Tables

**For Docker Compose:**
```bash
docker exec -it pathfinder-postgres psql -U postgres pathfinder_photography
\dt
SELECT * FROM "PhotoSubmissions" LIMIT 5;
```

**For local PostgreSQL:**
```bash
psql -U postgres pathfinder_photography
\dt
SELECT * FROM "PhotoSubmissions" LIMIT 5;
```

## Quick Links
- README overview: `./.github/README.md`
- Production deployment: [BARE_METAL_DEPLOYMENT.md](BARE_METAL_DEPLOYMENT.md)
- SigNoz details: `signoz/README.md`

## Support
For issues:
- Review logs / dashboard
- Check README & this guide
- Open GitHub issue if needed

---
CSDAC Pathfinders 2025
