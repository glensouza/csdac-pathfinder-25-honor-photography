# Step 7: Setup Automated Deployments (Optional)

## 📋 Quick Navigation

| [← SigNoz](06-install-signoz.md) | [Home](../DEPLOY.md) | [Next: Security & Performance →](08-security-performance.md) |
|:----------------------------------|:--------------------:|--------------------------------------------------------------:|

## 📑 Deployment Steps Index

- [Prerequisites](00-prerequisites.md)
- [Step 1: Install PostgreSQL](01-install-postgresql.md)
- [Step 2: Install .NET Runtime](02-install-dotnet.md)
- [Step 3: Install Application](03-install-application.md)
- [Step 4: Configure Systemd Service](04-configure-systemd.md)
- [Step 5: Install SigNoz](05-install-signoz.md)
- [Step 6: Install Nginx Reverse Proxy](06-install-nginx.md)
- **Step 7: Setup Automated Deployments** ← You are here
- [Security & Performance](08-security-performance.md)

---

## Overview

For automatic deployments on every code push to `main` branch, you can set up a self-hosted GitHub Actions runner on your server.

This step is **optional**. Skip to [Security & Performance](08-security-performance.md) if you don't need automated deployments.

**Estimated time**: 45-60 minutes

## Benefits

- ✅ Automatic deployment on every push to `main`
- ✅ Automated build and testing
- ✅ Automatic rollback on deployment failures
- ✅ Backup creation before each deployment
- ✅ Health checks and verification

## How It Works

The self-hosted GitHub runner will:
- Run on your deployment server
- Listen for workflow events from your GitHub repository
- Execute the deployment workflow automatically when code is pushed to `main` branch
- Deploy the .NET application to `/opt/pathfinder-photography`
- Manage service restarts and health checks
- Perform automatic rollbacks on deployment failures

**Deployment Workflow**: The automation is defined in `.github/workflows/deploy-bare-metal.yml` in the repository. This workflow handles building, testing, deploying, and verifying the application with built-in security features.

## Prerequisites

- Application already installed following [Steps 1-5](../DEPLOY.md)
- GitHub repository admin access
- At least 2GB free disk space for runner

## Step 7.1: Create Runner User

Create a dedicated user for running the GitHub Actions runner:

```bash
# Create user with no login shell for security
sudo useradd -r -m -s /bin/bash github-runner

# Add to necessary groups (NOT sudo - privileges granted via sudoers file only)
sudo usermod -aG pathfinder github-runner
sudo usermod -aG www-data github-runner
```

**Security Note**: The `github-runner` user is intentionally NOT added to the `sudo` group. All required privileges are explicitly granted through the sudoers file in the next step, following the principle of least privilege.

## Step 7.2: Configure Passwordless Sudo

Create a dedicated sudoers configuration file for the runner user:

```bash
# As root, create the sudoers file
sudo visudo -f /etc/sudoers.d/github-runner
```

Add the following content (this grants ONLY the minimum privileges needed for deployment):

