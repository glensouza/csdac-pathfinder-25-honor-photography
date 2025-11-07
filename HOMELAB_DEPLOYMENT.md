# Home Lab Deployment Guide

This guide walks you through deploying the Pathfinder Photography application in your home lab using the pre-built Docker image from GitHub Container Registry.

## Prerequisites

### On Your Home Lab Server
- Docker (20.10+)
- Docker Compose (v2+)
- At least2GB free disk space
- Internet connection (to pull images)
- Open ports: default8080 (app) and5432 (PostgreSQL) or customize

### Configuration Requirements
- Google OAuth Client ID & Secret (see Redirect URIs below)
- Secure PostgreSQL password
- (Optional) Email SMTP credentials for notifications
- (Optional) SigNoz observability stack
  - With Aspire: Automatically started and configured
  - With Docker Compose: Enable via `--profile signoz`

## Quick Start (Recommended)

Use the provided script to automate setup:

```bash
curl -sSL -o deploy-homelab.sh https://raw.githubusercontent.com/glensouza/csdac-pathfinder-25-honor-photography/main/deploy-homelab.sh
bash deploy-homelab.sh --signoz # add --signoz to enable observability stack
```

Options:
- `-d <dir>` custom deploy directory (default: `~/pathfinder-photography`)
- `-p <port>` custom host port for app (default: `8080`)
- `--update` pull latest images and recreate containers
- `--signoz` enable SigNoz profile services

## Manual Steps

###1. Download Deployment Files
```bash
mkdir -p ~/pathfinder-photography
cd ~/pathfinder-photography
curl -o docker-compose.yml https://raw.githubusercontent.com/glensouza/csdac-pathfinder-25-honor-photography/main/docker-compose.yml
curl -o .env https://raw.githubusercontent.com/glensouza/csdac-pathfinder-25-honor-photography/main/.env.example
```

###2. Configure Environment Variables
Edit `.env` and set at least:
```env
GOOGLE_CLIENT_ID=your_google_client_id_here.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your_google_client_secret_here
POSTGRES_PASSWORD=your_secure_password_here
# Optional Email config
EMAIL_SMTP_HOST=smtp.gmail.com
EMAIL_SMTP_PORT=587
EMAIL_SMTP_USERNAME=your-email@gmail.com
EMAIL_SMTP_PASSWORD=your-16-char-app-password
EMAIL_USE_SSL=true
EMAIL_FROM_ADDRESS=your-email@gmail.com
EMAIL_FROM_NAME=Pathfinder Photography
```

###3. Start
```bash
# Basic
docker compose pull
docker compose up -d
# With SigNoz observability stack
docker compose --profile signoz up -d
```

###4. Access
- App: `http://your-server-ip:8080`
- Health: `/health`, `/alive`, `/ready`
- Metrics: `/metrics`
- SigNoz UI (if enabled): `http://your-server-ip:3301`

## Google OAuth Redirect URIs (Add in Google Console)
- `http://your-server-ip:8080/signin-google`
- `http://hostname:8080/signin-google`
- If reverse proxy + HTTPS: `https://pathfinder.yourdomain.com/signin-google`

## First Admin User Bootstrap
The first authenticated user becomes Admin automatically. Promote additional admins via SQL:
```sql
UPDATE "Users" SET "Role" =2 WHERE "Email" = 'email@example.com';
```

## Reverse Proxy (Optional)

### Nginx
```nginx
server {
 listen80;
 server_name pathfinder.yourdomain.com;
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
TLS via Certbot:
```bash
sudo certbot --nginx -d pathfinder.yourdomain.com
```

### Traefik
```yaml
labels:
 - "traefik.enable=true"
 - "traefik.http.routers.pathfinder.rule=Host(`pathfinder.yourdomain.com`)"
 - "traefik.http.routers.pathfinder.entrypoints=websecure"
 - "traefik.http.routers.pathfinder.tls.certresolver=letsencrypt"
 - "traefik.http.services.pathfinder.loadbalancer.server.port=8080"
```

## Environment Variables Summary
Core:
- `GOOGLE_CLIENT_ID`, `GOOGLE_CLIENT_SECRET`
- `POSTGRES_PASSWORD`
Compose injects:
- `ConnectionStrings__DefaultConnection`
Optional Email:
- `EMAIL_SMTP_HOST`, `EMAIL_SMTP_PORT`, `EMAIL_SMTP_USERNAME`, `EMAIL_SMTP_PASSWORD`, `EMAIL_USE_SSL`, `EMAIL_FROM_ADDRESS`, `EMAIL_FROM_NAME`
App runtime:
- `ASPNETCORE_URLS`, `ASPNETCORE_ENVIRONMENT`
SigNoz (optional):
- `OTEL_EXPORTER_OTLP_ENDPOINT` (already set), `OTEL_RESOURCE_ATTRIBUTES`

## Management Commands
```bash
# Status
docker compose ps
# Logs (all)
docker compose logs -f
# App logs
docker compose logs -f pathfinder-app
# Restart
docker compose restart pathfinder-app
# Stop
docker compose down
# Update
docker compose pull && docker compose up -d
# Start with SigNoz profile
docker compose --profile signoz up -d
```

## Database Access
```bash
docker exec -it pathfinder-postgres psql -U postgres -d pathfinder_photography
```

Backup:
```bash
docker exec -t pathfinder-postgres pg_dump -U postgres pathfinder_photography > backup-$(date +%Y%m%d).sql
```

Restore:
```bash
docker exec -i pathfinder-postgres psql -U postgres pathfinder_photography < backup-YYYYMMDD.sql
```

## Admin & PDF Export
- Admin Dashboard: `/admin/dashboard`
- User Management: `/admin/users`
  - Promote/demote users between Pathfinder and Instructor roles
  - Delete users and all their submissions (permanently removes user, photos, and votes)
  - View all registered users
- PDF Export: `/admin/export`

## SigNoz (Optional)

### With Aspire (Development)
When running via `dotnet run --project PathfinderPhotography.AppHost`, SigNoz is automatically included:
- All 5 SigNoz containers start automatically
- OTLP endpoints and configuration auto-injected
- UI available at http://localhost:3301

### With Docker Compose (Home Lab)
Enable via profile:
```bash
docker compose --profile signoz up -d
```
UI: `http://localhost:3301`
Ports exposed (default): 3301(UI), 4317/4318(OTLP), 9000/8123(ClickHouse), 6060(Query), 8081(API), 9093(Alertmanager)

## Troubleshooting
- Check logs: `docker compose logs pathfinder-app`
- Validate config: `docker compose config`
- Check ports & firewall
- Verify Google redirect URIs match exactly
- SigNoz issues: ensure profile enabled & collector reachable

---
Deployed from image: `ghcr.io/glensouza/csdac-pathfinder-25-honor-photography`
