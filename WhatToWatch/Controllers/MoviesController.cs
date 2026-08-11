using Microsoft.AspNetCore.Mvc;
using WhatToWatch.DTOs;
using WhatToWatch.Mapping;
using WhatToWatch.Services;

namespace WhatToWatch.Controllers;

[ApiController]
[Route("api/movies")]
public class MoviesController(IMovieService movieService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<MovieDto>>> GetMoviesAsync(
        [FromQuery] MovieFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var movies = await movieService
            .GetMoviesAsync(MovieMapper.ToFilter(filter), cancellationToken)
            .ConfigureAwait(false);

        if (movies is null)
        {
            return NotFound();
        }

        return Ok(movies.Select(MovieMapper.ToDto).ToList());
    }

    [HttpGet("random")]
    public async Task<ActionResult<MovieDto>> GetRandomMovieAsync(
        [FromQuery] MovieFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var movie = await movieService
            .GetRandomMovieAsync(MovieMapper.ToFilter(filter), cancellationToken)
            .ConfigureAwait(false);

        if (movie is null)
        {
            return NotFound();
        }

        return Ok(MovieMapper.ToDto(movie));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<MovieDto>> GetMovieAsync(
        string id,
        [FromQuery] string watchlistId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(watchlistId))
        {
            return BadRequest(new { error = "watchlistId is required." });
        }

        var movie = await movieService
            .GetMovieAsync(watchlistId, id, cancellationToken)
            .ConfigureAwait(false);

        if (movie is null)
        {
            return NotFound();
        }

        return Ok(MovieMapper.ToDto(movie));
    }
}
