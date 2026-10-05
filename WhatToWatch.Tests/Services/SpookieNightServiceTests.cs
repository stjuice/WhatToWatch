using ImdbWatchlists.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WhatToWatch.Options;
using WhatToWatch.Services;

namespace WhatToWatch.Tests.Services;

public sealed class SpookieNightServiceTests
{
    private readonly Mock<IMovieService> _movies = new();

    public SpookieNightServiceTests()
    {
        _movies
            .Setup(service => service.GetMovieAsync("ls1", "tt1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Movie
            {
                Id = "tt1",
                Title = "One",
                Genres = ["Horror"],
            });
    }

    [Fact]
    public async Task GetTicketAsync_ReturnsTrimmedWatchLinkFromOptions()
    {
        var ticket = await CreateSut(" https://watch.example/one ").GetTicketAsync("1");

        Assert.NotNull(ticket);
        Assert.Equal("https://watch.example/one", ticket.Link);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    public async Task GetTicketAsync_OmitsWatchLink_WhenItIsMissingOrNotHttp(string? link)
    {
        var ticket = await CreateSut(link).GetTicketAsync("1");

        Assert.NotNull(ticket);
        Assert.Null(ticket.Link);
    }

    private SpookieNightService CreateSut(string? link) =>
        new(
            _movies.Object,
            TimeProvider.System,
            Microsoft.Extensions.Options.Options.Create(new SpookieNightOptions
            {
                TimeZone = "UTC",
                WatchlistId = "ls1",
                Tickets =
                [
                    new SpookieTicketOptions
                    {
                        Key = "1",
                        MovieId = "tt1",
                        UnlockDate = new DateOnly(2020, 1, 1),
                        Link = link ?? string.Empty,
                    },
                ],
            }),
            NullLogger<SpookieNightService>.Instance);
}
