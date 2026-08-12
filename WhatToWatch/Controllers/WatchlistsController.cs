using ImdbWatchlists;
using Microsoft.AspNetCore.Mvc;
using WhatToWatch.DTOs;
using WhatToWatch.Mapping;
using WhatToWatch.Services;

namespace WhatToWatch.Controllers;

[ApiController]
[Route("api/watchlists")]
public class WatchlistsController(IWatchlistService watchlistService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<WatchlistDto>> ImportWatchlistAsync(
        [FromBody] CreateWatchlistRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var watchlist = await watchlistService
                .ImportAsync(request.Url, cancellationToken)
                .ConfigureAwait(false);

            return Ok(MovieMapper.ToDto(watchlist));
        }
        catch (ImdbWatchlistException exception)
        {
            return MapImdbException(exception);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<WatchlistDto>>> GetWatchlistsAsync(
        CancellationToken cancellationToken)
    {
        var watchlists = await watchlistService
            .GetWatchlistsAsync(cancellationToken)
            .ConfigureAwait(false);

        return Ok(watchlists.Select(MovieMapper.ToDto).ToList());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WatchlistDto>> GetWatchlistAsync(
        string id,
        CancellationToken cancellationToken)
    {
        var watchlist = await watchlistService
            .GetWatchlistAsync(id, cancellationToken)
            .ConfigureAwait(false);

        if (watchlist is null)
            return NotFound();

        return Ok(MovieMapper.ToDto(watchlist));
    }

    [HttpPost("{id}/refresh")]
    public async Task<ActionResult<WatchlistDto>> RefreshWatchlistAsync(
        string id,
        CancellationToken cancellationToken)
    {
        try
        {
            var watchlist = await watchlistService
                .RefreshWatchlistAsync(id, cancellationToken)
                .ConfigureAwait(false);

            if (watchlist is null)
                return NotFound();

            return Ok(MovieMapper.ToDto(watchlist));
        }
        catch (ImdbWatchlistException exception)
        {
            return MapImdbException(exception);
        }
    }

    private ActionResult MapImdbException(ImdbWatchlistException exception)
    {
        if (IsClientError(exception))
            return BadRequest(new { error = exception.Message });

        return StatusCode(
            StatusCodes.Status502BadGateway,
            new { error = exception.Message });
    }

    private static bool IsClientError(ImdbWatchlistException exception)
    {
        var message = exception.Message;
        return message.Contains("URL", StringComparison.OrdinalIgnoreCase)
            || message.Contains("recognised", StringComparison.OrdinalIgnoreCase)
            || message.Contains("recognized", StringComparison.OrdinalIgnoreCase)
            || message.Contains("empty", StringComparison.OrdinalIgnoreCase);
    }
}
