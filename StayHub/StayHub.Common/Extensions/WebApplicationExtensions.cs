using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

namespace StayHub.Common.Extensions;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Behind Docker/Caddy the app listens on HTTP only; TLS terminates at the reverse proxy.
    /// </summary>
    public static WebApplication UseStayHubHttpScheme(this WebApplication app)
    {
        if (!app.Environment.IsEnvironment("Docker"))
        {
            app.UseHttpsRedirection();
        }

        return app;
    }
}
