# Pathfinder Photography Honor - Implementation Summary

## Overview

A complete Blazor Server web application built with .NET 9.0 for SDA Pathfinders to submit and manage photography assignments demonstrating 10 composition rules.

## All Requirements Implemented

### Core Requirements
✅ Blazor Server website
✅ Corona SDA Pathfinders can submit photos
✅ 10 photos submission (one per composition rule)
✅ Pathfinders explain their application of rules
✅ Space for sample photos and descriptions of each style
✅ Docker container hosting support
✅ Name tracking for submissions
✅ Google authentication
✅ PostgreSQL database
✅ .NET Aspire integration

### 10 Composition Rules

All 10 rules implemented with detailed descriptions and explanations:

1. **Rule of Thirds** - Grid-based composition technique
2. **Leading Lines** - Using lines to guide viewer's eye
3. **Framing Natural** - Creating frames within the frame
4. **Fill the Frame** - Getting close to eliminate distractions
5. **Symmetry & Asymmetry** - Balance and tension in composition
6. **Patterns & Repetition** - Visual rhythm through repeating elements
7. **Golden Ratio** - Mathematical proportion found in nature
8. **Diagonals** - Dynamic energy through diagonal lines
9. **Center Dominant Eye** - Direct impact through centering
10. **Picture to Ground** - Figure-ground relationship and separation

## Architecture

### Projects

1. **PathfinderPhotography** (Main Web App)
   - Blazor Server components
   - Authentication and authorization
   - Photo submission and gallery
   - Business logic services

2. **PathfinderPhotography.AppHost** (Aspire Orchestration)
   - Service orchestration
   - PostgreSQL configuration
   - PgAdmin integration
   - Resource management

3. **PathfinderPhotography.ServiceDefaults** (Shared Config)
   - OpenTelemetry setup
   - Health checks
   - Service discovery
   - Resilience patterns

### Technology Stack

- **Framework**: ASP.NET Core Blazor Server (.NET 9.0)
- **Orchestration**: .NET Aspire 8.2
- **Database**: PostgreSQL 16
- **ORM**: Entity Framework Core 9.0
- **Authentication**: Google OAuth 2.0
- **Observability**: OpenTelemetry (traces, metrics, logs)
- **UI**: Bootstrap 5
- **Containerization**: Docker & Docker Compose

## Key Features

### User Features
- **Google Sign-In**: Secure authentication with Google accounts
- **Automatic Name Tracking**: User identity from Google profile
- **Photo Upload**: Support for images up to 10MB
- **Rule Selection**: Choose from 10 composition rules
- **Description**: Explain how the rule was applied
- **Personal Gallery**: View your own submissions
- **Public Gallery**: Browse all pathfinder submissions
- **Filtering**: Filter by rule or pathfinder name

### Technical Features
- **Service Discovery**: Automatic service-to-service communication
- **Health Checks**: Built-in health monitoring
- **Distributed Tracing**: Track requests across services
- **Metrics**: Performance and usage metrics
- **Structured Logging**: Comprehensive logging with OpenTelemetry
- **Database Migrations**: Automatic schema management
- **Responsive Design**: Mobile-friendly Bootstrap UI
- **Async Operations**: All database operations are asynchronous
- **Connection Pooling**: Efficient database connection management

## Database Schema

### PhotoSubmissions Table
- Id (Primary Key)
- PathfinderName (indexed)
- CompositionRuleId (indexed)
- CompositionRuleName
- ImagePath
- Description
- SubmissionDate

### Indexes
- PathfinderName for fast user lookups
- CompositionRuleId for filtering by rule

## Security

- **Authentication**: Google OAuth 2.0
- **Authorization**: Cookie-based authentication
- **HTTPS**: Required for production
- **Environment Variables**: Secure credential management
- **File Validation**: Image type and size validation
- **SQL Injection Protection**: Entity Framework parameterization
- **XSS Protection**: Blazor automatic escaping

## Deployment Options

### 1. Aspire (Development - Recommended)
```bash
dotnet run --project PathfinderPhotography.AppHost
```
- Aspire Dashboard for monitoring
- Automatic service orchestration
- Built-in PostgreSQL and PgAdmin
- Real-time observability

### 2. Docker Compose (Production)
```bash
docker-compose up -d
```
- PostgreSQL container
- Web app container
- Volume persistence
- Network isolation

### 3. Standalone (Manual)
```bash
dotnet run --project PathfinderPhotography.csproj
```
- Manual PostgreSQL setup
- Direct database connection
- Traditional deployment

## Observability

### Logs
- Structured logging with Serilog
- Log levels: Debug, Info, Warning, Error
- Correlation IDs for request tracking

