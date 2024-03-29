using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using MockEsu.Application.Common.Interfaces;

namespace MockEsu.Application.UnitTests.Caching;

internal static class CachedTestsHelper
{
    public static IDistributedCache GetInMemoryCache()
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.GetRequiredService<IDistributedCache>();
    }

    //public static ICachedKeysProvider GetCachedKeysProvider()
    //{
    //}
}
