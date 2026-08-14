using Microsoft.Extensions.Configuration;
using WhatToWatch.Auth;

namespace WhatToWatch.Tests.Auth;

public class AdminApiKeyTests
{
    [Fact]
    public void Resolve_PrefersAdminApiKeyEnvironmentVariable()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ADMIN_API_KEY"] = "from-env",
                ["WhatToWatch:AdminApiKey"] = "from-section",
            })
            .Build();

        Assert.Equal("from-env", AdminApiKey.Resolve(configuration));
    }

    [Fact]
    public void Resolve_UsesSectionWhenEnvironmentVariableMissing()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["WhatToWatch:AdminApiKey"] = "  section-key  ",
            })
            .Build();

        Assert.Equal("section-key", AdminApiKey.Resolve(configuration));
    }

    [Fact]
    public void Resolve_ReturnsNull_WhenOnlyBlankValuesExist()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ADMIN_API_KEY"] = "   ",
                ["WhatToWatch:AdminApiKey"] = "",
            })
            .Build();

        Assert.Null(AdminApiKey.Resolve(configuration));
    }
}
