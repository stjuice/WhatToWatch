using Microsoft.Playwright;

namespace ImdbWatchlists.Extraction;

public interface IImdbPageExtractor
{
    Task<ExtractedWatchlistPage> ExtractCurrentPageAsync(
        IPage page,
        CancellationToken cancellationToken = default);
}
