namespace ImdbWatchlists.Options;

public class ImdbWatchlistsOptions
{
    public const string SectionName = "ImdbWatchlists";

    public string CacheDirectory { get; set; } =
        Path.Combine(AppContext.BaseDirectory, "imdb-cache");

    public string BrowserProfileDirectory { get; set; } =
        Path.Combine(Path.GetTempPath(), "whattowatch-imdb-profile");

    public bool BrowserHeadless { get; set; }
}
