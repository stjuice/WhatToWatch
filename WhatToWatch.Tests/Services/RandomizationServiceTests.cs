using ImdbWatchlists.Models;
using WhatToWatch.Models;
using WhatToWatch.Services;

namespace WhatToWatch.Tests.Services;

public class RandomizationServiceTests
{
    private readonly RandomizationService _sut = new(new Random(42));

    private static readonly IReadOnlyList<Movie> Movies =
    [
        new Movie
        {
            Id = "tt1",
            Title = "The Matrix",
            Year = 1999,
            Rating = 8.7,
            Genres = ["Action", "Sci-Fi"],
        },
        new Movie
        {
            Id = "tt2",
            Title = "Inception",
            Year = 2010,
            Rating = 8.8,
            Genres = ["Action", "Sci-Fi", "Thriller"],
        },
        new Movie
        {
            Id = "tt3",
            Title = "Amelie",
            Year = 2001,
            Rating = 8.3,
            Genres = ["Comedy", "Romance"],
        },
        new Movie
        {
            Id = "tt4",
            Title = "Untitled",
            Year = null,
            Rating = null,
            Genres = [],
        },
    ];

    [Fact]
    public void Filter_AppliesQueryYearRatingAndGenres()
    {
        var filter = new MovieFilter
        {
            WatchlistId = "ls1",
            Query = "in",
            YearFrom = 2000,
            YearTo = 2015,
            MinRating = 8.5,
            Genres = ["Sci-Fi"],
        };

        var result = _sut.Filter(Movies, filter);

        Assert.Single(result);
        Assert.Equal("tt2", result[0].Id);
    }

    [Fact]
    public void Filter_ExcludesMoviesMissingYearOrRating_WhenThoseFiltersSet()
    {
        var filter = new MovieFilter
        {
            WatchlistId = "ls1",
            YearFrom = 1990,
            MinRating = 1,
        };

        var result = _sut.Filter(Movies, filter);

        Assert.DoesNotContain(result, movie => movie.Id == "tt4");
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public void PickRandom_ReturnsNull_WhenEmpty()
    {
        Assert.Null(_sut.PickRandom([]));
    }

    [Fact]
    public void PickRandom_ReturnsMovieFromList()
    {
        var picked = _sut.PickRandom(Movies);

        Assert.NotNull(picked);
        Assert.Contains(Movies, movie => movie.Id == picked.Id);
    }

    [Fact]
    public void Shuffle_ReturnsPermutationWithoutChangingInput()
    {
        int[] input = [1, 2, 2, 3, 4, 5];

        var result = _sut.Shuffle(input);

        Assert.Equal(input.Order(), result.Order());
        Assert.Equal([1, 2, 2, 3, 4, 5], input);
    }

    [Fact]
    public void Shuffle_IsDeterministicForSeededRandom()
    {
        var first = new RandomizationService(new Random(42));
        var second = new RandomizationService(new Random(42));
        int[] input = [1, 2, 3, 4, 5, 6, 7, 8];

        Assert.Equal(first.Shuffle(input), second.Shuffle(input));
    }

    [Fact]
    public void Shuffle_DifferentSeedsProduceDifferentOrders()
    {
        var first = new RandomizationService(new Random(1));
        var second = new RandomizationService(new Random(2));
        int[] input = [1, 2, 3, 4, 5, 6, 7, 8];

        Assert.False(first.Shuffle(input).SequenceEqual(second.Shuffle(input)));
    }

    [Fact]
    public void Shuffle_ReturnsEmptyList_WhenInputIsEmpty()
    {
        Assert.Empty(_sut.Shuffle(Array.Empty<int>()));
    }

    [Fact]
    public void Shuffle_ReturnsSingleElement_WhenInputHasOneElement()
    {
        Assert.Equal([42], _sut.Shuffle<int>([42]));
    }
}
