using Microsoft.AspNetCore.Mvc;

namespace WhatToWatch.Controllers;

[ApiController]
[Route("api/tvshows")]
public class TvShowsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetTvShows() =>
        StatusCode(
            StatusCodes.Status501NotImplemented,
            new { error = "TV shows are not implemented yet." });
}
