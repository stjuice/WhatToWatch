using ImdbWatchlists.DependencyInjection;
using ImdbWatchlists.Options;
using Microsoft.EntityFrameworkCore;
using WhatToWatch.Data;
using WhatToWatch.Options;
using WhatToWatch.Repositories;
using WhatToWatch.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    // Capacitor Android WebView calls the Render API cross-origin (https://localhost).
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});
builder.Services.Configure<WhatToWatchOptions>(
    builder.Configuration.GetSection(WhatToWatchOptions.SectionName));

var whatToWatchOptions = builder.Configuration
    .GetSection(WhatToWatchOptions.SectionName)
    .Get<WhatToWatchOptions>() ?? new WhatToWatchOptions();

var imdbOptions = builder.Configuration
    .GetSection(ImdbWatchlistsOptions.SectionName)
    .Get<ImdbWatchlistsOptions>() ?? new ImdbWatchlistsOptions();

EnsureDirectoryForSqlite(whatToWatchOptions.ConnectionString);
EnsureDirectoryForFile(imdbOptions.StorageStatePath);
EnsureDirectoryForPath(imdbOptions.CacheDirectory);

builder.Services.AddDbContext<WhatToWatchDbContext>(options =>
    options.UseSqlite(whatToWatchOptions.ConnectionString));
builder.Services.AddScoped<IWatchlistRepository, SqliteWatchlistRepository>();
builder.Services.AddImdbWatchlists(builder.Configuration);
builder.Services.AddSingleton<IRandomizationService, RandomizationService>();
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<IWatchlistService, WatchlistService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WhatToWatchDbContext>();
    await db.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();
app.UseAuthorization();

app.MapGet("/health", async (WhatToWatchDbContext db) =>
{
    await db.Database.CanConnectAsync();
    return Results.Ok(new { status = "healthy" });
});

app.MapControllers();
app.MapFallbackToFile("index.html");
app.Run();

static void EnsureDirectoryForSqlite(string connectionString)
{
    const string prefix = "Data Source=";
    var start = connectionString.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
    if (start < 0)
        return;

    var path = connectionString[(start + prefix.Length)..].Trim().Trim('"');
    var separator = path.IndexOf(';');
    if (separator >= 0)
        path = path[..separator];

    EnsureDirectoryForFile(path);
}

static void EnsureDirectoryForFile(string? filePath)
{
    if (string.IsNullOrWhiteSpace(filePath))
        return;

    var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
    EnsureDirectoryForPath(directory);
}

static void EnsureDirectoryForPath(string? directory)
{
    if (string.IsNullOrWhiteSpace(directory))
        return;

    Directory.CreateDirectory(directory);
}
