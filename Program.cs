using PathfinderPhotography.Components;
using PathfinderPhotography.Services;
using PathfinderPhotography.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.DataProtection;
using System.IO;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (telemetry, health checks, service discovery)
builder.AddServiceDefaults();
    
// Register Npgsql DataSource from Aspire PostgreSQL resource "pathfinder-photography"
builder.AddNpgsqlDbContext<ApplicationDbContext>(connectionName: "pathfinder-photography");

// Configure HTTPS redirection options via DI (fixes UseHttpsRedirection overload error)
builder.Services.Configure<HttpsRedirectionOptions>(options =>
{
    options.HttpsPort =443;
});

// Persist DataProtection keys to the filesystem in production so auth cookies remain valid across restarts
builder.Services.AddDataProtection()
 .PersistKeysToFileSystem(new DirectoryInfo("/var/lib/pathfinder-keys"))
 .SetApplicationName("pathfinder-photography");

builder.Services.Configure<HttpsRedirectionOptions>(options =>
{
 options.HttpsPort =443;
});

// Add services to the container.
builder.Services.AddRazorComponents()
 .AddInteractiveServerComponents();

builder.Services.AddServerSideBlazor()
 .AddCircuitOptions(o => o.DetailedErrors = true);

// Add authentication services
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "";
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Add database context
builder.Services.AddDbContextFactory<ApplicationDbContext>((sp, options) =>
{
    string? cs = builder.Configuration.GetConnectionString("pathfinder-photography");
    if (string.IsNullOrWhiteSpace(cs))
    {
        throw new InvalidOperationException("Missing connection string 'pathfinder-photography'.");
    }

    options.UseNpgsql(cs, npgsqlOptions =>
    {
        npgsqlOptions.CommandTimeout(60); // seconds
    });
});

// Add custom services
builder.Services.AddSingleton<CompositionRuleService>();
builder.Services.AddScoped<PhotoSubmissionService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<VotingService>();
builder.Services.AddScoped<PdfExportService>();
builder.Services.AddScoped<EmailNotificationService>();
builder.Services.AddSingleton<IOllamaClientProvider, OllamaClientProvider>();
builder.Services.AddScoped<PhotoAnalysisService>();

// Add AI Processing Background Service
builder.Services.AddSingleton<AiProcessingBackgroundService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<AiProcessingBackgroundService>());

WebApplication app = builder.Build();

// Ensure database is created
using (IServiceScope scope = app.Services.CreateScope())
{
 IDbContextFactory<ApplicationDbContext> dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
 await using ApplicationDbContext context = await dbContextFactory.CreateDbContextAsync();
 await context.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
 app.UseExceptionHandler("/Error", createScopeForErrors: true);
 app.UseHsts();
}

// Configure forwarded headers so the app sees the original request scheme from the reverse proxy (e.g. nginx terminating TLS)
ForwardedHeadersOptions forwardedHeaderOptions = new()
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};

// If your reverse proxy is not on localhost, clear the default restrictions so X-Forwarded-* is honored.
// Note: allowlist specific proxies/networks for better security in production when possible.
forwardedHeaderOptions.KnownNetworks.Clear();
forwardedHeaderOptions.KnownProxies.Clear();

app.UseForwardedHeaders(forwardedHeaderOptions);

// UseHttpsRedirection has no overload that accepts options here.
// The middleware reads configured HttpsRedirectionOptions from DI.
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Map Aspire default endpoints (health checks, etc.)
app.MapDefaultEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>()
 .AddInteractiveServerRenderMode();

// Add login/logout endpoints
app.MapGet("/login", (HttpContext _) => Results.Challenge(
 new AuthenticationProperties { RedirectUri = "/" },
 [GoogleDefaults.AuthenticationScheme]));

app.MapPost("/logout", async context =>
{
 await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
 context.Response.Redirect("/");
});

// Add endpoint to serve images from database
app.MapGet("/api/images/{id:int}", async (int id, IDbContextFactory<ApplicationDbContext> contextFactory) =>
{
 await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
 var imageInfo = await context.PhotoSubmissions
 .Where(p => p.Id == id)
 .Select(p => new { p.ImageData, p.ImageContentType })
 .FirstOrDefaultAsync();
 
 if (imageInfo?.ImageData == null)
 {
    return Results.NotFound();
 }
 
 const int maxImageSizeBytes =10 *1024 *1024; //10MB
 return imageInfo.ImageData.Length > maxImageSizeBytes
 ? Results.StatusCode(413) // Payload Too Large
 : Results.File(imageInfo.ImageData, imageInfo.ImageContentType ?? "image/jpeg");
});

app.Run();
