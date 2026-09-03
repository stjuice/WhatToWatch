using ImdbWatchlists.Models;

namespace WhatToWatch.Media;

public static class MediaCategoryRules
{
    public static bool IsMovie(MediaCategory category) =>
        category is MediaCategory.Movie or MediaCategory.Unknown;

    public static bool IsMovie(Movie movie) =>
        IsMovie(movie.MediaCategory);

    public static Watchlist WithMoviesOnly(Watchlist watchlist) =>
        watchlist with
        {
            Movies = [.. watchlist.Movies.Where(IsMovie)],
        };

    public static IReadOnlyCollection<Movie> MoviesOnly(
        IEnumerable<Movie> movies) =>
        [.. movies.Where(IsMovie)];
}
