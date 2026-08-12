using ImdbWatchlists.Parsing;

namespace ImdbWatchlists.Tests.Parsing;

public class ImdbListUrlTests
{
    [Theory]
    [InlineData("https://www.imdb.com/list/ls055592025/", "ls055592025")]
    [InlineData("https://www.imdb.com/list/ls055592025", "ls055592025")]
    [InlineData("https://m.imdb.com/list/ls123/?sort=alpha", "ls123")]
    [InlineData("https://www.imdb.com/list/ls4117371353/edit/?ref_=cr_lst_crte", "ls4117371353")]
    [InlineData("https://www.imdb.com/list/ls4117371353/edit", "ls4117371353")]
    public void ExtractId_ReturnsListId_ForCustomLists(string url, string expected)
    {
        Assert.Equal(expected, ImdbListUrl.ExtractId(url));
    }

    [Theory]
    [InlineData("https://www.imdb.com/chart/moviemeter/?ref_=wl_nv_menu", "chart-moviemeter")]
    [InlineData("https://www.imdb.com/chart/top", "chart-top")]
    public void ExtractId_ReturnsChartId_ForChartUrls(string url, string expected)
    {
        Assert.Equal(expected, ImdbListUrl.ExtractId(url));
    }

    [Theory]
    [InlineData(
        "https://www.imdb.com/list/ls4117371353/edit/?ref_=cr_lst_crte",
        "https://www.imdb.com/list/ls4117371353/")]
    [InlineData(
        "https://www.imdb.com/chart/moviemeter/?ref_=wl_nv_menu",
        "https://www.imdb.com/chart/moviemeter/")]
    [InlineData(
        "https://m.imdb.com/list/ls123/?sort=alpha",
        "https://www.imdb.com/list/ls123/")]
    [InlineData(
        "  https://www.imdb.com/user/ur12345678/watchlist?ref_=nv_usr_wl_all_0  ",
        "https://www.imdb.com/user/ur12345678/watchlist/")]
    [InlineData(
        "https://www.imdb.com/list/ls055592025/",
        "https://www.imdb.com/list/ls055592025/")]
    public void Normalize_ReturnsCanonicalUrl(string url, string expected)
    {
        Assert.Equal(expected, ImdbListUrl.Normalize(url));
    }

    [Fact]
    public void Normalize_Throws_WithSupportedFormats_ForUnrelatedPages()
    {
        var exception = Assert.Throws<ImdbWatchlistException>(() =>
            ImdbListUrl.Normalize("https://www.imdb.com/name/nm0000206/?ref_=tt_ov_dr"));

        Assert.Contains("not a recognised IMDb list URL", exception.Message);
        Assert.Contains("/user/ur12345678/watchlist/", exception.Message);
        Assert.Contains("/chart/moviemeter/", exception.Message);
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
