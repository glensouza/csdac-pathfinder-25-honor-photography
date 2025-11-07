#!/bin/bash

# Pathfinder Photography - Home Lab Deployment Script
# Consolidated deployment using single docker-compose.yml

set -euo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

echo -e "${BLUE}======================================${NC}"
echo -e "${BLUE}Pathfinder Photography Deployment${NC}"
echo -e "${BLUE}======================================${NC}\n"

# Configuration
DEPLOY_DIR="${HOME}/pathfinder-photography"
COMPOSE_FILE_URL="https://raw.githubusercontent.com/glensouza/csdac-pathfinder-25-honor-photography/main/docker-compose.yml"
ENV_TEMPLATE_URL="https://raw.githubusercontent.com/glensouza/csdac-pathfinder-25-honor-photography/main/.env.example"
APP_PORT="8080"
WITH_SIGNOZ="false"

usage() {
 echo "Usage: $0 [-d deploy_dir] [-p port] [--update] [--signoz]"
 echo " -d Deployment directory (default: $DEPLOY_DIR)"
 echo " -p Host port for application (default:8080)"
 echo " --update Pull latest image & recreate containers"
 echo " --signoz Enable SigNoz observability stack (profile)"
 echo "Example: $0 -p9090 --signoz"
}

UPDATE_MODE="false"

while [[ $# -gt0 ]]; do
 case "$1" in
 -d) DEPLOY_DIR="$2"; shift2;;
 -p) APP_PORT="$2"; shift2;;
 --update) UPDATE_MODE="true"; shift;;
 --signoz) WITH_SIGNOZ="true"; shift;;
 -h|--help) usage; exit0;;
 *) echo -e "${RED}Unknown option: $1${NC}"; usage; exit1;;
 esac
done

# Check prerequisites
type docker >/dev/null2>&1 || { echo -e "${RED}Docker not installed.${NC}"; exit1; }
if ! docker compose version >/dev/null2>&1; then
 echo -e "${RED}Docker Compose V2 not available (docker compose).${NC}"; exit1
fi

# Create directory
mkdir -p "$DEPLOY_DIR"
cd "$DEPLOY_DIR"

# Download compose file (unless update only)
if [[ ! -f docker-compose.yml ]]; then
 echo "Downloading docker-compose.yml..."
 curl -sSL -o docker-compose.yml "$COMPOSE_FILE_URL"
fi

# Patch port mapping if custom port requested
if [[ "$APP_PORT" != "8080" ]]; then
 sed -i.bak "s/8080:8080/${APP_PORT}:8080/" docker-compose.yml || { echo -e "${RED}Port patch failed.${NC}"; exit1; }
fi

# Download .env template if missing
if [[ ! -f .env ]]; then
 echo -e "${YELLOW}Creating .env from template...${NC}"
 curl -sSL -o .env "$ENV_TEMPLATE_URL"
 cat <<EOF >> .env

# --- Added by deploy-homelab.sh ---
# Optional Email configuration (leave blank to disable)
EMAIL_SMTP_HOST=
EMAIL_SMTP_PORT=587
EMAIL_SMTP_USERNAME=
EMAIL_SMTP_PASSWORD=
EMAIL_USE_SSL=true
EMAIL_FROM_ADDRESS=
EMAIL_FROM_NAME=Pathfinder Photography
EOF
 echo -e "${YELLOW}Edit .env and set GOOGLE_CLIENT_ID / GOOGLE_CLIENT_SECRET${NC}"
 echo -e "Redirect URIs to add in Google Console (adjust for port ${APP_PORT}):"
 echo " http://$(hostname -I | awk '{print $1}'):${APP_PORT}/signin-google"
 echo " http://localhost:${APP_PORT}/signin-google"
 read -p "Press Enter after editing .env..."
fi

# Load env for validation
set -o allexport; source .env; set +o allexport

if [[ -z "${GOOGLE_CLIENT_ID:-}" || -z "${GOOGLE_CLIENT_SECRET:-}" ]]; then
 echo -e "${RED}Missing GOOGLE_CLIENT_ID or GOOGLE_CLIENT_SECRET in .env${NC}"; exit1
fi

COMPOSE_ARGS=( )
if [[ "$WITH_SIGNOZ" == "true" ]]; then
 COMPOSE_ARGS+=( --profile signoz )
fi

if [[ "$UPDATE_MODE" == "true" ]]; then
 echo "Pulling latest image & recreating containers..."
 docker compose "${COMPOSE_ARGS[@]}" pull
 docker compose "${COMPOSE_ARGS[@]}" up -d
else
 echo "Pulling images..."
 docker compose "${COMPOSE_ARGS[@]}" pull
 echo "Starting services..."
 docker compose "${COMPOSE_ARGS[@]}" up -d
fi

# Health wait loop for postgres then app
echo "Waiting for PostgreSQL to become healthy..."
for i in {1..30}; do
 STATUS=$(docker inspect -f '{{json .State.Health.Status}}' pathfinder-postgres2>/dev/null || echo "null")
 if [[ "$STATUS" == '"healthy"' ]]; then
 echo -e "${GREEN}PostgreSQL healthy.${NC}"; break
 fi
 sleep2
done

APP_URL="http://localhost:${APP_PORT}"
APP_IP_URL="http://$(hostname -I | awk '{print $1}'):${APP_PORT}"

echo "Checking application endpoint..."
for i in {1..30}; do
 if curl -fsS --max-time2 "${APP_URL}/alive" >/dev/null2>&1; then
 echo -e "${GREEN}Application responded on /alive.${NC}"; break
 fi
 sleep2
done

echo -e "\n${GREEN}======================================"
echo -e "Deployment Successful! ✅"
echo -e "======================================${NC}\n"

echo "App URLs:"; echo " ${APP_URL}"; echo " ${APP_IP_URL}"; echo ""
echo "Health / Metrics:"; echo " ${APP_URL}/health"; echo " ${APP_URL}/alive"; echo " ${APP_URL}/ready"; echo " ${APP_URL}/metrics"; echo ""

if [[ "$WITH_SIGNOZ" == "true" ]]; then
 echo "SigNoz:"; echo " UI: http://localhost:3301"; echo " Collector gRPC:4317 (internal)"; echo " Query API:8081"; echo ""
fi

echo "Admin Bootstrap:"; echo " First authenticated Google user becomes Admin automatically."; echo " Additional admins: UPDATE \"Users\" SET \"Role\" =2 WHERE \"Email\"='user@example.com';"; echo ""

echo "Email Configuration (optional):"; echo " Set EMAIL_SMTP_* values in .env for notifications."; echo " Leave EMAIL_SMTP_HOST blank to disable."; echo ""

echo "Common Commands:"; echo " cd ${DEPLOY_DIR}"; echo " docker compose logs -f"; echo " docker compose restart"; echo " docker compose pull && docker compose up -d"; echo " docker compose down"; echo ""

echo "Backup Examples:"; echo " docker exec -t pathfinder-postgres pg_dump -U postgres pathfinder_photography > backup.sql"; echo " tar -czf uploads.tar.gz uploads/"; echo ""

echo "Google Redirect URIs:"; echo " http://$(hostname -I | awk '{print $1}'):${APP_PORT}/signin-google"; echo " http://localhost:${APP_PORT}/signin-google"; echo " (Add HTTPS domain if reverse proxy enabled)"; echo ""

echo "Documentation References:"; echo " SETUP.md, HOMELAB_DEPLOYMENT.md, .github/README.md"; echo ""

echo -e "${YELLOW}If enabling HTTPS via reverse proxy, update OAuth redirect URIs accordingly.${NC}"; echo ""

exit0
