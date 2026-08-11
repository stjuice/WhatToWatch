using ImdbWatchlists.Options;
using ImdbWatchlists.Providers;
using ImdbWatchlists.Repositories;
using ImdbWatchlists.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddHttpClient<PublicWatchlistProvider>();
        services.AddTransient<IWatchlistProvider>(sp =>
            sp.GetRequiredService<PublicWatchlistProvider>());

        services.AddSingleton<PrivateWatchlistProvider>();
        services.AddTransient<IWatchlistProvider>(sp =>
            sp.GetRequiredService<PrivateWatchlistProvider>());

        services.AddScoped<WatchlistService>();
        services.AddScoped<IImdbWatchlists, ImdbWatchlistsClient>();
        services.AddScoped<IWatchlistRepository, SqliteWatchlistRepository>();

        return services;
    }
}
