using ImdbWatchlists.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using WhatToWatch.Data;
using WhatToWatch.Options;
using WhatToWatch.Repositories;
using WhatToWatch.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<WhatToWatchOptions>(
    builder.Configuration.GetSection(WhatToWatchOptions.SectionName));

var whatToWatchOptions = builder.Configuration
    .GetSection(WhatToWatchOptions.SectionName)
    .Get<WhatToWatchOptions>() ?? new WhatToWatchOptions();

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
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
