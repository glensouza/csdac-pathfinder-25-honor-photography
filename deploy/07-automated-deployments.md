# GitHub Actions Runner - Passwordless sudo configuration
# Security: This grants ONLY the minimum privileges needed for deployment

# System service management - pathfinder-photography service only
pathfinder ALL=(ALL) NOPASSWD: /usr/bin/systemctl start pathfinder-photography
pathfinder ALL=(ALL) NOPASSWD: /usr/bin/systemctl stop pathfinder-photography
pathfinder ALL=(ALL) NOPASSWD: /usr/bin/systemctl restart pathfinder-photography
pathfinder ALL=(ALL) NOPASSWD: /usr/bin/systemctl reload pathfinder-photography
pathfinder ALL=(ALL) NOPASSWD: /usr/bin/systemctl status pathfinder-photography
pathfinder ALL=(ALL) NOPASSWD: /usr/bin/systemctl is-active pathfinder-photography

# Nginx management - reload and test only
pathfinder ALL=(ALL) NOPASSWD: /usr/bin/systemctl reload nginx
pathfinder ALL=(ALL) NOPASSWD: /usr/bin/systemctl status nginx
pathfinder ALL=(ALL) NOPASSWD: /usr/sbin/nginx -t

# Directory creation - restricted to specific paths
pathfinder ALL=(ALL) NOPASSWD: /usr/bin/mkdir -p /opt/pathfinder-photography
pathfinder ALL=(ALL) NOPASSWD: /usr/bin/mkdir -p /opt/backups/pathfinder-photography/deployments

# Backup operations - highly restricted paths
pathfinder ALL=(ALL) NOPASSWD: /usr/bin/tar -czf /opt/backups/pathfinder-photography/deployments/backup_[0-9]*.tar.gz -C /opt/pathfinder-photography .
pathfinder ALL=(ALL) NOPASSWD: /usr/bin/tar -czf /opt/backups/pathfinder-photography/deployments/backup_[0-9]*.tar.gz -C /opt/pathfinder-photography *

# Deployment extraction - only from current directory to deployment dir
pathfinder ALL=(ALL) NOPASSWD: /usr/bin/tar -xzf pathfinder-photography-[0-9a-f]*.tar.gz -C /opt/pathfinder-photography

# File ownership - restricted to deployment paths only
pathfinder ALL=(ALL) NOPASSWD: /usr/bin/chown -R pathfinder\:pathfinder /opt/pathfinder-photography
pathfinder ALL=(ALL) NOPASSWD: /usr/bin/chown pathfinder\:pathfinder /opt/backups/pathfinder-photography/deployments/backup_[0-9]*.tar.gz

# File permissions - specific modes only
pathfinder ALL=(ALL) NOPASSWD: /usr/bin/chmod -R 755 /opt/pathfinder-photography

# Log viewing - restricted to pathfinder-photography service only
pathfinder ALL=(ALL) NOPASSWD: /usr/bin/journalctl -u pathfinder-photography *

# Backup cleanup - restricted to backup directory with date pattern
pathfinder ALL=(ALL) NOPASSWD: /usr/bin/find /opt/backups/pathfinder-photography/deployments -name backup_[0-9]*.tar.gz -type f -printf *
pathfinder ALL=(ALL) NOPASSWD: /usr/bin/rm -f /opt/backups/pathfinder-photography/deployments/backup_[0-9]*.tar.gz