using ImdbWatchlists.DependencyInjection;
using ImdbWatchlists.Providers;
using ImdbWatchlists.Repositories;
using ImdbWatchlists.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ImdbWatchlists.Tests.DependencyInjection;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddImdbWatchlists_RegistersCoreAbstractions()
    {
        var services = new ServiceCollection();
        services.AddImdbWatchlists(options =>
        {
            options.ConnectionString = "Data Source=:memory:";
        });

        using var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IImdbWatchlists>());
        Assert.NotNull(provider.GetService<WatchlistService>());
        Assert.NotNull(provider.GetService<IWatchlistRepository>());

        var providers = provider.GetServices<IWatchlistProvider>().ToList();
        Assert.Contains(providers, p => p is PublicWatchlistProvider);
        Assert.Contains(providers, p => p is PrivateWatchlistProvider);
    }
}
