using Microsoft.AspNetCore.Mvc;
using WhatToWatch.DTOs;

namespace WhatToWatch.Controllers;

[ApiController]
[Route("api/watchlists")]
public class WatchlistsController : ControllerBase
{
    [HttpGet]
    public Task<ActionResult<IReadOnlyCollection<WatchlistDto>>> GetWatchlists(
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    [HttpGet("{id}")]
    public Task<ActionResult<WatchlistDto>> GetWatchlist(
        string id,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    [HttpPost("{id}/refresh")]
    public Task<IActionResult> RefreshWatchlist(
        string id,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
