using WhatToWatch.Models;

namespace WhatToWatch.Services;

public class RandomizationService(Random? random = null) : IRandomizationService
{
    private readonly Random _random = random ?? Random.Shared;

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

        return query.ToList();
    }

    public Movie? PickRandom(IReadOnlyList<Movie> movies)
    {
        ArgumentNullException.ThrowIfNull(movies);

        if (movies.Count == 0)
        {
            return null;
        }

        return movies[_random.Next(movies.Count)];
    }
}
