using WhatToWatch.Models;

namespace WhatToWatch.Services;

public interface IRandomizationService
{
    IReadOnlyList<Movie> Filter(
        IEnumerable<Movie> movies,
        MovieFilter filter);

    Movie? PickRandom(IReadOnlyList<Movie> movies);
}
