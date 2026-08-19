using ImdbWatchlists.Models;
using MovieModel = WhatToWatch.Models.Movie;
using WatchlistModel = WhatToWatch.Models.Watchlist;

namespace WhatToWatch.Media;

public static class MediaCategoryRules
{
    public static bool IsMovie(MediaCategory category) =>
        category == MediaCategory.Movie;

    public static bool IsMovie(MovieModel movie) =>
        IsMovie(movie.MediaCategory);

    public static WatchlistModel WithMoviesOnly(WatchlistModel watchlist) =>
        watchlist with
        {
            Movies = [.. watchlist.Movies.Where(IsMovie)],
        };

    public static IReadOnlyCollection<MovieModel> MoviesOnly(
        IEnumerable<MovieModel> movies) =>
        [.. movies.Where(IsMovie)];
}
