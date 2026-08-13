using ImdbWatchlists.Browser;
using ImdbWatchlists.Options;
using ImdbWatchlists.Providers;
using ImdbWatchlists.Repositories;
using ImdbWatchlists.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Playwright;

namespace ImdbWatchlists.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddImdbWatchlists(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<ImdbWatchlistsOptions>(
            configuration.GetSection(ImdbWatchlistsOptions.SectionName));

        return RegisterCore(services);
    }

    public static IServiceCollection AddImdbWatchlists(
        this IServiceCollection services,
        Action<ImdbWatchlistsOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.Configure(configure);

        return RegisterCore(services);
    }

    private static IServiceCollection RegisterCore(IServiceCollection services)
    {
        services.TryAddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton(_ => Playwright.CreateAsync().GetAwaiter().GetResult());
        services.AddSingleton<IBrowserManager, PlaywrightBrowserManager>();
        services.AddSingleton<PlaywrightPublicWatchlistProvider>();
        services.AddSingleton<IWatchlistProvider>(sp =>
            sp.GetRequiredService<PlaywrightPublicWatchlistProvider>());

        services.AddSingleton<PrivateWatchlistProvider>();
        services.AddSingleton<IWatchlistProvider>(sp =>
            sp.GetRequiredService<PrivateWatchlistProvider>());

        services.AddScoped<WatchlistService>();
        services.AddScoped<IImdbWatchlists, ImdbWatchlistsClient>();
        services.AddSingleton<IWatchlistRepository, JsonWatchlistRepository>();

        return services;
    }
}
