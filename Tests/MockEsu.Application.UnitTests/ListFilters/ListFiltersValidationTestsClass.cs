using AutoMapper;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using MockEsu.Application.Common.BaseRequests.ListQuery;
using MockEsu.Application.Common.Dtos;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private readonly IAppDbContext _context;
        private readonly IMapper _mapper;
        private readonly IDistributedCache _cache;

        public TestKontragentsQueryHandler(IAppDbContext context, IMapper mapper, IDistributedCache cache)
        {
            _context = context;
            _mapper = mapper;
            _cache = cache;
        }

        public async Task<TestKontragentsResponse> Handle(
            TestKontragentsQuery request,
            CancellationToken cancellationToken)
        {
            return default;
        }
    }

    public class TestEntity : BaseEntity
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public DateOnly Date { get; set; }

        public int SomeCount { get; set; }

        public HashSet<TestNestedEntity> TestNestedEntities { get; set; }
    }

    public class TestNestedEntity : BaseEntity
    {
        public string Name { get; set; }

        public double Number { get; set; }
    }

    public record TestEntityDto : IBaseDto
    {
        public string EntityName { get; set; }

        public string OriginalDescription { get; set; }

        public string ReverseDescription { get; set; }

        public string DateString { get; set; }

        public string SomeCountString { get; set; }

        public List<TestNestedEntityDto> NestedThings { get; set; }

        public static Type GetOriginType()
        {
            return typeof(TestEntity);
        }

        private class Mapping : Profile
        {
            public Mapping()
            {
                CreateMap<TestEntity, TestEntityDto>()
                    .ForMember(m => m.EntityName, opt => opt.MapFrom(o => o.Name))
                    .ForMember(m => m.OriginalDescription, opt => opt.MapFrom(o => o.Description))
                    .ForMember(m => m.ReverseDescription, opt => opt.MapFrom(o => o.Description.ToCharArray().Reverse()))
                    .ForMember(m => m.DateString, opt => opt.MapFrom(o => o.Date.ToLongDateString()))
                    .ForMember(m => m.SomeCountString, opt => opt.MapFrom(o => o.SomeCount))
                    .ForMember(m => m.NestedThings, opt => opt.MapFrom(o => o.TestNestedEntities));
            }
        }
    }

    public record TestNestedEntityDto : IBaseDto
    {
        public string NestedName { get; set; }

        public int Number { get; set; }

        public static Type GetOriginType()
        {
            return typeof(TestNestedEntity);
        }

        private class Mapping : Profile
        {
            public Mapping()
            {
                CreateMap<TestNestedEntity, TestNestedEntityDto>()
                    .ForMember(m => m.NestedName, opt => opt.MapFrom(o => o.Name))
                    .ForMember(m => m.Number, opt => opt.MapFrom(o => o.Number));
            }
        }
    }

    public static List<TestEntity> SeedTestEntities(int count = 10)
    {
        List<TestEntity> entities = new List<TestEntity>();

        for (int i = 0; i < count; i++)
        {
            var entity = new TestEntity
            {
                Id = i,
                Name = $"Name{i}",
                Description = $"Description{i}",
                Date = DateOnly.FromDateTime(new DateTime(2024, 1, 0).AddDays(i)),
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
                }
            };

            entities.Add(entity);
        }

        return entities;
    }
}
