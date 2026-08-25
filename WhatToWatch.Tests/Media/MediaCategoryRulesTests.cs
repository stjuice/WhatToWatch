using ImdbWatchlists.Models;
using WhatToWatch.Media;
using MovieModel = WhatToWatch.Models.Movie;

namespace WhatToWatch.Tests.Media;

public class MediaCategoryRulesTests
{
    [Theory]
    [InlineData(MediaCategory.Movie, true)]
    [InlineData(MediaCategory.Unknown, true)]
    [InlineData(MediaCategory.TvShow, false)]
    public void IsMovie_IncludesLegacyUnknownAndExcludesTv(MediaCategory category, bool expected)
    {
        Assert.Equal(expected, MediaCategoryRules.IsMovie(category));
        Assert.Equal(
            expected,
            MediaCategoryRules.IsMovie(new MovieModel
            {
                Id = "tt1",
                Title = "Title",
                MediaCategory = category,
            }));
    }

    [Fact]
    public void WithMoviesOnly_KeepsUnknownLegacyRows()
    {
        var filtered = MediaCategoryRules.WithMoviesOnly(new WhatToWatch.Models.Watchlist
        {
            Id = "ls1",
            Name = "Mixed",
            Movies =
            [
                new MovieModel { Id = "tt1", Title = "Film", MediaCategory = MediaCategory.Movie },
                new MovieModel { Id = "tt2", Title = "Show", MediaCategory = MediaCategory.TvShow },
                new MovieModel { Id = "tt3", Title = "Legacy", MediaCategory = MediaCategory.Unknown },
            ],
        });

        Assert.Equal(["tt1", "tt3"], filtered.Movies.Select(movie => movie.Id));
    }
}
