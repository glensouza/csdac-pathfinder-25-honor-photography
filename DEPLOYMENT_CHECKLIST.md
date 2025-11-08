# Deployment Checklist

Use this checklist when deploying to your home lab.

## Pre-Deployment

### Required Information
- [ ] Google OAuth Client ID
- [ ] Google OAuth Client Secret 
- [ ] Home lab server IP address or hostname
- [ ] Desired port (default:8080)
- [ ] PostgreSQL password (secure, random)
- [ ] (Optional) SMTP settings for email notifications

### Prerequisites Verified
- [ ] Docker installed (version20.10+)
- [ ] Docker Compose installed (version2.0+)
- [ ] At least2GB free disk space
- [ ] Port8080 available (or alternative port)
- [ ] Port5432 available (or adjust compose mapping)
- [ ] Internet connection for pulling images

## Google OAuth Configuration

- [ ] Created Google Cloud project
- [ ] Enabled Google+ API
- [ ] Created OAuth2.0 credentials
- [ ] Configured OAuth consent screen
- [ ] Added authorized redirect URIs:
 - [ ] `http://localhost:8080/signin-google`
 - [ ] `http://your-server:8080/signin-google`
 - [ ] `https://your-domain.com/signin-google` (if using reverse proxy/HTTPS)
- [ ] Saved Client ID and Client Secret

## Deployment Steps

### Quick Deployment
- [ ] Ran one-command deployment script:
 ```bash
 curl -sSL https://raw.githubusercontent.com/glensouza/csdac-pathfinder-25-honor-photography/main/deploy-homelab.sh | bash
 ```
- [ ] (Optional) Ran with SigNoz enabled:
 ```bash
 curl -sSL https://raw.githubusercontent.com/glensouza/csdac-pathfinder-25-honor-photography/main/deploy-homelab.sh | bash -s -- --signoz
 ```

### OR Manual Deployment
- [ ] Created deployment directory: `mkdir -p ~/pathfinder-photography`
- [ ] Downloaded `docker-compose.yml`
- [ ] Created `.env` file with credentials (GOOGLE_CLIENT_ID/SECRET, POSTGRES_PASSWORD, optional EMAIL_* settings)
- [ ] Pulled images: `docker compose pull`
- [ ] Started services: `docker compose up -d`
- [ ] (Optional) Started with observability: `docker compose --profile signoz up -d`

## Post-Deployment Verification

### Services Running
- [ ] Check containers: `docker compose ps`
- [ ] All containers show "Up"
- [ ] No error messages in status

### Application Access
- [ ] Accessed http://localhost:8080 (from server)
- [ ] Accessed http://your-server-ip:8080 (from network)
- [ ] Home page loads correctly
- [ ] Can see all10 composition rules

### Google Authentication
- [ ] "Sign in with Google" button appears
- [ ] Clicking redirects to Google OAuth
- [ ] After authentication, redirected back to app
- [ ] Signed in with correct user name

### User Roles
- [ ] First signed-in user automatically has Admin role
- [ ] (Optional) Promoted additional admin via SQL if needed
- [ ] Admin can access `/admin/users` page
- [ ] Admin can promote users to Instructor
- [ ] Admin can delete unauthorized users (removes user, submissions, and votes; recalculates ELO ratings)
- [ ] Verified ELO ratings are recalculated correctly when users are deleted

### Photo Upload
- [ ] Navigated to Submit page
- [ ] Selected a composition rule
- [ ] Uploaded a test photo (<10MB)
- [ ] Added description
- [ ] Submitted successfully
- [ ] Photo appears in gallery
- [ ] Uploads persist after container restart (volume mounted)

### Database
- [ ] Database is accessible: 
 ```bash
 docker exec -it pathfinder-postgres psql -U postgres -d pathfinder_photography -c "SELECT COUNT(*) FROM \"PhotoSubmissions\";"
 ```
- [ ] Data persists after container restart

## Optional Configuration

