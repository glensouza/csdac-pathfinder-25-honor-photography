# Setup Guide - Pathfinder Photography Honor Application

This guide will walk you through setting up and running the Pathfinder Photography Honor application.

## Quick Start (Docker - Recommended)

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

## Running with .NET Aspire (Recommended for Local Dev + Observability)

.NET Aspire provides service orchestration, automatic service discovery, and integrated OpenTelemetry.

### Steps
1. Configure Google OAuth (below) and ensure PostgreSQL is not already bound to required ports.
2. Run the AppHost:
 ```bash
 dotnet run --project PathfinderPhotography.AppHost
 ```
3. The Aspire Dashboard will auto-open (or check console output for its URL).
4. Click the `webapp` endpoint in the dashboard to open the Blazor Server application.
5. Dashboard gives access to logs, traces, metrics, and PostgreSQL utility tooling.

### Aspire Benefits
- Automatic connection string injection (`ConnectionStrings__DefaultConnection`)
- Centralized logs, traces, and metrics (OpenTelemetry)
- Built?in health checks and resource overview

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

### Step4: Create OAuth2.0 Credentials
1. "APIs & Services" > "Credentials" > "CREATE CREDENTIALS" > "OAuth client ID"
2. Application type: Web application
3. Authorized redirect URIs:
 - Local dev HTTPS: `https://localhost:5001/signin-google`
 - Local dev HTTP: `http://localhost:5000/signin-google`
 - Docker local: `http://localhost:8080/signin-google`
 - Aspire (check port): e.g. `https://localhost:7152/signin-google`
 - Production: `https://your-domain.com/signin-google`
4. Create and copy Client ID & Secret

### Step5: Configure Application

`.env` file:
```env
GOOGLE_CLIENT_ID=your_client_id_here.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your_client_secret_here
```

For non?Docker local development edit `appsettings.Development.json`:
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

## Email Notifications (Optional)

Used to notify instructors/admins of new submissions and pathfinders of grading results.

### Gmail App Password
1. Enable2?Step Verification on your Google Account.
2. Create an App Password (select Mail, custom name e.g. "Pathfinder Photography").
3. Copy the16?character password.

### Configure (Environment Variables recommended)
Add to `.env`:
```env
EMAIL_SMTP_HOST=smtp.gmail.com
EMAIL_SMTP_PORT=587
EMAIL_SMTP_USERNAME=your-email@gmail.com
EMAIL_SMTP_PASSWORD=your-16-char-app-password
EMAIL_USE_SSL=true
EMAIL_FROM_ADDRESS=your-email@gmail.com
EMAIL_FROM_NAME=Pathfinder Photography
```
Restart the application/container.

### Disable Email
Leave `EMAIL_SMTP_HOST` blank or omit all email variables.

### Test
1. Submit a photo (instructor/admin should receive notification).
2. Grade a photo (pathfinder should receive email).
3. Check logs and your Gmail Sent folder if issues arise.

## Local Development Setup (Without Docker)

### Prerequisites
- .NET9.0 SDK
- PostgreSQL16 or later
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

### Docker Deployment
1. Build image (optional if not using GHCR prebuilt):
 ```bash
 docker build -t pathfinder-photography:latest .
 ```
2. Set environment variables (or use `.env`).
3. Start services:
 ```bash
 docker-compose up -d
 ```
4. Reverse proxy (Nginx example):
 ```nginx
 server {
 listen80;
 server_name your-domain.com;
 location / {
 proxy_pass http://localhost:8080;
 proxy_http_version1.1;
 proxy_set_header Upgrade $http_upgrade;
 proxy_set_header Connection keep-alive;
 proxy_set_header Host $host;
 proxy_cache_bypass $http_upgrade;
 proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
 proxy_set_header X-Forwarded-Proto $scheme;
 }
 }
 ```
5. Enable HTTPS (Let's Encrypt):
 ```bash
 sudo certbot --nginx -d your-domain.com
 ```

### Important Notes
- Use HTTPS for Google OAuth.
- Keep PostgreSQL credentials secure.
- Back up database + uploads regularly.

## Observability & Health

### Aspire / OpenTelemetry
When running via AppHost, traces, logs, metrics are collected automatically. Use the dashboard to inspect:
- Request traces
- EF Core database spans
- Runtime metrics

### Health & Metrics Endpoints (Production / Docker)
- Liveness: `/alive`
- Readiness: `/ready`
- Health: `/health`
- Metrics (Prometheus): `/metrics`

## PDF Export
Admin PDF export features (requires Admin role):
- Endpoint: `/admin/export`
- Generate: All submissions report, per-Pathfinder progress report, filtered by composition rule.
- Uses QuestPDF; images embedded.

## SigNoz (Optional Advanced Telemetry)
Use the `docker-compose.signoz.yml` to run SigNoz locally:
```bash
docker-compose -f docker-compose.signoz.yml up -d
```
SigNoz UI default: `http://localhost:3301`
Provides deeper dashboards, traces, metrics, alerts.

## User Roles & Admin Management
Roles:
-0 Pathfinder (default)
-1 Instructor
-2 Admin
First authenticated user becomes Admin automatically. Subsequent Admins must be promoted via direct DB update.

Promote example:
```bash
psql -U postgres pathfinder_photography
UPDATE "Users" SET "Role" =2 WHERE "Email" = 'email@example.com';
SELECT "Name","Email","Role" FROM "Users";
```

## Environment Variables Summary
Core:
- `ConnectionStrings__DefaultConnection`
- `Authentication__Google__ClientId`
- `Authentication__Google__ClientSecret`
- `ASPNETCORE_URLS`
- `ASPNETCORE_ENVIRONMENT`
Optional Email:
- `EMAIL_SMTP_HOST`, `EMAIL_SMTP_PORT`, `EMAIL_SMTP_USERNAME`, `EMAIL_SMTP_PASSWORD`, `EMAIL_USE_SSL`, `EMAIL_FROM_ADDRESS`, `EMAIL_FROM_NAME`

## Troubleshooting

### "Invalid redirect_uri"
Match protocol, port, and path exactly in Google Console.

### Database Connection Issues
Check PostgreSQL service status, credentials, container network linkage (Docker).

### Photos Not Uploading
Validate `wwwroot/uploads` exists & writable; check file size (<10MB) & disk space.

### Email Failures
Confirm App Password usage; inspect logs for SMTP errors; verify port587 open.

## Database Management

### Admin Users
See User Roles above. Only first user auto-admin; others manual SQL updates.

### Backup Database
```bash
pg_dump -U postgres pathfinder_photography > backup.sql
```
(Docker variant use `docker exec`.)

### Restore Database
```bash
psql -U postgres pathfinder_photography < backup.sql
```

### Inspect Tables
```bash
psql -U postgres pathfinder_photography
\dt
SELECT * FROM "PhotoSubmissions" LIMIT5;
```

## Quick Links
- README overview: `./.github/README.md`
- Home Lab guide: `HOMELAB_DEPLOYMENT.md`
- Deployment checklist: `DEPLOYMENT_CHECKLIST.md`
- SigNoz details: `signoz/README.md`

## Support
For issues:
- Review logs / dashboard
- Check README & this guide
- Open GitHub issue if needed

---
Corona SDA Church Pathfinders2025
