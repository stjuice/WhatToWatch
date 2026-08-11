using Microsoft.AspNetCore.Mvc;
using WhatToWatch.DTOs;
using WhatToWatch.Services;

namespace WhatToWatch.Controllers;

[ApiController]
[Route("api/watchlists")]
public class WatchlistsController(IWatchlistService watchlistService) : ControllerBase
{
    [HttpGet]
    public Task<ActionResult<IReadOnlyCollection<WatchlistDto>>> GetWatchlistsAsync(
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    [HttpGet("{id}")]
    public Task<ActionResult<WatchlistDto>> GetWatchlistAsync(
        string id,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    [HttpPost("{id}/refresh")]
    public Task<IActionResult> RefreshWatchlistAsync(
        string id,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
