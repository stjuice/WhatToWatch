using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ImdbWatchlists.Models;
using ImdbWatchlists.Options;
using Microsoft.Extensions.Options;

namespace ImdbWatchlists.Repositories;

public sealed class JsonWatchlistRepository(IOptions<ImdbWatchlistsOptions> options)
    : IWatchlistRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly string _cacheDirectory = options.Value.CacheDirectory;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<Watchlist?> GetAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var path = GetPath(id);
        if (!File.Exists(path))
        {
            return null;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<Watchlist>(
            stream,
            SerializerOptions,
            cancellationToken);
    }

    public async Task<IReadOnlyCollection<Watchlist>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_cacheDirectory);

        var watchlists = new List<Watchlist>();
        foreach (var path in Directory.EnumerateFiles(_cacheDirectory, "*.json"))
        {
            cancellationToken.ThrowIfCancellationRequested();

            await using var stream = File.OpenRead(path);
            var watchlist = await JsonSerializer.DeserializeAsync<Watchlist>(
                stream,
                SerializerOptions,
                cancellationToken);

            if (watchlist is not null)
            {
                watchlists.Add(watchlist);
            }
        }

        return watchlists;
    }

    public async Task SaveAsync(
        Watchlist watchlist,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(watchlist);
        ArgumentException.ThrowIfNullOrWhiteSpace(watchlist.Id);

        Directory.CreateDirectory(_cacheDirectory);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var path = GetPath(watchlist.Id);
            var temporaryPath = path + ".tmp";

            await using (var stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    watchlist,
                    SerializerOptions,
                    cancellationToken);
            }

            File.Move(temporaryPath, path, overwrite: true);
        }
        finally
        {
            _gate.Release();
        }
    }

    private string GetPath(string id)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safeId = string.Concat(id.Select(character =>
            invalid.Contains(character) ? '_' : character));

        return Path.Combine(_cacheDirectory, safeId + ".json");
    }
}
