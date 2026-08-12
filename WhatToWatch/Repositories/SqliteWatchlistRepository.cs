using Microsoft.EntityFrameworkCore;
using WhatToWatch.Data;
using WhatToWatch.Models;

namespace WhatToWatch.Repositories;

public sealed class SqliteWatchlistRepository(WhatToWatchDbContext db)
    : IWatchlistRepository
{
    public async Task<Watchlist?> GetAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var entity = await db.Watchlists
            .AsNoTracking()
            .Include(watchlist => watchlist.Movies)
            .FirstOrDefaultAsync(watchlist => watchlist.Id == id, cancellationToken)
            .ConfigureAwait(false);

        return entity is null ? null : ToModel(entity);
    }

    public async Task<Watchlist?> GetByUrlAsync(
        string url,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        var normalized = NormalizeUrl(url);

        var candidates = await db.Watchlists
            .AsNoTracking()
            .Where(watchlist => watchlist.Url != null)
            .Select(watchlist => new { watchlist.Id, watchlist.Url })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var match = candidates
            .FirstOrDefault(candidate => NormalizeUrl(candidate.Url!) == normalized);

        return match is null
            ? null
            : await GetAsync(match.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyCollection<Watchlist>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var entities = await db.Watchlists
            .AsNoTracking()
            .Include(watchlist => watchlist.Movies)
            .OrderBy(watchlist => watchlist.Name.ToLower())
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return [.. entities.Select(ToModel)];
    }

    public async Task SaveAsync(
        Watchlist watchlist,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(watchlist);
        ArgumentException.ThrowIfNullOrWhiteSpace(watchlist.Id);

        var entity = await db.Watchlists
            .FirstOrDefaultAsync(item => item.Id == watchlist.Id, cancellationToken)
            .ConfigureAwait(false);

        if (entity is null)
        {
            entity = new WatchlistEntity
            {
                Id = watchlist.Id,
                Name = watchlist.Name,
            };
            db.Watchlists.Add(entity);
        }

        entity.Name = watchlist.Name;
        entity.Url = watchlist.Url;
        entity.LastRefreshedAt = watchlist.LastRefreshedAt;

        await db.Movies
            .Where(movie => movie.WatchlistId == watchlist.Id)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var entry in db.ChangeTracker.Entries<MovieEntity>()
            .Where(entry => entry.Entity.WatchlistId == watchlist.Id)
            .ToList())
        {
            entry.State = EntityState.Detached;
        }

        foreach (var movie in watchlist.Movies)
        {
            db.Movies.Add(new MovieEntity
            {
                WatchlistId = watchlist.Id,
                Id = movie.Id,
                Title = movie.Title,
                Year = movie.Year,
                PosterUrl = movie.PosterUrl,
                Rating = movie.Rating,
                Plot = movie.Plot,
                RuntimeMinutes = movie.RuntimeMinutes,
                Director = movie.Director,
                Genres = [.. movie.Genres],
            });
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string NormalizeUrl(string url) =>
        url.Trim().TrimEnd('/').ToLowerInvariant();

    private static Watchlist ToModel(WatchlistEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Url = entity.Url,
        LastRefreshedAt = entity.LastRefreshedAt,
        Movies = [.. entity.Movies
            .Select(movie => new Movie
            {
                Id = movie.Id,
                Title = movie.Title,
                Year = movie.Year,
                PosterUrl = movie.PosterUrl,
                Rating = movie.Rating,
                Plot = movie.Plot,
                RuntimeMinutes = movie.RuntimeMinutes,
                Director = movie.Director,
                Genres = [.. movie.Genres],
            })],
    };
}
