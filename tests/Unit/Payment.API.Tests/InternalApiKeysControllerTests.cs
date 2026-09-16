using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Payment.API.Configuration;
using Payment.API.Controllers;
using Payment.API.Middlewares;
using Payment.Application.DTOs;

namespace Payment.API.Tests;

public class InternalApiKeysControllerTests
{
    [Fact]
    public void Purge_RemovesMerchantEntriesAndReportsCount()
    {
        var cache = new ApiKeyResolutionCache(
            new MemoryCache(new MemoryCacheOptions()),
            Mock.Of<IOptionsMonitor<ApiKeyAuthOptions>>(o =>
                o.CurrentValue == new ApiKeyAuthOptions { CacheTtlSeconds = 120 }));
        var merchantId = Guid.NewGuid();
        var key = ApiKeyResolutionCache.BuildCacheKey("sk_test_", "sk_test_purge-me");
        cache.Set(key, new MerchantKeyResolution(merchantId, "Active", "test"));
        var controller = new InternalApiKeysController(cache, NullLogger<InternalApiKeysController>.Instance);

        var result = controller.Purge(new InternalApiKeysController.PurgeApiKeyCacheRequest(merchantId));

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, ok.StatusCode);
        Assert.False(cache.TryGet(key, out _));
    }
}
