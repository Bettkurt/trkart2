using Hangfire.Dashboard;

namespace TRKart.API.Filters;

/// <summary>
/// Simple authorization filter that allows all requests to the Hangfire dashboard
/// WARNING: Only use this in development environments
/// </summary>
public class DashboardNoAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // Allow everyone to access the dashboard
        return true;
    }
}
