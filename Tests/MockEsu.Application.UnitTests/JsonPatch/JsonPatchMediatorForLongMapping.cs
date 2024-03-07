using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.JsonPatch;
using MockEsu.Application.Common.BaseRequests;
using MockEsu.Application.Common.BaseRequests.JsonPatchCommand;
using MockEsu.Application.UnitTests;
using MockEsu.Application.Extensions.JsonPatch;
using static MockEsu.Application.UnitTests.ValidationTestsEntites;
using Microsoft.EntityFrameworkCore;
using AutoMapper.QueryableExtensions;

namespace MockEsu.Application.Services.Tariffs;

public record TestJsonPatchLongMappingCommand : BaseJsonPatchCommand<TestJsonPatchLongMappingResponse, TestEditDtoWithLongNameMapping>
{
    public override JsonPatchDocument<TestEditDtoWithLongNameMapping> Patch { get; set; }
}

public class TestJsonPatchLongMappingResponse : BaseResponse
{
    public List<TestEntityDto> TestEntities { get; set; }
}

public class TestJsonPatchLongMappingCommandValidator : BaseJsonPatchValidator
    <TestJsonPatchLongMappingCommand, TestJsonPatchLongMappingResponse, TestEditDtoWithLongNameMapping>
{
    public TestJsonPatchLongMappingCommandValidator(IMapper mapper) : base(mapper) { }
}

public class TestJsonPatchLongMappingCommandHandler : IRequestHandler<TestJsonPatchLongMappingCommand, TestJsonPatchLongMappingResponse>
{
    private readonly TestDbContext _context;
    private readonly IMapper _mapper;

    public TestJsonPatchLongMappingCommandHandler(TestDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<TestJsonPatchLongMappingResponse> Handle(TestJsonPatchLongMappingCommand request, CancellationToken cancellationToken)
    {
        request.Patch.ApplyDtoTransactionToSource(_context.TestEntities, _mapper.ConfigurationProvider);

        var entities = _context.TestEntities
            .Include(e => e.InnerEntity)
            .Include(e => e.TestNestedEntities)
            .AsNoTracking()
            .ProjectTo<TestEntityDto>(_mapper.ConfigurationProvider)
            .ToList();

        return new TestJsonPatchLongMappingResponse { TestEntities = entities };
    }
}