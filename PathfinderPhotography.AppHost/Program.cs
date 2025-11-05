var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var pathfinderDb = postgres.AddDatabase("pathfinder_photography");

// Add the main web application
var webApp = builder.AddProject<Projects.PathfinderPhotography>("webapp")
    .WithReference(pathfinderDb)
    .WithExternalHttpEndpoints();

builder.Build().Run();
