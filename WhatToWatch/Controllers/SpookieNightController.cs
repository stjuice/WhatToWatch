using Microsoft.AspNetCore.Mvc;
using WhatToWatch.DTOs;
using WhatToWatch.Mapping;
using WhatToWatch.Services;

namespace WhatToWatch.Controllers;

[ApiController]
[Route("api/spookie-night")]
public sealed class SpookieNightController(ISpookieNightService spookieNightService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<SpookieTicketDto>>> GetTicketsAsync(
        CancellationToken cancellationToken)
    {
        var tickets = await spookieNightService
            .GetUnlockedTicketsAsync(cancellationToken)
            .ConfigureAwait(false);

        return Ok(tickets.Select(ToDto).ToList());
    }

    [HttpGet("{key}")]
    public async Task<ActionResult<MovieDto>> GetMovieAsync(
        string key,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
            return NotFound();

        var ticket = await spookieNightService
            .GetTicketAsync(key, cancellationToken)
            .ConfigureAwait(false);

        if (ticket is null)
            return NotFound();

        return Ok(MovieMapper.ToDto(ticket.Movie));
    }

    private static SpookieTicketDto ToDto(SpookieTicket ticket) =>
        new()
        {
            Key = ticket.Key,
            IsBonus = ticket.IsBonus,
            IsCurrent = ticket.IsCurrent,
            Movie = MovieMapper.ToDto(ticket.Movie),
        };
}
