# Step 1: Install PostgreSQL

## 📋 Quick Navigation

| [← Prerequisites](00-prerequisites.md) | [Home](../DEPLOY.md) | [Next: .NET Runtime →](02-install-dotnet.md) |
|:---------------------------------------|:--------------------:|---------------------------------------------:|

## 📑 Deployment Steps Index

- [Prerequisites](00-prerequisites.md)
- **Step 1: Install PostgreSQL** ← You are here
- [Step 2: Install .NET Runtime](02-install-dotnet.md)
- [Step 3: Install Application](03-install-application.md)
- [Step 4: Configure Systemd Service](04-configure-systemd.md)
- [Step 5: Install SigNoz](05-install-signoz.md)
- [Step 6: Install Nginx Reverse Proxy](06-install-nginx.md)
- [Step 7: Setup Automated Deployments](07-automated-deployments.md)
- [Security & Performance](08-security-performance.md)

---

## Overview

In this step, you'll install:
- PostgreSQL 16 (database server)
- Cockpit (system management interface)
- PGAdmin 4 (database management tool)

**Estimated time**: 30-45 minutes

## Install PostgreSQL 16

PostgreSQL 16 is the recommended version.

```bash
# Update system packages
sudo apt update && sudo apt upgrade -y

# Install PostgreSQL
sudo apt install -y postgresql-16 postgresql-contrib-16

# Start and enable PostgreSQL
sudo systemctl start postgresql
sudo systemctl enable postgresql

# Verify installation
sudo systemctl status postgresql
```

## Install Cockpit (System Management Interface)

Cockpit provides a web-based management interface for your server, making it easier to monitor system resources, manage services, and perform administrative tasks.

```bash
# Install Cockpit
sudo apt install -y cockpit

# Start and enable Cockpit
sudo systemctl start cockpit
sudo systemctl enable cockpit

# Enable root login (comment out root from disallowed users)
sudo sed -i 's/^root$/#root/' /etc/cockpit/disallowed-users

# Verify installation
sudo systemctl status cockpit
```

**Note**: By default, Cockpit disables root login for security. The configuration above enables root access by commenting out the root user from `/etc/cockpit/disallowed-users`. If you prefer to keep root disabled, skip the `sed` command and create a separate admin user instead.

Cockpit will be accessible at `https://your-server-ip:9090`. To access it securely, you can configure Nginx as a reverse proxy or use SSH tunneling:

```bash
# SSH tunnel to access Cockpit securely
ssh -L 9090:localhost:9090 user@your-server-ip
# Then access http://localhost:9090 from your local browser
```

### Troubleshooting: Cockpit Updates Page Error

If you encounter "Cannot refresh cache whilst offline" error on the Cockpit updates page, this is a known PackageKit issue on some systems where it requires a network interface with a gateway. Workaround:

```bash
# Create a dummy network interface with gateway
sudo nmcli con add type dummy con-name fake ifname fake0 ip4 1.2.3.4/24 gw4 1.2.3.1
```

Alternatively, you can manage updates via command line instead:
```bash
sudo apt update && sudo apt upgrade
```

## Install PGAdmin 4 (PostgreSQL Management Tool)

PGAdmin 4 provides a web-based interface for managing PostgreSQL databases.

```bash
# Add PGAdmin repository GPG key
curl -fsSL https://www.pgadmin.org/static/packages_pgadmin_org.pub | sudo gpg --dearmor -o /usr/share/keyrings/packages-pgadmin-org.gpg

# Add PGAdmin repository
sudo sh -c 'echo "deb [signed-by=/usr/share/keyrings/packages-pgadmin-org.gpg] https://ftp.postgresql.org/pub/pgadmin/pgadmin4/apt/$(lsb_release -cs) pgadmin4 main" > /etc/apt/sources.list.d/pgadmin4.list'

# Update package list
sudo apt update

# Install PGAdmin 4 web mode
sudo apt install -y pgadmin4-web

# Run the setup script to configure pgAdmin4
sudo /usr/pgadmin4/bin/setup-web.sh
```

**During the setup script, you will be prompted for:**
- Email address (this will be your pgAdmin login username)
- Password (choose a strong password - this is for pgAdmin access, not PostgreSQL)
- Whether to configure Apache (answer 'y' for Yes - this will automatically configure Apache)

The setup script will:
- Create the pgAdmin configuration database
- Set up the initial admin user
- Configure the required directories and permissions
- Configure Apache to serve pgAdmin

