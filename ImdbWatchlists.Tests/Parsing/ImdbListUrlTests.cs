using ImdbWatchlists.Parsing;

namespace ImdbWatchlists.Tests.Parsing;

public class ImdbListUrlTests
{
    [Theory]
    [InlineData("https://www.imdb.com/list/ls055592025/", "ls055592025")]
    [InlineData("https://www.imdb.com/list/ls055592025", "ls055592025")]
    [InlineData("https://m.imdb.com/list/ls123/?sort=alpha", "ls123")]
    public void ExtractId_ReturnsListId_ForCustomLists(string url, string expected)
    {
        Assert.Equal(expected, ImdbListUrl.ExtractId(url));
    }

    [Theory]
    [InlineData(
        "https://www.imdb.com/user/ur12345678/watchlist",
        "ur12345678")]
    [InlineData(
        "https://www.imdb.com/user/p.sx47zylgs4uarc76oeqpyxoheq/watchlist/?ref_=ext_shr_lnk",
        "p.sx47zylgs4uarc76oeqpyxoheq")]
    public void ExtractId_ReturnsUserId_ForWatchlistUrls(string url, string expected)
    {
        Assert.Equal(expected, ImdbListUrl.ExtractId(url));
    }

    [Theory]
    [InlineData("https://www.imdb.com/list/ls055592025/", 1, "https://www.imdb.com/list/ls055592025/")]
    [InlineData("https://www.imdb.com/list/ls055592025/", 2, "https://www.imdb.com/list/ls055592025/?page=2")]
    [InlineData(
        "https://www.imdb.com/user/p.abc/watchlist/?ref_=ext_shr_lnk",
        3,
        "https://www.imdb.com/user/p.abc/watchlist/?ref_=ext_shr_lnk&page=3")]
    [InlineData(
        "https://www.imdb.com/list/ls055592025/?page=2",
        5,
        "https://www.imdb.com/list/ls055592025/?page=5")]
    public void WithPage_AddsOrReplacesPageParameter(string url, int page, string expected)
    {
        Assert.Equal(expected, ImdbListUrl.WithPage(url, page));
    }

    [Theory]
    [InlineData("")]
    [InlineData("https://www.imdb.com/title/tt0133093/")]
    [InlineData("http://www.imdb.com/list/ls055592025/")]
    [InlineData("https://example.com/user/ur12345678/watchlist")]
    public void ExtractId_Throws_ForUnsupportedUrls(string url)
    {
        Assert.Throws<ImdbWatchlistException>(() => ImdbListUrl.ExtractId(url));
    }
}
