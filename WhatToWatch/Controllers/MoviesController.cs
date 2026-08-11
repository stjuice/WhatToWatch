using Microsoft.AspNetCore.Mvc;
using WhatToWatch.DTOs;
using WhatToWatch.Services;

namespace WhatToWatch.Controllers;

[ApiController]
[Route("api/movies")]
public class MoviesController(IMovieService movieService) : ControllerBase
{
    [HttpGet]
    public Task<ActionResult<IReadOnlyCollection<MovieDto>>> GetMoviesAsync(
        [FromQuery] MovieFilterRequest filter,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    [HttpGet("random")]
    public Task<ActionResult<MovieDto>> GetRandomMovieAsync(
        [FromQuery] MovieFilterRequest filter,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    [HttpGet("{id}")]
    public Task<ActionResult<MovieDto>> GetMovieAsync(
        string id,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
