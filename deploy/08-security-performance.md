# Security & Performance Best Practices

## 📋 Quick Navigation

| [← Automated Deployments](07-automated-deployments.md) | [Home](../DEPLOY.md) |
|:-------------------------------------------------------|---------------------:|

## 📑 Deployment Steps Index

- [Prerequisites](00-prerequisites.md)
- [Step 1: Install PostgreSQL](01-install-postgresql.md)
- [Step 2: Install .NET Runtime](02-install-dotnet.md)
- [Step 3: Install Application](03-install-application.md)
- [Step 4: Configure Systemd Service](04-configure-systemd.md)
- [Step 5: Install SigNoz](05-install-signoz.md)
- [Step 6: Install Nginx Reverse Proxy](06-install-nginx.md)
- [Step 7: Setup Automated Deployments](07-automated-deployments.md)
- **Security & Performance** ← You are here

---

## Overview

This guide covers security best practices and performance tuning to ensure your deployment is secure and performs optimally.

## Security Best Practices

### 1. Keep System Updated

Regularly update your system packages:

```bash
sudo apt update && sudo apt upgrade -y
```

### 2. Use Strong Passwords

- Use strong, unique passwords for database and application
- Generate secure passwords with: `openssl rand -base64 32`
- Never reuse passwords across services
- Store passwords securely (password manager recommended)

### 3. Enable Firewall

Configure firewall to only open necessary ports (already covered in [Step 5](05-install-nginx.md)):

```bash
# CRITICAL: Allow SSH first to prevent lockout
sudo ufw allow 22/tcp

# Allow HTTP and HTTPS
sudo ufw allow 80/tcp comment 'HTTP'
sudo ufw allow 443/tcp comment 'HTTPS'

# Set default policies
sudo ufw default deny incoming
sudo ufw default allow outgoing

# Enable firewall
sudo ufw enable

# Check status
sudo ufw status verbose
```

### 4. Regular Backups

Automate database backups (photos are stored in database):

```bash
# Create backup script
sudo nano /usr/local/bin/backup-pathfinder-db.sh
```

Add the following content:

```bash
#!/bin/bash
BACKUP_DIR="/opt/backups/pathfinder-photography/database"
DATE=$(date +%Y%m%d_%H%M%S)
mkdir -p "$BACKUP_DIR"
sudo -u postgres pg_dump pathfinder_photography | gzip > "$BACKUP_DIR/pathfinder_photography_$DATE.sql.gz"
# Keep only last 30 days of backups
find "$BACKUP_DIR" -name "*.sql.gz" -mtime +30 -delete
```

Make it executable and schedule:

```bash
sudo chmod +x /usr/local/bin/backup-pathfinder-db.sh

# Add to crontab (daily at 2 AM)
sudo crontab -e
# Add: 0 2 * * * /usr/local/bin/backup-pathfinder-db.sh
```

### 5. Monitor Logs

Monitor logs for suspicious activity:

```bash
# View application errors
sudo journalctl -u pathfinder-photography | grep -i error

# View recent application logs
sudo journalctl -u pathfinder-photography -n 100 --no-pager

# View Nginx access logs
sudo tail -f /var/log/nginx/pathfinder-photography-access.log

# View Nginx error logs
sudo tail -f /var/log/nginx/pathfinder-photography-error.log
```

### 6. Use HTTPS Only

Ensure all traffic uses HTTPS:
- When using Cloudflare, this is handled automatically
- If using Let's Encrypt, Certbot configures this during certificate installation
- Verify HTTP redirects to HTTPS

### 7. Limit SSH Access

Harden SSH configuration:

```bash
# Edit SSH config
sudo nano /etc/ssh/sshd_config
```

Recommended settings:

```
# Disable root login
PermitRootLogin no

# Use SSH keys only (disable password authentication)
PasswordAuthentication no

# Limit users who can SSH
AllowUsers your-username

# Change default port (adds security through obscurity)
# Port 2222
```

Restart SSH:

```bash
sudo systemctl restart sshd
```

**Warning**: Test SSH access in a new terminal before closing your current session to avoid lockout!

### 8. Keep .NET SDK and Runtime Updated

Regularly update .NET:

```bash
sudo apt update
sudo apt install --only-upgrade dotnet-sdk-9.0
```

### 9. File Permissions

Verify proper file permissions:

```bash
# Application directory
ls -la /opt/pathfinder-photography/
# Should be owned by pathfinder:pathfinder

# Configuration file
ls -la /opt/pathfinder-photography/appsettings.Production.json
# Should have 600 permissions (owner read/write only)

# Sudoers file
ls -la /etc/sudoers.d/github-runner
# Should have 440 permissions (if using automated deployments)
```

### 10. Security Headers

Verify Nginx security headers are configured (already included in [Step 5](05-install-nginx.md)):

