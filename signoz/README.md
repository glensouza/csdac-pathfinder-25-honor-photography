# SigNoz Integration for Pathfinder Photography

This directory contains configuration files for integrating SigNoz, an open-source observability platform, with the Pathfinder Photography application.

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

1. **OpenTelemetry Collector**: Receives telemetry data from the application
2. **ClickHouse**: Stores traces, metrics, and logs
3. **Query Service**: Processes queries from the frontend
4. **Frontend**: Web UI for viewing telemetry data
5. **Alert Manager**: Handles alerting rules

## Quick Start

### Using Docker Compose with SigNoz

```bash
# Start all services including SigNoz
docker-compose -f docker-compose.signoz.yml up -d

# Access the application
http://localhost:8080

# Access SigNoz UI
http://localhost:3301
```

### Services and Ports

| Service | Port | Description |
|---------|------|-------------|
| Pathfinder Photography | 8080 | Main application |
| SigNoz Frontend | 3301 | SigNoz UI dashboard |
| SigNoz Query Service | 6060 | Query service API |
| OpenTelemetry Collector | 4317, 4318 | OTLP receivers (gRPC, HTTP) |
| ClickHouse | 9000, 8123 | Database |
| PostgreSQL | 5432 | Application database |
| Alert Manager | 9093 | Alert management |

## Configuration Files

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

The application automatically exports telemetry when these variables are set:

```bash
OTEL_EXPORTER_OTLP_ENDPOINT=http://signoz-otel-collector:4317
OTEL_RESOURCE_ATTRIBUTES=service.name=pathfinder-photography
```

These are pre-configured in `docker-compose.signoz.yml`.

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

## Disabling SigNoz

To run the application without SigNoz:

```bash
# Use the standard docker-compose file
docker-compose up -d

# Or use the homelab compose file
docker-compose -f docker-compose.homelab.yml up -d
```

## Resources

- [SigNoz Documentation](https://signoz.io/docs/)
- [OpenTelemetry Documentation](https://opentelemetry.io/docs/)
- [ClickHouse Documentation](https://clickhouse.com/docs/)

## Support

For issues specific to:
- **SigNoz**: https://github.com/SigNoz/signoz/issues
- **This Integration**: Create an issue in this repository
