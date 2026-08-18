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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var genresConverter = new ValueConverter<List<string>, string>(
            genres => JsonSerializer.Serialize(genres, JsonOptions),
            json => JsonSerializer.Deserialize<List<string>>(json, JsonOptions)
                ?? new List<string>());

        var genresComparer = new ValueComparer<List<string>>(
            (left, right) =>
                (left == null && right == null)
                || (left != null && right != null && left.SequenceEqual(right)),
            genres => genres.Aggregate(
                0,
                (hash, genre) => HashCode.Combine(hash, genre.GetHashCode())),
            genres => genres.ToList());

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
                .HasConversion(genresConverter)
                .HasColumnName("GenresJson")
                .IsRequired()
                .Metadata.SetValueComparer(genresComparer);

            entity.HasOne(movie => movie.Watchlist)
                .WithMany(watchlist => watchlist.Movies)
                .HasForeignKey(movie => movie.WatchlistId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(movie => movie.WatchlistId);
        });
    }
}
