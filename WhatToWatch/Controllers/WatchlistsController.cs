using ImdbWatchlists;
using ImdbWatchlists.Extraction;
using Microsoft.AspNetCore.Mvc;
using WhatToWatch.Auth;
using WhatToWatch.DTOs;
using WhatToWatch.Mapping;
using WhatToWatch.Services;

namespace WhatToWatch.Controllers;

[ApiController]
[Route("api/watchlists")]
public class WatchlistsController(IWatchlistService watchlistService) : ControllerBase
{
    [HttpPost]
    [AdminAuthorize]
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

    [HttpPost("import-imdb")]
    [AdminAuthorize]
    public async Task<ActionResult<WatchlistDto>> ImportImdbWatchlistAsync(
        [FromBody] ExtractedWatchlistPage request,
        CancellationToken cancellationToken)
    {
        try
        {
            var watchlist = await watchlistService
                .ImportFromImdbPayloadAsync(request, cancellationToken)
                .ConfigureAwait(false);

            return Ok(MovieMapper.ToDto(watchlist));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
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

    [HttpPut("{id}")]
    [AdminAuthorize]
    public async Task<ActionResult<WatchlistDto>> UpdateWatchlistAsync(
        string id,
        [FromBody] UpdateWatchlistRequest request,
        CancellationToken cancellationToken)
    {
        var watchlist = await watchlistService
            .UpdateWatchlistAsync(id, request, cancellationToken)
            .ConfigureAwait(false);

        if (watchlist is null)
            return NotFound();

        return Ok(MovieMapper.ToDto(watchlist));
    }

    [HttpDelete("{id}")]
    [AdminAuthorize]
    public async Task<IActionResult> DeleteWatchlistAsync(
        string id,
        CancellationToken cancellationToken)
    {
        var deleted = await watchlistService
            .DeleteWatchlistAsync(id, cancellationToken)
            .ConfigureAwait(false);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    [HttpPost("{id}/refresh")]
    [AdminAuthorize]
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
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { error = exception.Message });
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
