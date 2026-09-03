using ImdbWatchlists.Models;

namespace WhatToWatch.Services;

public sealed record MovieReference(string WatchlistId, Movie Movie);
