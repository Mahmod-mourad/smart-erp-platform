using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Infrastructure.Auth;
using NexaFlow.Infrastructure.Chatbot;
using NexaFlow.Infrastructure.Identity;
using NexaFlow.Infrastructure.Integrations;
using NexaFlow.Infrastructure.Jobs;
using NexaFlow.Infrastructure.Persistence;
using NexaFlow.Infrastructure.Services;
using NexaFlow.Infrastructure.Services.Automation;
using NexaFlow.Infrastructure.Persistence.Interceptors;

namespace NexaFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<AuditInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
            options.UseSqlServer(config.GetConnectionString("Default"))
                   .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = config.GetConnectionString("Redis") ?? "localhost:6379";
            options.InstanceName = "NexaFlow_";
        });
        services.AddScoped<ICacheService, RedisCacheService>();

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = false;
                options.User.RequireUniqueEmail = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.Configure<JwtSettings>(config.GetSection(JwtSettings.SectionName));

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITenantService, TenantService>();
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IAccountingService, AccountingService>();
        services.AddScoped<IUserPreferencesService, UserPreferencesService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<ITenantArchiveService, TenantArchiveService>();
        services.AddScoped<IInvitationService, InvitationService>();
        services.AddMemoryCache(); // Required by PredictionService

        // Services
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ILeadService, LeadService>();
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<ILeaveService, LeaveService>();
        services.AddScoped<IPayrollService, PayrollService>();
        services.AddScoped<IInventoryService, InventoryService>();
        
        services.AddScoped<IStorageService, BlobStorageService>();
        services.AddScoped<IReportGenerator, ReportGenerator>();

        services.AddScoped<IWebhookDispatcher, WebhookDispatcher>();
        services.AddHttpClient<BackgroundJobs.WebhookJob>();

        // ---- ML.NET predictions (Sprint 7) ----
        services.AddScoped<IPredictionService, ML.PredictionService>();

        // ---- Integrations (Sprint 6): per-tenant credentials encrypted at rest ----
        services.AddScoped<IntegrationCrypto>();
        services.AddScoped<IIntegrationConfigProvider, IntegrationConfigProvider>();
        services.AddScoped<IGoogleSheetsSender, GoogleSheetsSender>();
        services.AddScoped<IIntegrationService, IntegrationService>();
        services.AddScoped<IEmailSender, GmailEmailSender>();
        services.AddHttpClient<IWhatsAppSender, WhatsAppSender>();
        services.AddHttpClient<ISlackSender, SlackWebhookSender>();

        // ---- AI chatbot (Sprint 6): grounded in tenant data, served by local Ollama ----
        services.AddScoped<ChatContextBuilder>();
        services.AddHttpClient<IChatbotService, OllamaChatbotService>(client =>
        {
            client.BaseAddress = new Uri(config["OllamaSettings:BaseUrl"] ?? "http://localhost:11434");
            client.Timeout = TimeSpan.FromSeconds(60);
        });

        // ---- Automation engine (Sprint 5) ----
        services.AddScoped<ITriggerHandler, StockLowTriggerHandler>();
        services.AddScoped<ITriggerHandler, EmployeeAbsentTriggerHandler>();
        services.AddScoped<ITriggerHandler, LeaveRequestPendingTriggerHandler>();
        services.AddScoped<ITriggerHandler, ScheduledDailyTriggerHandler>();
        services.AddScoped<ITriggerHandler, ScheduledWeeklyTriggerHandler>();

        services.AddScoped<IActionHandler, SendEmailActionHandler>();
        services.AddScoped<IActionHandler, SendWhatsAppActionHandler>();
        services.AddScoped<IActionHandler, SendSlackActionHandler>();
        services.AddScoped<IActionHandler, CreateActivityActionHandler>();
        services.AddScoped<IActionHandler, AppendSheetActionHandler>();

        services.AddScoped<IAutomationService, AutomationService>();
        services.AddScoped<AutomationJob>();

        return services;
    }
}
