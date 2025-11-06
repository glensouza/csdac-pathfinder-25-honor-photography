IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
IResourceBuilder<PostgresServerResource> postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin(pgAdmin => pgAdmin.WithLifetime(ContainerLifetime.Persistent));

IResourceBuilder<PostgresDatabaseResource> pathfinderDb = postgres.AddDatabase("pathfinder-photography"); // resource name (kebab-case)

// If your version exposes it, set the actual DB name explicitly:
// pathfinderDb.WithDatabaseName("pathfinder_photography");

// Add the main web application
IResourceBuilder<ProjectResource> webApp = builder.AddProject<Projects.PathfinderPhotography>("webapp")
    .WithReference(pathfinderDb)
    .WithExternalHttpEndpoints();

builder.Build().Run();
