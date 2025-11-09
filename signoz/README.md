# SigNoz Integration for Pathfinder Photography

This directory contains configuration files for integrating SigNoz, an open-source observability platform, with the Pathfinder Photography application.

## Integration with .NET Aspire

**SigNoz is now fully integrated with .NET Aspire!** When you run the AppHost, all SigNoz components are automatically started and configured.

### Quick Start with Aspire (Recommended)

```bash
# From the repository root
dotnet run --project PathfinderPhotography.AppHost
```

This single command will start:
1. PostgreSQL database
2. Pathfinder Photography web application  
3. **SigNoz Zookeeper** - Cluster coordination (required for ClickHouse)
4. **SigNoz ClickHouse** - Data storage
5. **SigNoz OpenTelemetry Collector** - Telemetry receiver
6. **SigNoz Query Service** - Query processor
7. **SigNoz Frontend** - Web UI (http://localhost:3301)
8. **SigNoz Alert Manager** - Alert handling

All connection strings and environment variables are automatically configured by Aspire - no manual setup needed!

## What is SigNoz?

SigNoz is a comprehensive open-source Application Performance Monitoring (APM) solution that provides:

- **Distributed Tracing**: Track requests across services
- **Metrics Monitoring**: Monitor application and infrastructure metrics
- **Log Management**: Centralized log aggregation and analysis
- **Alerts**: Set up alerts based on metrics and traces
- **Dashboards**: Create custom dashboards for visualization

Website: https://signoz.io/

## Architecture

The SigNoz setup includes:

1. **Zookeeper**: Provides cluster coordination for ClickHouse (required for distributed DDL)
2. **ClickHouse**: Stores traces, metrics, and logs
3. **OpenTelemetry Collector**: Receives telemetry data from the application
4. **Query Service**: Processes queries from the frontend
5. **Frontend**: Web UI for viewing telemetry data
6. **Nginx Reverse Proxy**: Routes browser requests - proxies `/api/*` to query service, everything else to frontend
7. **Alert Manager**: Handles alerting rules

### Request Flow
```
Browser → Nginx Proxy (port 3301/3302)
          ├─ /api/* → Query Service (handles API requests)
          └─ /*     → Frontend (serves UI)
```

This architecture ensures that API calls from the browser are properly routed to the backend query service.

## Quick Start

### Option 1: With .NET Aspire (Recommended)

```bash
# From the repository root
dotnet run --project PathfinderPhotography.AppHost
```

Benefits:
- ✅ Zero configuration required
- ✅ All services start automatically
- ✅ Connection strings auto-injected
- ✅ Persistent data volumes
- ✅ Service orchestration and health checks
- ✅ Access Aspire Dashboard for additional insights

Access points:
- **Application**: Check Aspire Dashboard for URL
- **SigNoz UI**: http://localhost:3301
- **Aspire Dashboard**: Shown in console output

### Option 2: Using Docker Compose (Manual Setup)

```bash
# Start all services including SigNoz
docker-compose --profile signoz up -d

# Access the application
http://localhost:8080

# Access SigNoz UI
http://localhost:3301
```

### Services and Ports

| Service | Port | Description |
|---------|------|-------------|
| Pathfinder Photography | 8080 | Main application |
| **SigNoz Nginx Proxy** | **3301** (Docker), **3302** (Aspire) | **Reverse proxy for SigNoz UI (use this!)** |
| SigNoz Frontend | Internal only | SigNoz UI (accessed through nginx proxy) |
| SigNoz Query Service | 6060, 8081 | Query service API (accessed through nginx proxy) |
| OpenTelemetry Collector | 4317, 4318 | OTLP receivers (gRPC, HTTP) |
| ClickHouse | 9000, 8123 | Database |
| PostgreSQL | 5432 | Application database |
| Alert Manager | 9093 | Alert management |

**Important:** Access SigNoz UI through the nginx proxy at:
- Docker Compose: http://localhost:3301
- Aspire: http://localhost:3302

## Configuration Files

### nginx.conf (NEW)
**Reverse proxy configuration** that routes browser requests:
- Proxies `/api/*` requests to the query service backend
- Proxies all other requests to the frontend UI
- Ensures API calls from the browser work correctly

This solves the 405 error when registering the first admin account by properly routing POST requests to `/api/v1/register` to the backend.

### otel-collector-config.yaml
Configures the OpenTelemetry Collector to:
- Receive telemetry data via OTLP (gRPC and HTTP)
- Process data with batching and memory limits
- Export to ClickHouse for storage

### prometheus.yml
Prometheus scrape configuration for collecting metrics from the application.

### clickhouse-config.xml
ClickHouse database server configuration including:
- Connection limits
- Memory settings
- Query logging

### clickhouse-users.xml
ClickHouse user permissions and quotas.

### alertmanager-config.yaml
Alert manager routing and notification configuration.

## Using SigNoz

### Viewing Traces

1. Open http://localhost:3301
2. Navigate to "Traces" in the left sidebar
3. View distributed traces of HTTP requests, database queries, and more

### Viewing Metrics

1. Navigate to "Metrics" in SigNoz UI
2. Create custom dashboards
3. Monitor:
   - HTTP request rates
   - Response times
   - Error rates
   - Database query performance
   - .NET runtime metrics

### Viewing Logs

1. Navigate to "Logs" in SigNoz UI
2. Search and filter logs
3. Correlate logs with traces

### Creating Alerts

1. Navigate to "Alerts" in SigNoz UI
2. Create alert rules based on:
   - Error rates
   - Response time thresholds
   - Custom metrics

## Environment Variables

### With Aspire Integration (Automatic)

When running via `dotnet run --project PathfinderPhotography.AppHost`, all environment variables are automatically configured:

- ✅ `OTEL_EXPORTER_OTLP_ENDPOINT` - Auto-set to point to SigNoz collector
- ✅ `OTEL_RESOURCE_ATTRIBUTES` - Auto-set with service name
- ✅ All SigNoz container configurations - Auto-configured from these YAML files

No manual configuration needed!

### With Docker Compose (Manual)

The application exports telemetry when these variables are set:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://signoz-otel-collector:4317
OTEL_RESOURCE_ATTRIBUTES=service.name=pathfinder-photography
```

These are pre-configured in `docker-compose.yml` when using the `signoz` profile.

## Data Persistence

SigNoz data is persisted in Docker volumes:
- `clickhouse-data`: ClickHouse database files
- `signoz-data`: SigNoz application data

## Customization

### Adjusting Data Retention

Edit `clickhouse-config.xml` to modify retention policies.

### Custom Metrics

The application uses .NET Aspire's built-in telemetry, which automatically exports:
- HTTP metrics
- Database metrics
- Runtime metrics

### Alert Notifications

Edit `alertmanager-config.yaml` to configure:
- Email notifications
- Slack webhooks
- PagerDuty integration
- Custom webhooks

## Performance Considerations

SigNoz adds some overhead:
- **Memory**: ~2-4GB for all SigNoz components
- **CPU**: Minimal impact on application
- **Disk**: Depends on data retention settings

For production:
- Adjust batch sizes in `otel-collector-config.yaml`
- Configure data retention in ClickHouse
- Use sampling for high-traffic applications

## Troubleshooting

### SigNoz UI not accessible
```bash
# Check if services are running
docker-compose -f docker-compose.signoz.yml ps

# Check frontend logs
docker-compose -f docker-compose.signoz.yml logs signoz-frontend
```

### No data showing in SigNoz
```bash
# Check collector logs
docker-compose -f docker-compose.signoz.yml logs signoz-otel-collector

# Verify application is sending data
docker-compose -f docker-compose.signoz.yml logs pathfinder-photography | grep -i otel
```

### ClickHouse connection issues
```bash
# Check ClickHouse health
docker-compose -f docker-compose.signoz.yml exec signoz-clickhouse wget -qO- localhost:8123/ping
```

### 405 Error when registering first admin account

If you encounter a 405 (Method Not Allowed) error when trying to register the first admin account:

```
POST http://localhost:3301/api/v1/register 405 (Not Allowed)
```

**Solution:** This is fixed by the nginx reverse proxy configuration. The proxy routes `/api/*` requests to the query service backend.

**Verification:**
```bash
# Check all SigNoz containers are running
docker compose --profile signoz ps

# Verify nginx proxy is running
docker compose --profile signoz logs signoz-nginx

# Test the proxy routing
curl -v http://localhost:3301/api/v1/version
```

The nginx proxy configuration (`signoz/nginx.conf`) handles:
- Browser → `/api/v1/register` → Query Service (POST succeeds)
- Browser → `/` → Frontend UI (static files)

**Architecture:**
```
Browser makes API call
    ↓
Nginx Proxy (localhost:3301)
    ↓
Routes /api/* to Query Service
    ↓
Registration succeeds!
```

If you're still experiencing issues:
1. Ensure the nginx proxy container is running: `docker compose --profile signoz ps signoz-nginx`
2. Check nginx logs: `docker compose --profile signoz logs signoz-nginx`
3. Verify the nginx.conf file exists: `ls -la signoz/nginx.conf`
4. Restart the nginx container: `docker compose --profile signoz restart signoz-nginx`

## Disabling SigNoz

### With Aspire

To run without SigNoz, you would need to modify `PathfinderPhotography.AppHost/Program.cs` and comment out the SigNoz container definitions. However, since SigNoz runs in containers with minimal overhead, it's recommended to keep it enabled for development.

### With Docker Compose

To run the application without SigNoz:

```bash
# Use the standard docker-compose file without the signoz profile
docker-compose up -d
```

## Resources

- [SigNoz Documentation](https://signoz.io/docs/)
- [OpenTelemetry Documentation](https://opentelemetry.io/docs/)
- [ClickHouse Documentation](https://clickhouse.com/docs/)

## Support

For issues specific to:
- **SigNoz**: https://github.com/SigNoz/signoz/issues
- **This Integration**: Create an issue in this repository
