#!/bin/bash
# Pathfinder Photography - Home Lab Deployment Script (enhanced for SigNoz config bootstrap)

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
# Base raw URL for signoz config folder in repo
SIGNOZ_BASE_URL="https://raw.githubusercontent.com/glensouza/csdac-pathfinder-25-honor-photography/main/signoz"

APP_PORT="8080"
WITH_SIGNOZ="false"
UPDATE_MODE="false"

usage() {
  echo "Usage: $0 [-d deploy_dir] [-p port] [--update] [--signoz]"
  echo " -d Deployment directory (default: $DEPLOY_DIR)"
  echo " -p Host port for application (default: 8080)"
  echo " --update Pull latest image & recreate containers"
  echo " --signoz Enable SigNoz observability stack (profile)"
  echo "Example: $0 -p 9090 --signoz"
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    -d) DEPLOY_DIR="$2"; shift 2 ;;
    -p) APP_PORT="$2"; shift 2 ;;
    --update) UPDATE_MODE="true"; shift ;;
    --signoz) WITH_SIGNOZ="true"; shift ;;
    -h|--help) usage; exit 0 ;;
    *) echo -e "${RED}Unknown option: $1${NC}"; usage; exit 1 ;;
  esac
done

# Prerequisites
type docker >/dev/null 2>&1 || { echo -e "${RED}Docker not installed.${NC}"; exit 1; }
if ! docker compose version >/dev/null 2>&1; then
  echo -e "${RED}Docker Compose V2 not available (docker compose).${NC}"; exit 1
fi

mkdir -p "$DEPLOY_DIR"
cd "$DEPLOY_DIR"

# Download compose file if missing
if [[ ! -f docker-compose.yml ]]; then
  echo "Downloading docker-compose.yml..."
  curl -fsSL -o docker-compose.yml "$COMPOSE_FILE_URL" || { echo -e "${RED}Failed to download docker-compose.yml${NC}"; exit 1; }
fi

# Patch port if requested
if [[ "$APP_PORT" != "8080" ]]; then
  sed -i.bak "s/8080:8080/${APP_PORT}:8080/" docker-compose.yml || { echo -e "${RED}Port patch failed.${NC}"; exit 1; }
fi

# .env handling
if [[ ! -f .env ]]; then
  echo -e "${YELLOW}Creating .env from template...${NC}"
  curl -fsSL -o .env "$ENV_TEMPLATE_URL" || { echo -e "${RED}Failed to download .env template${NC}"; exit 1; }
  cat <<EOF >> .env

# --- Added by deploy-homelab.sh ---
EMAIL_SMTP_HOST=
EMAIL_SMTP_PORT=587
EMAIL_SMTP_USERNAME=
EMAIL_SMTP_PASSWORD=
EMAIL_USE_SSL=true
EMAIL_FROM_ADDRESS=
EMAIL_FROM_NAME=Pathfinder_Photography
EOF
  echo -e "${YELLOW}Edit .env and set GOOGLE_CLIENT_ID / GOOGLE_CLIENT_SECRET then press Enter.${NC}"
  echo "Redirect URIs (adjust for port ${APP_PORT}):"
  echo " http://$(hostname -I | awk '{print $1}'):${APP_PORT}/signin-google"
  echo " http://localhost:${APP_PORT}/signin-google"
  read -p "Press Enter after editing .env..."
fi

# Normalize line endings
sed -i 's/\r$//' .env
# Quote EMAIL_FROM_NAME if needed
sed -i -E 's/^EMAIL_FROM_NAME=([^"\047].* [^"\047])$/EMAIL_FROM_NAME="\1"/' .env

