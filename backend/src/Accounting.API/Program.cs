using System.Text;
using System.Threading.RateLimiting;
using Accounting.Application;
using Accounting.Application.Auth;
using Accounting.API.Middleware;
using Accounting.API.Services;
using Accounting.Core.Interfaces;
using Accounting.Infrastructure;
using Accounting.Infrastructure.Data;
using Accounting.Infrastructure.Data.Seeders;
using Accounting.Infrastructure.Services;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

// ─── Serilog Bootstrap Logger ────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ─── Serilog Full Logger ──────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // ─── Application Layers ───────────────────────────────────────────────────
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // ─── Hangfire (background jobs) ───────────────────────────────────────────
    // Use PostgreSQL as the job storage backend (same connection string).
    builder.Services.AddHangfire(hf => hf
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UsePostgreSqlStorage(c =>
            c.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))));

    // Hangfire server: processes queued/recurring jobs within this process.
    builder.Services.AddHangfireServer(opts =>
    {
        opts.WorkerCount = 2;
        opts.ServerName = "accounting-alert-server";
    });

    // ─── Startup Secret Validation ────────────────────────────────────────────
    // Reject placeholder secrets in production to prevent accidental deployment
    // with insecure defaults. Override via environment variables in production:
    //   Auth__JwtSecret=<your-secret>
    var jwtSecret = builder.Configuration["Auth:JwtSecret"]
        ?? throw new InvalidOperationException("Auth:JwtSecret must be configured.");

    const string PlaceholderSecret = "CHANGE_THIS_TO_A_STRONG_SECRET_KEY_IN_PRODUCTION_MIN_32_CHARS";
    if (!builder.Environment.IsDevelopment() && jwtSecret == PlaceholderSecret)
        throw new InvalidOperationException(
            "Auth:JwtSecret is set to the placeholder value. " +
            "Set a strong secret (≥32 chars) via the Auth__JwtSecret environment variable before deploying.");

    if (jwtSecret.Length < 32)
        throw new InvalidOperationException(
            $"Auth:JwtSecret must be at least 32 characters. Current length: {jwtSecret.Length}.");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opts =>
        {
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = builder.Configuration["Auth:JwtIssuer"]   ?? "accounting-api",
                ValidAudience            = builder.Configuration["Auth:JwtAudience"] ?? "accounting-client",
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ClockSkew                = TimeSpan.FromSeconds(30),
            };
        });

    // ─── Permission-based Authorization Policies ──────────────────────────────
    builder.Services.AddAuthorization(opts =>
    {
        foreach (var permission in PermissionNames.All)
        {
            opts.AddPolicy(permission, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireClaim("permission", permission));
        }
    });

    // ─── Current User (scoped, resolved from JWT claims) ─────────────────────
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    // ─── API Services ─────────────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Accounting API", Version = "v1" });

        // Swagger: add Bearer token input
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name         = "Authorization",
            Type         = SecuritySchemeType.Http,
            Scheme       = "bearer",
            BearerFormat = "JWT",
            In           = ParameterLocation.Header,
            Description  = "Enter your JWT access token.",
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                        { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AccountingDbContext>();

    // ─── CORS (dev: allow all; prod: restrict to frontend origin) ─────────────
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
            policy.WithOrigins(
                    builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                    ?? new[] { "http://localhost:5173" })
                .AllowAnyHeader()
                .AllowAnyMethod());
    });

    // ─── Rate Limiting ────────────────────────────────────────────────────────
    // Fixed-window limiter on auth endpoints: 10 requests per IP per minute.
    // Prevents brute-force attacks on login and refresh without blocking legitimate use.
    builder.Services.AddRateLimiter(opts =>
    {
        opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        opts.AddFixedWindowLimiter("auth", limiterOpts =>
        {
            limiterOpts.PermitLimit = 10;
            limiterOpts.Window = TimeSpan.FromMinutes(1);
            limiterOpts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOpts.QueueLimit = 0; // reject immediately when limit is hit
        });
    });

    // ─── Explicit Port Binding ────────────────────────────────────────────────
    // Ensures the app always listens on 0.0.0.0:8080 inside Docker,
    // regardless of ASPNETCORE_URLS environment variable resolution order.
    builder.WebHost.UseUrls("http://0.0.0.0:8080");

    var app = builder.Build();

    // ─── Auto-migrate + Seed on startup ──────────────────────────────────────
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AccountingDbContext>();
        await db.Database.MigrateAsync();

        // Idempotent seeder: runs in all environments.
        // Seeds permissions, Admin role, and initial admin user.
        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        await seeder.SeedAsync();
    }

    // ─── Middleware Pipeline ──────────────────────────────────────────────────
    app.UseGlobalExceptionHandler();
    app.UseSerilogRequestLogging();
    app.UseRateLimiter();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // HTTPS redirection is handled by the reverse proxy (Nginx/Cloudflare) outside the container.
    // Enabling it inside Docker with no certificate causes redirect loops.
    if (!app.Environment.IsProduction())
        app.UseHttpsRedirection();

    app.UseCors("AllowFrontend");
    app.UseAuthentication();
    app.UseAuthorization();

    // ─── Hangfire ─────────────────────────────────────────────────────────────
    // Dashboard is restricted to development; in production add auth filter.
    if (app.Environment.IsDevelopment())
    {
        app.UseHangfireDashboard("/hangfire");
    }

    // Register alert scanner as a recurring hourly job.
    // Cron "0 * * * *" = at minute 0 of every hour.
    RecurringJob.AddOrUpdate<AlertScanner>(
        recurringJobId: "alert-scanner",
        methodCall: scanner => scanner.ScanAllAsync(CancellationToken.None),
        cronExpression: "0 * * * *",
        options: new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

    app.MapControllers();
    app.MapHealthChecks("/health");

    Log.Information("Accounting API starting up...");
    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
