using ImdbWatchlists.Models;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WhatToWatch.Controllers;
using WhatToWatch.DTOs;
using WhatToWatch.Services;

namespace WhatToWatch.Tests.Controllers;

public sealed class SpookieNightControllerTests
{
    private readonly Mock<ISpookieNightService> _service = new();

    private SpookieNightController CreateSut() => new(_service.Object);

    [Fact]
    public async Task GetTicketsAsync_ReturnsUnlockedTicketsInOrder()
    {
        _service
            .Setup(s => s.GetUnlockedTicketsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new SpookieTicket("1", false, false, CreateMovie("tt1", "First")),
                new SpookieTicket("2", false, true, CreateMovie("tt2", "Second")),
            ]);

        var result = await CreateSut().GetTicketsAsync(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsAssignableFrom<IReadOnlyCollection<SpookieTicketDto>>(ok.Value);
        Assert.Collection(
            dtos,
            first =>
            {
                Assert.Equal("1", first.Key);
                Assert.False(first.IsCurrent);
                Assert.Equal("tt1", first.Movie.Id);
            },
            second =>
            {
                Assert.Equal("2", second.Key);
                Assert.True(second.IsCurrent);
                Assert.Equal("Second", second.Movie.Title);
            });
    }

    [Fact]
    public async Task GetTicketsAsync_ReturnsEmptyList_WhenNothingIsUnlocked()
    {
        _service
            .Setup(s => s.GetUnlockedTicketsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await CreateSut().GetTicketsAsync(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Empty(Assert.IsAssignableFrom<IReadOnlyCollection<SpookieTicketDto>>(ok.Value));
    }

    [Fact]
    public async Task GetMovieAsync_ReturnsMovie_WhenTicketIsUnlocked()
    {
        _service
            .Setup(s => s.GetTicketAsync("bonus", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SpookieTicket(
                "bonus",
                true,
                true,
                CreateMovie("tt5", "Bonus"),
                "https://watch.example/bonus"));

        var result = await CreateSut().GetMovieAsync("bonus", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<SpookieMovieDto>(ok.Value);
        Assert.Equal("tt5", dto.Movie.Id);
        Assert.Equal("https://watch.example/bonus", dto.Link);
    }

    [Theory]
    [InlineData("3")]
    [InlineData("unknown")]
    public async Task GetMovieAsync_ReturnsNotFound_WhenTicketIsLockedOrUnknown(string key)
    {
        _service
            .Setup(s => s.GetTicketAsync(key, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SpookieTicket?)null);

        var result = await CreateSut().GetMovieAsync(key, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetMovieAsync_ReturnsNotFound_WithoutCallingService_WhenKeyIsBlank(string key)
    {
        var result = await CreateSut().GetMovieAsync(key, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        _service.Verify(
            s => s.GetTicketAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static Movie CreateMovie(string id, string title) =>
        new()
        {
            Id = id,
            Title = title,
            Year = 2000,
            Genres = ["Horror"],
        };
}
