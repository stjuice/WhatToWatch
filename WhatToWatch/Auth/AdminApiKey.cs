using WhatToWatch.Options;

namespace WhatToWatch.Auth;

public static class AdminApiKey
{
    public const string HeaderName = "X-Admin-Key";
    public const string EnvironmentVariable = "ADMIN_API_KEY";

    public static string? Resolve(IConfiguration configuration)
    {
        foreach (var candidate in ReadCandidates(configuration))
        {
            if (!string.IsNullOrWhiteSpace(candidate))
                return candidate.Trim();
        }

        return null;
    }

    public static bool TryReadPresentedKey(HttpRequest request, out string key)
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

    private static IEnumerable<string?> ReadCandidates(IConfiguration configuration)
    {
        yield return configuration[EnvironmentVariable];
        yield return Environment.GetEnvironmentVariable(EnvironmentVariable);
        yield return configuration[$"{WhatToWatchOptions.SectionName}:AdminApiKey"];
        yield return Environment.GetEnvironmentVariable("WhatToWatch__AdminApiKey");
    }
}
