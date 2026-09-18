using ImdbWatchlists.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using WhatToWatch.Data;
using WhatToWatch.DTOs;
using WhatToWatch.Options;
using WhatToWatch.Repositories;
using WhatToWatch.Services;

namespace WhatToWatch.Tests.Services;

public sealed class PartyServiceTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly WhatToWatchDbContext _db;
    private readonly Mock<IMovieService> _movies = new();
    private readonly MutableTimeProvider _time = new(new DateTimeOffset(2026, 9, 3, 12, 0, 0, TimeSpan.Zero));
    private readonly PartyOptions _options = new();
    private readonly PartyService _sut;
    private readonly Dictionary<(string WatchlistId, string MovieId), Movie> _movieLookup = [];

    public PartyServiceTests()
    {
        _connection.Open();
        _db = new WhatToWatchDbContext(
            new DbContextOptionsBuilder<WhatToWatchDbContext>().UseSqlite(_connection).Options);
        _db.Database.EnsureCreated();

        _movies.Setup(service => service.GetMovieAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string watchlistId, string movieId, CancellationToken _) =>
                _movieLookup.GetValueOrDefault((watchlistId, movieId)));

        _sut = new PartyService(
            new SqlitePartyRepository(_db),
            _movies.Object,
            new RandomizationService(new Random(7)),
            new JoinCodeGenerator(new Random(3)),
            _time,
            Microsoft.Extensions.Options.Options.Create(_options));
    }

    [Fact]
    public async Task Create_IsImmediatelyPlayable_FreezesDeduplicatedSet()
    {
        SetupMovieSet(
            ("list/a", Movie("tt1", "First")),
            ("other|list", Movie("tt1", "Duplicate")),
            ("other|list", Movie("tt2", "Second")));

        var result = await _sut.CreateAsync(new CreatePartyRequest { JoinCode = "0042" });

        Assert.Equal("Playing", result.Status);
        Assert.Equal("0042", result.JoinCode);
        Assert.NotEmpty(result.PlayerToken);
        Assert.NotNull(result.CurrentMovie);
        Assert.Equal(2, result.Batch.Count);
        Assert.Equal([0, 1], result.Batch.Select(item => item.OrderIndex));
        Assert.Equal(1, result.PlayerCount);
        Assert.False(result.OpponentPresent);

        var stored = await _db.Parties.Include(party => party.Players).SingleAsync();
        Assert.Equal(2, stored.MovieSet.Count);
        Assert.Equal(2, stored.Players[0].MovieOrder.Count);
        Assert.All(stored.MovieSet, value => Assert.StartsWith("{", value));
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("12345")]
    [InlineData("12a4")]
    public async Task Create_RejectsInvalidExplicitCode(string code)
    {
        var exception = await Assert.ThrowsAsync<PartyException>(
            () => _sut.CreateAsync(new CreatePartyRequest { JoinCode = code }));

        Assert.Equal("InvalidCode", exception.Code);
    }

    [Fact]
    public async Task Create_RejectsEmptyMovieSet()
    {
        SetupMovieSet();

        var exception = await Assert.ThrowsAsync<PartyException>(
            () => _sut.CreateAsync(new CreatePartyRequest()));

        Assert.Equal("NoMovies", exception.Code);
    }

    [Fact]
    public async Task Create_RejectsTakenExplicitCode()
    {
        SetupMovieSet(("list", Movie("tt1", "One")));
        await _sut.CreateAsync(new CreatePartyRequest { JoinCode = "1234" });

        var exception = await Assert.ThrowsAsync<PartyException>(
            () => _sut.CreateAsync(new CreatePartyRequest { JoinCode = "1234" }));

        Assert.Equal("CodeTaken", exception.Code);
    }

    [Fact]
    public async Task Join_CreatesSecondIndependentPlayer_AndRejectsThird()
    {
        SetupMovieSet(
            ("list", Movie("tt1", "One")),
            ("list", Movie("tt2", "Two")),
            ("list", Movie("tt3", "Three")));
        var owner = await _sut.CreateAsync(new CreatePartyRequest { JoinCode = "1234" });

        var guest = await _sut.JoinAsync("1234");

        Assert.NotEqual(owner.PlayerToken, guest.PlayerToken);
        Assert.Equal(2, guest.PlayerCount);
        Assert.True(guest.OpponentPresent);
        var players = await _db.PartyPlayers.OrderBy(player => player.Slot).ToListAsync();
        Assert.Equal([1, 2], players.Select(player => player.Slot));
        Assert.NotSame(players[0].MovieOrder, players[1].MovieOrder);

        var exception = await Assert.ThrowsAsync<PartyException>(() => _sut.JoinAsync("1234"));
        Assert.Equal("PartyFull", exception.Code);
    }

    [Fact]
    public async Task Join_SameWatchlist_ReusesFrozenSetAndOwnerQueue()
    {
        SetupMovieSet(
            ("list", Movie("tt1", "One")),
            ("list", Movie("tt2", "Two")),
            ("list", Movie("tt3", "Three")));
        await _sut.CreateAsync(new CreatePartyRequest
        {
            JoinCode = "1234",
            WatchlistId = "list",
        });
        var partyBeforeJoin = await _db.Parties
            .Include(party => party.Players)
            .SingleAsync();
        var frozenSet = partyBeforeJoin.MovieSet.ToArray();
        var ownerOrder = partyBeforeJoin.Players.Single().MovieOrder.ToArray();

        await _sut.JoinAsync("1234", "LIST");

        var party = await _db.Parties
            .Include(item => item.Players)
            .SingleAsync();
        Assert.Equal(frozenSet, party.MovieSet);
        Assert.Equal(ownerOrder, party.Players.Single(player => player.Slot == 1).MovieOrder);
        Assert.Equal(
            frozenSet.OrderBy(item => item),
            party.Players.Single(player => player.Slot == 2).MovieOrder.OrderBy(item => item));
    }

    [Fact]
    public async Task Join_DifferentWatchlist_MergesSetsAndRebuildsOwnerUnseenQueue()
    {
        SetupMovieSet(
            ("host", Movie("tt1", "One")),
            ("host", Movie("tt2", "Two")),
            ("host", Movie("tt3", "Three")),
            ("guest", Movie("tt2", "Duplicate Two")),
            ("guest", Movie("tt4", "Four")),
            ("guest", Movie("tt5", "Five")));
        var owner = await _sut.CreateAsync(new CreatePartyRequest
        {
            JoinCode = "1234",
            WatchlistId = "host",
        });
        var seenMovieId = owner.CurrentMovie!.Id;
        var ownerBeforeJoin = await _sut.VoteAsync(
            owner.PartyId,
            owner.PlayerToken,
            new PartyVoteRequest { MovieId = seenMovieId, Liked = true });
        var currentMovieId = ownerBeforeJoin.CurrentMovie!.Id;

        var guest = await _sut.JoinAsync("1234", "guest");

        _db.ChangeTracker.Clear();
        var party = await _db.Parties
            .Include(item => item.Players)
            .Include(item => item.Likes)
            .SingleAsync();
        var combinedReferences = party.MovieSet.Select(PartyMovieReference.Decode).ToArray();
        Assert.Equal(5, combinedReferences.Length);
        Assert.Equal(
            "host",
            combinedReferences.Single(reference => reference.MovieId == "tt2").WatchlistId);

        var storedOwner = party.Players.Single(player => player.Slot == 1);
        var ownerMovieIds = storedOwner.MovieOrder
            .Select(encoded => PartyMovieReference.Decode(encoded).MovieId)
            .ToArray();
        Assert.Equal(0, storedOwner.CurrentIndex);
        Assert.Equal(currentMovieId, ownerMovieIds[0]);
        Assert.DoesNotContain(seenMovieId, ownerMovieIds);
        Assert.Single(party.Likes);

        var guestMovieIds = party.Players.Single(player => player.Slot == 2).MovieOrder
            .Select(encoded => PartyMovieReference.Decode(encoded).MovieId)
            .ToArray();
        Assert.Equal(5, guestMovieIds.Length);
        Assert.Equal(5, guestMovieIds.Distinct(StringComparer.OrdinalIgnoreCase).Count());

        var ownerAfterVote = await _sut.VoteAsync(
            owner.PartyId,
            owner.PlayerToken,
            new PartyVoteRequest { MovieId = currentMovieId, Liked = false });
        Assert.Equal(1, ownerAfterVote.Progress.CurrentIndex);
        Assert.Equal(4, ownerAfterVote.Progress.TotalMovies);
        Assert.Contains(ownerAfterVote.Batch, item => item.Movie.Id == "tt4");
        Assert.Contains(ownerAfterVote.Batch, item => item.Movie.Id == "tt5");
        Assert.Equal(2, guest.PlayerCount);
    }

    [Fact]
    public async Task Vote_No_AdvancesOnlyCaller()
    {
        SetupMovieSet(("list", Movie("tt1", "One")), ("list", Movie("tt2", "Two")));
        var session = await _sut.CreateAsync(new CreatePartyRequest { JoinCode = "1234" });
        var currentId = session.CurrentMovie!.Id;

        var state = await _sut.VoteAsync(
            session.PartyId,
            session.PlayerToken,
            new PartyVoteRequest { MovieId = currentId, Liked = false });

        Assert.Equal(1, state.Progress.CurrentIndex);
        Assert.Empty(await _db.PartyLikes.ToListAsync());
    }

    [Fact]
    public async Task State_ReturnsFiveMovieBatch_AndShrinksAtTail()
    {
        SetupMovieSet(Enumerable.Range(1, 7)
            .Select(index => ("list", Movie($"tt{index}", $"Movie {index}")))
            .ToArray());
        var session = await _sut.CreateAsync(new CreatePartyRequest { JoinCode = "1234" });

        Assert.Equal(5, session.Batch.Count);
        Assert.Equal([0, 1, 2, 3, 4], session.Batch.Select(item => item.OrderIndex));

        PartyStateDto state = session;
        for (var vote = 0; vote < 3; vote++)
        {
            state = await _sut.VoteAsync(
                session.PartyId,
                session.PlayerToken,
                new PartyVoteRequest
                {
                    MovieId = state.Batch[0].Movie.Id,
                    Liked = false,
                });
        }

        Assert.Equal(3, state.Progress.CurrentIndex);
        Assert.Equal(4, state.Batch.Count);
        Assert.Equal([3, 4, 5, 6], state.Batch.Select(item => item.OrderIndex));
        Assert.Equal(state.Batch[0].Movie.Id, state.CurrentMovie!.Id);
    }

    [Fact]
    public async Task State_RejectsInvalidPlayerToken()
    {
        SetupMovieSet(("list", Movie("tt1", "One")));
        var session = await _sut.CreateAsync(new CreatePartyRequest { JoinCode = "1234" });

        var exception = await Assert.ThrowsAsync<PartyException>(
            () => _sut.GetStateAsync(session.PartyId, "wrong-token"));

        Assert.Equal("InvalidPlayerToken", exception.Code);
    }

    [Fact]
    public async Task Vote_RejectsMovieOtherThanCurrent()
    {
        SetupMovieSet(("list", Movie("tt1", "One")));
        var session = await _sut.CreateAsync(new CreatePartyRequest { JoinCode = "1234" });

        var exception = await Assert.ThrowsAsync<PartyException>(() => _sut.VoteAsync(
            session.PartyId,
            session.PlayerToken,
            new PartyVoteRequest { MovieId = "tt999", Liked = true }));

        Assert.Equal("NotYourCurrentMovie", exception.Code);
        Assert.Equal(0, (await _db.PartyPlayers.SingleAsync()).CurrentIndex);
    }

    [Fact]
    public async Task Vote_TwoYesVotes_ProduceMatch_AndReturnItToTheOtherPlayer()
    {
        SetupMovieSet(("list", Movie("tt1", "One")));
        var owner = await _sut.CreateAsync(new CreatePartyRequest { JoinCode = "1234" });
        var guest = await _sut.JoinAsync("1234");

        var first = await _sut.VoteAsync(
            owner.PartyId, owner.PlayerToken,
            new PartyVoteRequest { MovieId = "tt1", Liked = true });
        Assert.Equal("Playing", first.Status);

        var matched = await _sut.VoteAsync(
            guest.PartyId, guest.PlayerToken,
            new PartyVoteRequest { MovieId = "tt1", Liked = true });

        Assert.Equal("Matched", matched.Status);
        Assert.Equal("tt1", matched.MatchedMovie!.Id);
        Assert.Null(matched.CurrentMovie);
        Assert.Empty(matched.Batch);
        Assert.Equal(2, await _db.PartyLikes.CountAsync());

        var ownerMatch = await _sut.VoteAsync(
            owner.PartyId, owner.PlayerToken,
            new PartyVoteRequest { MovieId = "tt1", Liked = true });

        Assert.Equal("Matched", ownerMatch.Status);
        Assert.Equal("tt1", ownerMatch.MatchedMovie!.Id);
        Assert.Equal(2, await _db.PartyLikes.CountAsync());
    }

    [Fact]
    public async Task Exhaustion_FinishesOnlyAfterBothPlayersExhausted()
    {
        SetupMovieSet(("list", Movie("tt1", "One")));
        var owner = await _sut.CreateAsync(new CreatePartyRequest { JoinCode = "1234" });
        var guest = await _sut.JoinAsync("1234");

        var ownerState = await _sut.VoteAsync(
            owner.PartyId, owner.PlayerToken,
            new PartyVoteRequest { MovieId = "tt1", Liked = false });
        Assert.Equal("Playing", ownerState.Status);
        Assert.True(ownerState.Progress.IsExhausted);

        var guestState = await _sut.VoteAsync(
            guest.PartyId, guest.PlayerToken,
            new PartyVoteRequest { MovieId = "tt1", Liked = false });
        Assert.Equal("Finished", guestState.Status);
    }

    [Fact]
    public async Task State_SkipsVanishedMovies_AndRefreshesPresence()
    {
        SetupMovieSet(("list", Movie("tt1", "One")), ("list", Movie("tt2", "Two")));
        var owner = await _sut.CreateAsync(new CreatePartyRequest { JoinCode = "1234" });
        var player = await _db.PartyPlayers.SingleAsync();
        var first = PartyMovieReference.Decode(player.MovieOrder[0]);
        _movieLookup.Remove((first.WatchlistId, first.MovieId));
        _time.Advance(TimeSpan.FromMinutes(1));

        var state = await _sut.GetStateAsync(owner.PartyId, owner.PlayerToken);

        Assert.Equal(1, state.Progress.CurrentIndex);
        Assert.NotNull(state.CurrentMovie);
        Assert.Single(state.Batch);
        Assert.Equal(1, state.Batch[0].OrderIndex);
        Assert.Equal(_time.GetUtcNow(), (await _db.PartyPlayers.SingleAsync()).LastSeenAt);
    }

    [Fact]
    public async Task State_OmitsVanishedMoviesInsideBatch_WithoutAdvancingPastCurrent()
    {
        SetupMovieSet(Enumerable.Range(1, 6)
            .Select(index => ("list", Movie($"tt{index}", $"Movie {index}")))
            .ToArray());
        var owner = await _sut.CreateAsync(new CreatePartyRequest { JoinCode = "1234" });
        var player = await _db.PartyPlayers.SingleAsync();
        var vanished = PartyMovieReference.Decode(player.MovieOrder[2]);
        _movieLookup.Remove((vanished.WatchlistId, vanished.MovieId));

        var state = await _sut.GetStateAsync(owner.PartyId, owner.PlayerToken);

        Assert.Equal(0, state.Progress.CurrentIndex);
        Assert.Equal(5, state.Batch.Count);
        Assert.Equal([0, 1, 3, 4, 5], state.Batch.Select(item => item.OrderIndex));
    }

    [Fact]
    public async Task LazyCleanup_ExpiresParty_WhenAllPlayersAreStale()
    {
        SetupMovieSet(("list", Movie("tt1", "One")));
        await _sut.CreateAsync(new CreatePartyRequest { JoinCode = "1234" });
        _time.Advance(TimeSpan.FromMinutes(121));

        var exception = await Assert.ThrowsAsync<PartyException>(() => _sut.JoinAsync("1234"));

        Assert.Equal("PartyExpired", exception.Code);
        Assert.Equal(PartyStatus.Expired, (await _db.Parties.SingleAsync()).Status);
    }

    [Fact]
    public async Task State_DoesNotExposeOpponentSecrets()
    {
        SetupMovieSet(("list", Movie("tt1", "One")));
        var owner = await _sut.CreateAsync(new CreatePartyRequest { JoinCode = "1234" });
        var guest = await _sut.JoinAsync("1234");

        var state = await _sut.GetStateAsync(owner.PartyId, owner.PlayerToken);
        var json = System.Text.Json.JsonSerializer.Serialize(state);

        Assert.DoesNotContain(guest.PlayerToken, json);
        Assert.DoesNotContain("MovieOrder", json);
        Assert.DoesNotContain("Likes", json);
    }

    [Fact]
    public async Task Preview_IsPublicAndReportsCapacity()
    {
        SetupMovieSet(("list", Movie("tt1", "One")));
        await _sut.CreateAsync(new CreatePartyRequest
        {
            JoinCode = "1234",
            WatchlistId = "list",
        });
        await _sut.JoinAsync("1234", "list");

        var preview = await _sut.GetPreviewAsync("1234");

        Assert.Equal(2, preview.PlayerCount);
        Assert.True(preview.IsFull);
        Assert.Equal("Playing", preview.Status);
        Assert.Equal("list", preview.WatchlistId);
    }

    private void SetupMovieSet(params (string WatchlistId, Movie Movie)[] entries)
    {
        _movieLookup.Clear();
        foreach (var entry in entries)
            _movieLookup.TryAdd((entry.WatchlistId, entry.Movie.Id), entry.Movie);
        _movies.Setup(service => service.GetMovieSetAsync(
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string? watchlistId, CancellationToken _) =>
                _movieLookup
                    .Where(entry => watchlistId is null
                        || entry.Key.WatchlistId.Equals(
                            watchlistId,
                            StringComparison.OrdinalIgnoreCase))
                    .Select(entry => new MovieReference(
                        entry.Key.WatchlistId,
                        entry.Value))
                    .ToArray());
    }

    private static Movie Movie(string id, string title) =>
        new() { Id = id, Title = title, Genres = ["Drama"] };

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private sealed class MutableTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
        public void Advance(TimeSpan amount) => now += amount;
    }
}
