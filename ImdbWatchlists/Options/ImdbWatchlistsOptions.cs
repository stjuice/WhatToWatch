namespace ImdbWatchlists.Options;

public class ImdbWatchlistsOptions
{
    public const string SectionName = "ImdbWatchlists";

    public string CacheDirectory { get; set; } =
        Path.Combine(AppContext.BaseDirectory, "imdb-cache");

    public string BrowserProfileDirectory { get; set; } =
        Path.Combine(Path.GetTempPath(), "whattowatch-imdb-profile");

    public string StorageStatePath { get; set; } = "imdb-session.json";

    public bool BrowserHeadless { get; set; }

    public bool BlockNonEssentialResources { get; set; } = true;

    public int ManualChallengeTimeoutSeconds { get; set; } = 180;
}
