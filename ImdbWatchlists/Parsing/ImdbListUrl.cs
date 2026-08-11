using System.Text.RegularExpressions;

namespace ImdbWatchlists.Parsing;

public static partial class ImdbListUrl
{
    [GeneratedRegex(@"^/list/(?<id>ls\d+)/?$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ListIdRegex();

    [GeneratedRegex(
        @"^/user/(?<id>(?:ur\d+|p\.[a-z0-9]+))/watchlist/?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UserWatchlistRegex();

    public static string ExtractId(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new ImdbWatchlistException("Watchlist URL must not be empty.");
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !IsImdbHost(uri.Host))
        {
            throw new ImdbWatchlistException(
                $"'{url}' is not a valid HTTPS IMDb URL.");
        }

        var listMatch = ListIdRegex().Match(uri.AbsolutePath);
        if (listMatch.Success)
        {
            return listMatch.Groups["id"].Value;
        }

        var userMatch = UserWatchlistRegex().Match(uri.AbsolutePath);
        if (userMatch.Success)
        {
            return userMatch.Groups["id"].Value;
        }

        throw new ImdbWatchlistException(
            $"'{url}' is not a recognised IMDb list URL.");
    }

    public static string WithPage(string url, int page)
    {
        if (page <= 1)
        {
            return url;
        }

        var uri = new Uri(url, UriKind.Absolute);
        var query = uri.Query.TrimStart('?');
        var parts = query
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(part => !part.StartsWith("page=", StringComparison.OrdinalIgnoreCase))
            .Append($"page={page}");

        return new UriBuilder(uri) { Query = string.Join('&', parts) }.Uri.ToString();
    }

    private static bool IsImdbHost(string host) =>
        host.Equals("imdb.com", StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith(".imdb.com", StringComparison.OrdinalIgnoreCase);
}
