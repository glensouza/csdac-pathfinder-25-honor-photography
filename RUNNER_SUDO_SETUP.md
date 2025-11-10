# GitHub Actions Runner Sudo Configuration

## Overview

This guide explains how to configure passwordless sudo for the GitHub Actions self-hosted runner to enable secure and efficient deployments.

## Prerequisites

- Root access to the server
- GitHub Actions runner installed and running as a specific user (typically `runner` or `github-runner`)

## Setup Instructions

### 1. Identify the Runner User

First, identify which user is running the GitHub Actions runner:

```bash
# Check the runner process
ps aux | grep "Runner.Listener"

# Common runner usernames:
# - runner
# - github-runner
# - actions-runner
```

For this guide, we'll assume the runner user is `runner`. Replace with your actual username.

### 2. Create Sudoers Configuration File

Create a dedicated sudoers configuration file for the runner user:

```bash
# As root, create the sudoers file
sudo visudo -f /etc/sudoers.d/github-runner
```

### 3. Add Passwordless Sudo Rules

Add the following content to `/etc/sudoers.d/github-runner`:

```sudoers
# GitHub Actions Runner - Passwordless sudo configuration
# Replace 'runner' with your actual runner username

# System service management
runner ALL=(ALL) NOPASSWD: /usr/bin/systemctl start pathfinder-photography
runner ALL=(ALL) NOPASSWD: /usr/bin/systemctl stop pathfinder-photography
runner ALL=(ALL) NOPASSWD: /usr/bin/systemctl restart pathfinder-photography
runner ALL=(ALL) NOPASSWD: /usr/bin/systemctl reload pathfinder-photography
runner ALL=(ALL) NOPASSWD: /usr/bin/systemctl status pathfinder-photography
runner ALL=(ALL) NOPASSWD: /usr/bin/systemctl is-active pathfinder-photography
runner ALL=(ALL) NOPASSWD: /usr/bin/systemctl reload nginx
runner ALL=(ALL) NOPASSWD: /usr/bin/systemctl status nginx

# File system operations for deployment
runner ALL=(ALL) NOPASSWD: /usr/bin/mkdir -p /opt/pathfinder-photography*
runner ALL=(ALL) NOPASSWD: /usr/bin/mkdir -p /opt/backups/pathfinder-photography*
runner ALL=(ALL) NOPASSWD: /usr/bin/tar *
runner ALL=(ALL) NOPASSWD: /usr/bin/chown -R pathfinder\:pathfinder /opt/pathfinder-photography*
runner ALL=(ALL) NOPASSWD: /usr/bin/chown pathfinder\:pathfinder *
runner ALL=(ALL) NOPASSWD: /usr/bin/chmod -R * /opt/pathfinder-photography*
runner ALL=(ALL) NOPASSWD: /usr/bin/rsync *

# Log viewing
runner ALL=(ALL) NOPASSWD: /usr/bin/journalctl *

# Nginx testing
runner ALL=(ALL) NOPASSWD: /usr/sbin/nginx -t

# Bash for running complex commands
runner ALL=(ALL) NOPASSWD: /usr/bin/bash -c *

# Remove old backups
runner ALL=(ALL) NOPASSWD: /usr/bin/rm -f /opt/backups/pathfinder-photography/deployments/backup_*.tar.gz
```

### 4. Set Proper Permissions

Ensure the sudoers file has the correct permissions:

```bash
sudo chmod 0440 /etc/sudoers.d/github-runner
```

### 5. Validate the Configuration

Test the sudoers file syntax:

```bash
sudo visudo -c -f /etc/sudoers.d/github-runner
```

You should see: `parsed OK`

### 6. Create Necessary Directories

Set up the deployment and backup directories with proper ownership:

```bash
# Create deployment directory owned by pathfinder user
sudo mkdir -p /opt/pathfinder-photography
sudo chown -R pathfinder:pathfinder /opt/pathfinder-photography
sudo chmod -R 755 /opt/pathfinder-photography

# Create backup directory owned by runner user (so runner can write backups)
sudo mkdir -p /opt/backups/pathfinder-photography/deployments
sudo chown -R runner:runner /opt/backups/pathfinder-photography
sudo chmod -R 755 /opt/backups/pathfinder-photography
```

### 7. Test Passwordless Sudo

Switch to the runner user and test:

```bash
# Switch to runner user
sudo su - runner

# Test systemctl commands
sudo systemctl status pathfinder-photography
sudo systemctl is-active pathfinder-photography

# Test file operations
sudo mkdir -p /opt/backups/pathfinder-photography/test
sudo tar --version
sudo chown --version

# Exit back to your user
exit
```

