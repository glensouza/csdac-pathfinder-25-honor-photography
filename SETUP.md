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

## Google OAuth 2.0 Setup

### Step 1: Create a Google Cloud Project

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Click "Select a project" at the top
3. Click "NEW PROJECT"
4. Enter a project name (e.g., "Pathfinder Photography")
5. Click "CREATE"

### Step 2: Enable Google+ API

1. In your project, go to "APIs & Services" > "Library"
2. Search for "Google+ API"
3. Click on it and click "ENABLE"

### Step 3: Configure OAuth Consent Screen

1. Go to "APIs & Services" > "OAuth consent screen"
2. Select "External" user type (unless you have a Google Workspace)
3. Click "CREATE"
4. Fill in the required information:
   - **App name**: Pathfinder Photography
   - **User support email**: Your email
   - **Developer contact**: Your email
5. Click "SAVE AND CONTINUE"
6. On Scopes page, click "SAVE AND CONTINUE"
7. On Test users page (if applicable), add test users or skip
8. Click "SAVE AND CONTINUE"
9. Review and click "BACK TO DASHBOARD"

### Step 4: Create OAuth 2.0 Credentials

1. Go to "APIs & Services" > "Credentials"
2. Click "CREATE CREDENTIALS" > "OAuth client ID"
3. Select "Web application"
4. Enter a name (e.g., "Pathfinder Photography Web")
5. Add Authorized redirect URIs:
   
   **For local development:**
   ```
   https://localhost:5001/signin-google
   http://localhost:5000/signin-google
   ```
   
   **For Docker local:**
   ```
   http://localhost:8080/signin-google
   ```
   
   **For production (replace with your domain):**
   ```
   https://your-domain.com/signin-google
   ```

6. Click "CREATE"
7. Copy your **Client ID** and **Client Secret**

### Step 5: Configure Application

Create or edit `.env` file:
```env
GOOGLE_CLIENT_ID=your_client_id_here.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your_client_secret_here
```

For local development without Docker, edit `appsettings.Development.json`:
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

## Local Development Setup (Without Docker)

### Prerequisites
- .NET 9.0 SDK
- PostgreSQL 16 or later
- Google OAuth credentials

### Steps

1. **Install PostgreSQL**
   - Download from [postgresql.org](https://www.postgresql.org/download/)
   - Install and remember your postgres user password

2. **Create Database**
   ```bash
   # Connect to PostgreSQL
   psql -U postgres
   
   # Create database
   CREATE DATABASE pathfinder_photography;
   
   # Exit
   \q
   ```

3. **Configure Connection String**
   
   Edit `appsettings.Development.json`:
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

4. **Apply Migrations**
   ```bash
   dotnet ef database update
   ```

5. **Run Application**
   ```bash
   dotnet run
   ```

6. **Access Application**
   - HTTPS: https://localhost:5001
   - HTTP: http://localhost:5000

## Production Deployment

### Docker Deployment

1. **Build Docker Image**
   ```bash
   docker build -t pathfinder-photography:latest .
   ```

2. **Run with Docker Compose**
   ```bash
   # Set environment variables
   export GOOGLE_CLIENT_ID="your_client_id"
   export GOOGLE_CLIENT_SECRET="your_client_secret"
   
   # Start services
   docker-compose up -d
   ```

3. **Configure Reverse Proxy** (Nginx example)
   ```nginx
   server {
       listen 80;
       server_name your-domain.com;
       
       location / {
           proxy_pass http://localhost:8080;
           proxy_http_version 1.1;
           proxy_set_header Upgrade $http_upgrade;
           proxy_set_header Connection keep-alive;
           proxy_set_header Host $host;
           proxy_cache_bypass $http_upgrade;
           proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
           proxy_set_header X-Forwarded-Proto $scheme;
       }
   }
   ```

4. **Enable HTTPS** (using Let's Encrypt)
   ```bash
   sudo certbot --nginx -d your-domain.com
   ```

### Important Production Notes

- Always use HTTPS in production (required by Google OAuth)
- Update Google OAuth authorized redirect URIs with production domain
- Set strong passwords for PostgreSQL
- Consider using Docker secrets for sensitive data
- Regular backups of PostgreSQL database
- Monitor disk space for uploaded photos

## Troubleshooting

### "Invalid redirect_uri" Error
- Check that redirect URI in Google Console matches exactly
- Ensure protocol (http/https) matches
- Make sure port number is correct

### Cannot Connect to Database
- Verify PostgreSQL is running: `sudo systemctl status postgresql`
- Check connection string credentials
- For Docker: Ensure containers are on same network

### Photos Not Uploading
- Check `wwwroot/uploads/` directory exists and is writable
- Verify file size is under 10MB
- Check available disk space

### Application Won't Start
- Check all environment variables are set
- Review logs: `docker-compose logs pathfinder-photography`
- Verify no port conflicts (8080, 5432)

## Database Management

### Setting Up Admin Users

The application has three user roles:
- **Pathfinder** (default): Can submit photos and vote
- **Instructor**: Can grade submissions in addition to Pathfinder capabilities
- **Admin**: Can manage user roles (promote/demote between Pathfinder and Instructor)

Admin users must be created manually in the database. To create an admin user:

```bash
# Using Docker
docker exec -it postgres psql -U postgres pathfinder_photography

# Local PostgreSQL
psql -U postgres pathfinder_photography

# Set a user as admin (replace email@example.com with the actual email)
UPDATE "Users" SET "Role" = 2 WHERE "Email" = 'email@example.com';

# Verify the change
SELECT "Name", "Email", "Role" FROM "Users";

# Exit
\q
```

User role values:
- `0` = Pathfinder (default)
- `1` = Instructor
- `2` = Admin

Once a user is set as Admin, they can:
- Access the User Management page at `/admin/users`
- Promote Pathfinders to Instructors
- Demote Instructors back to Pathfinders
- View all users and their roles

**Note**: Admin role can only be assigned/removed through direct database updates, not through the web interface.

### Backup Database
```bash
# Using Docker
docker exec -t postgres pg_dump -U postgres pathfinder_photography > backup.sql

# Local PostgreSQL
pg_dump -U postgres pathfinder_photography > backup.sql
```

### Restore Database
```bash
# Using Docker
docker exec -i postgres psql -U postgres pathfinder_photography < backup.sql

# Local PostgreSQL
psql -U postgres pathfinder_photography < backup.sql
```

### View Database Contents
```bash
# Using Docker
docker exec -it postgres psql -U postgres pathfinder_photography

# Local
psql -U postgres pathfinder_photography

# List tables
\dt

# Query submissions
SELECT * FROM "PhotoSubmissions";

# Exit
\q
```

## Support

For issues or questions:
- Check the [README.md](README.md)
- Review application logs
- Contact the development team

---

Corona SDA Church Pathfinders 2025
