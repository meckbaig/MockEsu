using AutoMapper;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using MockEsu.Application.Common.Attributes;
using MockEsu.Application.Common.Dtos;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
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

        [DatabaseRelation(Domain.Enums.Relation.ManyToMany)]
        public HashSet<TestNestedEntity> TestNestedEntities { get; set; }

        [ForeignKey(nameof(InnerEntityId))]
        public TestNestedEntity InnerEntity { get; set; }

        public int InnerEntityId { get; set; }
    }

    public class TestNestedEntity : BaseEntity
    {
        public string Name { get; set; }

        public double Number { get; set; }

        [DatabaseRelation(Domain.Enums.Relation.ManyToMany)]
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
        public List<TestNestedEntityEditDto> NestedThings { get; set; }
        public TestNestedEntityEditDto SomeInnerEntity { get; set; }
        public int SomeInnerEntityId { get; set; }

        public static Type GetOriginType()
        {
            return typeof(TestEntity);
        }

        public static Type GetValidatorType()
        {
            return typeof(Validator);
        }

        public class Mapping : Profile
        {
            public Mapping()
            {
                CreateMap<TestEntityEditDto, TestEntity>()
                    .ForMember(m => m.Name, opt => opt.MapFrom(o => o.EntityName))
                    .ForMember(m => m.Description, opt => opt.MapFrom(o => o.OriginalDescription))
                    .ForMember(m => m.Date, opt => opt.MapFrom(o => ConvertToDateOnly(o.DateString)))
                    .ForMember(m => m.TestNestedEntities, opt => opt.MapFrom(o => o.NestedThings))
                    .ForMember(m => m.InnerEntity, opt => opt.MapFrom(o => o.SomeInnerEntity))
                    .ForMember(m => m.InnerEntityId, opt => opt.MapFrom(o => o.SomeInnerEntityId));
            }

            private DateOnly ConvertToDateOnly(string dateString)
            {
                //CultureInfo provider = CultureInfo.GetCultureInfo("ru-RU");
                DateTime dateTime = DateTime.Parse(dateString);
                DateOnly dateOnly = DateOnly.FromDateTime(dateTime);
                return dateOnly;
            }
        }

        internal class Validator : AbstractValidator<TestEntityEditDto>
        {
            public Validator()
            {
                RuleFor(x => x.EntityName)
                    .NotEmpty()
                    .MinimumLength(2)
                    .MaximumLength(100);
                RuleFor(x => x.OriginalDescription)
                    .NotEmpty()
                    .MinimumLength(2)
                    .MaximumLength(100);
                RuleFor(x => x.DateString)
                    .NotEmpty()
                    .Must(BeValidDateString);
                RuleFor(x => x.SomeInnerEntity)
                    .SetValidator(new TestNestedEntityEditDto.Validator());
                RuleForEach(x => x.NestedThings)
                    .SetValidator(new TestNestedEntityEditDto.Validator());
            }

            private bool BeValidDateString(string dateString)
            {
                return DateTime.TryParse(dateString, out var _);
            }
        }
    }

    public record TestNestedEntityEditDto : IEditDto
    {
        public int Id { get; set; }

        public string NestedName { get; set; }

        public int Number { get; set; }

        public static Type GetOriginType()
        {
            return typeof(TestNestedEntity);
        }

        public static Type GetValidatorType()
        {
            return typeof(Validator);
        }

        public class Mapping : Profile
        {
            public Mapping()
            {
                CreateMap<TestNestedEntityEditDto, TestNestedEntity>()
                    .ForMember(m => m.Id, opt => opt.MapFrom(o => o.Id))
                    .ForMember(m => m.Name, opt => opt.MapFrom(o => o.NestedName))
                    .ForMember(m => m.Number, opt => opt.MapFrom(o => o.Number));
            }
        }

        internal class Validator : AbstractValidator<TestNestedEntityEditDto>
        {
            public Validator()
            {
                RuleFor(x => x.Id)
                    .GreaterThan(0);
                RuleFor(x => x.NestedName)
                    .NotEmpty()
                    .MinimumLength(2)
                    .MaximumLength(100);
                RuleFor(x => x.Number)
                    .NotNull();
            }
        }
    }

    public record TestEditDtoWithLongNameMapping : IEditDto
    {
        public int NestedId { get; set; }
        public string NestedName { get; set; }

        public static Type GetOriginType()
        {
            return typeof(TestEntity);
        }

        public static Type GetValidatorType()
        {
            return typeof(Validator);
        }

        public class Mapping : Profile
        {
            public Mapping()
            {
                CreateMap<TestEditDtoWithLongNameMapping, TestEntity>()
                    .ForMember(m => m.InnerEntityId, opt => opt.MapFrom(o => o.NestedId))
                    .ForPath(m => m.InnerEntity.Name, opt => opt.MapFrom(o => o.NestedName));
            }
        }

        internal class Validator : AbstractValidator<TestEditDtoWithLongNameMapping>
        {
            public Validator()
            {
                RuleFor(x => x.NestedName)
                    .NotEmpty()
                    .MinimumLength(2)
                    .MaximumLength(100);
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