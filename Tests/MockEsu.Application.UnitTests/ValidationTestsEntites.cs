using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MockEsu.Application.Common.Attributes;
using MockEsu.Application.Common.BaseRequests.ListQuery;
using MockEsu.Application.Common.Dtos;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.DTOs.Kontragents;
using MockEsu.Application.Extensions.ListFilters;
using MockEsu.Domain.Common;
using MockEsu.Domain.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static MockEsu.Application.UnitTests.ValidationTestsEntites;

namespace MockEsu.Application.UnitTests;

public static class ValidationTestsEntites
{
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

        public HashSet<TestEntity> TestEntities { get; set; }
    }

    public class TestAndNested
    {
        [Required]
        [ForeignKey(nameof(TestEntity))]
        public int TestEntityId { get; set; }

        public TestEntity TestEntity { get; set; }

        [Required]
        [ForeignKey(nameof(TestNestedEntity))]
        public int TestNestedEntityId { get; set; }

        public TestNestedEntity TestNestedEntity { get; set; }
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
        [Filterable(CompareMethod.Equals)]
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

    public record TestEntityEditDto : IEditDto
    {
        public string EntityName { get; set; }
        public string OriginalDescription { get; set; }
        public string DateString { get; set; }
        public List<TestNestedEntityDto> NestedThings { get; set; }
        public TestNestedEntityDto SomeInnerEntity { get; set; }

        public static Type GetOriginType()
        {
            return typeof(TestEntity);
        }

        public static Type GetValidatorType()
        {
            throw new NotImplementedException();
        }

        /// TODO: ДОПИСАТЬ
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
                Date = DateOnly.FromDateTime(new DateTime(2024, 1, 1).AddDays(-i)),
                SomeCount = 100 - i * 10,
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
                    Id = 100 + i,
                    Name = $"NestedName{100 + i}",
                    Number = (100 + i) * 1.5
                },
                InnerEntityId = 100 + i
            };

            entities.Add(entity);
        }

        return entities.AsQueryable();
    }

    public static List<TestEntity> GetTestEntities(int count = 10)
    {
        List<TestEntity> entities = new List<TestEntity>();

        int itemsCount = count != 0 ? count : 10;
        for (int i = 1; i <= itemsCount; i++)
        {
            var entity = new TestEntity
            {
                Id = i,
                Name = $"Name{i}",
                Description = $"Description{i}",
                Date = DateOnly.FromDateTime(new DateTime(2024, 1, 1).AddDays(-i)),
                SomeCount = 100 - i * 10,
                TestNestedEntities = new HashSet<TestNestedEntity>(),
                InnerEntityId = 100 + i
            };

            entities.Add(entity);
        }

        return entities;
    }

    public static HashSet<TestNestedEntity> GetTestNestedEntities(int count = 10)
    {
        HashSet<TestNestedEntity> entities = new HashSet<TestNestedEntity>();

        int itemsCount = count != 0 ? count : 10;
        for (int i = 1; i <= itemsCount + 2; i++)
        {
            var entity = new TestNestedEntity
            {
                Id = i,
                Name = $"NestedName{i}",
                Number = i * 1.5
            };
            entities.Add(entity);

            if (!entities.Any(x => x.Id == 100 + i))
            {
                var entity100 = new TestNestedEntity
                {
                    Id = 100 + i,
                    Name = $"NestedName{100 + i}",
                    Number = (100 + i) * 1.5
                };
                entities.Add(entity100);
            }
        }
        return entities;
    }

    public static List<TestAndNested> GetTestAndNestedEntities(int count = 10)
    {
        List<TestAndNested> entities = new List<TestAndNested>();

        int itemsCount = count != 0 ? count : 10;
        for (int i = 1; i <= itemsCount; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                var entity = new TestAndNested
                {
                    TestEntityId = i,
                    TestNestedEntityId = i+j,
                };

                entities.Add(entity);
            }
        }

        return entities;
    }

    internal static void TestMigration(TestDbContext context, int count = 10)
    {
        try
        {
            var TestNestedEntities = GetTestNestedEntities(count);
            var TestEntities = GetTestEntities(count);
            var TestAndNesteds = GetTestAndNestedEntities(count);

            InitScript(context);
            context.TestNestedEntities.AddRange(TestNestedEntities);
            context.TestEntities.AddRange(TestEntities);
            context.TestAndNesteds.AddRange(TestAndNesteds);
            context.SaveChanges();

        }
        catch (Exception ex)
        {

            throw;
        }
    }

    private static void InitScript(TestDbContext context)
    {
        string script = File.ReadAllText(@"..\..\..\dump-unit-test.sql");
        
        context.Database.ExecuteSqlRaw(script);
    }
}


public class TestDbContext : DbContext, IDbContext
{
    public DbSet<TestEntity> TestEntities { get; set; }
    public DbSet<TestNestedEntity> TestNestedEntities { get; set; }
    public DbSet<TestAndNested> TestAndNesteds { get; set; }
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options)
    { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TestEntity>()
            .HasMany(x => x.TestNestedEntities)
            .WithMany(x => x.TestEntities)
            .UsingEntity<TestAndNested>();
        modelBuilder.Entity<TestEntity>()
            .HasOne(t => t.InnerEntity)
            .WithMany()
            .HasForeignKey(t => t.InnerEntityId);
        base.OnModelCreating(modelBuilder);
    }
}