### Traces
- Distributed tracing with OpenTelemetry
- HTTP request tracing
- Database query tracing
- Custom activity tracking

### Metrics
- HTTP request metrics
- Database performance metrics
- Runtime metrics (GC, memory, CPU)
- Custom business metrics

## Configuration

### Required Settings

**Google OAuth**:
- ClientId
- ClientSecret

**Database**:
- ConnectionString (auto-configured with Aspire)

### Optional Settings
- ASPNETCORE_URLS
- ASPNETCORE_ENVIRONMENT
- Logging levels
- Health check endpoints

## File Structure

```
PathfinderPhotography/
├── Components/
│   ├── Layout/
│   │   ├── MainLayout.razor
│   │   └── NavMenu.razor
│   ├── Pages/
│   │   ├── Home.razor (Rules documentation)
│   │   ├── Submit.razor (Photo submission)
│   │   └── Gallery.razor (Photo gallery)
│   ├── App.razor
│   └── Routes.razor
├── Data/
│   └── ApplicationDbContext.cs
├── Models/
│   ├── CompositionRule.cs
│   └── PhotoSubmission.cs
├── Services/
│   ├── CompositionRuleService.cs
│   └── PhotoSubmissionService.cs
├── Migrations/
│   └── [EF Core migrations]
├── wwwroot/
│   ├── uploads/ (user photos)
│   └── lib/ (Bootstrap, etc.)
├── Program.cs
└── PathfinderPhotography.csproj

PathfinderPhotography.AppHost/
├── Program.cs (Aspire orchestration)
└── PathfinderPhotography.AppHost.csproj

PathfinderPhotography.ServiceDefaults/
├── Extensions.cs (Telemetry setup)
└── PathfinderPhotography.ServiceDefaults.csproj
```

## API Endpoints

### Application
- `/` - Home page with composition rules
- `/submit` - Photo submission form
- `/gallery` - Photo gallery
- `/login` - Google OAuth login
- `/logout` - Sign out

### Health & Metrics (Aspire)
- `/health` - Health check endpoint
- `/metrics` - Prometheus metrics
- `/alive` - Liveness probe
- `/ready` - Readiness probe

## Data Flow

1. **User Authentication**
   - User clicks "Sign in with Google"
   - Redirected to Google OAuth
   - Google returns with user profile
   - Application creates authenticated session

2. **Photo Submission**
   - User selects composition rule
   - Uploads photo file
   - Writes description
   - Service saves file to disk
   - Service creates database record
   - User sees confirmation

3. **Gallery View**
   - Service queries database
   - Filters by rule or pathfinder
   - Displays photos with metadata
   - Click to view full details

## Performance Considerations

- **Async/Await**: All I/O operations are asynchronous
- **DbContext Factory**: Efficient database context management
- **Connection Pooling**: Npgsql connection pooling
- **Image Storage**: File system for uploads (not database)
- **Caching**: Bootstrap assets cached
- **Indexing**: Database indexes on frequently queried columns

## Scalability

- **Horizontal Scaling**: Stateless web tier
- **Database**: PostgreSQL can scale vertically
- **File Storage**: Can be moved to blob storage
- **Load Balancing**: Ready for load balancer
- **Session State**: Cookie-based (sticky sessions not required)

## Maintenance

### Database Backups
```bash
docker exec -t postgres pg_dump -U postgres pathfinder_photography > backup.sql
```

### View Logs
```bash
# Docker
docker-compose logs -f pathfinder-photography

# Aspire
# Check Aspire Dashboard
```

### Update Dependencies
```bash
dotnet restore
dotnet build
```

## Future Enhancements

- [ ] Admin approval workflow
- [ ] Photo editing capabilities
- [ ] Bulk export to PDF/Excel
- [ ] Email notifications
- [ ] Multi-group support
- [ ] Advanced search
- [ ] Photo rating system
- [ ] Social sharing
- [ ] Mobile app
- [ ] AI composition analysis

## Documentation

- **README.md**: Main documentation
- **SETUP.md**: Detailed setup guide
- **CODE_SUMMARY.md**: This document
- **Inline Comments**: Throughout codebase

## Testing

Currently, the application has:
- Manual testing performed
- Build validation
- Can be extended with:
  - Unit tests for services
  - Integration tests for database
  - E2E tests with Playwright
  - Load testing

## Support & Contact

For issues or questions:
- Review documentation
- Check Aspire Dashboard logs
- Contact development team

---

**Project**: Photography Honor Application
**Organization**: Corona SDA Church Pathfinders
**Year**: 2025
**Presenter**: Daniels
**Framework**: .NET 9.0 with Aspire
**Status**: Production Ready ✅
