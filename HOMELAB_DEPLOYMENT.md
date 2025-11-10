# Home Lab Deployment Guide

This guide walks you through deploying the Pathfinder Photography application in your home lab using the pre-built Docker image from GitHub Container Registry.

## Prerequisites

### On Your Home Lab Server
- Docker (20.10+)
- Docker Compose (v2+)
- At least 2GB free disk space
- Internet connection (to pull images)
- Open ports: default 8080 (app) or customize

### External Services (Optional)
If you're hosting PostgreSQL or SigNoz on separate LXC containers in Proxmox:
- **PostgreSQL**: A running PostgreSQL 16+ instance accessible from the application container
- **SigNoz**: A running SigNoz instance with OTLP collector accessible on port 4317

### Configuration Requirements
- Google OAuth Client ID & Secret (see Redirect URIs below)
- (If using local PostgreSQL) Secure PostgreSQL password
- (If using external PostgreSQL) Full database connection details
- (Optional) Email SMTP credentials for notifications
- (Optional) External SigNoz OTLP endpoint URL

## Quick Start (Recommended)

Use the provided script to automate setup:

```bash
curl -sSL -o deploy-homelab.sh https://raw.githubusercontent.com/glensouza/csdac-pathfinder-25-honor-photography/main/deploy-homelab.sh
bash deploy-homelab.sh [OPTIONS]
```

Options:
- `-d <dir>` custom deploy directory (default: `~/pathfinder-photography`)
- `-p <port>` custom host port for app (default: `8080`)
- `--update` pull latest images and recreate containers
- `--postgres` enable local PostgreSQL container (use profile)
- `--signoz` enable local SigNoz observability stack (use profile)
- `--external-db` use external PostgreSQL (requires DB_CONNECTION_STRING in .env)
- `--external-signoz` use external SigNoz (requires OTEL_EXPORTER_OTLP_ENDPOINT in .env)

Examples:
```bash
# Deploy with local PostgreSQL and SigNoz
bash deploy-homelab.sh --postgres --signoz

# Deploy with external PostgreSQL on separate LXC
bash deploy-homelab.sh --external-db

# Deploy with both external services
bash deploy-homelab.sh --external-db --external-signoz

# Deploy app only (no database, no observability)
bash deploy-homelab.sh
```

## Manual Steps

###1. Download Deployment Files
```bash
mkdir -p ~/pathfinder-photography
cd ~/pathfinder-photography
curl -o docker-compose.yml https://raw.githubusercontent.com/glensouza/csdac-pathfinder-25-honor-photography/main/docker-compose.yml
curl -o .env https://raw.githubusercontent.com/glensouza/csdac-pathfinder-25-honor-photography/main/.env.example
```

###2. Configure Environment Variables
Edit `.env` and configure based on your deployment scenario:

#### Scenario 1: Local PostgreSQL and Local SigNoz (All-in-One)
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

#### Scenario 2: External PostgreSQL (Separate LXC in Proxmox)
```env
GOOGLE_CLIENT_ID=your_google_client_id_here.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your_google_client_secret_here
# Full connection string for external PostgreSQL
DB_CONNECTION_STRING=Host=192.168.1.100;Port=5432;Database=pathfinder_photography;Username=postgres;Password=your_password
# Optional Email config...
```

#### Scenario 3: External SigNoz (Separate LXC in Proxmox)
```env
GOOGLE_CLIENT_ID=your_google_client_id_here.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your_google_client_secret_here
POSTGRES_PASSWORD=your_secure_password_here
# External SigNoz OTLP endpoint
OTEL_EXPORTER_OTLP_ENDPOINT=http://192.168.1.101:4317
# Optional Email config...
```

#### Scenario 4: Both External (PostgreSQL and SigNoz on Separate LXCs)
```env
GOOGLE_CLIENT_ID=your_google_client_id_here.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your_google_client_secret_here
# External PostgreSQL
DB_CONNECTION_STRING=Host=192.168.1.100;Port=5432;Database=pathfinder_photography;Username=postgres;Password=your_password
# External SigNoz
OTEL_EXPORTER_OTLP_ENDPOINT=http://192.168.1.101:4317
# Optional Email config...
```

#### Scenario 5: No Observability
```env
GOOGLE_CLIENT_ID=your_google_client_id_here.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your_google_client_secret_here
POSTGRES_PASSWORD=your_secure_password_here
# Disable SigNoz
OTEL_EXPORTER_OTLP_ENDPOINT=
# Optional Email config...
```

### 3. Start Services
Choose the appropriate command based on your deployment scenario:

