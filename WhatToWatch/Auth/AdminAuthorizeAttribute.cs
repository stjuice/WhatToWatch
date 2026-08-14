using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using WhatToWatch.Options;

namespace WhatToWatch.Auth;

/// <summary>
/// Requires the <c>X-Admin-Key</c> header (or Bearer token) to match
/// <see cref="WhatToWatchOptions.AdminApiKey"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AdminAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public const string HeaderName = "X-Admin-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var options = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<WhatToWatchOptions>>()
            .Value;

        if (string.IsNullOrWhiteSpace(options.AdminApiKey))
        {
            context.Result = new ObjectResult(new { error = "Admin API key is not configured." })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable,
            };
            return;
        }

        if (!TryReadPresentedKey(context.HttpContext.Request, out var presented)
            || !string.Equals(presented, options.AdminApiKey, StringComparison.Ordinal))
        {
            context.Result = new UnauthorizedObjectResult(new { error = "Invalid or missing admin key." });
        }
    }

    private static bool TryReadPresentedKey(HttpRequest request, out string key)
    {
        if (request.Headers.TryGetValue(HeaderName, out var headerValues))
        {
            var header = headerValues.ToString();
            if (!string.IsNullOrWhiteSpace(header))
            {
                key = header.Trim();
                return true;
            }
        }

        var authorization = request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            key = authorization["Bearer ".Length..].Trim();
            return key.Length > 0;
        }

        key = string.Empty;
        return false;
    }
}
