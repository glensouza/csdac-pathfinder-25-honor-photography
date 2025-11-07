IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin(pgAdmin => pgAdmin.WithLifetime(ContainerLifetime.Persistent));

IResourceBuilder<PostgresDatabaseResource> pathfinderDb = postgres.AddDatabase("pathfinder-photography"); // resource name (kebab-case)

// Add SigNoz observability stack
// 1. ClickHouse - database for storing traces, metrics, and logs
IResourceBuilder<ContainerResource> clickhouse = builder.AddContainer("signoz-clickhouse", "clickhouse/clickhouse-server", "24.1.2-alpine")
    .WithBindMount("./signoz/clickhouse-config.xml", "/etc/clickhouse-server/config.d/docker_related_config.xml")
    .WithBindMount("./signoz/clickhouse-users.xml", "/etc/clickhouse-server/users.d/users.xml")
    .WithVolume("clickhouse-data", "/var/lib/clickhouse")
    .WithEnvironment("CLICKHOUSE_DB", "signoz")
    .WithHttpEndpoint(port: 8123, targetPort: 8123, name: "http")
    .WithEndpoint(port: 9000, targetPort: 9000, name: "native")
    .WithLifetime(ContainerLifetime.Persistent);

// 2. OpenTelemetry Collector - receives telemetry data from the application
IResourceBuilder<ContainerResource> otelCollector = builder.AddContainer("signoz-otel-collector", "signoz/signoz-otel-collector", "0.102.9")
    .WithBindMount("./signoz/otel-collector-config.yaml", "/etc/otel-collector-config.yaml")
    .WithArgs("--config=/etc/otel-collector-config.yaml")
    .WithEnvironment("OTEL_RESOURCE_ATTRIBUTES", "host.name=signoz-host")
    .WithHttpEndpoint(port: 4318, targetPort: 4318, name: "otlp-http")
    .WithEndpoint(port: 4317, targetPort: 4317, name: "otlp-grpc", scheme: "http")
    .WaitFor(clickhouse);

// 3. Query Service - processes queries from the frontend
IResourceBuilder<ContainerResource> queryService = builder.AddContainer("signoz-query-service", "signoz/query-service", "0.54.1")
    .WithBindMount("./signoz/prometheus.yml", "/root/config/prometheus.yml")
    .WithArgs("-config=/root/config/prometheus.yml")
    .WithVolume("signoz-data", "/var/lib/signoz")
    .WithEnvironment("ClickHouseUrl", "tcp://signoz-clickhouse:9000")
    .WithEnvironment("STORAGE", "clickhouse")
    .WithEnvironment("GODEBUG", "netdns=go")
    .WithEnvironment("TELEMETRY_ENABLED", "true")
    .WithEnvironment("DEPLOYMENT_TYPE", "docker-standalone-amd")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "api")
    .WithHttpEndpoint(port: 6060, targetPort: 6060, name: "query")
    .WaitFor(clickhouse);

// 4. Frontend - web UI for viewing telemetry data
IResourceBuilder<ContainerResource> signozFrontend = builder.AddContainer("signoz-frontend", "signoz/frontend", "0.54.1")
    .WithEnvironment("FRONTEND_API_ENDPOINT", "http://signoz-query-service:8080")
    .WithHttpEndpoint(port: 3301, targetPort: 3301, name: "ui")
    .WithExternalHttpEndpoints()
    .WaitFor(queryService);

// 5. Alert Manager - handles alerting rules
IResourceBuilder<ContainerResource> alertManager = builder.AddContainer("signoz-alertmanager", "signoz/alertmanager", "0.23.5")
    .WithBindMount("./signoz/alertmanager-config.yaml", "/etc/alertmanager/config.yml")
    .WithVolume("alertmanager-data", "/data")
    .WithArgs("--queryService.url=http://signoz-query-service:8080", "--storage.path=/data")
    .WithHttpEndpoint(port: 9093, targetPort: 9093, name: "api")
    .WaitFor(queryService);

// Add the main web application with reference to OpenTelemetry collector
IResourceBuilder<ProjectResource> webApp = builder.AddProject<Projects.PathfinderPhotography>("webapp")
    .WithReference(pathfinderDb)
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", otelCollector.GetEndpoint("otlp-grpc"))
    .WithEnvironment("OTEL_RESOURCE_ATTRIBUTES", "service.name=pathfinder-photography")
    .WithExternalHttpEndpoints();

builder.Build().Run();
