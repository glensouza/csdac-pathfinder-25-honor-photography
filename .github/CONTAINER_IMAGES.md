# Container Images

This repository automatically builds and publishes Docker images to GitHub Container Registry (GHCR).

## Available Images

- **Repository**: `ghcr.io/glensouza/csdac-pathfinder-25-honor-photography`
- **Platforms**: `linux/amd64`, `linux/arm64`

## Tags

Images are tagged with the following conventions:

- `latest` - Latest build from the main branch
- `main` - Latest build from the main branch
- `develop` - Latest build from the develop branch
- `v1.0.0` - Specific version tags (semantic versioning)
- `1.0` - Major.minor version
- `1` - Major version
- `main-abc1234` - Branch name with commit SHA
- `pr-123` - Pull request number

## Pulling Images

### Latest Version

```bash
docker pull ghcr.io/glensouza/csdac-pathfinder-25-honor-photography:latest
```

### Specific Version

```bash
docker pull ghcr.io/glensouza/csdac-pathfinder-25-honor-photography:v1.0.0
```

### Platform-Specific

Docker automatically pulls the correct platform. To explicitly pull:

```bash
# For ARM64 (e.g., Raspberry Pi, Apple Silicon)
docker pull --platform linux/arm64 ghcr.io/glensouza/csdac-pathfinder-25-honor-photography:latest

# For AMD64 (e.g., x86_64 servers)
docker pull --platform linux/amd64 ghcr.io/glensouza/csdac-pathfinder-25-honor-photography:latest
```

## Image Details

### Base Images

- **Build**: `mcr.microsoft.com/dotnet/sdk:9.0`
- **Runtime**: `mcr.microsoft.com/dotnet/aspnet:9.0`

### Size

Approximately 250MB compressed

### Security

Images are scanned for vulnerabilities on build

## Running the Image

### Quick Start

```bash
docker run -d \
  -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=postgres;Database=pathfinder_photography;Username=postgres;Password=postgres" \
  -e Authentication__Google__ClientId="your_client_id" \
  -e Authentication__Google__ClientSecret="your_secret" \
  ghcr.io/glensouza/csdac-pathfinder-25-honor-photography:latest
```

### With Docker Compose

See [docker-compose.homelab.yml](../docker-compose.homelab.yml) for a complete example.

## Build Information

Images are built using GitHub Actions on every:
- Push to main or develop branches
- Tag creation (v*.*.*)
- Pull request to main branch

### Build Workflow

The build process:
1. Checks out code
2. Sets up Docker Buildx for multi-platform builds
3. Logs in to GHCR using `GITHUB_TOKEN`
4. Extracts metadata (tags, labels)
5. Builds for both amd64 and arm64
6. Pushes to GitHub Container Registry
7. Uses GitHub Actions cache for faster builds

## Verifying Images

### Check Image Labels

```bash
docker inspect ghcr.io/glensouza/csdac-pathfinder-25-honor-photography:latest
```

### View Image History

```bash
docker history ghcr.io/glensouza/csdac-pathfinder-25-honor-photography:latest
```

## Troubleshooting

### Authentication Issues

If you need to pull from a private repository:

```bash
# Create a personal access token with read:packages scope
echo $GITHUB_TOKEN | docker login ghcr.io -u USERNAME --password-stdin
```

### Platform Issues

If you get platform mismatch errors:

```bash
# Check your system architecture
uname -m

# Pull the correct platform
docker pull --platform linux/$(uname -m) ghcr.io/glensouza/csdac-pathfinder-25-honor-photography:latest
```

## Local Development

To build the image locally:

```bash
# Build for your platform
docker build -t pathfinder-photography:local .

# Build for multiple platforms
docker buildx build --platform linux/amd64,linux/arm64 -t pathfinder-photography:local .
```

## Updating Your Deployment

```bash
# Pull latest
docker compose pull

# Recreate containers
docker compose up -d

# Clean old images
docker image prune -f
```

## Support

For image-related issues:
- Check the [GitHub Actions logs](https://github.com/glensouza/csdac-pathfinder-25-honor-photography/actions)
- Review the [Dockerfile](../Dockerfile)
- See [Home Lab Deployment Guide](../HOMELAB_DEPLOYMENT.md)

---

**Registry**: GitHub Container Registry (GHCR)
**Visibility**: Public
**Auto-build**: Enabled via GitHub Actions
