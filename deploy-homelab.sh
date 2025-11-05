#!/bin/bash

# Pathfinder Photography - Home Lab Deployment Script
# This script simplifies deployment to your home lab

set -e

echo "======================================"
echo "Pathfinder Photography Deployment"
echo "======================================"
echo ""

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
DEPLOY_DIR="${HOME}/pathfinder-photography"
COMPOSE_FILE_URL="https://raw.githubusercontent.com/glensouza/csdac-pathfinder-25-honor-photography/main/docker-compose.homelab.yml"
ENV_TEMPLATE_URL="https://raw.githubusercontent.com/glensouza/csdac-pathfinder-25-honor-photography/main/.env.example"

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo -e "${RED}Error: Docker is not installed. Please install Docker first.${NC}"
    exit 1
fi

# Check if Docker Compose is installed
if ! command -v docker compose &> /dev/null; then
    echo -e "${RED}Error: Docker Compose is not installed. Please install Docker Compose first.${NC}"
    exit 1
fi

# Create deployment directory
echo "Creating deployment directory at ${DEPLOY_DIR}..."
mkdir -p "${DEPLOY_DIR}"
cd "${DEPLOY_DIR}"

# Download docker-compose file
echo "Downloading docker-compose.yml..."
curl -sSL -o docker-compose.yml "${COMPOSE_FILE_URL}"

# Check if .env file exists
if [ ! -f .env ]; then
    echo ""
    echo -e "${YELLOW}Creating .env file...${NC}"
    curl -sSL -o .env "${ENV_TEMPLATE_URL}"
    
    echo ""
    echo -e "${YELLOW}================================================${NC}"
    echo -e "${YELLOW}IMPORTANT: Configure your .env file!${NC}"
    echo -e "${YELLOW}================================================${NC}"
    echo ""
    echo "Please edit ${DEPLOY_DIR}/.env and add:"
    echo "  1. Google OAuth Client ID"
    echo "  2. Google OAuth Client Secret"
    echo "  3. PostgreSQL Password (optional, defaults to 'postgres')"
    echo ""
    read -p "Press Enter when you have configured the .env file..."
fi

# Pull latest images
echo ""
echo "Pulling latest Docker images..."
docker compose pull

# Start services
echo ""
echo "Starting services..."
docker compose up -d

# Wait for services to be healthy
echo ""
echo "Waiting for services to start..."
sleep 5

# Check if services are running
if docker compose ps | grep -q "Up"; then
    echo ""
    echo -e "${GREEN}======================================"
    echo -e "Deployment Successful! ✅"
    echo -e "======================================${NC}"
    echo ""
    echo "Application is running at:"
    echo "  http://localhost:8080"
    echo "  http://$(hostname -I | awk '{print $1}'):8080"
    echo ""
    echo "Useful commands:"
    echo "  View logs:    cd ${DEPLOY_DIR} && docker compose logs -f"
    echo "  Stop app:     cd ${DEPLOY_DIR} && docker compose down"
    echo "  Restart app:  cd ${DEPLOY_DIR} && docker compose restart"
    echo "  Update app:   cd ${DEPLOY_DIR} && docker compose pull && docker compose up -d"
    echo ""
else
    echo -e "${RED}Warning: Services may not have started correctly.${NC}"
    echo "Check logs with: docker compose logs"
fi
