# Home Lab Deployment Guide

This guide walks you through deploying the Pathfinder Photography application in your home lab using the pre-built Docker image from GitHub Container Registry.

## Prerequisites

### On Your Home Lab Server

- Docker installed (version 20.10 or later)
- Docker Compose installed (version 2.0 or later)
- At least 2GB free disk space
- Internet connection to pull images
- Ports 8080 available (or customize in docker-compose)

### Configuration Requirements

- Google OAuth credentials (Client ID and Secret)
- GitHub Container Registry access (public images don't require authentication)

## Quick Start

### 1. Download Deployment Files

On your home lab server:

```bash
# Create deployment directory
mkdir -p ~/pathfinder-photography
cd ~/pathfinder-photography

# Download docker-compose file
curl -o docker-compose.yml https://raw.githubusercontent.com/glensouza/csdac-pathfinder-25-honor-photography/main/docker-compose.homelab.yml

# Download environment file template
curl -o .env https://raw.githubusercontent.com/glensouza/csdac-pathfinder-25-honor-photography/main/.env.example
```

### 2. Configure Environment Variables

Edit the `.env` file:

```bash
nano .env
```

Add your credentials:

```env
# Google OAuth Configuration
GOOGLE_CLIENT_ID=your_google_client_id_here.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your_google_client_secret_here

# Database Configuration
POSTGRES_PASSWORD=your_secure_password_here
```

### 3. Pull and Start the Application

```bash
# Pull the latest image
docker compose pull

# Start all services
docker compose up -d

# View logs
docker compose logs -f pathfinder-app
```

### 4. Access the Application

Open your browser to:
- **Application**: http://your-server-ip:8080
- **Or**: http://localhost:8080 (if accessing from server)

## Detailed Setup

### Port Configuration

To use a different port, edit `docker-compose.homelab.yml`:

```yaml
services:
  pathfinder-photography:
    ports:
      - "YOUR_PORT:8080"  # Change YOUR_PORT to desired port
```

### Reverse Proxy Setup (Recommended)

#### With Nginx

Create `/etc/nginx/sites-available/pathfinder`:

```nginx
server {
    listen 80;
    server_name pathfinder.yourdomain.com;

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

Enable the site:

```bash
sudo ln -s /etc/nginx/sites-available/pathfinder /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl reload nginx
```

#### With Traefik

Add labels to `docker-compose.homelab.yml`:

```yaml
services:
  pathfinder-photography:
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.pathfinder.rule=Host(`pathfinder.yourdomain.com`)"
      - "traefik.http.routers.pathfinder.entrypoints=websecure"
      - "traefik.http.routers.pathfinder.tls.certresolver=letsencrypt"
      - "traefik.http.services.pathfinder.loadbalancer.server.port=8080"
```

### SSL/TLS Configuration

#### With Let's Encrypt (Certbot)

```bash
# Install certbot
sudo apt install certbot python3-certbot-nginx

# Get certificate
sudo certbot --nginx -d pathfinder.yourdomain.com

# Auto-renewal is configured automatically
```

#### Update Google OAuth Redirect URIs

Add to your Google Cloud Console:
```
https://pathfinder.yourdomain.com/signin-google
```

## Management Commands

### View Logs

```bash
# All services
docker compose logs -f

# Just the app
docker compose logs -f pathfinder-app

# Just PostgreSQL
docker compose logs -f postgres
```

### Update to Latest Version

```bash
# Pull latest image
docker compose pull

# Recreate containers
docker compose up -d

# Clean up old images
docker image prune -f
```

### Restart Services

```bash
# Restart all services
docker compose restart

# Restart just the app
docker compose restart pathfinder-app
```

### Stop Services

```bash
# Stop all services
docker compose down

# Stop and remove volumes (WARNING: deletes data)
docker compose down -v
```

### Database Access

```bash
# Access PostgreSQL shell
docker exec -it pathfinder-postgres psql -U postgres -d pathfinder_photography

# Backup database
docker exec -t pathfinder-postgres pg_dump -U postgres pathfinder_photography > backup-$(date +%Y%m%d).sql

# Restore database
docker exec -i pathfinder-postgres psql -U postgres pathfinder_photography < backup-20250101.sql
```

## Monitoring

### Check Service Health

```bash
# Check if services are running
docker compose ps

# Check resource usage
docker stats pathfinder-app pathfinder-postgres
```

### Health Endpoints

- **Health Check**: http://your-server-ip:8080/health
- **Liveness**: http://your-server-ip:8080/alive
- **Readiness**: http://your-server-ip:8080/ready

### Log Monitoring

Set up log rotation for Docker:

Create `/etc/docker/daemon.json`:

```json
{
  "log-driver": "json-file",
  "log-opts": {
    "max-size": "10m",
    "max-file": "3"
  }
}
```

Restart Docker:

```bash
sudo systemctl restart docker
docker compose up -d
```

## Backup Strategy

### Automated Backups

Create a backup script `/usr/local/bin/backup-pathfinder.sh`:

```bash
#!/bin/bash
BACKUP_DIR="/backups/pathfinder"
DATE=$(date +%Y%m%d_%H%M%S)

# Create backup directory
mkdir -p $BACKUP_DIR

# Backup database
docker exec -t pathfinder-postgres pg_dump -U postgres pathfinder_photography > $BACKUP_DIR/db-$DATE.sql

# Backup uploads
tar -czf $BACKUP_DIR/uploads-$DATE.tar.gz -C ~/pathfinder-photography uploads/

# Keep only last 7 days
find $BACKUP_DIR -name "*.sql" -mtime +7 -delete
find $BACKUP_DIR -name "*.tar.gz" -mtime +7 -delete
```

Make executable and schedule:

```bash
chmod +x /usr/local/bin/backup-pathfinder.sh

# Add to crontab (daily at 2 AM)
crontab -e
0 2 * * * /usr/local/bin/backup-pathfinder.sh
```

## Troubleshooting

### Container Won't Start

```bash
# Check logs
docker compose logs pathfinder-app

# Check if ports are in use
sudo netstat -tulpn | grep 8080

# Verify environment variables
docker compose config
```

### Cannot Connect to Database

```bash
# Check PostgreSQL is running
docker compose ps postgres

# Check PostgreSQL logs
docker compose logs postgres

# Test connection
docker exec -it pathfinder-postgres psql -U postgres -c "SELECT version();"
```

### Image Pull Errors

```bash
# For private repositories, login to GHCR
echo $GITHUB_TOKEN | docker login ghcr.io -u USERNAME --password-stdin

# Pull specific version
docker pull ghcr.io/glensouza/csdac-pathfinder-25-honor-photography:v1.0.0
```

### High Memory Usage

```bash
# Set memory limits in docker-compose.homelab.yml
services:
  pathfinder-photography:
    deploy:
      resources:
        limits:
          memory: 512M
        reservations:
          memory: 256M
```

## Security Hardening

### Firewall Configuration

```bash
# Allow only necessary ports
sudo ufw allow 8080/tcp
sudo ufw allow 443/tcp  # If using HTTPS
sudo ufw enable
```

### Network Isolation

The compose file uses a dedicated network. Containers can only communicate within this network.

### Secrets Management

Instead of `.env` file, use Docker secrets:

```bash
# Create secrets
echo "your_secret" | docker secret create google_client_id -
echo "your_secret" | docker secret create google_client_secret -

# Update docker-compose.homelab.yml to use secrets
```

## Updating Configuration

### Change Environment Variables

```bash
# Edit .env file
nano .env

# Recreate containers
docker compose up -d
```

### Change Image Version

Edit `docker-compose.homelab.yml`:

```yaml
services:
  pathfinder-photography:
    image: ghcr.io/glensouza/csdac-pathfinder-25-honor-photography:v1.2.0
```

Then:

```bash
docker compose pull
docker compose up -d
```

## Performance Optimization

### Enable Docker BuildKit

```bash
# Add to /etc/docker/daemon.json
{
  "features": {
    "buildkit": true
  }
}
```

### Use Docker Volumes for Better Performance

Already configured in the compose file for uploads and database.

## Migration from Development

If migrating from a development setup:

1. Backup your data
2. Export uploads directory
3. Dump PostgreSQL database
4. Deploy on home lab
5. Restore database
6. Copy uploads

## Monitoring with Prometheus/Grafana (Optional)

The application exposes metrics at `/metrics` endpoint.

Add to your existing Prometheus configuration:

```yaml
scrape_configs:
  - job_name: 'pathfinder-app'
    static_configs:
      - targets: ['localhost:8080']
```

## Support

For issues:
1. Check logs: `docker compose logs`
2. Verify configuration: `docker compose config`
3. Check service status: `docker compose ps`
4. Review this guide
5. Check GitHub issues

## Quick Reference

### Common Commands

```bash
# Start
docker compose up -d

# Stop
docker compose down

# Logs
docker compose logs -f

# Update
docker compose pull && docker compose up -d

# Backup DB
docker exec -t pathfinder-postgres pg_dump -U postgres pathfinder_photography > backup.sql

# Restart
docker compose restart pathfinder-app
```

### Directory Structure

```
~/pathfinder-photography/
├── docker-compose.yml        # Deployment configuration
├── .env                       # Environment variables
├── uploads/                   # Photo uploads (created automatically)
└── backups/                   # Backups (if using backup script)
```

---

**Deployed From**: GitHub Container Registry
**Image**: ghcr.io/glensouza/csdac-pathfinder-25-honor-photography
**Organization**: Corona SDA Church Pathfinders
**Year**: 2025