```nginx
add_header X-Frame-Options "SAMEORIGIN" always;
add_header X-Content-Type-Options "nosniff" always;
add_header X-XSS-Protection "1; mode=block" always;
add_header Referrer-Policy "strict-origin-when-cross-origin" always;
```

## Performance Tuning

### PostgreSQL Optimization

For better performance with moderate workload, tune PostgreSQL settings:

```bash
sudo nano /etc/postgresql/16/main/postgresql.conf
```

Recommended settings for 8GB RAM server:

```ini
# Memory
shared_buffers = 2GB
effective_cache_size = 6GB
maintenance_work_mem = 512MB
work_mem = 32MB

# Checkpoint and WAL
checkpoint_completion_target = 0.9
wal_buffers = 16MB
min_wal_size = 1GB
max_wal_size = 4GB

# Query planner
random_page_cost = 1.1
effective_io_concurrency = 200

# Logging
log_min_duration_statement = 1000  # Log slow queries (>1s)
```

**Note**: Adjust these values based on your server's RAM. General rule:
- `shared_buffers`: 25% of system RAM
- `effective_cache_size`: 50-75% of system RAM

Restart PostgreSQL:

```bash
sudo systemctl restart postgresql
```

### Nginx Optimization

Optimize Nginx for better performance:

```bash
sudo nano /etc/nginx/nginx.conf
```

Add/modify in `http` block:

```nginx
# Connection handling
keepalive_timeout 65;
keepalive_requests 100;

# Compression
gzip on;
gzip_vary on;
gzip_min_length 1024;
gzip_types text/plain text/css text/xml text/javascript application/javascript application/json;

# File caching
open_file_cache max=2000 inactive=20s;
open_file_cache_valid 60s;
open_file_cache_min_uses 2;
```

Test and reload Nginx:

```bash
sudo nginx -t
sudo systemctl reload nginx
```

### Application Performance

Monitor application performance:

```bash
# View application resource usage
ps aux | grep PathfinderPhotography

# Check memory usage
free -h

# Check disk usage
df -h

# View active connections
sudo ss -tlnp | grep :5000
```

If using SigNoz ([Step 6](06-install-signoz.md)), monitor application metrics through the SigNoz dashboard for detailed performance insights.

## Maintenance Tasks

### Weekly

- [ ] Review application logs for errors
- [ ] Check disk space usage
- [ ] Verify backups are being created
- [ ] Review Nginx access logs for unusual traffic patterns

### Monthly

- [ ] Update system packages: `sudo apt update && sudo apt upgrade -y`
- [ ] Review and clean old backups
- [ ] Review database size and performance
- [ ] Check SSL certificate expiration (if using Let's Encrypt)

### Quarterly

- [ ] Review and update security configurations
- [ ] Test database restore from backup
- [ ] Review user access and permissions
- [ ] Update documentation with any configuration changes

## Monitoring Checklist

Regularly check these items:

- [ ] Application is running: `sudo systemctl status pathfinder-photography`
- [ ] PostgreSQL is running: `sudo systemctl status postgresql`
- [ ] Nginx is running: `sudo systemctl status nginx`
- [ ] Disk space is adequate: `df -h`
- [ ] No errors in logs: `sudo journalctl -u pathfinder-photography -n 50`
- [ ] Backups are current: `ls -lh /opt/backups/pathfinder-photography/database/`
- [ ] SSL certificates are valid (if using Let's Encrypt)
- [ ] Application is accessible via domain
- [ ] PGAdmin is accessible (if needed)
- [ ] SigNoz is showing telemetry (if installed)

## Troubleshooting Resources

If you encounter issues:

1. Check the specific step in the deployment guide for troubleshooting sections
2. Review system logs: `sudo journalctl -xe`
3. Check application logs: `sudo journalctl -u pathfinder-photography -n 100`
4. Verify all services are running
5. Review the [Deployment Checklist](../DEPLOYMENT_CHECKLIST.md)

## Additional Resources

- **[DEPLOYMENT_CHECKLIST.md](../DEPLOYMENT_CHECKLIST.md)** - Complete verification checklist
- **[SETUP.md](../SETUP.md)** - Local development setup guide
- **GitHub Repository Issues** - Report issues or ask questions

---

## 🎉 Deployment Complete!

Congratulations! You've successfully deployed the Pathfinder Photography application. Your deployment is now:

- ✅ Secure with proper permissions and firewall rules
- ✅ Optimized for performance
- ✅ Backed up regularly
- ✅ Monitored for issues
- ✅ Ready for production use

Remember to review security and performance settings periodically and keep your system updated.

| [← Automated Deployments](07-automated-deployments.md) | [Home](../DEPLOY.md) |
|:-------------------------------------------------------|---------------------:|
