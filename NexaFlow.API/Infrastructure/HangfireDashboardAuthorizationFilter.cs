using Hangfire.Dashboard;

namespace NexaFlow.API.Infrastructure;

/// <summary>
/// Guards the Hangfire dashboard. The dashboard is a browser UI (no JWT bearer header), so we
/// only expose it in Development. In other environments access is denied — wire it to a reverse
/// proxy / network policy or an admin cookie scheme before enabling.
/// </summary>
public class HangfireDashboardAuthorizationFilter(IWebHostEnvironment env) : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context) => env.IsDevelopment();
}
