using AutoMapper;
using Microsoft.Extensions.Caching.Distributed;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.UnitTests.Common.DTOs;
using MockEsu.Application.UnitTests.JsonPatch;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace MockEsu.Application.UnitTests.Caching;

public class CachingTests : TestWithContainer
{
    private readonly IMapper _mapper;
    private readonly IDistributedCache _cache;
    private readonly ICachedKeysProvider _cachedKeysProvider;
    private readonly Newtonsoft.Json.JsonSerializer _jsonSerializer;

    public CachingTests()
    {

        var config = new MapperConfiguration(c =>
        {
            c.AddProfile<TestEntityDto.Mapping>();
            c.AddProfile<TestNestedEntityDto.Mapping>();
            c.AddProfile<TestEntityEditDto.Mapping>();
            c.AddProfile<TestNestedEntityEditDto.Mapping>();
            c.AddProfile<TestEditDtoWithLongNameMapping.Mapping>();
        });
        _mapper = config.CreateMapper();
        _cache = CachedTestsHelper.GetInMemoryCache();
        if (Context == null)
            throw new Exception("Context is null");
        _jsonSerializer = new Newtonsoft.Json.JsonSerializer() { ContractResolver = new CamelCasePropertyNamesContractResolver() };
    }


    //[Fact]
    //public async Task TestCaching_ReturnsWithoutCaching_WhenWithoutCaching()
    //{

    //}
}
