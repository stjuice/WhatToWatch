using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WhatToWatch.Auth;

/// <summary>
/// Requires the <c>X-Admin-Key</c> header (or Bearer token) to match the
/// server admin key from <c>ADMIN_API_KEY</c> or <c>WhatToWatch:AdminApiKey</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AdminAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var services = context.HttpContext.RequestServices;
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetService<ILoggerFactory>()?.CreateLogger("AdminAuthorize");
        var path = context.HttpContext.Request.Path;
        var configured = AdminApiKey.Resolve(configuration);

        if (string.IsNullOrWhiteSpace(configured))
        {
            logger?.LogError(
                "Admin write blocked on {Path}: no server admin key. " +
                "Set ADMIN_API_KEY on the Render service and restart. " +
                "The key typed in Studio is only a client header.",
                path);
            context.Result = new ObjectResult(new
            {
                error = "Admin API key is not configured on the server. " +
                    "Set ADMIN_API_KEY in the Render environment and restart the service.",
            })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable,
            };
            return;
        }

        if (!AdminApiKey.TryReadPresentedKey(context.HttpContext.Request, out var presented)
            || !string.Equals(presented, configured, StringComparison.Ordinal))
        {
            logger?.LogWarning(
                "Admin write rejected on {Path}: {Reason}.",
                path,
                presented.Length == 0 ? "no admin key presented" : "admin key mismatch");
            context.Result = new UnauthorizedObjectResult(new { error = "Invalid or missing admin key." });
        }
    }
}