# Robust env loader
declare -A DOTENV
while IFS= read -r line || [ -n "$line" ]; do
  line="${line#"${line%%[![:space:]]*}"}"
  line="${line%"${line##*[![:space:]]}"}"
  [[ -z "$line" || "${line:0:1}" == "#" ]] && continue
  key="${line%%=*}"
  val="${line#*=}"
  if [[ "$val" =~ ^\".*\"$ ]]; then
    val="${val%\"}"; val="${val#\"}"
  elif [[ "$val" =~ ^\'.*\'$ ]]; then
    val="${val%\'}"; val="${val#\'}"
  fi
  export "$key=$val"
  DOTENV["$key"]="$val"
done < .env

if [[ -z "${GOOGLE_CLIENT_ID:-}" || -z "${GOOGLE_CLIENT_SECRET:-}" ]]; then
  echo -e "${RED}Missing GOOGLE_CLIENT_ID or GOOGLE_CLIENT_SECRET in .env${NC}"; exit 1
fi

# --- SigNoz Config Bootstrap (only if --signoz specified) ---
if [[ "$WITH_SIGNOZ" == "true" ]]; then
  echo "Preparing SigNoz configuration files..."
  mkdir -p signoz
  # Map of filename -> URL
  declare -A SIGNOZ_FILES=(
    ["nginx.conf"]="${SIGNOZ_BASE_URL}/nginx.conf"
    ["otel-collector-config.yaml"]="${SIGNOZ_BASE_URL}/otel-collector-config.yaml"
    ["prometheus.yml"]="${SIGNOZ_BASE_URL}/prometheus.yml"
    ["clickhouse-config.xml"]="${SIGNOZ_BASE_URL}/clickhouse-config.xml"
    ["clickhouse-users.xml"]="${SIGNOZ_BASE_URL}/clickhouse-users.xml"
    ["alertmanager-config.yaml"]="${SIGNOZ_BASE_URL}/alertmanager-config.yaml"
  )

  DOWNLOADED=()
  for f in "${!SIGNOZ_FILES[@]}"; do
    if [[ ! -f "signoz/$f" ]]; then
      if curl -fsSL -o "signoz/$f" "${SIGNOZ_FILES[$f]}"; then
        DOWNLOADED+=("$f")
      else
        echo -e "${YELLOW}Remote ${f} not found or download failed; creating fallback.${NC}"
        case "$f" in
          nginx.conf)
            cat > signoz/nginx.conf <<'EOF'
events {}
http {
  server {
    listen 80;
    server_name _;
    location / {
      proxy_pass http://signoz-frontend:3301;
      proxy_set_header Host $host;
      proxy_set_header X-Real-IP $remote_addr;
    }
    location /api/ {
      proxy_pass http://signoz-query-service:8080/;
      proxy_set_header Host $host;
      proxy_set_header X-Real-IP $remote_addr;
    }
  }
}
EOF
            ;;
          otel-collector-config.yaml)
            cat > signoz/otel-collector-config.yaml <<'EOF'
receivers:
  otlp:
    protocols:
      grpc:
      http:
exporters:
  clickhouse:
    endpoint: tcp://signoz-clickhouse:9000
    database: signoz
processors:
  batch:
extensions:
  health_check:
service:
  extensions: [health_check]
  pipelines:
    traces:
      receivers: [otlp]
      processors: [batch]
      exporters: [clickhouse]
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [clickhouse]
EOF
            ;;
          prometheus.yml)
            cat > signoz/prometheus.yml <<'EOF'
global:
  scrape_interval: 15s
scrape_configs:
  - job_name: 'pathfinder-photography'
    metrics_path: /metrics
    static_configs:
      - targets: ['pathfinder-app:8080']
EOF
            ;;
          clickhouse-config.xml)
            cat > signoz/clickhouse-config.xml <<'EOF'
<clickhouse>
  <logger>
    <level>info</level>
  </logger>
</clickhouse>
EOF
            ;;
          clickhouse-users.xml)
            cat > signoz/clickhouse-users.xml <<'EOF'
<clickhouse>
  <users>
    <default>
      <profile>default</profile>
      <networks>
        <ip>::/0</ip>
      </networks>
      <quotas>
        <quota>default</quota>
      </quotas>
    </default>
  </users>
</clickhouse>
EOF
            ;;
          alertmanager-config.yaml)
            cat > signoz/alertmanager-config.yaml <<'EOF'
