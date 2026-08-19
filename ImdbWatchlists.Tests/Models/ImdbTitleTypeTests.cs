using ImdbWatchlists.Models;

namespace ImdbWatchlists.Tests.Models;

public class ImdbTitleTypeTests
{
    [Theory]
    [InlineData(null, MediaCategory.Unknown)]
    [InlineData("", MediaCategory.Unknown)]
    [InlineData("  ", MediaCategory.Unknown)]
    [InlineData("movie", MediaCategory.Movie)]
    [InlineData("TVMovie", MediaCategory.Movie)]
    [InlineData("short", MediaCategory.Movie)]
    [InlineData("video", MediaCategory.Movie)]
    [InlineData("tvSeries", MediaCategory.TvShow)]
    [InlineData("tvMiniSeries", MediaCategory.TvShow)]
    [InlineData("tvEpisode", MediaCategory.TvShow)]
    [InlineData("tvShort", MediaCategory.TvShow)]
    [InlineData("tvSpecial", MediaCategory.TvShow)]
    [InlineData("podcastSeries", MediaCategory.Unknown)]
    public void FromId_MapsKnownAndUnknownTitleTypes(
        string? titleTypeId,
        MediaCategory expected)
    {
        Assert.Equal(expected, ImdbTitleType.FromId(titleTypeId));
    }
}
