using System.Text;
using System.Threading.RateLimiting;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NexaFlow.API.Endpoints;
using NexaFlow.API.Hubs;
using NexaFlow.API.Infrastructure;
using NexaFlow.API.Middleware;
using Microsoft.AspNetCore.Authorization;
using NexaFlow.API.Infrastructure.Authorization;
using NexaFlow.Application;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Application.Common.Security;
using NexaFlow.Core.Constants;
using NexaFlow.Infrastructure;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using NexaFlow.Infrastructure.Auth;
using NexaFlow.Infrastructure.Jobs;
using NexaFlow.Infrastructure.Persistence;
using Serilog;

// QuestPDF runs under the free Community license (payslip generation).
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ---- Logging (T-014) ----
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console());

// ---- Application + Infrastructure ----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ---- Request-scoped context (T-004) ----
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<ITenantContext, TenantContext>();

// ---- Data Protection: encrypts per-tenant integration credentials at rest ----
builder.Services.AddDataProtection();

// ---- Real-time notifications (SignalR) for the automation engine ----
builder.Services.AddSignalR();
builder.Services.AddScoped<IWorkflowNotifier, SignalRWorkflowNotifier>();
builder.Services.AddTransient<IExportNotifier, SignalRExportNotifier>();

// ---- Hangfire (background scheduler for the automation engine) ----
builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(
        builder.Configuration.GetConnectionString("Default"),
        new Hangfire.SqlServer.SqlServerStorageOptions
        {
            CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
            SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
            QueuePollInterval = TimeSpan.Zero,
            UseRecommendedIsolationLevel = true,
            DisableGlobalLocks = true
        }));
builder.Services.AddHangfireServer(options => options.WorkerCount = 5);

// ---- AuthN: JWT bearer ----
var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        // SignalR sends the JWT via the query string on the WebSocket handshake.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// ---- AuthZ policies (T-013) ----
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

var authBuilder = builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AppPolicies.RequireSuperAdmin, p => p.RequireRole(AppRoles.SuperAdmin))
    .AddPolicy(AppPolicies.RequireCompanyAdmin, p => p.RequireRole(AppRoles.SuperAdmin, AppRoles.CompanyAdmin))
    .AddPolicy(AppPolicies.RequireManager, p => p.RequireRole(AppRoles.SuperAdmin, AppRoles.CompanyAdmin, AppRoles.Manager));

foreach (var perm in AppPermissions.All)
{
    authBuilder.AddPolicy(perm, p => p.Requirements.Add(new PermissionRequirement(perm)));
}

// ---- CORS (Angular dev) ----
const string corsPolicy = "AllowFrontend";
builder.Services.AddCors(o => o.AddPolicy(corsPolicy, p => p
    .WithOrigins(builder.Configuration.GetSection("App:AllowedOrigins").Get<string[]>() ?? ["http://localhost:4200"])
    .AllowAnyHeader()
    .AllowAnyMethod()));

// ---- Tenant-based Rate limiting (Built-in .NET 8+) ----
builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        // Extract TenantId from JWT if authenticated, otherwise fallback to IP
        var tenantClaim = httpContext.User.FindFirst("tenant");
        var partitionKey = tenantClaim?.Value ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetTokenBucketLimiter(partitionKey, _ =>
            new TokenBucketRateLimiterOptions
            {
                TokenLimit = 100,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10,
                ReplenishmentPeriod = TimeSpan.FromSeconds(10),
                TokensPerPeriod = 50,
                AutoReplenishment = true
            });
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ---- Health Checks (Phase 3) ----
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("Default") ?? "", name: "Database")
    .AddRedis(builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379", name: "Redis Cache");

// ---- OpenAPI (built-in) + Bearer scheme for the Authorize button ----
builder.Services.AddOpenApi(options =>
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

var app = builder.Build();

// ---- Pipeline ----
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", "NexaFlow API v1"));
}

app.UseHttpsRedirection();

// ---- Localization (Arabic & English) ----
var supportedCultures = new[] { "en", "ar" };
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures));

app.UseCors(corsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapTenantEndpoints();
app.MapBranchEndpoints();
app.MapInvitationEndpoints();
app.MapCustomerEndpoints();
app.MapLeadEndpoints();
app.MapEmployeeEndpoints();
app.MapAttendanceEndpoints();
app.MapLeavesEndpoints();
app.MapPayrollEndpoints();
app.MapProductsEndpoints();
app.MapAutomationEndpoints();
app.MapIntegrationEndpoints();
app.MapBranchEndpoints();
app.MapRoleEndpoints();
app.MapAccountingEndpoints();
app.MapUserPreferencesEndpoints();
app.MapExportEndpoints();
app.MapBackupEndpoints();
app.MapChatEndpoints();
app.MapPredictionEndpoints();
app.MapReportEndpoints();
app.MapHub<NotificationHub>("/hubs/notifications");

// System Health Endpoints
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse,
    ResultStatusCodes =
    {
        [HealthStatus.Healthy] = StatusCodes.Status200OK,
        [HealthStatus.Degraded] = StatusCodes.Status200OK,
        [HealthStatus.Unhealthy] = StatusCodes.Status503ServiceUnavailable
    }
}).WithTags("System");

// ---- Hangfire dashboard (Development only — see filter) ----
app.MapHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new HangfireDashboardAuthorizationFilter(app.Environment)]
});

// ---- Recurring automation job: evaluate every active tenant's rules every 5 minutes ----
try
{
    app.Services.GetRequiredService<IRecurringJobManager>().AddOrUpdate<AutomationJob>(
        "automation-engine",
        job => job.RunForAllTenantsAsync(CancellationToken.None),
        "*/5 * * * *",
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Automation recurring job not scheduled (Hangfire storage not reachable yet).");
}

// ---- Seed roles (idempotent) ----
await using (var scope = app.Services.CreateAsyncScope())
{
    try
    {
        await DbSeeder.SeedRolesAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Role seeding skipped (database not reachable yet).");
    }
}

// ---- Seed demo data (opt-in via DemoData:Enabled; idempotent) ----
if (app.Configuration.GetValue<bool>("DemoData:Enabled"))
{
    await using var scope = app.Services.CreateAsyncScope();
    try
    {
        await DemoDataSeeder.SeedAsync(scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Demo data seeding skipped.");
    }
}

app.Run();

public partial class Program; // exposed for integration tests
