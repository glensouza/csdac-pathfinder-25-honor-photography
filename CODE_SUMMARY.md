# Pathfinder Photography Honor - Implementation Summary

## Overview

A Blazor Server web application built with .NET9.0 for SDA Pathfinders to submit and manage photography assignments demonstrating 10 composition rules. Includes Google authentication, PostgreSQL persistence, email notifications, PDF export, and integrated observability (Aspire for dev; SigNoz in production).

## All Requirements Implemented

### Core Requirements
✅ Blazor Server website
✅ CSDAC Pathfinders can submit photos
✅10 photos submission (one per composition rule)
✅ Pathfinders explain their application of rules
✅ Space for sample photos and descriptions of each style
✅ Name tracking for submissions
✅ Google authentication
✅ PostgreSQL database
✅ .NET Aspire integration
✅ Admin dashboard and user management
✅ PDF export (reports and progress)
✅ Email notifications (SMTP)

### 10 Composition Rules

All 10 rules implemented with detailed descriptions and explanations:

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

## Architecture

### Projects

1. `PathfinderPhotography` (Main Web App)
 - Blazor Server components
 - Authentication and authorization
 - Photo submission and gallery
 - Business logic services
 - Admin features (dashboard, user management, grading, export)

2. `PathfinderPhotography.AppHost` (Aspire Orchestration - development)
 - Service orchestration
 - PostgreSQL configuration
 - **SigNoz observability stack integration** (ClickHouse, OTLP Collector, Query Service, Frontend, Alert Manager)
 - Resource management
 - Observability dashboard (logs, traces, metrics)

3. `PathfinderPhotography.ServiceDefaults` (Shared Config)
 - OpenTelemetry setup
 - Health checks
 - Service discovery
 - Resilience patterns

### Technology Stack

- Framework: ASP.NET Core Blazor Server (.NET9.0)
- Orchestration: .NET Aspire (dev)
- Database: PostgreSQL16
- ORM: Entity Framework Core9.0
- Authentication: Google OAuth2.0
- Observability: OpenTelemetry (traces, metrics, logs)
- Telemetry Platform: SigNoz
- UI: Bootstrap5

## Key Features

### User Features
- Google Sign-In (OAuth2.0)
- Automatic name tracking from Google profile
- Photo upload (images up to 10MB)
- Rule selection (10 composition rules)
- Description input explaining the rule application
- Personal and public galleries with filtering

### Admin/Instructor Features
- Admin dashboard with statistics
- User management (promote/demote Pathfinder/Instructor)
- Delete users who shouldn't have access (removes user, submissions, and votes; recalculates ELO ratings)
- Grading workflow (Instructor/Admin)
- PDF export (all submissions, per-pathfinder progress, filter by rule)
- Email notifications for submissions and grading

### Technical Features
- Health checks and metrics endpoints
- Distributed tracing via OpenTelemetry
- Structured logging and runtime metrics
- Database migrations
- Photo storage directly in PostgreSQL database (ImageData byte array)

## Database Schema (high level)

- `PhotoSubmissions` table: `Id`, `PathfinderName`, `PathfinderEmail`, `CompositionRuleId`, `CompositionRuleName`, `ImagePath`, `ImageData`, `Description`, `SubmissionDate`, `GradeStatus`, `EloRating`
- `Users` table: `Id`, `Email`, `Name`, `Role`, `CreatedDate`
- `PhotoVotes` table: `Id`, `VoterEmail`, `WinnerPhotoId`, `LoserPhotoId`, `VoteDate`
- Indexes: `PathfinderName`, `PathfinderEmail`, `CompositionRuleId`, `GradeStatus`, `Email` (unique)
- Note: Deleting a user removes their submissions and all related votes (deleted explicitly in code, not via database cascade)

## Security

- Authentication: Google OAuth2.0
- Authorization: Cookie-based
- HTTPS required for production
- Secrets via environment variables
- Basic image validation (type/size)
- EF Core parameterization

## Deployment Options

