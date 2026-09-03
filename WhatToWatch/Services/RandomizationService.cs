using ImdbWatchlists.Models;
using WhatToWatch.Models;

namespace WhatToWatch.Services;

public class RandomizationService(Random? random = null) : IRandomizationService
{
    private readonly Random _random = random ?? Random.Shared;
    private readonly object _randomLock = new();

    public IReadOnlyList<Movie> Filter(
        IEnumerable<Movie> movies,
        MovieFilter filter)
    {
        ArgumentNullException.ThrowIfNull(movies);
        ArgumentNullException.ThrowIfNull(filter);

        IEnumerable<Movie> query = movies;

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var term = filter.Query.Trim();
            query = query.Where(movie =>
                movie.Title.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        if (filter.YearFrom is int yearFrom)
        {
            query = query.Where(movie => movie.Year is int year && year >= yearFrom);
        }

        if (filter.YearTo is int yearTo)
        {
            query = query.Where(movie => movie.Year is int year && year <= yearTo);
        }

        if (filter.MinRating is double minRating)
        {
            query = query.Where(movie =>
                movie.Rating is double rating && rating >= minRating);
        }

        if (filter.Genres.Count > 0)
        {
            query = query.Where(movie =>
                filter.Genres.Any(required =>
                    movie.Genres.Any(genre =>
                        genre.Equals(required, StringComparison.OrdinalIgnoreCase))));
        }

        return [.. query];
    }

    public Movie? PickRandom(IReadOnlyList<Movie> movies)
    {
        ArgumentNullException.ThrowIfNull(movies);

        if (movies.Count == 0)
            return null;

        return movies[_random.Next(movies.Count)];
    }

    public IReadOnlyList<T> Shuffle<T>(IReadOnlyList<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        var shuffled = items.ToArray();

        lock (_randomLock)
        {
            for (var index = shuffled.Length - 1; index > 0; index--)
            {
                var swapIndex = _random.Next(index + 1);
                (shuffled[index], shuffled[swapIndex]) =
                    (shuffled[swapIndex], shuffled[index]);
            }
        }

        return shuffled;
    }
}
