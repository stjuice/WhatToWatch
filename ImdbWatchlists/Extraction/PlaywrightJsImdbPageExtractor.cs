using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Playwright;

namespace ImdbWatchlists.Extraction;

public sealed class PlaywrightJsImdbPageExtractor : IImdbPageExtractor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    };

    private static readonly Lazy<string> ExtractScript = new(LoadExtractScript);

    public async Task<ExtractedWatchlistPage> ExtractCurrentPageAsync(
        IPage page,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);

        var json = await page.EvaluateAsync<string?>(ExtractScript.Value)
            .WaitAsync(cancellationToken);

        return ParseExtractedJson(json, page.Url ?? string.Empty);
    }

    public static ExtractedWatchlistPage ParseExtractedJson(string? json, string pageUrl)
    {
        if (string.IsNullOrWhiteSpace(json) || string.Equals(json, "null", StringComparison.Ordinal))
        {
            throw new ImdbWatchlistException(
                $"Could not find embedded list data in the page for '{pageUrl}'. " +
                "IMDb markup may have changed, or the response was a bot challenge.");
        }

        ExtractedWatchlistPage? page;
        try
        {
            page = JsonSerializer.Deserialize<ExtractedWatchlistPage>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new ImdbWatchlistException(
                $"Could not parse embedded list data for '{pageUrl}'.", exception);
        }

        if (page is null || page.Movies.Count == 0)
        {
            throw new ImdbWatchlistException(
                $"Could not find embedded list data in the page for '{pageUrl}'. " +
                "IMDb markup may have changed, or the response was a bot challenge.");
        }

        return page;
    }

    private static string LoadExtractScript()
    {
        var assembly = typeof(PlaywrightJsImdbPageExtractor).Assembly;
        const string resourceName = "ImdbWatchlists.Scripts.extractWatchlist.js";

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"Missing embedded IMDb extractor script '{resourceName}'.");

        using var reader = new StreamReader(stream);
        var script = reader.ReadToEnd().Trim();

        if (string.IsNullOrWhiteSpace(script))
        {
            throw new InvalidOperationException(
                $"Embedded IMDb extractor script '{resourceName}' is empty.");
        }

        return script;
    }
}
