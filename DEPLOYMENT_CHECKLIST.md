# Deployment Checklist

This checklist helps ensure successful deployment of the Pathfinder Photography application.

**Choose your deployment type:**
- **Local Development**: .NET Aspire (recommended) or Docker Compose (see SETUP.md)
- **Production Deployment**: Ubuntu VM/Bare Metal (use this checklist with [DEPLOY.md](DEPLOY.md))

---

## Production Deployment Checklist (Ubuntu VM/Bare Metal)

Use this checklist with the wizard-style deployment guide: **[DEPLOY.md](DEPLOY.md)**

For detailed step-by-step instructions, see:
- [Prerequisites](deploy/00-prerequisites.md)
- [Step 1: PostgreSQL](deploy/01-install-postgresql.md)
- [Step 2: .NET Runtime](deploy/02-install-dotnet.md)
- [Step 3: Application](deploy/03-install-application.md)
- [Step 4: Systemd Service](deploy/04-configure-systemd.md)
- [Step 5: SigNoz](deploy/05-install-signoz.md)
- [Step 6: Nginx](deploy/06-install-nginx.md)
- [Step 7: Automated Deployments](deploy/07-automated-deployments.md)
- [Security & Performance](deploy/08-security-performance.md)

### Pre-Deployment

#### Required Information
- [ ] Server/VM IP address or domain name
- [ ] Google OAuth Client ID
- [ ] Google OAuth Client Secret
- [ ] PostgreSQL password (secure, random - use `openssl rand -base64 32`)
- [ ] Email SMTP settings for notifications
- [ ] SSL certificate domain (if using Let's Encrypt)

#### Prerequisites Verified
- [ ] Ubuntu 22.04 LTS or later installed (physical server, VM, or cloud instance)
- [ ] Root or sudo access available
- [ ] At least 20GB free disk space (50GB+ recommended)
- [ ] Server/VM has network connectivity
- [ ] Public IP or domain name configured
- [ ] Firewall rules planned (ports 22, 80, 443)
- [ ] Internet connection available

### Installation Steps

See the detailed wizard-style guide at [DEPLOY.md](DEPLOY.md) or individual step pages linked in the checklist above.

#### Step 1: PostgreSQL
- [ ] Installed PostgreSQL 16
- [ ] Created database: `pathfinder_photography`
- [ ] Created user: `pathfinder` with strong password
- [ ] Configured pg_hba.conf for authentication
- [ ] Verified connection works

#### Step 2: .NET Runtime
- [ ] Installed ASP.NET Core Runtime 9.0
- [ ] Verified installation: `dotnet --list-runtimes`

#### Step 3: Application
- [ ] Created pathfinder system user
- [ ] Deployed application to `/opt/pathfinder-photography`
- [ ] Created uploads directory with correct permissions
- [ ] Configured `appsettings.Production.json` with proper permissions (600)

> Production-specific configuration to add/check:
> - [ ] Forwarded Headers middleware added before `UseHttpsRedirection()` in `Program.cs`.
> - [ ] DataProtection keys directory configured via `StateDirectory=pathfinder-keys` in systemd service file (creates `/var/lib/pathfinder-keys` automatically with proper permissions).

#### Step 4: Systemd Service
- [ ] Created service file: `/etc/systemd/system/pathfinder-photography.service`
- [ ] Enabled and started service
- [ ] Verified service is running
- [ ] Checked logs for errors

> Troubleshooting note:
> - If the service repeatedly times out with `Type=notify`, use this temporary fix to stabilize the service:
> - Change unit to `Type=simple` and add `TimeoutStartSec=120` then `systemctl daemon-reload && systemctl restart pathfinder-photography`.
> - Recommended: enable systemd notifications in the app (`builder.Host.UseSystemd();`) and revert to `Type=notify` for proper readiness handling.

#### Step 5: Nginx
- [ ] Installed and configured Nginx
- [ ] Configured reverse proxy for application
- [ ] Obtained SSL certificate (Let's Encrypt)
- [ ] Verified HTTPS working
- [ ] Added security headers

> Nginx gotchas:
> - Prefer `proxy_pass http://127.0.0.1:5000;` to avoid IPv6/localhost bind mismatch.
> - Ensure `proxy_set_header X-Forwarded-Proto $scheme;` is present to allow HTTPS redirects to work.

#### Step 6: Firewall
- [ ] Allowed SSH (port 22) BEFORE enabling firewall
- [ ] Allowed HTTP (port 80) and HTTPS (port 443)
- [ ] Set default-deny policy
- [ ] Enabled and verified firewall

#### Step 7: Automated Deployments
- [ ] Created github-runner user (NOT in sudo group)
- [ ] Configured passwordless sudo via `/etc/sudoers.d/github-runner`
- [ ] Downloaded and configured GitHub Actions runner
- [ ] Created systemd service for runner
- [ ] Created backup directories
- [ ] Verified runner shows "Idle" on GitHub

## Google OAuth Configuration

- [ ] Created Google Cloud project
- [ ] Enabled Google+ API
- [ ] Created OAuth2.0 credentials
- [ ] Configured OAuth consent screen
- [ ] Added authorized redirect URIs:
 - [ ] `https://your-domain.com/signin-google` (production with HTTPS)
 - [ ] `http://your-server-ip/signin-google` (if testing without HTTPS)
- [ ] Saved Client ID and Client Secret

### Post-Deployment Verification

#### Services Running
- [ ] PostgreSQL service active: `systemctl is-active postgresql`
- [ ] Application service active: `systemctl is-active pathfinder-photography`
- [ ] Nginx service active: `systemctl is-active nginx`
- [ ] GitHub runner active: `systemctl is-active actions.runner.*`
- [ ] No errors in application logs: `journalctl -u pathfinder-photography -n 100`

#### Application Access
- [ ] Can access https://your-domain.com from browser
- [ ] Home page loads correctly
- [ ] Can see all 10 composition rules
- [ ] SSL certificate is valid (no browser warnings)

#### Google Authentication
- [ ] "Sign in with Google" button appears
- [ ] Clicking redirects to Google OAuth
- [ ] After authentication, redirected back to app
- [ ] Signed in with correct user name

#### User Roles
- [ ] First signed-in user automatically has Admin role
- [ ] Admin can access `/admin/users` page
- [ ] Admin can promote users to Instructor
- [ ] Admin can delete unauthorized users
- [ ] Verified ELO ratings recalculate when users are deleted

#### Photo Upload
- [ ] Navigated to Submit page
- [ ] Selected a composition rule
- [ ] Uploaded a test photo (<10MB)
- [ ] Added description
- [ ] Submitted successfully
- [ ] Photo appears in gallery
- [ ] Photo displays correctly when clicked

#### Database
- [ ] Database is accessible:
  ```bash
  sudo -u pathfinder psql -h localhost -U pathfinder -d pathfinder_photography -c "SELECT COUNT(*) FROM \"PhotoSubmissions\";"
  ```
- [ ] Data persists after service restart

#### Health Endpoints
- [ ] `/health` endpoint responds
- [ ] `/alive` endpoint responds
- [ ] `/ready` endpoint responds
- [ ] `/metrics` endpoint responds (if enabled)

### Security Hardening
- [ ] Changed PostgreSQL password to strong random password
- [ ] Firewall configured with default-deny policy
- [ ] Using strong passwords for all services
- [ ] Secrets not in version control
- [ ] SSL/TLS enabled and working
- [ ] `appsettings.Production.json` has 600 permissions
- [ ] Application runs as non-root user (pathfinder)
- [ ] PostgreSQL only listens on localhost
- [ ] Nginx security headers configured

### Backup Strategy
- [ ] Created backup script: `/opt/backups/backup-pathfinder-db.sh`
- [ ] Script backs up database
- [ ] Script backs up uploaded photos
- [ ] Script keeps last 7 days of backups
- [ ] Scheduled backup script in crontab (daily at 2 AM)
- [ ] Test backup created successfully
- [ ] Test restore successful

### Monitoring
- [ ] Configured log rotation in journald
- [ ] Set up external monitoring/alerting
- [ ] Configured health check script
- [ ] Know where to find logs: `journalctl -u pathfinder-photography`

## Troubleshooting Completed

If issues occurred, verify resolved:

- [ ] Service logs checked: `journalctl -u pathfinder-photography`
- [ ] Database connection working
- [ ] Google OAuth redirect URI matches exactly
- [ ] File permissions correct for uploads
- [ ] Nginx configuration valid: `nginx -t`
- [ ] SSL certificate valid and not expired
- [ ] Firewall not blocking required ports
- [ ] Disk space available: `df -h`

## Documentation Reviewed

- [ ] Read [DEPLOY.md](DEPLOY.md) - Wizard-style production deployment guide
- [ ] Read [SETUP.md](SETUP.md) - If doing development
- [ ] Bookmarked useful commands
- [ ] Know where to find logs

## Performance & Maintenance

- [ ] Noted current resource usage (CPU, RAM, disk)
- [ ] Response time acceptable (<2s for page loads)
- [ ] Can handle expected concurrent users
- [ ] Update schedule determined (monthly recommended)
- [ ] Backup schedule set (daily recommended)
- [ ] Disaster recovery plan created

## Final Verification

- [ ] Application accessible from all intended devices
- [ ] Multiple users can sign in
- [ ] Photos upload and display correctly
- [ ] Gallery filtering works
- [ ] Performance acceptable
- [ ] No errors in logs
- [ ] SSL certificate valid
- [ ] Automated deployments working (if configured)
- [ ] Backups working
- [ ] Ready for users

## Production Readiness

### For Church/Organization Use
- [ ] Announced to pathfinders
- [ ] Instructions provided to users
- [ ] Support contact available
- [ ] Deadline for submissions set
- [ ] Storage capacity verified for expected photos

## Sign-Off

- **Deployed by**: ________________
- **Date**: ________________
- **Server/VM**: ________________
- **Domain**: ________________
- **Application Version**: ________________
- **Deployment Type**: ☐ Physical Server ☐ Virtual Machine ☐ Cloud Instance
- **Status**: ☐ Development ☐ Staging ☐ Production

## Notes

```
Add any deployment-specific notes, customizations, or issues encountered:




```

---

## Next Steps After Deployment

1. Monitor logs for first 24-48 hours: `journalctl -u pathfinder-photography -f`
2. Test with small group before full rollout
3. Ensure backup is working: check `/opt/backups/pathfinder-photography/`
4. Share access information with pathfinders
5. Set submission deadline
6. Plan for photo review and grading

## Support Resources

- **Production Deployment**: [DEPLOY.md](DEPLOY.md)
- **Local Development**: [SETUP.md](SETUP.md)
- **Repository**: https://github.com/glensouza/csdac-pathfinder-25-honor-photography
- **GitHub Issues**: Report problems
- **Logs**: `journalctl -u pathfinder-photography -f`

## Quick Commands (Production)

```bash
# Application management
sudo systemctl start pathfinder-photography
sudo systemctl stop pathfinder-photography
sudo systemctl restart pathfinder-photography
sudo systemctl status pathfinder-photography
sudo journalctl -u pathfinder-photography -f

# Database backup
sudo -u postgres pg_dump pathfinder_photography > backup.sql

# Nginx
sudo nginx -t
sudo systemctl reload nginx

# View logs
sudo journalctl -u pathfinder-photography -n 100
sudo tail -f /var/log/nginx/pathfinder-photography-access.log
```

---

## Local Development Checklist

For local development with .NET Aspire or local .NET, see [SETUP.md](SETUP.md).

### Quick Start with Aspire
- [ ] Cloned repository
- [ ] Configured Google OAuth in `appsettings.Development.json`
- [ ] Started Aspire: `dotnet run --project PathfinderPhotography.AppHost`
- [ ] Accessed Aspire Dashboard (auto-opens)
- [ ] Clicked 'webapp' endpoint to access application
- [ ] Verified all services running in Aspire Dashboard

See [SETUP.md](SETUP.md) for complete local development instructions with .NET Aspire or local .NET.

## Cloudflare / Tunnel Troubleshooting

- [ ] If users see `502 Bad Gateway` from Cloudflare, follow these steps to isolate and fix the Cloudflare ↔ origin path:
 - [ ] Check Nginx access & error logs for upstream connection errors:
 ```bash
 sudo tail -n200 /var/log/nginx/pathfinder-photography-access.log
 sudo tail -n200 /var/log/nginx/pathfinder-photography-error.log
 ```
 - [ ] Verify local services and listening ports:
 ```bash
 sudo ss -tlnp | egrep ':80|:5000'
 curl -I -H "Host: photohonor.coronasda.church" http://127.0.0.1/
 curl -I http://127.0.0.1:5000/
 ```
 - [ ] Check DNS A/AAAA records. If you do not support IPv6 on origin, remove AAAA records or ensure IPv6 works:
 ```bash
 dig +short A photohonor.coronasda.church
 dig +short AAAA photohonor.coronasda.church
 ```
 - [ ] If using Cloudflare Tunnel (cloudflared):
 - Confirm `config.yml` ingress points to Nginx (http://127.0.0.1:80), not Kestrel (5000).
 - Example:
 ```yaml
 ingress:
 - hostname: photohonor.coronasda.church
 service: http://127.0.0.1:80
 - service: http_status:404
 ```
 - Restart & check tunnel logs:
 ```bash
 sudo systemctl restart cloudflared
 sudo systemctl status cloudflared --no-pager
 sudo journalctl -u cloudflared -f
 ```
 - [ ] Use Cloudflare DNS toggle (DNS-only / grey cloud) to isolate: if DNS-only works, problem is Cloudflare↔origin (tunnel, firewall, IPv6, or SSL mode).
 - [ ] After fixing tunnel/config, re-enable Cloudflare proxy (orange cloud) for CDN/WAF benefits.
