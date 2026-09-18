namespace WhatToWatch.Options;

public class WhatToWatchOptions
{
    public const string SectionName = "WhatToWatch";

    public string ConnectionString { get; set; } = "Data Source=whattowatch.db";

    public PartyOptions Party { get; set; } = new();

    /// <summary>
    /// Shared secret for studio / Android write operations. Sent as <c>X-Admin-Key</c>.
    /// </summary>
    public string AdminApiKey { get; set; } = string.Empty;
}

public class PartyOptions
{
    public int BatchSize { get; set; } = 5;

    public int InactivityTimeoutMinutes { get; set; } = 120;

    public int FinishedRetentionHours { get; set; } = 24;

    public int PresenceWindowSeconds { get; set; } = 90;

    public int JoinCodeLength { get; set; } = 3;
}
