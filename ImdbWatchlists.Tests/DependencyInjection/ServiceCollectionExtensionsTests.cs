using ImdbWatchlists.Browser;
using ImdbWatchlists.DependencyInjection;
using ImdbWatchlists.Providers;
using ImdbWatchlists.Repositories;
using ImdbWatchlists.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ImdbWatchlists.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public async Task AddImdbWatchlists_RegistersCoreAbstractions()
    {
        var services = new ServiceCollection();
        services.AddImdbWatchlists(options =>
        {
            options.CacheDirectory = Path.Combine(
                Path.GetTempPath(),
                $"whattowatch-di-{Guid.NewGuid():N}");
        });

        await using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IImdbWatchlists>());
        Assert.NotNull(provider.GetService<IBrowserManager>());
        Assert.NotNull(provider.GetService<WatchlistService>());
        Assert.IsType<JsonWatchlistRepository>(provider.GetService<IWatchlistRepository>());

        var providers = provider.GetServices<IWatchlistProvider>().ToList();
        Assert.Contains(providers, p => p is PlaywrightPublicWatchlistProvider);
        Assert.Contains(providers, p => p is PrivateWatchlistProvider);
    }
}
