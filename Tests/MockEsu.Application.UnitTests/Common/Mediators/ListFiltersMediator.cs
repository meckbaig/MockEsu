using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using MockEsu.Application.Common.BaseRequests.ListQuery;
using MockEsu.Application.Extensions.ListFilters;
using MockEsu.Application.UnitTests.Common.DTOs;
using MockEsu.Application.UnitTests.Common.Entities;
using static MockEsu.Application.UnitTests.Common.ValidationTestsEntites;

namespace MockEsu.Application.UnitTests.Common.Mediators;


public record TestQuery : BaseListQuery<TestResponse>
{

}

public class TestResponse : BaseListQueryResponse<TestEntityDto>
{

}

public class TestQueryValidator : BaseListQueryValidator
    <TestQuery, TestResponse, TestEntityDto, TestEntity>
{
    public TestQueryValidator(IMapper mapper) : base(mapper)
    {

    }
}

public class TestQueryHandler : IRequestHandler<TestQuery, TestResponse>
{
    private readonly IMapper _mapper;

    public TestQueryHandler(IMapper mapper)
    {
        _mapper = mapper;
    }

    public async Task<TestResponse> Handle(
        TestQuery request,
        CancellationToken cancellationToken)
    {
        int total = request.skip + (request.take != 0 ? request.take : 10);
        var result = SeedTestEntities(total)
            .AddFilters(request.GetFilterExpressions())
            .AddOrderBy(request.GetOrderExpressions())
            .Skip(request.skip).Take(request.take > 0 ? request.take : int.MaxValue)
            .ProjectTo<TestEntityDto>(_mapper.ConfigurationProvider)
            .ToList();
        return new TestResponse { Items = result };
    }
}
