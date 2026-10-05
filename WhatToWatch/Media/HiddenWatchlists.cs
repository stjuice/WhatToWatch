namespace WhatToWatch.Media;

public static class HiddenWatchlists
{
    public const string SpookieNightId = "ls4117970169";

    public static bool IsHidden(string id) =>
        id.Equals(SpookieNightId, StringComparison.OrdinalIgnoreCase);
}