```sudoers
# GitHub Actions Runner - Passwordless sudo configuration
# Security: This grants ONLY the minimum privileges needed for deployment

# System service management - pathfinder-photography service only
github-runner ALL=(ALL) NOPASSWD: /usr/bin/systemctl start pathfinder-photography
github-runner ALL=(ALL) NOPASSWD: /usr/bin/systemctl stop pathfinder-photography
github-runner ALL=(ALL) NOPASSWD: /usr/bin/systemctl restart pathfinder-photography
github-runner ALL=(ALL) NOPASSWD: /usr/bin/systemctl reload pathfinder-photography
github-runner ALL=(ALL) NOPASSWD: /usr/bin/systemctl status pathfinder-photography
github-runner ALL=(ALL) NOPASSWD: /usr/bin/systemctl is-active pathfinder-photography

# Nginx management - reload and test only
github-runner ALL=(ALL) NOPASSWD: /usr/bin/systemctl reload nginx
github-runner ALL=(ALL) NOPASSWD: /usr/bin/systemctl status nginx
github-runner ALL=(ALL) NOPASSWD: /usr/sbin/nginx -t

# Directory creation - restricted to specific paths
github-runner ALL=(ALL) NOPASSWD: /usr/bin/mkdir -p /opt/pathfinder-photography
github-runner ALL=(ALL) NOPASSWD: /usr/bin/mkdir -p /opt/backups/pathfinder-photography/deployments

# Backup operations - highly restricted paths
github-runner ALL=(ALL) NOPASSWD: /usr/bin/tar -czf /opt/backups/pathfinder-photography/deployments/backup_[0-9]*.tar.gz -C /opt/pathfinder-photography .
github-runner ALL=(ALL) NOPASSWD: /usr/bin/tar -czf /opt/backups/pathfinder-photography/deployments/backup_[0-9]*.tar.gz -C /opt/pathfinder-photography *

# Deployment extraction - only from current directory to deployment dir
github-runner ALL=(ALL) NOPASSWD: /usr/bin/tar -xzf pathfinder-photography-[0-9a-f]*.tar.gz -C /opt/pathfinder-photography

# File ownership - restricted to deployment paths only
github-runner ALL=(ALL) NOPASSWD: /usr/bin/chown -R pathfinder\:pathfinder /opt/pathfinder-photography
github-runner ALL=(ALL) NOPASSWD: /usr/bin/chown pathfinder\:pathfinder /opt/backups/pathfinder-photography/deployments/backup_[0-9]*.tar.gz

# File permissions - specific modes only
github-runner ALL=(ALL) NOPASSWD: /usr/bin/chmod -R 755 /opt/pathfinder-photography

# Log viewing - restricted to pathfinder-photography service only
github-runner ALL=(ALL) NOPASSWD: /usr/bin/journalctl -u pathfinder-photography *

# Backup cleanup - restricted to backup directory with date pattern
github-runner ALL=(ALL) NOPASSWD: /usr/bin/find /opt/backups/pathfinder-photography/deployments -name backup_[0-9]*.tar.gz -type f -printf *
github-runner ALL=(ALL) NOPASSWD: /usr/bin/rm -f /opt/backups/pathfinder-photography/deployments/backup_[0-9]*.tar.gz
```

**Security Note**: This configuration follows the principle of least privilege:
- ✅ Only specific commands are allowed - no wildcard command execution
- ✅ Paths are restricted to deployment directories only
- ✅ File patterns use `[0-9]*` for timestamps and `[0-9a-f]*` for SHA hashes to prevent path traversal
- ✅ No ability to obtain a root shell or execute arbitrary commands
- ✅ Nginx can only be reloaded (not stopped), preventing service disruption

Validate the sudoers configuration:

```bash
# Check syntax
sudo visudo -c -f /etc/sudoers.d/github-runner

# Set proper permissions
sudo chmod 0440 /etc/sudoers.d/github-runner
```

## Step 7.3: Download and Configure GitHub Runner

Create runner directory and download the runner software:

```bash
# Create runner directory
sudo mkdir -p /home/github-runner/actions-runner
sudo chown github-runner:github-runner /home/github-runner/actions-runner

# Install jq (needed for runner setup)
sudo apt update
sudo apt install -y jq

# Download and extract runner as github-runner user
sudo -u github-runner bash << 'EOF'
cd /home/github-runner/actions-runner
RUNNER_VERSION=$(curl -s https://api.github.com/repos/actions/runner/releases/latest | jq -r '.tag_name' | sed 's/^v//')
curl -o actions-runner-linux-x64.tar.gz -L "https://github.com/actions/runner/releases/download/v${RUNNER_VERSION}/actions-runner-linux-x64-${RUNNER_VERSION}.tar.gz"
tar xzf actions-runner-linux-x64.tar.gz
rm actions-runner-linux-x64.tar.gz
EOF
```

## Step 7.4: Configure the Runner

1. **Get Runner Registration Token from GitHub:**
   - Go to your GitHub repository
   - Navigate to **Settings** → **Actions** → **Runners**
   - Click **New self-hosted runner**
   - Select **Linux** as the operating system
   - Copy the registration token from the configuration command

