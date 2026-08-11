namespace ImdbWatchlists.Options;

public class ImdbWatchlistsOptions
{
    public const string SectionName = "ImdbWatchlists";

    /// <summary>SQLite connection string used by the watchlist repository.</summary>
    public string ConnectionString { get; set; } = "Data Source=whattowatch.db";
}
