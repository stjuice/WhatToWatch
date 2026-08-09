using Microsoft.AspNetCore.Mvc;
using WhatToWatch.DTOs;

namespace WhatToWatch.Controllers;

[ApiController]
[Route("api/movies")]
public class MoviesController : ControllerBase
{
    [HttpGet]
    public Task<ActionResult<IReadOnlyCollection<MovieDto>>> GetMovies(
        [FromQuery] MovieFilterRequest filter,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    [HttpGet("random")]
    public Task<ActionResult<MovieDto>> GetRandomMovie(
        [FromQuery] MovieFilterRequest filter,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    [HttpGet("{id}")]
    public Task<ActionResult<MovieDto>> GetMovie(
        string id,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
