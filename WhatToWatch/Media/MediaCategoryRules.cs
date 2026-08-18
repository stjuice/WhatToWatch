using ImdbWatchlists.Models;
using AppMovie = WhatToWatch.Models.Movie;
using AppWatchlist = WhatToWatch.Models.Watchlist;

namespace WhatToWatch.Media;

public static class MediaCategoryRules
{
    public static bool IsMovie(MediaCategory category) =>
        category == MediaCategory.Movie;

    public static bool IsMovie(AppMovie movie) =>
        IsMovie(movie.MediaCategory);

    public static AppWatchlist WithMoviesOnly(AppWatchlist watchlist) =>
        watchlist with
        {
            Movies = [.. watchlist.Movies.Where(IsMovie)],
        };

    public static IReadOnlyCollection<AppMovie> MoviesOnly(
        IEnumerable<AppMovie> movies) =>
        [.. movies.Where(IsMovie)];
}
