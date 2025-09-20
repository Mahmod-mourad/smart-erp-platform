using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NexaFlow.Application.Common.Interfaces;

namespace NexaFlow.Infrastructure.Persistence;

/// <summary>
/// Lets the EF Core CLI build the context at design time (for migrations) — the runtime
/// normally injects <see cref="ITenantContext"/>, which design-time tooling can't provide.
/// Uses the same local-dev connection string the API targets (overridable via the
/// ConnectionStrings__Default environment variable) so migrations hit the same database.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    private const string DefaultConnectionString =
        "Server=localhost,1433;Database=NexaFlow;User Id=sa;Password=Your_strong_Passw0rd;TrustServerCertificate=True;Encrypt=False";

    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__Default") ?? DefaultConnectionString;

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AppDbContext(options, new NullTenantContext());
    }

    private sealed class NullTenantContext : ITenantContext
    {
        public Guid? TenantId => null;
        public void SetTenant(Guid tenantId) { }
    }
}
