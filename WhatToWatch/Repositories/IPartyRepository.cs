using WhatToWatch.Data;

namespace WhatToWatch.Repositories;

public interface IPartyRepository
{
    Task<IReadOnlySet<string>> GetActiveCodesAsync(CancellationToken cancellationToken = default);
    Task<PartyEntity?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<PartyEntity?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
    Task AddAsync(PartyEntity party, CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
    Task CleanupAsync(DateTimeOffset now, TimeSpan inactivityTimeout, TimeSpan retention, CancellationToken cancellationToken = default);
    Task<PartyEntity> VoteAsync(
        string partyId,
        string playerToken,
        string movieReference,
        bool liked,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);
}
