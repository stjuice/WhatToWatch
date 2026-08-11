using Microsoft.AspNetCore.Mvc;
using Moq;
using WhatToWatch.Controllers;
using WhatToWatch.DTOs;
using WhatToWatch.Models;
using WhatToWatch.Services;

namespace WhatToWatch.Tests.Controllers;

public class MoviesControllerTests
{
    private const string WatchlistId = "ls1";

    private readonly Mock<IMovieService> _service = new();

    private MoviesController CreateSut() => new(_service.Object);

    [Fact]
    public async Task GetMoviesAsync_ReturnsOkWithMovies_WhenFilterMatchesResults()
    {
        _service
            .Setup(s => s.GetMoviesAsync(
                It.Is<MovieFilter>(f => f.WatchlistId == WatchlistId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([CreateMovie("tt1", "Film")]);

        var result = await CreateSut().GetMoviesAsync(
            new MovieFilterRequest { WatchlistId = WatchlistId },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsAssignableFrom<IReadOnlyCollection<MovieDto>>(ok.Value);
        Assert.Single(dtos);
        Assert.Equal("tt1", dtos.First().Id);
    }

    [Fact]
    public async Task GetMoviesAsync_ReturnsNotFound_WhenWatchlistMissing()
    {
        _service
            .Setup(s => s.GetMoviesAsync(
                It.IsAny<MovieFilter>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Movie>?)null);

        var result = await CreateSut().GetMoviesAsync(
            new MovieFilterRequest { WatchlistId = WatchlistId },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetRandomMovieAsync_ReturnsOkWithMovie_WhenMatchesExist()
    {
        _service
            .Setup(s => s.GetRandomMovieAsync(
                It.Is<MovieFilter>(f => f.WatchlistId == WatchlistId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMovie("tt1", "Film"));

        var result = await CreateSut().GetRandomMovieAsync(
            new MovieFilterRequest { WatchlistId = WatchlistId },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<MovieDto>(ok.Value);
        Assert.Equal("tt1", dto.Id);
    }

    [Fact]
    public async Task GetRandomMovieAsync_ReturnsNotFound_WhenNoMoviesMatchFilter()
    {
        _service
            .Setup(s => s.GetRandomMovieAsync(
                It.IsAny<MovieFilter>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Movie?)null);

        var result = await CreateSut().GetRandomMovieAsync(
            new MovieFilterRequest { WatchlistId = WatchlistId },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task GetMovieAsync_ReturnsOkWithMovie_WhenIdExists()
    {
        _service
            .Setup(s => s.GetMovieAsync(WatchlistId, "tt1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateMovie("tt1", "Film"));

        var result = await CreateSut().GetMovieAsync(
            "tt1",
            WatchlistId,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<MovieDto>(ok.Value);
        Assert.Equal("Film", dto.Title);
    }

    [Fact]
    public async Task GetMovieAsync_ReturnsNotFound_WhenIdDoesNotExist()
    {
        _service
            .Setup(s => s.GetMovieAsync(WatchlistId, "missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Movie?)null);

        var result = await CreateSut().GetMovieAsync(
            "missing",
            WatchlistId,
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    private static Movie CreateMovie(string id, string title) =>
        new()
        {
            Id = id,
            Title = title,
            Year = 2000,
            Genres = ["Drama"],
        };
}
