using AutoMapper;
using MockEsu.Application.DTOs.Kontragents;
using Xunit;
using static MockEsu.Application.UnitTests.ListFilters.ListFiltersValidationTestsClass;

namespace MockEsu.Application.UnitTests.ListFilters;

public class ListFiltersValidationTests
{
    private readonly IMapper _mapper;

    public ListFiltersValidationTests()
    {
        var config = new MapperConfiguration(c =>
        {
            c.AddProfile<TestEntityDto.Mapping>();
            c.AddProfile<TestNestedEntityDto.Mapping>();
        });
        _mapper = config.CreateMapper();
    }

    [Fact]
    public async Task ValidateFilters_ReturnsOk_WhenDefault()
    {
        // Arrange
        var query = new TestKontragentsQuery();

        var validator = new TestKontragentsQueryValidator(_mapper);

        // Act
        var validationResult = validator.Validate(query);

        // Assert
        Assert.NotNull(validationResult);
        Assert.True(validationResult.IsValid);
        Assert.Equal(0, validationResult.Errors.Count);
    }

    [Fact]
    public async Task ValidateSkipValue_ReturnsError_WhenSkipIsBelow0()
    {
        // Arrange
        var query = new TestKontragentsQuery { skip = -1 };

        var validator = new TestKontragentsQueryValidator(_mapper);

        // Act
        var validationResult = validator.Validate(query);

        // Assert
        Assert.NotNull(validationResult);
        Assert.False(validationResult.IsValid);
        Assert.True(validationResult.Errors.Count > 0);
        Assert.Equal("GreaterThanOrEqualValidator", validationResult.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task ValidateTakeValue_ReturnsError_WhenTakeIsBelow0()
    {
        // Arrange
        var query = new TestKontragentsQuery { take = -1 };

        var validator = new TestKontragentsQueryValidator(_mapper);

        // Act
        var validationResult = validator.Validate(query);

        // Assert
        Assert.NotNull(validationResult);
        Assert.False(validationResult.IsValid);
        Assert.True(validationResult.Errors.Count > 0);
        Assert.Equal("GreaterThanOrEqualValidator", validationResult.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task ValidateSkipTakeValue_ReturnsListOf10StartingFromId10_WhenSkip10Take10()
    {
        // Arrange
        var query = new TestKontragentsQuery { skip = 10, take = 10 };

        var validator = new TestKontragentsQueryValidator(_mapper);
        var handler = new TestKontragentsQueryHandler(_mapper);

        // Act
        var validationResult = validator.Validate(query);
        var result = await handler.Handle(query, default);

        // Assert
        Assert.True(validationResult.IsValid);
        Assert.NotNull(result);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal(10, result.Items[0].Id);
    }

    [Fact]
    public async Task ValidateDtoValue_ReturnsListOfSingleStartingWithId10_WhenSkip10Take1()
    {
        // Arrange
        var query = new TestKontragentsQuery { skip = 10, take = 1 };

        var validator = new TestKontragentsQueryValidator(_mapper);
        var handler = new TestKontragentsQueryHandler(_mapper);

        var referenceDto = new TestEntityDto
        {
            Id = 10,
            EntityName = "Name10",
            OriginalDescription = "Description10",
            ReverseDescription = "01noitpircseD",
            SomeInnerEntity = new() { NestedName = "NestedName110", Number = 165 },
            DateString = "11 января 2024 г.",
            SomeCount = 100,
            NestedThings = new List<TestNestedEntityDto>
            {
                new() {
                    NestedName = $"NestedName10",
                    Number = 15
                },
                new() {
                    NestedName = $"NestedName11",
                    Number = 16
                },
                new() {
                    NestedName = $"NestedName12",
                    Number = 18
                }
            }
        };

        // Act
        var validationResult = validator.Validate(query);
        var result = await handler.Handle(query, default);

        // Assert
        Assert.True(validationResult.IsValid);
        Assert.NotNull(result);
        foreach (var prop in typeof(TestEntityDto).GetProperties())
        {
            object v1 = prop.GetValue(result.Items[0]);
            object v2 = prop.GetValue(referenceDto);
            Assert.Equal(v1, v2);
        }
    }
}