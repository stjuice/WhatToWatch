using WhatToWatch.DTOs;

namespace WhatToWatch.Services;

public interface IPartyService
{
    Task<SuggestedCodeDto> SuggestCodeAsync(CancellationToken cancellationToken = default);
    Task<PartySessionDto> CreateAsync(CreatePartyRequest request, CancellationToken cancellationToken = default);
    Task<PartySessionDto> JoinAsync(string joinCode, CancellationToken cancellationToken = default);
    Task<PartyPreviewDto> GetPreviewAsync(string joinCode, CancellationToken cancellationToken = default);
    Task<PartyStateDto> GetStateAsync(string partyId, string playerToken, CancellationToken cancellationToken = default);
    Task<PartyStateDto> VoteAsync(string partyId, string playerToken, PartyVoteRequest request, CancellationToken cancellationToken = default);
}
