namespace WhatToWatch.Options;

public class WhatToWatchOptions
{
    public const string SectionName = "WhatToWatch";

    public string ConnectionString { get; set; } = "Data Source=whattowatch.db";

    /// <summary>
    /// Shared secret for studio / Android write operations. Sent as <c>X-Admin-Key</c>.
    /// </summary>
    public string AdminApiKey { get; set; } = string.Empty;
}
