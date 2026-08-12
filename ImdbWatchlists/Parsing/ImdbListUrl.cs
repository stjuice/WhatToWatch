using System.Text.RegularExpressions;

namespace ImdbWatchlists.Parsing;

public static partial class ImdbListUrl
{
    private const string BaseUrl = "https://www.imdb.com";

    [GeneratedRegex(
        @"^/list/(?<id>ls\d+)(?:/.*)?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ListIdRegex();

    [GeneratedRegex(
        @"^/user/(?<id>(?:ur\d+|p\.[a-z0-9]+))/watchlist(?:/.*)?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex UserWatchlistRegex();

    [GeneratedRegex(
        @"^/chart/(?<slug>[a-z0-9][a-z0-9-]*)(?:/.*)?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ChartRegex();

    public static string ExtractId(string url) => Parse(url).Id;

    public static string Normalize(string url) => Parse(url).CanonicalUrl;

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

    private static (string Id, string CanonicalUrl) Parse(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ImdbWatchlistException("Watchlist URL must not be empty.");

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !IsImdbHost(uri.Host))
            throw new ImdbWatchlistException(
                $"'{url}' is not a valid HTTPS IMDb URL.");

        var listMatch = ListIdRegex().Match(uri.AbsolutePath);
        if (listMatch.Success)
        {
            var id = listMatch.Groups["id"].Value;
            return (id, $"{BaseUrl}/list/{id}/");
        }

        var userMatch = UserWatchlistRegex().Match(uri.AbsolutePath);
        if (userMatch.Success)
        {
            var id = userMatch.Groups["id"].Value;
            return (id, $"{BaseUrl}/user/{id}/watchlist/");
        }

        var chartMatch = ChartRegex().Match(uri.AbsolutePath);
        if (chartMatch.Success)
        {
            var slug = chartMatch.Groups["slug"].Value.ToLowerInvariant();
            return ($"chart-{slug}", $"{BaseUrl}/chart/{slug}/");
        }

        throw new ImdbWatchlistException(
            $"'{url}' is not a recognised IMDb list URL. Use a list page such as " +
            $"{BaseUrl}/list/ls123456789/, a public watchlist such as " +
            $"{BaseUrl}/user/ur12345678/watchlist/, or a chart such as " +
            $"{BaseUrl}/chart/moviemeter/.");
    }

    private static bool IsImdbHost(string host) =>
        host.Equals("imdb.com", StringComparison.OrdinalIgnoreCase) ||
        host.EndsWith(".imdb.com", StringComparison.OrdinalIgnoreCase);
}