2. **Configure the runner:**

```bash
# Configure runner as github-runner user
sudo -u github-runner bash << 'EOF'
cd /home/github-runner/actions-runner
./config.sh --url https://github.com/glensouza/csdac-pathfinder-25-honor-photography --token YOUR_TOKEN_HERE
EOF
```

Replace `YOUR_TOKEN_HERE` with the token from GitHub.

During configuration:
- **Runner group**: Press Enter for default
- **Runner name**: Press Enter for default or provide a name (e.g., `production-server`)
- **Labels**: Press Enter for default or add custom labels
- **Work folder**: Press Enter for default

## Step 7.5: Install and Start Runner Service

Install the runner as a systemd service:

```bash
# Install service
sudo /home/github-runner/actions-runner/svc.sh install github-runner

# Start service
sudo /home/github-runner/actions-runner/svc.sh start

# Check status
sudo /home/github-runner/actions-runner/svc.sh status

# Enable auto-start on boot
sudo systemctl enable actions.runner.glensouza-csdac-pathfinder-25-honor-photography.$(hostname).service
```

## Step 7.6: Verify Runner is Connected

1. Go to your GitHub repository
2. Navigate to **Settings** → **Actions** → **Runners**
3. You should see your runner listed with a green "Idle" status

## Testing Automated Deployment

To test the automated deployment:

1. Make a small change to the code
2. Commit and push to the `main` branch:
   ```bash
   git add .
   git commit -m "Test automated deployment"
   git push origin main
   ```
3. Go to **Actions** tab in GitHub to watch the workflow run
4. The application will be automatically deployed to your server

## Monitoring Deployments

View deployment logs:

```bash
# View runner service logs
sudo journalctl -u actions.runner.*.service -f

# View application deployment logs
sudo journalctl -u pathfinder-photography -f

# View backup directory
ls -lh /opt/backups/pathfinder-photography/deployments/
```

## Troubleshooting

### Runner Not Appearing in GitHub

1. Check runner service status:
   ```bash
   sudo /home/github-runner/actions-runner/svc.sh status
   ```

2. View runner logs:
   ```bash
   sudo journalctl -u actions.runner.*.service -n 100 --no-pager
   ```

### Deployment Fails with Permission Errors

1. Verify sudoers configuration:
   ```bash
   sudo visudo -c -f /etc/sudoers.d/github-runner
   ```

2. Test sudo permissions as github-runner user:
   ```bash
   sudo -u github-runner sudo systemctl status pathfinder-photography
   ```

### Workflow Fails

Check the Actions tab in GitHub for detailed error messages and logs.

## Security Considerations

The runner security model includes:
- ✅ Dedicated non-privileged user (github-runner)
- ✅ Restricted sudo access via sudoers file
- ✅ No wildcard command execution
- ✅ Path restrictions to prevent directory traversal
- ✅ Service isolation (can only manage specific services)
- ✅ Automatic backups before deployment
- ✅ Automatic rollback on failures
- ✅ Health checks after deployment

For more details, see the **Runner Security Considerations** section in the [original deployment guide](../BARE_METAL_DEPLOYMENT.md#runner-security-considerations).

## Verification Checklist

Before moving to the next step, verify:

- [ ] github-runner user is created
- [ ] Sudoers configuration is validated and has correct permissions
- [ ] Runner software is downloaded and extracted
- [ ] Runner is configured and connected to GitHub
- [ ] Runner service is installed and running
- [ ] Runner appears as "Idle" in GitHub repository settings
- [ ] Test deployment completes successfully

---

## Next Steps

Automated deployments are now configured! Review [Security & Performance](08-security-performance.md) best practices to complete your deployment.

| [← SigNoz](06-install-signoz.md) | [Home](../DEPLOY.md) | [Next: Security & Performance →](08-security-performance.md) |
|:----------------------------------|:--------------------:|--------------------------------------------------------------:|
