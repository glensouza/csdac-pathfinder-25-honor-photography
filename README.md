# Photography Honor - Corona SDA Pathfinders

A Blazor Server web application for SDA Pathfinders from Corona SDA church to submit photos demonstrating 10 photography composition rules.

## Features

- **Google Authentication**: Secure login with Google accounts to track submissions
- **Educational Content**: Learn about 10 essential photography composition rules with descriptions and explanations
- **Photo Submission**: Upload photos for each composition rule with personal descriptions
- **Automatic Name Tracking**: User names are automatically pulled from Google account
- **Gallery View**: Browse all submitted photos with filtering by rule or pathfinder name
- **PostgreSQL Database**: Robust data persistence for submissions
- **Docker Support**: Easy deployment using Docker containers

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

## Prerequisites

- .NET 9.0 SDK (for local development)
- PostgreSQL 16+ (for local development)
- Docker and Docker Compose (for containerized deployment)
- Google OAuth 2.0 credentials (see setup instructions below)

## Google OAuth Setup

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project or select an existing one
3. Enable the Google+ API
4. Go to "Credentials" → "Create Credentials" → "OAuth 2.0 Client ID"
5. Configure the OAuth consent screen
6. For Application type, select "Web application"
7. Add authorized redirect URIs:
   - For local development: `https://localhost:5001/signin-google`
   - For production: `https://your-domain.com/signin-google`
8. Copy the Client ID and Client Secret
9. Create a `.env` file (copy from `.env.example`) and add your credentials

## Running Locally

### With Docker Compose (Recommended)

1. Copy `.env.example` to `.env` and add your Google OAuth credentials:
   ```bash
   cp .env.example .env
   # Edit .env and add your GOOGLE_CLIENT_ID and GOOGLE_CLIENT_SECRET
   ```

2. Start the application:
   ```bash
   docker-compose up -d
   ```

3. The application will be available at `http://localhost:8080`

4. To view logs:
   ```bash
   docker-compose logs -f pathfinder-photography
   ```

5. To stop the application:
   ```bash
   docker-compose down
   ```

### Manual Setup (Development)

1. Install PostgreSQL and create a database:
   ```sql
   CREATE DATABASE pathfinder_photography;
   ```

2. Update `appsettings.Development.json` with your PostgreSQL connection string and Google OAuth credentials

3. Apply database migrations:
   ```bash
   dotnet ef database update
   ```

4. Run the application:
   ```bash
   dotnet run
   ```

5. The application will be available at `https://localhost:5001` or `http://localhost:5000`

## Database Migrations

To create a new migration after model changes:
```bash
dotnet ef migrations add MigrationName
```

To apply migrations:
```bash
dotnet ef database update
```

## Data Persistence

- **Photos**: Stored in `wwwroot/uploads/` directory
- **Submission Data**: Stored in PostgreSQL database
- **Database**: When using Docker, PostgreSQL data is stored in a named volume

When using Docker, these are persisted across container restarts.

## Usage

1. **Sign In**: Click "Sign in with Google" to authenticate
2. **View Rules**: Navigate to the home page to learn about all 10 composition rules
3. **Submit Photos**: Click "Submit Photos" in the navigation menu
4. **Your Name**: Your name is automatically pulled from your Google account
5. **Select a Rule**: Choose which composition rule you're demonstrating
6. **Upload Photo**: Select your photo file (max 10MB)
7. **Describe**: Explain how you applied the rule in your photo
8. **Submit**: Click submit to save your photo
9. **View Gallery**: Check out all submitted photos in the gallery

## Technology Stack

- **Framework**: ASP.NET Core Blazor Server (.NET 9.0)
- **Database**: PostgreSQL 16
- **ORM**: Entity Framework Core
- **Authentication**: Google OAuth 2.0
- **UI**: Bootstrap 5
- **Container**: Docker & Docker Compose

## Project Structure

```
├── Components/
│   ├── Layout/         # Navigation and layout components
│   └── Pages/          # Blazor pages (Home, Submit, Gallery)
├── Data/              # Database context and migrations
├── Models/            # Data models
├── Services/          # Business logic services
├── wwwroot/          # Static files
│   └── uploads/      # Uploaded photos
├── Migrations/       # EF Core migrations
├── Dockerfile        # Docker configuration
├── docker-compose.yml # Docker Compose configuration
└── .env.example      # Example environment variables
```

## Environment Variables

- `ConnectionStrings__DefaultConnection`: PostgreSQL connection string
- `Authentication__Google__ClientId`: Google OAuth Client ID
- `Authentication__Google__ClientSecret`: Google OAuth Client Secret
- `ASPNETCORE_URLS`: Application URLs (default: http://+:8080)
- `ASPNETCORE_ENVIRONMENT`: Environment (Development/Production)

## Security Notes

- Never commit `.env` file or real credentials to version control
- Use environment variables for sensitive configuration
- Google OAuth credentials should be kept secure
- In production, use HTTPS for all connections
- Set appropriate CORS policies if needed

## Troubleshooting

### Cannot connect to PostgreSQL
- Ensure PostgreSQL service is running
- Check connection string in configuration
- When using Docker, ensure both services are on the same network

### Google OAuth not working
- Verify Client ID and Secret are correct
- Check authorized redirect URIs in Google Console
- Ensure HTTPS is used (required by Google OAuth)

### Photos not uploading
- Check disk space
- Verify uploads directory has write permissions
- Check file size (max 10MB)

## Future Enhancements

- Admin dashboard for reviewing submissions
- Export submissions to PDF or Excel
- Photo editing capabilities
- Support for multiple groups/churches
- Real-time collaboration features
- Email notifications

## License

See LICENSE file for details.

---

Presented by Daniels - Corona SDA Church Pathfinders 2025