### Email Notifications
- [ ] Configured EMAIL_* variables in `.env` (SMTP host, port, username, password, SSL, from address/name)
- [ ] Tested email by submitting and grading a photo

### Reverse Proxy (Nginx/Traefik)
- [ ] Configured reverse proxy
- [ ] SSL/TLS certificate installed
- [ ] HTTPS redirects working
- [ ] Updated Google OAuth redirect URIs to HTTPS domain

### Monitoring
- [ ] Configured log rotation
- [ ] Set up backup script
- [ ] Added to monitoring system
- [ ] Health endpoints responding:
 - [ ] `/health`
 - [ ] `/alive`
 - [ ] `/ready`
- [ ] Metrics endpoint responding: `/metrics`
- [ ] (Optional - Aspire Dev) SigNoz automatically started with `dotnet run --project PathfinderPhotography.AppHost`
- [ ] (Optional - Docker Compose) SigNoz profile enabled: `docker compose --profile signoz up -d`
- [ ] (Optional) SigNoz UI accessible: `http://your-server-ip:3301`

### Security Hardening
- [ ] Changed default PostgreSQL password
- [ ] Configured firewall rules (allow only needed ports)
- [ ] Using strong passwords
- [ ] Secrets not in version control
- [ ] SSL/TLS enabled (if exposed to internet)

### Backup Strategy
- [ ] Backup script created
- [ ] Backup script scheduled (cron)
- [ ] Test backup created
- [ ] Test restore successful
- [ ] Backup location accessible
- [ ] Backup retention policy set

## Troubleshooting Completed

If issues occurred, verify resolved:

- [ ] Container logs checked: `docker compose logs`
- [ ] Port conflicts resolved (app and Postgres)
- [ ] Environment variables correct
- [ ] Google OAuth redirect URI matches exactly
- [ ] Database connection working
- [ ] File permissions correct for uploads
- [ ] Network connectivity verified

## Documentation Reviewed

- [ ] Read README.md
- [ ] Read HOMELAB_DEPLOYMENT.md 
- [ ] Read SETUP.md (if doing development)
- [ ] Bookmarked useful commands
- [ ] Know where to find logs

## Maintenance Plan

- [ ] Update schedule determined (weekly/monthly)
- [ ] Backup schedule set (daily recommended)
- [ ] Monitoring in place
- [ ] Disaster recovery plan created
- [ ] Contact information for support documented

## Final Verification

- [ ] Application accessible from all intended devices
- [ ] Multiple users can sign in
- [ ] Photos upload and display correctly
- [ ] Gallery filtering works
- [ ] Performance acceptable
- [ ] No errors in logs
- [ ] Ready for users

## Production Readiness

### For Church Use
- [ ] Announced to pathfinders
- [ ] Instructions provided to users
- [ ] Support contact available
- [ ] Deadline for submissions set
- [ ] Storage capacity verified for expected photos

### Performance Baseline
- [ ] Noted current resource usage
- [ ] Response time acceptable
- [ ] Can handle expected load
- [ ] Backup working

## Sign-Off

- **Deployed by**: ________________
- **Date**: ________________
- **Server**: ________________
- **Version**: ________________
- **Status**: ☐ Development ☐ Staging ☐ Production

## Notes

```
Add any deployment-specific notes here:




```

---

**Next Steps After Deployment:**
1. Monitor logs for first24 hours
2. Test with small group before full rollout
3. Ensure backup is working
4. Share access information with pathfinders
5. Set submission deadline
6. Plan for photo review

**Support Resources:**
- README.md - General documentation
- HOMELAB_DEPLOYMENT.md - Detailed deployment guide
- GitHub Issues - Report problems
- Logs - `docker compose logs -f`

**Quick Commands:**
```bash
# View logs
docker compose logs -f

# Restart app
docker compose restart pathfinder-app

# Update to latest
docker compose pull && docker compose up -d

# Backup database
docker exec -t pathfinder-postgres pg_dump -U postgres pathfinder_photography > backup.sql

# Stop everything
docker compose down
