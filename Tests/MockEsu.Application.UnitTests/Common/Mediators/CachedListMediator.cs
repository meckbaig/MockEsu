using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MockEsu.Application.Common.BaseRequests.ListQuery;
using MockEsu.Application.Common.Extensions.Caching;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.UnitTests.Common.DTOs;
using MockEsu.Application.UnitTests.Common.Entities;

namespace MockEsu.Application.UnitTests.Common.Mediators;


public record CachedListTestQuery : BaseListQuery<CachedListTestResponse>
{
    public override int skip { get; set; }
    public override int take { get; set; }
    public bool useCaching { get; set; }
}

public class CachedListTestResponse : BaseListQueryResponse<TestEntityDto>
{

}

public class CachedListTestQueryValidator : BaseListQueryValidator
    <CachedListTestQuery, CachedListTestResponse, TestEntityDto, TestEntity>
{
    public CachedListTestQueryValidator(IMapper mapper) : base(mapper)
    {

    }
}

public class CachedListTestQueryHandler : IRequestHandler<CachedListTestQuery, CachedListTestResponse>
{
    private readonly TestDbContext _context;
    private readonly IMapper _mapper;
    private readonly IDistributedCache _cache;
    private readonly ICachedKeysProvider _cachedKeysProvider;

    public CachedListTestQueryHandler(TestDbContext context, IMapper mapper, IDistributedCache cache, ICachedKeysProvider cachedKeysProvider)
    {
        _context = context;
        _mapper = mapper;
        _cache = cache;
        _cachedKeysProvider = cachedKeysProvider;
    }

    public async Task<CachedListTestResponse> Handle(
        CachedListTestQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.TestEntities
            .Skip(request.skip)
            .Take(request.take > 0 ? request.take : int.MaxValue);

        var projection = (List<TestEntity> entities)
                => entities.Select(x => _mapper.Map<TestEntityDto>(x)).ToList();

        List<TestEntityDto> result;
        if (request.useCaching)
        {
            result = await _cache.GetOrCreateAsync(
                _cachedKeysProvider,
                request.GetKey(),
                () => query.ToListAsync(),
                projection,
                cancellationToken);
        }
        else
        {
            result = projection.Invoke(query.ToList());
        }

        return new CachedListTestResponse { Items = result };
    }
}
