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
using System.Text.Json;
using System.Diagnostics;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Enable systemd integration for proper service notifications
builder.Host.UseSystemd();

// Validate required configuration and repo automation before proceeding
ValidateRequiredConfigurations(builder.Configuration);

// Add Aspire service defaults (telemetry, health checks, service discovery)
builder.AddServiceDefaults();