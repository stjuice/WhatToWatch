using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using WhatToWatch.Data;
using WhatToWatch.DTOs;
using WhatToWatch.Mapping;
using WhatToWatch.Options;
using WhatToWatch.Repositories;

namespace WhatToWatch.Services;

public sealed class PartyService(
    IPartyRepository repository,
    IMovieService movieService,
    IRandomizationService randomizationService,
    JoinCodeGenerator joinCodeGenerator,
    TimeProvider timeProvider,
    IOptions<PartyOptions> options) : IPartyService
{
    private readonly PartyOptions _options = options.Value;

    public async Task<SuggestedCodeDto> SuggestCodeAsync(
        CancellationToken cancellationToken = default)
    {
        await CleanupAsync(cancellationToken).ConfigureAwait(false);

        return new SuggestedCodeDto
        {
            JoinCode = Allocate(await repository.GetActiveCodesAsync(cancellationToken)
                .ConfigureAwait(false)),
        };
    }

    public async Task<PartySessionDto> CreateAsync(
        CreatePartyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        await CleanupAsync(cancellationToken).ConfigureAwait(false);

        var explicitCode = request.JoinCode is not null;

        if (explicitCode && !IsValidCode(request.JoinCode!))
            throw PartyException.InvalidCode();

        var takenCodes = await repository.GetActiveCodesAsync(cancellationToken)
            .ConfigureAwait(false);

        var joinCode = explicitCode ? request.JoinCode! : Allocate(takenCodes);

        if (takenCodes.Contains(joinCode))
            throw PartyException.CodeTaken();

        var references = await movieService
            .GetMovieSetAsync(request.WatchlistId, cancellationToken)
            .ConfigureAwait(false);

        var movieSet = references?
            .DistinctBy(reference => reference.Movie.Id, StringComparer.OrdinalIgnoreCase)
            .Select(reference => PartyMovieReference.Encode(reference.WatchlistId, reference.Movie.Id))
            .ToArray() ?? [];

        if (movieSet.Length == 0)
            throw PartyException.NoMovies();

        var now = timeProvider.GetUtcNow();
        var player = CreatePlayer("1", randomizationService.Shuffle(movieSet), now);
        var party = new PartyEntity
        {
            Id = Guid.NewGuid().ToString("N"),
            JoinCode = joinCode,
            WatchlistId = request.WatchlistId,
            MovieSet = [.. movieSet],
            Status = PartyStatus.Playing,
            CreatedAt = now,
            Players = [player],
        };
        player.PartyId = party.Id;

        await repository.AddAsync(party, cancellationToken).ConfigureAwait(false);

        var state = await BuildStateAsync(party, player, now, cancellationToken)
            .ConfigureAwait(false);

        return ToSession(state, player.PlayerToken);
    }

    public async Task<PartySessionDto> JoinAsync(
        string joinCode,
        string? watchlistId = null,
        CancellationToken cancellationToken = default)
    {
        ValidateCode(joinCode);

        await CleanupAsync(cancellationToken).ConfigureAwait(false);

        var party = await repository.GetByCodeAsync(joinCode, cancellationToken)
            .ConfigureAwait(false)
            ?? throw PartyException.PartyExpired();

        if (party.Status == PartyStatus.Expired)
            throw PartyException.PartyExpired();

        if (party.Status != PartyStatus.Playing)
            throw PartyException.AlreadyFinished();

        if (party.Players.Count >= 2)
            throw PartyException.PartyFull();

        if (!string.Equals(
                party.WatchlistId,
                watchlistId,
                StringComparison.OrdinalIgnoreCase))
        {
            var guestReferences = await movieService
                .GetMovieSetAsync(watchlistId, cancellationToken)
                .ConfigureAwait(false);

            var combinedSet = party.MovieSet
                .Concat(guestReferences?.Select(reference =>
                    PartyMovieReference.Encode(
                        reference.WatchlistId,
                        reference.Movie.Id)) ?? [])
                .DistinctBy(
                    encoded => PartyMovieReference.Decode(encoded).MovieId,
                    StringComparer.OrdinalIgnoreCase)
                .ToArray();

            RebuildOwnerQueue(party.Players.Single(), combinedSet);
            party.MovieSet = [.. combinedSet];
        }

        var now = timeProvider.GetUtcNow();
        var player = CreatePlayer(
            "2",
            randomizationService.Shuffle(party.MovieSet),
            now);

        player.PartyId = party.Id;
        party.Players.Add(player);

        await repository.SaveAsync(cancellationToken).ConfigureAwait(false);

        var state = await BuildStateAsync(party, player, now, cancellationToken)
            .ConfigureAwait(false);

        return ToSession(state, player.PlayerToken);
    }

    public async Task<PartyPreviewDto> GetPreviewAsync(
        string joinCode,
        CancellationToken cancellationToken = default)
    {
        ValidateCode(joinCode);

        await CleanupAsync(cancellationToken).ConfigureAwait(false);

        var party = await repository.GetByCodeAsync(joinCode, cancellationToken)
            .ConfigureAwait(false)
            ?? throw PartyException.PartyExpired();

        if (party.Status == PartyStatus.Expired)
            throw PartyException.PartyExpired();

        return new PartyPreviewDto
        {
            PartyId = party.Id,
            JoinCode = party.JoinCode,
            WatchlistId = party.WatchlistId,
            Status = party.Status.ToString(),
            PlayerCount = party.Players.Count,
            IsFull = party.Players.Count >= 2,
        };
    }

    public async Task<PartyStateDto> GetStateAsync(
        string partyId,
        string playerToken,
        CancellationToken cancellationToken = default)
    {
        await CleanupAsync(cancellationToken).ConfigureAwait(false);

        var party = await repository.GetByIdAsync(partyId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw PartyException.PartyExpired();

        if (party.Status == PartyStatus.Expired)
            throw PartyException.PartyExpired();

        var player = FindPlayer(party, playerToken);
        var now = timeProvider.GetUtcNow();

        player.LastSeenAt = now;

        return await BuildStateAsync(party, player, now, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<PartyStateDto> VoteAsync(
        string partyId,
        string playerToken,
        PartyVoteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.MovieId))
            throw PartyException.NotYourCurrentMovie();

        await CleanupAsync(cancellationToken).ConfigureAwait(false);

        var now = timeProvider.GetUtcNow();
        var party = await repository.VoteAsync(
            partyId,
            playerToken,
            request.MovieId,
            request.Liked,
            now,
            cancellationToken).ConfigureAwait(false);
        var player = FindPlayer(party, playerToken);

        return await BuildStateAsync(party, player, now, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<PartyStateDto> BuildStateAsync(
        PartyEntity party,
        PartyPlayerEntity player,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var batch = party.Status == PartyStatus.Playing
            ? await BuildBatchAsync(player, cancellationToken).ConfigureAwait(false)
            : [];

        if (party.Status == PartyStatus.Playing
            && party.Players.Count == 2
            && party.Players.All(item => item.CurrentIndex >= item.MovieOrder.Count))
        {
            party.Status = PartyStatus.Finished;
            party.FinishedAt = now;
        }

        ImdbWatchlists.Models.Movie? matchedMovie = null;
        if (party.MatchedMovieId is not null && party.MatchedWatchlistId is not null)
        {
            matchedMovie = await movieService.GetMovieAsync(
                party.MatchedWatchlistId,
                party.MatchedMovieId,
                cancellationToken).ConfigureAwait(false);
        }

        await repository.SaveAsync(cancellationToken).ConfigureAwait(false);

        var opponent = party.Players.SingleOrDefault(item => item.Id != player.Id);

        return new PartyStateDto
        {
            PartyId = party.Id,
            JoinCode = party.JoinCode,
            Status = party.Status.ToString(),
            PlayerCount = party.Players.Count,
            OpponentPresent = opponent is not null,
            OpponentOnline = opponent?.LastSeenAt >= now.AddSeconds(-_options.PresenceWindowSeconds),
            Progress = new PartyProgressDto
            {
                CurrentIndex = player.CurrentIndex,
                TotalMovies = player.MovieOrder.Count,
                IsExhausted = player.CurrentIndex >= player.MovieOrder.Count,
            },
            Batch = batch,
            CurrentMovie = batch.FirstOrDefault()?.Movie,
            MatchedMovie = matchedMovie is null ? null : MovieMapper.ToDto(matchedMovie),
        };
    }

    private async Task<IReadOnlyList<PartyMovieBatchItemDto>> BuildBatchAsync(
        PartyPlayerEntity player,
        CancellationToken cancellationToken)
    {
        if (player.CurrentIndex >= player.MovieOrder.Count)
            return [];

        var remaining = player.MovieOrder
            .Skip(player.CurrentIndex)
            .Select((encoded, offset) => new
            {
                OrderIndex = player.CurrentIndex + offset,
                Reference = PartyMovieReference.Decode(encoded),
            })
            .ToArray();

        var moviesByReference = new Dictionary<string, ImdbWatchlists.Models.Movie>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var watchlistId in remaining
                     .Select(item => item.Reference.WatchlistId)
                     .Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var movies = await movieService
                .GetMovieSetAsync(watchlistId, cancellationToken)
                .ConfigureAwait(false);

            if (movies is null)
                continue;

            foreach (var reference in movies)
            {
                moviesByReference.TryAdd(
                    PartyMovieReference.Encode(reference.WatchlistId, reference.Movie.Id),
                    reference.Movie);
            }
        }

        var batch = new List<PartyMovieBatchItemDto>(Math.Max(1, _options.BatchSize));
        
        foreach (var item in remaining)
        {
            var encoded = PartyMovieReference.Encode(
                item.Reference.WatchlistId,
                item.Reference.MovieId);

            if (!moviesByReference.TryGetValue(encoded, out var movie))
            {
                if (item.OrderIndex == player.CurrentIndex)
                    player.CurrentIndex++;

                continue;
            }

            batch.Add(new PartyMovieBatchItemDto
            {
                OrderIndex = item.OrderIndex,
                Movie = MovieMapper.ToDto(movie),
            });

            if (batch.Count >= Math.Max(1, _options.BatchSize))
                break;
        }

        return batch;
    }

    private Task CleanupAsync(CancellationToken cancellationToken) =>
        repository.CleanupAsync(
            timeProvider.GetUtcNow(),
            TimeSpan.FromMinutes(_options.InactivityTimeoutMinutes),
            TimeSpan.FromHours(_options.FinishedRetentionHours),
            cancellationToken);

    private string Allocate(IReadOnlySet<string> takenCodes)
    {
        try
        {
            return joinCodeGenerator.Next(takenCodes);
        }
        catch (NoCodesAvailableException)
        {
            throw PartyException.NoCodesAvailable();
        }
    }

    private static PartyPlayerEntity CreatePlayer(
        string slot,
        IReadOnlyList<string> order,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.NewGuid().ToString("N"),
            PartyId = string.Empty,
            PlayerToken = GenerateToken(),
            Slot = int.Parse(slot),
            MovieOrder = [.. order],
            CurrentIndex = 0,
            JoinedAt = now,
            LastSeenAt = now,
        };

    private void RebuildOwnerQueue(
        PartyPlayerEntity owner,
        IReadOnlyList<string> combinedSet)
    {
        var seenMovieIds = owner.MovieOrder
            .Take(owner.CurrentIndex)
            .Select(encoded => PartyMovieReference.Decode(encoded).MovieId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var current = owner.CurrentIndex < owner.MovieOrder.Count
            ? owner.MovieOrder[owner.CurrentIndex]
            : null;
        var currentMovieId = current is null
            ? null
            : PartyMovieReference.Decode(current).MovieId;

        var remaining = combinedSet
            .Where(encoded =>
            {
                var movieId = PartyMovieReference.Decode(encoded).MovieId;
                return !seenMovieIds.Contains(movieId)
                    && !string.Equals(
                        movieId,
                        currentMovieId,
                        StringComparison.OrdinalIgnoreCase);
            })
            .ToArray();

        var rebuilt = new List<string>(remaining.Length + (current is null ? 0 : 1));
        if (current is not null && !seenMovieIds.Contains(currentMovieId!))
            rebuilt.Add(current);
        rebuilt.AddRange(randomizationService.Shuffle(remaining));

        owner.MovieOrder = rebuilt;
        owner.CurrentIndex = 0;
    }

    private static string GenerateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static PartyPlayerEntity FindPlayer(PartyEntity party, string token)
    {
        if (string.IsNullOrEmpty(token))
            throw PartyException.InvalidPlayerToken();

        var tokenBytes = System.Text.Encoding.UTF8.GetBytes(token);

        return party.Players.FirstOrDefault(player =>
            CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(player.PlayerToken),
                tokenBytes))
            ?? throw PartyException.InvalidPlayerToken();
    }

    private static PartySessionDto ToSession(PartyStateDto state, string token) =>
        new()
        {
            PartyId = state.PartyId,
            JoinCode = state.JoinCode,
            Status = state.Status,
            PlayerCount = state.PlayerCount,
            OpponentPresent = state.OpponentPresent,
            OpponentOnline = state.OpponentOnline,
            Progress = state.Progress,
            Batch = state.Batch,
            CurrentMovie = state.CurrentMovie,
            MatchedMovie = state.MatchedMovie,
            PlayerToken = token,
        };

    private static bool IsValidCode(string? code) =>
        code is { Length: 4 } && code.All(char.IsAsciiDigit);

    private static void ValidateCode(string? code)
    {
        if (!IsValidCode(code))
            throw PartyException.InvalidCode();
    }
}
