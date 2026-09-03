using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace WhatToWatch.Data;

public class WhatToWatchDbContext(DbContextOptions<WhatToWatchDbContext> options)
    : DbContext(options)
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    public DbSet<WatchlistEntity> Watchlists => Set<WatchlistEntity>();

    public DbSet<MovieEntity> Movies => Set<MovieEntity>();

    public DbSet<PartyEntity> Parties => Set<PartyEntity>();

    public DbSet<PartyPlayerEntity> PartyPlayers => Set<PartyPlayerEntity>();

    public DbSet<PartyLikeEntity> PartyLikes => Set<PartyLikeEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var stringListConverter = new ValueConverter<List<string>, string>(
            values => JsonSerializer.Serialize(values, JsonOptions),
            json => JsonSerializer.Deserialize<List<string>>(json, JsonOptions)
                ?? new List<string>());

        var stringListComparer = new ValueComparer<List<string>>(
            (left, right) =>
                (left == null && right == null)
                || (left != null && right != null && left.SequenceEqual(right)),
            values => values.Aggregate(
                0,
                (hash, value) => HashCode.Combine(hash, value.GetHashCode())),
            values => values.ToList());

        modelBuilder.Entity<WatchlistEntity>(entity =>
        {
            entity.ToTable("Watchlists");
            entity.HasKey(watchlist => watchlist.Id);
            entity.Property(watchlist => watchlist.Id).HasMaxLength(64);
            entity.Property(watchlist => watchlist.Name).IsRequired();
            entity.Property(watchlist => watchlist.Url);
            entity.Property(watchlist => watchlist.LastRefreshedAt);
        });

        modelBuilder.Entity<MovieEntity>(entity =>
        {
            entity.ToTable("Movies");
            entity.HasKey(movie => new { movie.WatchlistId, movie.Id });
            entity.Property(movie => movie.WatchlistId).HasMaxLength(64);
            entity.Property(movie => movie.Id).HasMaxLength(64);
            entity.Property(movie => movie.Title).IsRequired();
            entity.Property(movie => movie.Year);
            entity.Property(movie => movie.PosterUrl);
            entity.Property(movie => movie.Rating);
            entity.Property(movie => movie.Plot);
            entity.Property(movie => movie.RuntimeMinutes);
            entity.Property(movie => movie.Director);
            entity.Property(movie => movie.MediaCategory)
                .IsRequired()
                .HasDefaultValue(0);
            entity.Property(movie => movie.Genres)
                .HasConversion(stringListConverter)
                .HasColumnName("GenresJson")
                .IsRequired()
                .Metadata.SetValueComparer(stringListComparer);

            entity.HasOne(movie => movie.Watchlist)
                .WithMany(watchlist => watchlist.Movies)
                .HasForeignKey(movie => movie.WatchlistId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(movie => movie.WatchlistId);
        });

        modelBuilder.Entity<PartyEntity>(entity =>
        {
            entity.ToTable("Parties");
            entity.HasKey(party => party.Id);
            entity.Property(party => party.Id).HasMaxLength(64);
            entity.Property(party => party.JoinCode).IsRequired();
            entity.Property(party => party.WatchlistId).HasMaxLength(64);
            entity.Property(party => party.MovieSet)
                .HasConversion(stringListConverter)
                .HasColumnName("MovieSetJson")
                .IsRequired()
                .Metadata.SetValueComparer(stringListComparer);
            entity.Property(party => party.Status).IsRequired();
            entity.Property(party => party.CreatedAt).IsRequired();
            entity.Property(party => party.FinishedAt);
            entity.Property(party => party.MatchedMovieId).HasMaxLength(64);
            entity.Property(party => party.MatchedWatchlistId).HasMaxLength(64);

            entity.HasIndex(party => party.JoinCode)
                .IsUnique()
                .HasFilter("\"Status\" = 1");
        });

        modelBuilder.Entity<PartyPlayerEntity>(entity =>
        {
            entity.ToTable("PartyPlayers");
            entity.HasKey(player => player.Id);
            entity.Property(player => player.Id).HasMaxLength(64);
            entity.Property(player => player.PartyId).HasMaxLength(64);
            entity.Property(player => player.PlayerToken).IsRequired();
            entity.Property(player => player.Slot).IsRequired();
            entity.Property(player => player.MovieOrder)
                .HasConversion(stringListConverter)
                .HasColumnName("MovieOrderJson")
                .IsRequired()
                .Metadata.SetValueComparer(stringListComparer);
            entity.Property(player => player.CurrentIndex).IsRequired();
            entity.Property(player => player.JoinedAt).IsRequired();
            entity.Property(player => player.LastSeenAt).IsRequired();

            entity.HasOne(player => player.Party)
                .WithMany(party => party.Players)
                .HasForeignKey(player => player.PartyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(player => player.PlayerToken).IsUnique();
        });

        modelBuilder.Entity<PartyLikeEntity>(entity =>
        {
            entity.ToTable("PartyLikes");
            entity.HasKey(like => new { like.PartyId, like.PlayerId, like.MovieId });
            entity.Property(like => like.PartyId).HasMaxLength(64);
            entity.Property(like => like.PlayerId).HasMaxLength(64);
            entity.Property(like => like.MovieId).HasMaxLength(64);
            entity.Property(like => like.CreatedAt).IsRequired();

            entity.HasOne(like => like.Party)
                .WithMany(party => party.Likes)
                .HasForeignKey(like => like.PartyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(like => new { like.PartyId, like.MovieId });
        });
    }
}
