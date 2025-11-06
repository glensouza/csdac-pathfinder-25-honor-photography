using PathfinderPhotography.Components;
using PathfinderPhotography.Services;
using PathfinderPhotography.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (telemetry, health checks, service discovery)
builder.AddServiceDefaults();

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

// Add database context with Aspire PostgreSQL integration
builder.AddNpgsqlDbContext<ApplicationDbContext>("pathfinder_photography");

// Add custom services
builder.Services.AddSingleton<CompositionRuleService>();
builder.Services.AddScoped<PhotoSubmissionService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<VotingService>();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
    using var context = await dbContextFactory.CreateDbContextAsync();
    await context.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
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
app.MapGet("/login", (HttpContext context) =>
{
    return Results.Challenge(
        new AuthenticationProperties { RedirectUri = "/" },
        new[] { GoogleDefaults.AuthenticationScheme });
});

app.MapPost("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    context.Response.Redirect("/");
});

// Add endpoint to serve images from database, with authentication and authorization
app.MapGet("/api/images/{id:int}", async (HttpContext httpContext, int id, IDbContextFactory<ApplicationDbContext> contextFactory) =>
{
    // Require authentication
    if (!httpContext.User.Identity?.IsAuthenticated ?? true)
    {
        return Results.Unauthorized();
    }

    using var context = await contextFactory.CreateDbContextAsync();
    var photo = await context.PhotoSubmissions
        .Where(p => p.Id == id)
        .Select(p => new { p.ImageData, p.ImageContentType, p.UserId, p.IsPublic })
        .FirstOrDefaultAsync();

    if (photo?.ImageData == null)
    {
        return Results.NotFound();
    }

    // Get current user ID (assumes NameIdentifier claim is used for user ID)
    var userId = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

    // Only allow access if the image is public or belongs to the current user
    if (!(photo.IsPublic || (userId != null && photo.UserId == userId)))
    {
        return Results.Forbid();
    }

    return Results.File(photo.ImageData, photo.ImageContentType ?? "image/jpeg");
})
.RequireAuthorization();
app.Run();