route:
  receiver: 'null'
receivers:
  - name: 'null'
EOF
            ;;
        esac
      fi
    fi
  done

  if (( ${#DOWNLOADED[@]} > 0 )); then
    echo -e "${GREEN}Downloaded SigNoz files:${NC} ${DOWNLOADED[*]}"
  else
    echo "SigNoz files already present or fallbacks created."
  fi
fi

COMPOSE_ARGS=()
if [[ "$WITH_SIGNOZ" == "true" ]]; then
  COMPOSE_ARGS+=( --profile signoz )
fi

if [[ "$UPDATE_MODE" == "true" ]]; then
  echo "Pulling latest image & recreating containers..."
  docker compose "${COMPOSE_ARGS[@]}" pull
  docker compose "${COMPOSE_ARGS[@]}" up -d --pull always --force-recreate --remove-orphans
else
  echo "Pulling images..."
  docker compose "${COMPOSE_ARGS[@]}" pull
  echo "Starting services..."
  docker compose "${COMPOSE_ARGS[@]}" up -d
fi

# Wait for PostgreSQL health
echo "Waiting for PostgreSQL to become healthy..."
for i in {1..30}; do
  STATUS=$(docker inspect -f '{{json .State.Health.Status}}' pathfinder-postgres 2>/dev/null || echo "null")
  if [[ "$STATUS" == '"healthy"' ]]; then
    echo -e "${GREEN}PostgreSQL healthy.${NC}"; break
  fi
  sleep 2
done

APP_URL="http://localhost:${APP_PORT}"
APP_IP_URL="http://$(hostname -I | awk '{print $1}'):${APP_PORT}"

echo "Checking application endpoint..."
for i in {1..30}; do
  if curl -fsS --max-time 2 "${APP_URL}/alive" >/dev/null 2>&1; then
    echo -e "${GREEN}Application responded on /alive.${NC}"; break
  fi
  sleep 2
done

echo -e "\n${GREEN}======================================"
echo -e "Deployment Successful! ✅"
echo -e "======================================${NC}\n"

echo "App URLs:"
echo "  ${APP_URL}"
echo "  ${APP_IP_URL}"
echo ""
echo "Health / Metrics:"
echo "  ${APP_URL}/health"
echo "  ${APP_URL}/alive"
echo "  ${APP_URL}/ready"
echo "  ${APP_URL}/metrics"
echo ""

if [[ "$WITH_SIGNOZ" == "true" ]]; then
  echo "SigNoz:"
  echo "  UI: http://localhost:3301"
  echo "  Collector gRPC: 4317"
  echo "  Query API: 8081"
  echo ""
fi

echo "Development with Aspire:"
echo "  dotnet run --project PathfinderPhotography.AppHost"
echo ""

echo "Admin Bootstrap:"
echo "  First authenticated Google user becomes Admin."
echo "  Promote another user:"
echo "  UPDATE \"Users\" SET \"Role\" = 2 WHERE \"Email\"='user@example.com';"
echo ""

echo "Email Configuration:"
echo "  Set EMAIL_SMTP_* values in .env to enable notifications."
echo ""

echo "Common Commands:"
echo "  cd ${DEPLOY_DIR}"
echo "  docker compose logs -f"
echo "  docker compose restart"
echo "  docker compose pull && docker compose up -d"
echo "  docker compose down"
echo ""

echo "Backup Examples:"
echo "  docker exec -t pathfinder-postgres pg_dump -U postgres pathfinder_photography > backup.sql"
echo "  tar -czf uploads.tar.gz uploads/"
echo ""

echo "Google Redirect URIs:"
echo "  http://$(hostname -I | awk '{print $1}'):${APP_PORT}/signin-google"
echo "  http://localhost:${APP_PORT}/signin-google"
echo ""

echo -e "${YELLOW}If enabling HTTPS via reverse proxy, update OAuth redirect URIs accordingly.${NC}"
echo ""

exit 0
