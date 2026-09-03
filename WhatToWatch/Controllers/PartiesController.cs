using Microsoft.AspNetCore.Mvc;
using WhatToWatch.DTOs;
using WhatToWatch.Services;

namespace WhatToWatch.Controllers;

[ApiController]
[Route("api/parties")]
public sealed class PartiesController(IPartyService partyService) : ControllerBase
{
    private const string PlayerTokenHeader = "X-Player-Token";

    [HttpGet("suggest-code")]
    public Task<ActionResult<SuggestedCodeDto>> SuggestCodeAsync(
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            async () => await partyService.SuggestCodeAsync(cancellationToken)
                .ConfigureAwait(false));

    [HttpPost]
    public Task<ActionResult<PartySessionDto>> CreateAsync(
        [FromBody] CreatePartyRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            async () => await partyService.CreateAsync(request, cancellationToken)
                .ConfigureAwait(false));

    [HttpPost("join")]
    public Task<ActionResult<PartySessionDto>> JoinAsync(
        [FromBody] JoinPartyRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            async () => await partyService.JoinAsync(request.JoinCode, cancellationToken)
                .ConfigureAwait(false));

    [HttpGet("by-code/{joinCode}")]
    public Task<ActionResult<PartyPreviewDto>> GetByCodeAsync(
        string joinCode,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            async () => await partyService.GetPreviewAsync(joinCode, cancellationToken)
                .ConfigureAwait(false));

    [HttpGet("{partyId}")]
    public Task<ActionResult<PartyStateDto>> GetStateAsync(
        string partyId,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            async () => await partyService.GetStateAsync(
                partyId,
                GetPlayerToken(),
                cancellationToken).ConfigureAwait(false));

    [HttpPost("{partyId}/vote")]
    public Task<ActionResult<PartyStateDto>> VoteAsync(
        string partyId,
        [FromBody] PartyVoteRequest request,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            async () => await partyService.VoteAsync(
                partyId,
                GetPlayerToken(),
                request,
                cancellationToken).ConfigureAwait(false));

    private string GetPlayerToken() =>
        Request.Headers.TryGetValue(PlayerTokenHeader, out var values)
            ? values.ToString()
            : string.Empty;

    private async Task<ActionResult<T>> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            return Ok(await operation().ConfigureAwait(false));
        }
        catch (PartyException exception)
        {
            return StatusCode(
                exception.StatusCode,
                new PartyErrorDto
                {
                    Code = exception.Code,
                    Message = exception.Message,
                });
        }
    }
}