```bash
# Scenario 1: Local PostgreSQL and Local SigNoz
docker compose pull
docker compose --profile postgres --profile signoz up -d

# Scenario 2: External PostgreSQL, Local SigNoz
docker compose --profile signoz up -d

# Scenario 3: Local PostgreSQL, External SigNoz
docker compose --profile postgres up -d

# Scenario 4: Both External (PostgreSQL and SigNoz)
docker compose up -d

# Scenario 5: Local PostgreSQL, No Observability
docker compose --profile postgres up -d

# Basic (app only, both external)
docker compose pull
docker compose up -d
```

### 4. Access
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

## External PostgreSQL Setup (Proxmox LXC)

If you're running PostgreSQL on a separate LXC container in Proxmox:

### 1. Set up PostgreSQL LXC
```bash
# On the PostgreSQL LXC
apt update && apt install -y postgresql postgresql-contrib

# Create database and user
sudo -u postgres psql
CREATE DATABASE pathfinder_photography;
CREATE USER pathfinder WITH PASSWORD 'your_secure_password';
GRANT ALL PRIVILEGES ON DATABASE pathfinder_photography TO pathfinder;
\q

# Configure PostgreSQL to accept remote connections
# Edit /etc/postgresql/16/main/postgresql.conf
listen_addresses = '*'

# Edit /etc/postgresql/16/main/pg_hba.conf (add this line)
host    pathfinder_photography    pathfinder    0.0.0.0/0    scram-sha-256

# Restart PostgreSQL
systemctl restart postgresql
```

### 2. Configure Application .env
```env
DB_CONNECTION_STRING=Host=<postgresql-lxc-ip>;Port=5432;Database=pathfinder_photography;Username=pathfinder;Password=your_secure_password
```

### 3. Deploy without --profile postgres
```bash
docker compose up -d
```

## External SigNoz Setup (Proxmox LXC)

If you're running SigNoz on a separate LXC container in Proxmox:

### 1. Set up SigNoz LXC
```bash
# On the SigNoz LXC, follow SigNoz installation guide
# https://signoz.io/docs/install/docker/
git clone https://github.com/SigNoz/signoz.git
cd signoz/deploy
./install.sh
```

### 2. Ensure OTLP endpoint is accessible
Make sure port 4317 (gRPC) is accessible from the application container.

### 3. Configure Application .env
```env
OTEL_EXPORTER_OTLP_ENDPOINT=http://<signoz-lxc-ip>:4317
```

### 4. Deploy without --profile signoz
```bash
docker compose up -d
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
  - Delete users and all their data (permanently removes user, their photo submissions, votes on their photos, and votes made by the user)
  - View all registered users
- PDF Export: `/admin/export`

## SigNoz (Optional)

SigNoz provides observability with distributed tracing, metrics, and logs. It uses an nginx reverse proxy to route API requests.

### With Aspire (Development)
When running via `dotnet run --project PathfinderPhotography.AppHost`, SigNoz is automatically included:
- All SigNoz containers start automatically (including nginx proxy)
- OTLP endpoints and configuration auto-injected
- **UI available at http://localhost:3302** (through nginx reverse proxy)

### With Docker Compose (Home Lab)
Enable via profile:
```bash
docker compose --profile signoz up -d
```
**UI: http://localhost:3301** (through nginx reverse proxy)

**Architecture:**
- The nginx proxy (port 3301) routes `/api/*` to the query service backend
- All other requests go to the frontend UI
- This ensures registration and other API calls work correctly

Ports exposed: 3301(Proxy/UI), 4317/4318(OTLP), 9000/8123(ClickHouse), 6060(Query), 8081(API), 9093(Alertmanager)

## Troubleshooting

### Sysctl Permission Errors

If you encounter an error like:
```
OCI runtime create failed: runc create failed: unable to start container process: 
error during container init: open sysctl net.ipv4.ip_unprivileged_port_start file: 
reopen fd 8: permission denied
```

**This has been fixed in the docker-compose.yml** with explicit `sysctls` configuration. However, if you still encounter issues in highly restricted environments:

**Option 1: Remove sysctls (if still present in an older version)**
Remove or comment out the `sysctls:` sections from the `postgres` and `signoz-nginx` services in docker-compose.yml.

**Option 2: Use host network mode (NOT RECOMMENDED)**
Only as a last resort for testing, you can try host network mode, but this is not secure for production:
```yaml
# NOT RECOMMENDED - only for troubleshooting
network_mode: host
```

**Option 3: Run Docker with appropriate privileges**
Ensure Docker daemon has permission to modify sysctls. On some VPS providers, you may need to contact support or use a different container runtime.

### General Troubleshooting
- Check logs: `docker compose logs pathfinder-app`
- Validate config: `docker compose config`
- Check ports & firewall
- Verify Google redirect URIs match exactly
- SigNoz issues: ensure profile enabled & collector reachable

---
Deployed from image: `ghcr.io/glensouza/csdac-pathfinder-25-honor-photography`
