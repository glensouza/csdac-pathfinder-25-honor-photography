# Self-Hosted GitHub Runner Setup Guide

This guide explains how to set up a self-hosted GitHub Actions runner on your bare metal server to enable automatic deployments.

## Table of Contents
- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Installation Steps](#installation-steps)
- [Configuration](#configuration)
- [Security Considerations](#security-considerations)
- [Monitoring and Maintenance](#monitoring-and-maintenance)
- [Troubleshooting](#troubleshooting)

## Overview

The self-hosted GitHub runner will:
- Run on your deployment server
- Listen for workflow events from your GitHub repository
- Execute the deployment workflow automatically when code is pushed to `main` branch
- Deploy the .NET application to `/opt/pathfinder-photography`
- Manage service restarts and health checks
- Perform automatic rollbacks on deployment failures

## Prerequisites

- Server running Ubuntu 22.04 LTS or later
- Root or sudo access
- Application already installed following [BARE_METAL_DEPLOYMENT.md](BARE_METAL_DEPLOYMENT.md)
- GitHub repository admin access
- At least 2GB free disk space for runner

## Installation Steps

### 1. Create Runner User

Create a dedicated user for running the GitHub Actions runner:

```bash
# Create user
sudo useradd -r -m -s /bin/bash github-runner

# Add to necessary groups
sudo usermod -aG sudo github-runner
sudo usermod -aG pathfinder github-runner
sudo usermod -aG www-data github-runner

# Allow passwordless sudo for deployment tasks
sudo visudo
```

Add this line to sudoers file:
```
github-runner ALL=(ALL) NOPASSWD: /bin/systemctl start pathfinder-photography, /bin/systemctl stop pathfinder-photography, /bin/systemctl restart pathfinder-photography, /bin/systemctl status pathfinder-photography, /bin/systemctl is-active pathfinder-photography, /usr/bin/journalctl, /bin/tar, /bin/mkdir, /bin/chown, /bin/chmod, /usr/bin/rsync, /usr/sbin/nginx, /bin/ls, /bin/rm
```

### 2. Download and Configure GitHub Runner

Switch to the runner user:
```bash
sudo -u github-runner bash
cd ~
```

Create runner directory:
```bash
mkdir actions-runner && cd actions-runner
```

Download the latest runner package:
```bash
# Download latest runner (check https://github.com/actions/runner/releases for latest version)
RUNNER_VERSION="2.311.0"
curl -o actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz -L https://github.com/actions/runner/releases/download/v${RUNNER_VERSION}/actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz

# Verify the download (optional but recommended)
echo "29fc8cf2dab4c195bb147384e7e2c94cfd4d4022c793b346a6175435265aa278  actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz" | shasum -a 256 -c

# Extract
tar xzf ./actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz
```

### 3. Get Repository Token

You need to get a registration token from GitHub:

**Option A: Using GitHub Web UI (Recommended)**

1. Go to your repository on GitHub
2. Click **Settings** → **Actions** → **Runners**
3. Click **New self-hosted runner**
4. Select **Linux** as the operating system
5. Copy the token from the configuration command

**Option B: Using GitHub CLI**

```bash
# Install GitHub CLI if not already installed
sudo apt install gh

# Authenticate
gh auth login

# Get registration token
gh api -X POST repos/{owner}/{repo}/actions/runners/registration-token | jq -r .token
```

### 4. Configure the Runner

Run the configuration script with the token from step 3:

```bash
./config.sh --url https://github.com/glensouza/csdac-pathfinder-25-honor-photography --token YOUR_REGISTRATION_TOKEN --name production-server --labels self-hosted,linux,bare-metal,production --work _work
```

Parameters explained:
- `--url`: Your repository URL
- `--token`: Registration token from GitHub
- `--name`: A descriptive name for your runner (e.g., "production-server")
- `--labels`: Labels to identify this runner (used in workflow: `runs-on: self-hosted`)
- `--work`: Working directory for the runner

When prompted:
- Runner group: Press Enter (default)
- Runner name: Press Enter (or provide custom name)
- Runner labels: Press Enter (or add custom labels)
- Work folder: Press Enter (default: `_work`)

### 5. Create Systemd Service

Exit back to your regular user:
```bash
exit  # Exit github-runner user
```

Create systemd service file:
```bash
sudo nano /etc/systemd/system/github-runner.service
```

Add the following content:

```ini
[Unit]
Description=GitHub Actions Runner
After=network.target

[Service]
Type=simple
User=github-runner
Group=github-runner
WorkingDirectory=/home/github-runner/actions-runner
ExecStart=/home/github-runner/actions-runner/run.sh
Restart=always
RestartSec=10
KillMode=process
KillSignal=SIGTERM
TimeoutStopSec=5min

# Environment variables
Environment="RUNNER_ALLOW_RUNASROOT=0"
Environment="DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1"

# Security settings
# Note: NoNewPrivileges is not set here because the runner needs to use sudo
# for deployment operations (systemctl, chown, chmod, etc.). The sudoers file
# restricts which commands can be run with sudo (see step 1).
PrivateTmp=true

# Resource limits
LimitNOFILE=65536
TasksMax=4096

[Install]
WantedBy=multi-user.target
```

### 6. Start the Runner Service

```bash
# Reload systemd
sudo systemctl daemon-reload

# Enable service to start on boot
sudo systemctl enable github-runner

# Start the service
sudo systemctl start github-runner

# Check status
sudo systemctl status github-runner
```

Verify the runner is online:
- Go to your GitHub repository
- Navigate to **Settings** → **Actions** → **Runners**
- You should see your runner listed with a green "Idle" status

### 7. Create Backup Directory Structure

```bash
# Create backup directories
sudo mkdir -p /opt/backups/pathfinder-photography/deployments
sudo mkdir -p /opt/backups/pathfinder-photography/uploads

# Set ownership to github-runner so backups can be created without sudo
sudo chown -R github-runner:github-runner /opt/backups/pathfinder-photography

# Set permissions
sudo chmod -R 755 /opt/backups/pathfinder-photography
```

**Important**: The backup directories must be owned by the `github-runner` user so that the deployment workflow can create backups without requiring sudo privileges.

## Configuration

### Environment Secrets

Configure the following secrets in your GitHub repository:

1. Go to **Settings** → **Secrets and variables** → **Actions**
2. Click **New repository secret**
3. Add the following secrets (if needed for your deployment):

| Secret Name | Description | Example |
|-------------|-------------|---------|
| `DEPLOY_SSH_KEY` | SSH key for deployment (if using remote deployment) | Private SSH key |
| `DB_PASSWORD` | Database password (if needed for migrations) | Strong password |

### Environment Variables

Configure environment variables in GitHub repository:

1. Go to **Settings** → **Environments**
2. Create environment named `production`
3. Add environment variables:

| Variable Name | Description | Example |
|---------------|-------------|---------|
| `APP_URL` | Application URL | `https://pathfinder.yourdomain.com` |
| `DEPLOY_SERVER` | Server hostname or IP | `pathfinder-prod-01` |

### Workflow Customization

The deployment workflow is located at `.github/workflows/deploy-bare-metal.yml`.

Key customization points:

```yaml
env:
  DOTNET_VERSION: '9.0.x'          # .NET version
  PUBLISH_DIR: './publish'          # Build output directory
  DEPLOY_DIR: '/opt/pathfinder-photography'  # Deployment directory
  SERVICE_NAME: 'pathfinder-photography'     # Systemd service name
```

To modify deployment behavior:
1. Edit `.github/workflows/deploy-bare-metal.yml`
2. Commit and push changes
3. The workflow will automatically use the new configuration

## Security Considerations

### 1. Runner Security

**Isolation**: The runner runs as a dedicated user (`github-runner`) with limited sudo privileges.

**Sudo Access**: Only specific commands are allowed via sudoers configuration. Never grant full sudo access.

**Network**: Consider using firewall rules to restrict runner network access:

```bash
# Allow only necessary outbound connections
sudo ufw allow out to any port 443 proto tcp comment 'GitHub HTTPS'
sudo ufw allow out to any port 80 proto tcp comment 'HTTP for packages'
```

### 2. Repository Security

**Branch Protection**: Enable branch protection for `main` branch:
1. Go to **Settings** → **Branches**
2. Add rule for `main` branch
3. Enable:
   - Require pull request reviews before merging
   - Require status checks to pass before merging
   - Require branches to be up to date before merging

**Workflow Permissions**: Limit workflow permissions:
1. Go to **Settings** → **Actions** → **General**
2. Under "Workflow permissions", select "Read repository contents and packages permissions"
3. Enable "Allow GitHub Actions to create and approve pull requests" only if needed

### 3. Secret Management

**Never** commit secrets to the repository. Always use GitHub Secrets or environment files with proper permissions.

Protect configuration files:
```bash
sudo chmod 600 /opt/pathfinder-photography/appsettings.Production.json
sudo chown pathfinder:pathfinder /opt/pathfinder-photography/appsettings.Production.json
```

### 4. Audit Logging

Enable audit logging for the runner:

```bash
# Create log directory
sudo mkdir -p /var/log/github-runner
sudo chown github-runner:github-runner /var/log/github-runner

# Modify systemd service to include logging
sudo nano /etc/systemd/system/github-runner.service
```

Add to `[Service]` section:
```ini
StandardOutput=append:/var/log/github-runner/runner.log
StandardError=append:/var/log/github-runner/runner-error.log
```

Reload and restart:
```bash
sudo systemctl daemon-reload
sudo systemctl restart github-runner
```

## Monitoring and Maintenance

### Monitor Runner Status

Check runner status in real-time:

```bash
# Check service status
sudo systemctl status github-runner

# View logs
sudo journalctl -u github-runner -f

# View runner-specific logs
tail -f /var/log/github-runner/runner.log
```

Check runner status on GitHub:
- Go to **Settings** → **Actions** → **Runners**
- Runner should show "Idle" (green) when waiting for jobs
- Runner shows "Active" (yellow) when executing a job

### Update Runner

GitHub occasionally releases runner updates:

```bash
# Stop the runner
sudo systemctl stop github-runner

# Switch to runner user
sudo -u github-runner bash
cd ~/actions-runner

# Download new version (check GitHub releases for latest version)
RUNNER_VERSION="2.312.0"  # Update this
curl -o actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz -L https://github.com/actions/runner/releases/download/v${RUNNER_VERSION}/actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz

# Extract (this will update binaries)
tar xzf ./actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz

# Exit back to regular user
exit

# Restart the runner
sudo systemctl start github-runner
```

### Cleanup Old Deployments

Old deployment backups should be cleaned up periodically:

```bash
# Manual cleanup (keeps last 10 backups)
sudo find /opt/backups/pathfinder-photography/deployments -name "backup_*.tar.gz" -type f | sort -r | tail -n +11 | xargs -r sudo rm

# Automated cleanup (add to crontab)
sudo crontab -e
```

Add:
```cron
# Clean old deployment backups weekly (keeps last 10)
0 3 * * 0 find /opt/backups/pathfinder-photography/deployments -name "backup_*.tar.gz" -type f | sort -r | tail -n +11 | xargs -r rm
```

### Monitor Disk Space

Runner work directory can grow over time:

```bash
# Check disk usage
du -sh /home/github-runner/actions-runner/_work

# Clean old workflow artifacts (done automatically by runner)
# But you can manually clean if needed:
sudo -u github-runner bash
cd ~/actions-runner/_work
rm -rf */  # Warning: only do this when no jobs are running
exit
```

### Health Checks

Create a monitoring script:

```bash
sudo nano /opt/scripts/check-runner-health.sh
```

Add:
```bash
#!/bin/bash

# Check if runner service is running
if ! systemctl is-active --quiet github-runner; then
    echo "ERROR: GitHub runner service is not running"
    systemctl start github-runner
    exit 1
fi

# Check if runner is registered on GitHub
RUNNER_STATUS=$(curl -s -H "Authorization: token YOUR_GITHUB_PAT" \
    "https://api.github.com/repos/glensouza/csdac-pathfinder-25-honor-photography/actions/runners" \
    | jq -r '.runners[] | select(.name=="production-server") | .status')

if [ "$RUNNER_STATUS" != "online" ]; then
    echo "WARNING: Runner is not online on GitHub"
    echo "Current status: $RUNNER_STATUS"
fi

echo "GitHub runner is healthy"
```

Make executable and schedule:
```bash
sudo chmod +x /opt/scripts/check-runner-health.sh
sudo crontab -e
```

Add:
```cron
# Check runner health every 5 minutes
*/5 * * * * /opt/scripts/check-runner-health.sh >> /var/log/runner-health.log 2>&1
```

## Troubleshooting

### Runner Not Starting

**Check service status:**
```bash
sudo systemctl status github-runner
sudo journalctl -u github-runner -n 50
```

**Common issues:**
1. **Permission denied**: Check file ownership
   ```bash
   sudo chown -R github-runner:github-runner /home/github-runner/actions-runner
   ```

2. **Token expired**: Re-register runner
   ```bash
   sudo systemctl stop github-runner
   sudo -u github-runner bash
   cd ~/actions-runner
   ./config.sh remove --token YOUR_REMOVAL_TOKEN
   ./config.sh --url https://github.com/glensouza/csdac-pathfinder-25-honor-photography --token NEW_TOKEN
   exit
   sudo systemctl start github-runner
   ```

### Deployment Failures

**Check deployment logs:**
```bash
# View workflow logs on GitHub
# Go to Actions → Select failed workflow → View logs

# Check application logs on server
sudo journalctl -u pathfinder-photography -n 100
```

**Common deployment issues:**

1. **Insufficient disk space**:
   ```bash
   df -h
   # Clean up if needed
   sudo apt clean
   docker system prune -af  # If Docker is installed
   ```

2. **Permission errors during deployment**:
   ```bash
   # Fix permissions
   sudo chown -R pathfinder:pathfinder /opt/pathfinder-photography
   sudo chmod -R 755 /opt/pathfinder-photography
   ```

3. **Service won't start after deployment**:
   ```bash
   # Check service logs
   sudo journalctl -u pathfinder-photography -n 100
   
   # Check configuration
   sudo -u pathfinder dotnet /opt/pathfinder-photography/PathfinderPhotography.dll --urls http://localhost:5000
   ```

### Runner Shows Offline on GitHub

**Restart runner service:**
```bash
sudo systemctl restart github-runner
sudo systemctl status github-runner
```

**Check network connectivity:**
```bash
# Test GitHub API access
curl -I https://api.github.com

# Test runner connectivity
sudo -u github-runner bash
cd ~/actions-runner
./run.sh  # Run in foreground to see errors
# Press Ctrl+C to stop, then exit
exit
```

**Re-register if needed:**
```bash
sudo systemctl stop github-runner
sudo -u github-runner bash
cd ~/actions-runner

# Remove old registration
./config.sh remove

# Get new token from GitHub and re-register
./config.sh --url https://github.com/glensouza/csdac-pathfinder-25-honor-photography --token NEW_TOKEN

exit
sudo systemctl start github-runner
```

### Workflow Not Triggering

**Check workflow file syntax:**
```bash
# Install act for local testing (optional)
curl https://raw.githubusercontent.com/nektos/act/master/install.sh | sudo bash

# Test workflow locally
cd /path/to/repository
act -l  # List workflows
```

**Verify trigger configuration:**
- Check `.github/workflows/deploy-bare-metal.yml`
- Ensure `on.push.branches` includes your branch
- Verify no `paths-ignore` is blocking execution

**Check repository settings:**
- Go to **Settings** → **Actions** → **General**
- Ensure "Allow all actions and reusable workflows" is selected
- Check if workflows are enabled for the repository

### Database Migration Failures

**Manual migration:**
```bash
sudo -u pathfinder bash
cd /opt/pathfinder-photography
export ASPNETCORE_ENVIRONMENT=Production

# Check pending migrations
dotnet ef migrations list

# Apply migrations
dotnet ef database update

exit
```

**Rollback migration:**
```bash
sudo -u pathfinder bash
cd /opt/pathfinder-photography
export ASPNETCORE_ENVIRONMENT=Production

# Rollback to specific migration
dotnet ef database update PreviousMigrationName

exit
```

## Advanced Configuration

### Multiple Runners

For high availability or staging/production separation:

1. **Create separate runner for staging:**
   ```bash
   # On staging server
   sudo useradd -r -m -s /bin/bash github-runner-staging
   # Follow installation steps with label: staging
   ```

2. **Update workflow to use specific runner:**
   ```yaml
   deploy-staging:
     runs-on: [self-hosted, staging]
   
   deploy-production:
     runs-on: [self-hosted, production]
   ```

### Custom Deployment Scripts

Create custom deployment scripts in `/opt/scripts/`:

```bash
sudo mkdir -p /opt/scripts
sudo nano /opt/scripts/pre-deploy.sh
```

Example pre-deployment script:
```bash
#!/bin/bash
# Pre-deployment health checks
echo "Running pre-deployment checks..."

# Check database connectivity
if ! pg_isready -h localhost -U pathfinder; then
    echo "Database is not ready"
    exit 1
fi

# Check disk space (require at least 1GB free)
FREE_SPACE=$(df /opt | tail -1 | awk '{print $4}')
if [ $FREE_SPACE -lt 1048576 ]; then
    echo "Insufficient disk space"
    exit 1
fi

echo "Pre-deployment checks passed"
```

Reference in workflow:
```yaml
- name: Pre-deployment checks
  run: sudo /opt/scripts/pre-deploy.sh
```

## Best Practices

1. **Regular Updates**: Keep the runner software updated
2. **Monitor Resources**: Set up monitoring for CPU, memory, and disk
3. **Backup Strategy**: Maintain deployment backups and database backups
4. **Security Patches**: Apply system security updates regularly
5. **Access Control**: Limit who can trigger manual deployments
6. **Logging**: Maintain deployment logs for audit trail
7. **Testing**: Test deployments in staging before production
8. **Documentation**: Keep deployment procedures documented

## Quick Reference

```bash
# Runner management
sudo systemctl start github-runner
sudo systemctl stop github-runner
sudo systemctl restart github-runner
sudo systemctl status github-runner
sudo journalctl -u github-runner -f

# View runner logs
tail -f /var/log/github-runner/runner.log
tail -f /var/log/github-runner/runner-error.log

# Check runner on GitHub
# Settings → Actions → Runners

# Manual deployment
# Go to repository → Actions → Deploy to Bare Metal Server → Run workflow

# View deployment logs
sudo journalctl -u pathfinder-photography -f

# Check backups
ls -lh /opt/backups/pathfinder-photography/deployments/

# Test deployment locally
sudo -u github-runner bash
cd ~/actions-runner
./run.sh  # Run in foreground
```

## Support

For issues:
- Check workflow logs in GitHub Actions tab
- Review runner logs: `sudo journalctl -u github-runner -f`
- Check application logs: `sudo journalctl -u pathfinder-photography -f`
- GitHub Runner documentation: https://docs.github.com/en/actions/hosting-your-own-runners
- Open issue in repository if problem persists

---

For bare metal installation, see [BARE_METAL_DEPLOYMENT.md](BARE_METAL_DEPLOYMENT.md).  
For Docker deployment, see [HOMELAB_DEPLOYMENT.md](HOMELAB_DEPLOYMENT.md).
