using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WhatToWatch.Controllers;

namespace WhatToWatch.Tests.Controllers;

public class TvShowsControllerTests
{
    [Fact]
    public void GetTvShows_Returns501NotImplemented()
    {
        var result = new TvShowsController().GetTvShows();

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status501NotImplemented, objectResult.StatusCode);

        var json = JsonSerializer.Serialize(objectResult.Value);
        using var document = JsonDocument.Parse(json);
        Assert.Equal(
            "TV shows are not implemented yet.",
            document.RootElement.GetProperty("error").GetString());
    }
}
