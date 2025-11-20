using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexaFlow.Application.Common.Interfaces;
using NexaFlow.Core.Constants;
using NexaFlow.Infrastructure.Auth;
using NexaFlow.Infrastructure.Identity;
using NexaFlow.Infrastructure.Persistence;
using NexaFlow.Infrastructure.Services;

namespace NexaFlow.Tests;

/// <summary>Mutable tenant context so tests can switch the "current tenant".</summary>
public sealed class TestTenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }
    public void SetTenant(Guid tenantId) => TenantId = tenantId;
    public void Clear() => TenantId = null;
}

/// <summary>Stubbable current-user for service tests.</summary>
public sealed class TestCurrentUser : ICurrentUser
{
    public Guid? UserId { get; set; }
    public Guid? TenantId { get; set; }
    public string? Email { get; set; }
    public bool IsAuthenticated => UserId is not null;
    public bool IsInRole(string role) => true;
}

/// <summary>Builds a DI container backed by an isolated in-memory database for each test.</summary>
public sealed class TestHarness
{
    public ServiceProvider Provider { get; }
    public TestTenantContext TenantContext { get; } = new();
    public TestCurrentUser CurrentUser { get; } = new();

    public TestHarness()
    {
        var services = new ServiceCollection();
        var dbName = Guid.NewGuid().ToString();

        services.AddLogging();
        services.AddSingleton<ITenantContext>(TenantContext);
        services.AddSingleton<ICurrentUser>(CurrentUser);
        services.AddDbContext<AppDbContext>(o => o.UseInMemoryDatabase(dbName));

        services.AddIdentityCore<ApplicationUser>(o =>
            {
                o.Password.RequiredLength = 8;
                o.User.RequireUniqueEmail = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddAutoMapper(_ => { }, typeof(NexaFlow.Application.DependencyInjection).Assembly);

        services.AddSingleton(Options.Create(new JwtSettings
        {
            SecretKey = "test-secret-key-that-is-at-least-32-bytes-long-1234567890",
            AccessTokenMinutes = 60,
            RefreshTokenDays = 7
        }));

        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITenantService, TenantService>();

        Provider = services.BuildServiceProvider();
    }

    public T Get<T>() where T : notnull => Provider.GetRequiredService<T>();

    /// <summary>Seeds the canonical roles (Identity requires roles to exist before assignment).</summary>
    public async Task SeedRolesAsync()
    {
        var roleManager = Get<RoleManager<ApplicationRole>>();
        foreach (var role in AppRoles.All)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new ApplicationRole(role));
    }
}
