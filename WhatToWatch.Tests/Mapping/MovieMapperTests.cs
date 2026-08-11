using WhatToWatch.Mapping;
using WhatToWatch.Models;

namespace WhatToWatch.Tests.Mapping;

public class MovieMapperTests
{
    [Fact]
    public void ToApp_Movie_MapsImdbMovieToAppMovie()
    {
        var imdb = new ImdbWatchlists.Models.Movie
        {
            Id = "tt1",
            Title = "Test",
            Year = 2020,
            PosterUrl = "https://example.com/p.jpg",
            Rating = 8.1,
            Genres = ["Drama"],
        };

        var app = MovieMapper.ToApp(imdb);

        Assert.Equal(imdb.Id, app.Id);
        Assert.Equal(imdb.Title, app.Title);
        Assert.Equal(imdb.Year, app.Year);
        Assert.Equal(imdb.PosterUrl, app.PosterUrl);
        Assert.Equal(imdb.Rating, app.Rating);
        Assert.Equal(imdb.Genres, app.Genres);
    }

    [Fact]
    public void ToDto_Movie_MapsAppMovieToDto()
    {
        var movie = new Movie
        {
            Id = "tt1",
            Title = "Test",
            Year = 2020,
            PosterUrl = "https://example.com/p.jpg",
            Rating = 8.1,
            Genres = ["Drama"],
        };

        var dto = MovieMapper.ToDto(movie);

        Assert.Equal(movie.Id, dto.Id);
        Assert.Equal(movie.Title, dto.Title);
        Assert.Equal(movie.Year, dto.Year);
        Assert.Equal(movie.PosterUrl, dto.PosterUrl);
        Assert.Equal(movie.Rating, dto.Rating);
        Assert.Equal(movie.Genres, dto.Genres);
    }

    [Fact]
    public void ToFilter_MapsRequestFieldsToDomainFilter()
    {
        var request = new DTOs.MovieFilterRequest
        {
            WatchlistId = "ls1",
            Query = "matrix",
            YearFrom = 1990,
            YearTo = 2000,
            MinRating = 7,
            Genres = ["Action"],
        };

        var filter = MovieMapper.ToFilter(request);

        Assert.Equal(request.WatchlistId, filter.WatchlistId);
        Assert.Equal(request.Query, filter.Query);
        Assert.Equal(request.YearFrom, filter.YearFrom);
        Assert.Equal(request.YearTo, filter.YearTo);
        Assert.Equal(request.MinRating, filter.MinRating);
        Assert.Equal(request.Genres, filter.Genres);
    }

    [Fact]
    public void ToFilter_HandlesNullOptionalFields_WithoutThrowing()
    {
        var filter = MovieMapper.ToFilter(new DTOs.MovieFilterRequest
        {
            WatchlistId = "ls1",
        });

        Assert.Equal("ls1", filter.WatchlistId);
        Assert.Null(filter.Query);
        Assert.Empty(filter.Genres);
    }
}
