using ImdbWatchlists;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WhatToWatch.Controllers;
using WhatToWatch.DTOs;
using WhatToWatch.Models;
using WhatToWatch.Services;

namespace WhatToWatch.Tests.Controllers;

public class WatchlistsControllerTests
{
    private readonly Mock<IWatchlistService> _service = new();

    private WatchlistsController CreateSut() => new(_service.Object);

    [Fact]
    public async Task ImportWatchlistAsync_ReturnsOkWithWatchlist()
    {
        const string url = "https://www.imdb.com/list/ls1/";
        _service
            .Setup(s => s.ImportAsync(url, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWatchlist("ls1"));

        var result = await CreateSut().ImportWatchlistAsync(
            new CreateWatchlistRequest { Url = url },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<WatchlistDto>(ok.Value);
        Assert.Equal("ls1", dto.Id);
        Assert.Equal("Favourites", dto.Name);
    }

    [Fact]
    public async Task ImportWatchlistAsync_ReturnsBadRequest_WhenUrlInvalid()
    {
        _service
            .Setup(s => s.ImportAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ImdbWatchlistException("'bad' is not a valid HTTPS IMDb URL."));

        var result = await CreateSut().ImportWatchlistAsync(
            new CreateWatchlistRequest { Url = "bad" },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetWatchlistsAsync_ReturnsOkWithAllWatchlists()
    {
        _service
            .Setup(s => s.GetWatchlistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                CreateWatchlist("ls1"),
                CreateWatchlist("ls2"),
            ]);

        var result = await CreateSut().GetWatchlistsAsync(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsAssignableFrom<IReadOnlyCollection<WatchlistDto>>(ok.Value);
        Assert.Equal(2, dtos.Count);
    }

    [Fact]
    public async Task GetWatchlistAsync_ReturnsOkWithWatchlist_WhenIdExists()
    {
        _service
            .Setup(s => s.GetWatchlistAsync("ls1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWatchlist("ls1"));

        var result = await CreateSut().GetWatchlistAsync("ls1", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<WatchlistDto>(ok.Value);
        Assert.Equal("ls1", dto.Id);
    }

    [Fact]
    public async Task GetWatchlistAsync_ReturnsNotFound_WhenIdDoesNotExist()
    {
        _service
            .Setup(s => s.GetWatchlistAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Watchlist?)null);

        var result = await CreateSut().GetWatchlistAsync("missing", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task RefreshWatchlistAsync_ReturnsOk_WhenRefreshSucceeds()
    {
        _service
            .Setup(s => s.RefreshWatchlistAsync("ls1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateWatchlist("ls1"));

        var result = await CreateSut().RefreshWatchlistAsync("ls1", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<WatchlistDto>(ok.Value);
        Assert.Equal("ls1", dto.Id);
    }

    [Fact]
    public async Task RefreshWatchlistAsync_ReturnsNotFound_WhenIdDoesNotExist()
    {
        _service
            .Setup(s => s.RefreshWatchlistAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Watchlist?)null);

        var result = await CreateSut().RefreshWatchlistAsync("missing", CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    private static Watchlist CreateWatchlist(string id) =>
        new()
        {
            Id = id,
            Name = "Favourites",
            Url = $"https://www.imdb.com/list/{id}/",
            LastRefreshedAt = DateTimeOffset.UtcNow,
            Movies =
            [
                new Movie
                {
                    Id = "tt1",
                    Title = "Film",
                    Year = 2000,
                    Genres = ["Drama"],
                },
            ],
        };
}
