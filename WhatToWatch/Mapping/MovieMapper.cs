using ImdbWatchlists.Models;
using WhatToWatch.DTOs;
using WhatToWatch.Models;

namespace WhatToWatch.Mapping;

public static class MovieMapper
{
    public static MovieDto ToDto(Movie movie) => new()
    {
        Id = movie.Id,
        Title = movie.Title,
        Year = movie.Year,
        PosterUrl = movie.PosterUrl,
        Rating = movie.Rating,
        Plot = movie.Plot,
        RuntimeMinutes = movie.RuntimeMinutes,
        Director = movie.Director,
        Genres = movie.Genres,
    };

    public static WatchlistDto ToDto(Watchlist watchlist) => new()
    {
        Id = watchlist.Id,
        Name = watchlist.Name,
        Url = watchlist.Url,
        LastRefreshedAt = watchlist.LastRefreshedAt,
        Movies = [.. watchlist.Movies.Select(ToDto)],
    };

    public static MovieFilter ToFilter(MovieFilterRequest request) => new()
    {
        WatchlistId = request.WatchlistId,
        Query = request.Query,
        YearFrom = request.YearFrom,
        YearTo = request.YearTo,
        MinRating = request.MinRating,
        Genres = request.Genres ?? [],
    };
}
