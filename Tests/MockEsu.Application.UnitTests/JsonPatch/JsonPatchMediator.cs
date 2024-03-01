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

public record TestJsonPatchCommand : BaseJsonPatchCommand<TestJsonPatchResponse, TestEntityEditDto>
{
    public override JsonPatchDocument<TestEntityEditDto> Patch { get; set; }
}

public class TestJsonPatchResponse : BaseResponse
{
    public List<TestEntityDto> TestEntities { get; set; }
}

public class TestJsonPatchTariffsCommandValidator : BaseJsonPatchValidator
    <TestJsonPatchCommand, TestJsonPatchResponse, TestEntityEditDto>
{
    public TestJsonPatchTariffsCommandValidator(IMapper mapper) : base(mapper) { }
}

public class TestJsonPatchCommandHandler : IRequestHandler<TestJsonPatchCommand, TestJsonPatchResponse>
{
    private readonly TestDbContext _context;
    private readonly IMapper _mapper;

    public TestJsonPatchCommandHandler(TestDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<TestJsonPatchResponse> Handle(TestJsonPatchCommand request, CancellationToken cancellationToken)
    {
        request.Patch.ApplyDtoTransactionToSource(_context.TestEntities, _mapper.ConfigurationProvider);

        var entities = _context.TestEntities
            .Include(e => e.InnerEntity)
            .Include(e => e.TestNestedEntities)
            .AsNoTracking()
            .ProjectTo<TestEntityDto>(_mapper.ConfigurationProvider)
            .ToList();

        return new TestJsonPatchResponse { TestEntities = entities };
    }
}