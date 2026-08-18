namespace ImdbWatchlists.Models;

public static class ImdbTitleType
{
    public static MediaCategory FromId(string? titleTypeId)
    {
        if (string.IsNullOrWhiteSpace(titleTypeId))
            return MediaCategory.Unknown;

        return titleTypeId.Trim().ToLowerInvariant() switch
        {
            "movie" or "tvmovie" or "short" or "video" => MediaCategory.Movie,
            "tvseries" or "tvminiseries" or "tvepisode" or "tvshort" or "tvspecial"
                => MediaCategory.TvShow,
            _ => MediaCategory.Unknown,
        };
    }
}