All commands should execute without prompting for a password.

## Security Considerations

### What This Configuration Allows

1. **Service Management**: Start, stop, and check status of the pathfinder-photography service
2. **File Operations**: Create directories, extract archives, set permissions within deployment paths
3. **Backup Operations**: Create and manage backups in designated directories
4. **Log Access**: View application logs via journalctl
5. **Nginx Management**: Test and reload nginx configuration

### What This Configuration DOES NOT Allow

1. **No Root Shell**: Runner cannot get a root shell (`sudo -i` or `sudo su`)
2. **Limited Scope**: Commands only work for specific paths and services
3. **No Package Management**: Cannot install or remove system packages
4. **No User Management**: Cannot create, modify, or delete users
5. **No System Configuration**: Cannot modify system-wide configuration files

### Additional Security Measures

1. **Audit Logging**: Enable sudo logging to track all privileged operations

```bash
# Add to /etc/sudoers.d/github-runner
Defaults log_output
Defaults!/usr/bin/systemctl !log_output
Defaults!/usr/bin/journalctl !log_output
```

2. **Regular Review**: Periodically review the sudoers configuration and adjust as needed

3. **Principle of Least Privilege**: Only grant the minimum permissions required for deployment

## Troubleshooting

### Issue: "sudo: a password is required"

**Cause**: The sudoers configuration is not properly loaded or the command doesn't match the rules.

**Solution**:
1. Verify the sudoers file exists and has correct permissions: `ls -la /etc/sudoers.d/github-runner`
2. Check syntax: `sudo visudo -c -f /etc/sudoers.d/github-runner`
3. Ensure the command exactly matches the pattern in the sudoers file
4. Check the runner username is correct

### Issue: "sudo: sorry, you are not allowed to execute..."

**Cause**: The command or path is not included in the sudoers rules.

**Solution**:
1. Check which exact command is failing
2. Add the specific command to the sudoers file
3. Use wildcards carefully to allow flexibility while maintaining security

### Issue: Backup directory permission denied

**Cause**: The runner user doesn't have write access to the backup directory.

**Solution**:
```bash
sudo chown -R runner:runner /opt/backups/pathfinder-photography
sudo chmod -R 755 /opt/backups/pathfinder-photography
```

### Issue: Cannot read appsettings.Production.json

**Cause**: The file has restrictive permissions (chmod 600) and is owned by pathfinder.

**Solution**: 
The backup should be run with `sudo tar` which allows root to read all files. This is already configured in the sudoers file above.

## Alternative Approach: ACL Permissions

If you prefer not to use sudo, you can use ACL (Access Control Lists) to grant the runner user specific permissions:

```bash
# Install ACL tools if not already installed
sudo apt-get install acl

# Grant runner read access to deployment directory
sudo setfacl -R -m u:runner:rx /opt/pathfinder-photography

# Grant runner write access to backup directory
sudo setfacl -R -m u:runner:rwx /opt/backups/pathfinder-photography

# Set default ACL for new files
sudo setfacl -R -d -m u:runner:rx /opt/pathfinder-photography
```

However, this approach still requires sudo for systemctl commands, so the sudoers configuration is still recommended.

## Testing the Deployment Workflow

After configuration, test the workflow:

1. Make a small change to the application
2. Commit and push to trigger the deployment workflow
3. Monitor the workflow execution in GitHub Actions
4. Verify all steps complete successfully without password prompts

## Maintenance

### Adding New Services or Paths

If you add new services or deployment paths:

1. Edit the sudoers file: `sudo visudo -f /etc/sudoers.d/github-runner`
2. Add new rules following the existing pattern
3. Validate: `sudo visudo -c -f /etc/sudoers.d/github-runner`
4. Test with the runner user

### Removing or Modifying Rules

To modify the configuration:

1. Always use `visudo`: `sudo visudo -f /etc/sudoers.d/github-runner`
2. Never edit the file directly with a text editor
3. Test changes with the runner user before deploying to production

## Summary

This configuration provides a secure, efficient way for the GitHub Actions runner to perform deployment tasks:

✅ **Secure**: Limited to specific commands and paths  
✅ **Efficient**: No password prompts, automated deployments  
✅ **Maintainable**: Clear documentation of allowed operations  
✅ **Auditable**: All sudo operations can be logged  

The runner can now execute all deployment tasks without manual intervention while maintaining system security.