### Aspire (Development)
- Command: `dotnet run --project PathfinderPhotography.AppHost`
- Features: Orchestration, local Postgres, dashboard, **integrated SigNoz observability**
- All services (PostgreSQL, webapp, SigNoz stack) start automatically
- SigNoz UI: Check Aspire Dashboard for URL (typically http://localhost:3301 or http://localhost:3302)

### Production (Ubuntu Server)
- Direct installation on Ubuntu 22.04 LTS
- PostgreSQL, .NET Runtime, and application installed natively
- Nginx reverse proxy with SSL
- Systemd service for application management
- Optional: SigNoz can be deployed separately for observability
- See [DEPLOY.md](DEPLOY.md) for complete production setup guide

## Observability

### Aspire Development
- Health endpoints: `/health`, `/alive`, `/ready`
- Metrics endpoint: `/metrics` (Prometheus format)
- Tracing: OpenTelemetry
- **SigNoz**: Fully integrated - all containers start automatically with AppHost
  - SigNoz UI: `http://localhost:3301` (traces, metrics, logs, dashboards)
  - OpenTelemetry Collector at ports 4317 (gRPC) and 4318 (HTTP)
  - Automatic OTLP endpoint injection to webapp

### Production Deployment
- Health endpoints: `/health`, `/alive`, `/ready`
- Metrics endpoint: `/metrics` (Prometheus format)
- Tracing: OpenTelemetry
- SigNoz: Optional - can be deployed separately on Ubuntu server
  - See [DEPLOY.md](DEPLOY.md) Step 5 for SigNoz installation
  - When deployed, traces export to SigNoz OpenTelemetry Collector

## Configuration

Required
- `Authentication__Google__ClientId`
- `Authentication__Google__ClientSecret`

Database
- `ConnectionStrings__DefaultConnection` (injected via Aspire or configured in compose)

Email
- `Email__SmtpHost`, `Email__SmtpPort`, `Email__SmtpUsername`, `Email__SmtpPassword`, `Email__UseSsl`, `Email__FromAddress`, `Email__FromName`

## File Structure

```
PathfinderPhotography/
├── Components/
│ ├── Layout/
│ └── Pages/
├── Data/
├── Models/
├── Services/
├── Migrations/
├── wwwroot/
├── Program.cs
└── PathfinderPhotography.csproj

PathfinderPhotography.AppHost/
PathfinderPhotography.ServiceDefaults/
```

## API Endpoints

Application
- `/` Home
- `/submit` Submit photo
- `/gallery` Gallery
- `/login` Google OAuth login
- `/logout` Sign out

Health & Metrics
- `/health`, `/alive`, `/ready`, `/metrics`

## Data Flow

1. User authenticates with Google; app creates session
2. User submits photo, service stores file and creates DB record
3. Galleries query and display submissions with filters

## Performance Considerations

- Async I/O for DB and file operations
- Npgsql connection pooling
- File system storage for images
- DB indexing on common filters

## Scalability

- Stateless web tier (horizontal scale capable)
- PostgreSQL vertical scale; externalize to managed DB if needed
- Photos stored in database (can migrate to blob storage if needed for extreme scale)

## Maintenance

Database Backups
```bash
# Production server
sudo -u postgres pg_dump pathfinder_photography > backup.sql

# Local development (Aspire)
# PostgreSQL runs in container managed by Aspire
# Use Aspire Dashboard to access database or backup via connection string
```

View Logs
```bash
# Production server
sudo journalctl -u pathfinder-photography -f

# Aspire development
# View logs in Aspire Dashboard at http://localhost:15888
```

Update Dependencies
```bash
dotnet restore
dotnet build
```

## Future Enhancements

- Admin approval workflow
- Photo editing capabilities
- Bulk export to PDF/Excel
- Multi-group support
- Advanced search
- Photo rating system
- Social sharing
- Mobile app
- AI composition analysis

## Documentation

- `README.md`: Main documentation (in .github folder)
- `SETUP.md`: Detailed local development setup guide
- `DEPLOY.md`: Production deployment guide (wizard-style)
- `deploy/`: Step-by-step deployment instructions
- `CODE_SUMMARY.md`: This document

## Testing

Current
- Manual testing and build validation

Possible
- Unit tests for services
- Integration tests for database
- E2E tests with Playwright
- Load testing

## Support & Contact

- Review documentation
- Check Aspire Dashboard logs (dev)
- Check container logs (homelab)
- Open an issue on GitHub

---

Project: Photography Honor Application
Organization: CSDAC Pathfinders
Year:2025
Framework: .NET9.0 with Aspire
Status: Production Ready ✅
