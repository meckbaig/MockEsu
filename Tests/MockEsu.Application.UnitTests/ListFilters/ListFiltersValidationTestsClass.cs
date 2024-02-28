using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using MockEsu.Application.Common.Attributes;
using MockEsu.Application.Common.BaseRequests.ListQuery;
using MockEsu.Application.Common.Dtos;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.DTOs.Kontragents;
using MockEsu.Application.Extensions.ListFilters;
using MockEsu.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace MockEsu.Application.UnitTests.ListFilters;

public static class ListFiltersValidationTestsClass
{
    public record TestKontragentsQuery : BaseListQuery<TestKontragentsResponse>
    {

    }

    public class TestKontragentsResponse : BaseListQueryResponse<TestEntityDto>
    {

    }

    public class TestKontragentsQueryValidator : BaseListQueryValidator
        <TestKontragentsQuery, TestKontragentsResponse, TestEntityDto, TestEntity>
    {
        public TestKontragentsQueryValidator(IMapper mapper) : base(mapper)
        {

        }
    }

    public class TestKontragentsQueryHandler : IRequestHandler<TestKontragentsQuery, TestKontragentsResponse>
    {
        private readonly IMapper _mapper;

        public TestKontragentsQueryHandler(IMapper mapper)
        {
            _mapper = mapper;
        }

        public async Task<TestKontragentsResponse> Handle(
            TestKontragentsQuery request,
            CancellationToken cancellationToken)
        {
            int total = request.skip + (request.take != 0 ? request.take : 10);
            var result = SeedTestEntities(total)
                .AddFilters(request.GetFilterExpressions())
                .AddOrderBy(request.GetOrderExpressions())
                .Skip(request.skip).Take(request.take > 0 ? request.take : int.MaxValue)
                .ProjectTo<TestEntityDto>(_mapper.ConfigurationProvider)
                .ToList();
            return new TestKontragentsResponse { Items = result };
        }
    }

    public class TestEntity : BaseEntity
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public DateOnly Date { get; set; }

        public int SomeCount { get; set; }

        public HashSet<TestNestedEntity> TestNestedEntities { get; set; }

        [ForeignKey(nameof(InnerEntityId))]
        public TestNestedEntity InnerEntity { get; set; }

        public int InnerEntityId { get; set; }
    }

    public class TestNestedEntity : BaseEntity
    {
        public string Name { get; set; }

        public double Number { get; set; }
    }

    public record TestEntityDto : IBaseDto
    {
        [Filterable(CompareMethod.Equals)]
        public int Id { get; set; }

        public string EntityName { get; set; }

        public string OriginalDescription { get; set; }

        public string ReverseDescription { get; set; }

        public string DateString { get; set; }

        [Filterable(CompareMethod.Equals)]
        public int SomeCount { get; set; }

        [Filterable(CompareMethod.Nested)]
        public List<TestNestedEntityDto> NestedThings { get; set; }

        [Filterable(CompareMethod.ById)]
        public TestNestedEntityDto SomeInnerEntity { get; set; }

        public static Type GetOriginType()
        {
            return typeof(TestEntity);
        }

        public class Mapping : Profile
        {
            public Mapping()
            {
                CreateMap<TestEntity, TestEntityDto>()
                    .ForMember(m => m.EntityName, opt => opt.MapFrom(o => o.Name))
                    .ForMember(m => m.OriginalDescription, opt => opt.MapFrom(o => o.Description))
                    .ForMember(m => m.ReverseDescription, opt => opt.MapFrom(o => string.Concat(o.Description.ToCharArray().Reverse())))
                    .ForMember(m => m.DateString, opt => opt.MapFrom(o => o.Date.ToLongDateString()))
                    .ForMember(m => m.SomeCount, opt => opt.MapFrom(o => o.SomeCount))
                    .ForMember(m => m.NestedThings, opt => opt.MapFrom(o => o.TestNestedEntities))
                    .ForMember(m => m.SomeInnerEntity, opt => opt.MapFrom(o => o.InnerEntity));
            }
        }
    }

    public record TestNestedEntityDto : IBaseDto
    {
        public int Id { get; set; }

        public string NestedName { get; set; }

        [Filterable(CompareMethod.Equals)]
        public int Number { get; set; }

        public static Type GetOriginType()
        {
            return typeof(TestNestedEntity);
        }

        public class Mapping : Profile
        {
            public Mapping()
            {
                CreateMap<TestNestedEntity, TestNestedEntityDto>()
                    .ForMember(m => m.Id, opt => opt.MapFrom(o => o.Id))
                    .ForMember(m => m.NestedName, opt => opt.MapFrom(o => o.Name))
                    .ForMember(m => m.Number, opt => opt.MapFrom(o => (int)Math.Round(o.Number)));
            }
        }
    }

    public static IQueryable<TestEntity> SeedTestEntities(int count = 10)
    {
        List<TestEntity> entities = new List<TestEntity>();

        int itemsCount = count != 0 ? count : 10;
        for (int i = 0; i < itemsCount; i++)
        {
            var entity = new TestEntity
            {
                Id = i,
                Name = $"Name{i}",
                Description = $"Description{i}",
                Date = DateOnly.FromDateTime(new DateTime(2024, 1, 1).AddDays(i)),
                SomeCount = i * 10,
                TestNestedEntities = new HashSet<TestNestedEntity>
                {
                    new() {
                        Id = i,
                        Name = $"NestedName{i}",
                        Number = i * 1.5
                    },
                    new() {
                        Id = i+1,
                        Name = $"NestedName{i+1}",
                        Number = (i+1) * 1.5
                    },
                    new() {
                        Id = i+2,
                        Name = $"NestedName{i+2}",
                        Number = (i+2) * 1.5
                    }
                },
                InnerEntity = new()
                {
                    Id = 100+i,
                    Name = $"NestedName{100+i}",
                    Number = (100+i) * 1.5
                },
                InnerEntityId = 100 + i
            };

            entities.Add(entity);
        }

        return entities.AsQueryable();
    }
}
