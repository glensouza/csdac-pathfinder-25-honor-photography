using PathfinderPhotography.Components;
using PathfinderPhotography.Services;
using PathfinderPhotography.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (telemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Register Npgsql DataSource from Aspire PostgreSQL resource "pathfinder-photography"
builder.AddNpgsqlDbContext<ApplicationDbContext>(connectionName: "pathfinder-photography");

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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

    options.UseNpgsql(cs);
});

// Add custom services
builder.Services.AddSingleton<CompositionRuleService>();
builder.Services.AddScoped<PhotoSubmissionService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<VotingService>();

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
    
    const int maxImageSizeBytes = 10 * 1024 * 1024; // 10MB
    return imageInfo.ImageData.Length > maxImageSizeBytes
        ? Results.StatusCode(413) // Payload Too Large
        : Results.File(imageInfo.ImageData, imageInfo.ImageContentType ?? "image/jpeg");
});

app.Run();