**Important Security Note**: The email and password you set here are for accessing the pgAdmin web interface. This is separate from your PostgreSQL database credentials. Use a strong, unique password.

### Configure Apache for pgAdmin

After running setup-web.sh with Apache configuration enabled, you need to configure Apache to work with Nginx. 

**Important**: The setup script creates the configuration file in `/etc/apache2/conf-available/pgadmin4.conf` (not `sites-available`).

```bash
# Apache should already be installed and enabled by setup-web.sh
# First, configure Apache to listen on 127.0.0.1:8080
sudo nano /etc/apache2/ports.conf
```

Add this line (keep any existing Listen 80 if present):
```
Listen 127.0.0.1:8080
```

Now edit the pgAdmin Apache configuration file. **Important**: Replace the entire file content (don't just add to it):

```bash
sudo nano /etc/apache2/conf-available/pgadmin4.conf
```

Replace the entire file content with:
```apache
<VirtualHost 127.0.0.1:8080>
    WSGIDaemonProcess pgadmin processes=1 threads=25 python-home=/usr/pgadmin4/venv
    WSGIScriptAlias /pgadmin4 /usr/pgadmin4/web/pgAdmin4.wsgi
    
    <Directory /usr/pgadmin4/web>
        WSGIProcessGroup pgadmin
        WSGIApplicationGroup %{GLOBAL}
        Require all granted
    </Directory>
</VirtualHost>
```

**Critical**: The original file from setup-web.sh contains only WSGIDaemonProcess and WSGIScriptAlias directives without a VirtualHost wrapper. You must replace the entire file content with the above configuration to avoid duplicate WSGI daemon definitions.

Restart Apache:
```bash
sudo systemctl restart apache2
sudo systemctl status apache2
```

pgAdmin will now be accessible at `http://localhost:8080/pgadmin4`. 

### Verify Apache is Working

```bash
# Check if Apache is running
sudo systemctl status apache2

# Verify Apache is listening on port 8080
sudo ss -tlnp | grep 8080

# Test pgAdmin accessibility
curl -I http://localhost:8080/pgadmin4
# Should return HTTP 200 OK or 302 redirect
```

If the service fails to start, check the logs:

```bash
# View Apache error logs
sudo journalctl -u apache2 -n 50 --no-pager
sudo tail -f /var/log/apache2/error.log

# View pgAdmin application logs
sudo tail -f /var/log/pgadmin/pgadmin4.log
```

### Common Issues and Solutions

1. **"Name duplicates previous WSGI daemon definition"**: You appended the VirtualHost wrapper instead of replacing the file content.
   ```bash
   # Edit the file and replace ALL content with the VirtualHost block above
   sudo nano /etc/apache2/conf-available/pgadmin4.conf
   sudo systemctl restart apache2
   ```

2. **Apache fails to start**: Check Apache logs with `sudo journalctl -u apache2 -n 50 --no-pager`

3. **Connection refused on port 8080**: Verify ports.conf has `Listen 127.0.0.1:8080`
   ```bash
   grep 8080 /etc/apache2/ports.conf
   # If not found, add: Listen 127.0.0.1:8080
   sudo systemctl restart apache2
   ```

4. **Port 8080 already in use**: 
   ```bash
   sudo ss -tlnp | grep 8080
   # Change to a different port in both /etc/apache2/ports.conf and pgadmin4.conf
   ```

5. **Permission denied errors**: Check ownership of `/var/lib/pgadmin` and `/var/log/pgadmin` directories:
   ```bash
   sudo chown -R www-data:www-data /var/lib/pgadmin
   sudo chown -R www-data:www-data /var/log/pgadmin
   ```

6. **Database initialization errors**: Re-run the setup script: `sudo /usr/pgadmin4/bin/setup-web.sh`

7. **Configuration file not found**: The file is in `/etc/apache2/conf-available/pgadmin4.conf`, not `sites-available`:
   ```bash
   ls -la /etc/apache2/conf-available/pgadmin4.conf
   ls -la /etc/apache2/conf-enabled/pgadmin4.conf
   ```

8. **Apache configuration test fails**:
   ```bash
   sudo apache2ctl configtest
   # Fix any configuration errors shown
   ```

PGAdmin will run on `http://localhost:8080/pgadmin4` and will be proxied through Nginx at the subdomain `photohonorpgadmin.coronasda.church` (see [Step 5: Nginx](05-install-nginx.md)).

**Note**: Using single-level subdomains (e.g., `photohonorpgadmin`) instead of multi-level subdomains (e.g., `pgadmin.photohonor`) avoids SSL/TLS certificate issues with wildcard certificates.

**Security Note**: 
- PGAdmin 4 is a powerful database management tool with full access to your PostgreSQL databases
- Only expose it through Nginx with HTTPS (SSL/TLS)
- Use strong passwords for both pgAdmin login and PostgreSQL users
- Consider restricting access by IP address in Nginx configuration if only accessing from specific locations
- The pgAdmin interface should only be accessible by authorized administrators

## Update Login Message (Optional)

To display service URLs in the login message, update the `/etc/profile.d/00_lxc-details.sh` file (or create it if it doesn't exist):

```bash
sudo nano /etc/profile.d/00_lxc-details.sh
```

Add the following content to display service information on login:

```bash
echo -e ""
echo -e "Pathfinder Photography Server"
echo -e "    🏠   Hostname: $(hostname)"
echo -e "    💡   IP Address: $(hostname -I | awk '{print $1}')"
echo -e ""
echo -e "Available Services:"
echo -e "    🌐   Pathfinder Photography App:"
echo -e "        - Local: http://10.10.10.200"
echo -e "        - Public: https://photohonor.coronasda.church"
echo -e "    🗄️   PGAdmin 4 (Database Management):"
echo -e "        - Local: http://localhost:8080/pgadmin4"
echo -e "        - Public: https://photohonorpgadmin.coronasda.church"
echo -e "    📊   SigNoz (Observability - Optional):"
echo -e "        - Local: http://10.10.10.200:3301"
echo -e "        - Public: https://photohonorsignoz.coronasda.church"
echo -e "    🖥️   Cockpit (System Management):"
echo -e "        - Local: https://10.10.10.200:9090"
echo -e ""
```

**Note**: 
- Replace `10.10.10.200` with your actual local network IP address
- Comment out or remove the SigNoz section if you don't install SigNoz ([Step 6](06-install-signoz.md))
- Local URLs use HTTP and specific ports; public URLs use HTTPS via Cloudflare Tunnel

Make the script executable:

```bash
sudo chmod +x /etc/profile.d/00_lxc-details.sh
```

The login message will display on your next SSH login, showing quick access URLs for all services.

## Configure PostgreSQL Database

**Security Note**: Use the strong random password you generated in the [Prerequisites](00-prerequisites.md) step.

```bash
# Switch to postgres user
sudo -u postgres psql
```

Create the database and user with your generated password:

```sql
-- Create database and user
CREATE DATABASE pathfinder_photography;
CREATE USER pathfinder WITH PASSWORD 'paste_your_generated_password_here';
GRANT ALL PRIVILEGES ON DATABASE pathfinder_photography TO pathfinder;

-- Grant schema permissions
\c pathfinder_photography
GRANT ALL ON SCHEMA public TO pathfinder;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO pathfinder;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO pathfinder;

-- Exit PostgreSQL
\q
```

**Important**: Save the generated password securely - you'll need it for the application configuration in [Step 3](03-install-application.md).

## Secure PostgreSQL (Production)

Edit PostgreSQL configuration to only listen on localhost (if app is on same server):

```bash
sudo nano /etc/postgresql/16/main/postgresql.conf
```

Ensure this line exists:
```
listen_addresses = 'localhost'
```

Edit `pg_hba.conf` for authentication:
```bash
sudo nano /etc/postgresql/16/main/pg_hba.conf
```

Add this line for the pathfinder user:
```
local   pathfinder_photography    pathfinder                            scram-sha-256
```

Restart PostgreSQL:
```bash
sudo systemctl restart postgresql
```

## Verification Checklist

Before moving to the next step, verify:

- [ ] PostgreSQL 16 is installed and running
- [ ] Cockpit is accessible at `https://your-server-ip:9090`
- [ ] PGAdmin 4 is installed and Apache is running
- [ ] PGAdmin is accessible at `http://localhost:8080/pgadmin4`
- [ ] Database `pathfinder_photography` is created
- [ ] User `pathfinder` is created with a secure password
- [ ] PostgreSQL is configured to listen only on localhost
- [ ] You've saved your PostgreSQL password securely

---

## Next Step

PostgreSQL is now installed and configured! Continue with installing the .NET runtime.

| [← Prerequisites](00-prerequisites.md) | [Home](../DEPLOY.md) | [Next: .NET Runtime →](02-install-dotnet.md) |
|:---------------------------------------|:--------------------:|---------------------------------------------:|
