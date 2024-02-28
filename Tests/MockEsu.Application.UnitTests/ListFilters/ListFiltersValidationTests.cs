using AutoMapper;
using MockEsu.Application.Common.Exceptions;
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
    public async Task Validate_ReturnsOk_WhenDefault()
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
    public async Task TestDtoValue_ReturnsListOfSingleStartingWithId10_WhenSkip10Take1()
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
            SomeInnerEntity = new() { Id = 110, NestedName = "NestedName110", Number = 165 },
            DateString = "11 января 2024 г.",
            SomeCount = 100,
            NestedThings = new List<TestNestedEntityDto>
            {
                new() {
                    Id = 10,
                    NestedName = $"NestedName10",
                    Number = 15
                },
                new() {
                    Id = 11,
                    NestedName = $"NestedName11",
                    Number = 16
                },
                new() {
                    Id = 12,
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

    [Fact]
    public async Task ValidateFilters_ReturnsCorrespondingError_WhenFilterWithoutExpression()
    {
        // Arrange
        var query = new TestKontragentsQuery { filters = ["id=1"] };

        var validator = new TestKontragentsQueryValidator(_mapper);

        // Act
        var validationResult = validator.Validate(query);

        // Assert
        Assert.NotNull(validationResult);
        Assert.False(validationResult.IsValid);
        Assert.True(validationResult.Errors.Count > 0);
        Assert.Equal(
            ValidationErrorCode.ExpressionIsUndefinedValidator.ToString(),
            validationResult.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task ValidateFilters_ReturnsCorrespondingError_WhenFilterWithNotExistingKey()
    {
        // Arrange
        var query = new TestKontragentsQuery { filters = ["index:1"] };

        var validator = new TestKontragentsQueryValidator(_mapper);

        // Act
        var validationResult = validator.Validate(query);

        // Assert
        Assert.NotNull(validationResult);
        Assert.False(validationResult.IsValid);
        Assert.True(validationResult.Errors.Count > 0);
        Assert.Equal(
            ValidationErrorCode.PropertyDoesNotExistValidator.ToString(),
            validationResult.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task ValidateFilters_ReturnsCorrespondingError_WhenFilterWithNotFilterableProperty()
    {
        // Arrange
        var query = new TestKontragentsQuery { filters = ["dateString:2024"] };

        var validator = new TestKontragentsQueryValidator(_mapper);

        // Act
        var validationResult = validator.Validate(query);

        // Assert
        Assert.NotNull(validationResult);
        Assert.False(validationResult.IsValid);
        Assert.True(validationResult.Errors.Count > 0);
        Assert.Equal(
            ValidationErrorCode.PropertyIsNotFilterableValidator.ToString(),
            validationResult.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task ValidateFilters_ReturnsCorrespondingError_WhenFilterWithNotValidValue()
    {
        // Arrange
        var query = new TestKontragentsQuery { filters = ["id:one"] };

        var validator = new TestKontragentsQueryValidator(_mapper);

        // Act
        var validationResult = validator.Validate(query);

        // Assert
        Assert.NotNull(validationResult);
        Assert.False(validationResult.IsValid);
        Assert.True(validationResult.Errors.Count > 0);
        Assert.Equal(
            ValidationErrorCode.CanNotCreateExpressionValidator.ToString(),
            validationResult.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task ValidateOrderBy_ReturnsCorrespondingError_WhenOrderByWithNotExistingKey()
    {
        // Arrange
        var query = new TestKontragentsQuery { orderBy = ["index"] };

        var validator = new TestKontragentsQueryValidator(_mapper);

        // Act
        var validationResult = validator.Validate(query);

        // Assert
        Assert.NotNull(validationResult);
        Assert.False(validationResult.IsValid);
        Assert.True(validationResult.Errors.Count > 0);
        Assert.Equal(
            ValidationErrorCode.PropertyDoesNotExistValidator.ToString(),
            validationResult.Errors[0].ErrorCode);
    }

    [Fact]
    public async Task ValidateOrderBy_ReturnsCorrespondingError_WhenOrderByWithNotValidExpression()
    {
        // Arrange
        var query = new TestKontragentsQuery { orderBy = ["id ascending"] };

        var validator = new TestKontragentsQueryValidator(_mapper);

        // Act
        var validationResult = validator.Validate(query);

        // Assert
        Assert.NotNull(validationResult);
        Assert.False(validationResult.IsValid);
        Assert.True(validationResult.Errors.Count > 0);
        Assert.Equal(
            ValidationErrorCode.ExpressionIsUndefinedValidator.ToString(),
            validationResult.Errors[0].ErrorCode);
    }

    ///TODO: написать сюда тесты с применением фильтров и прочего

    [Fact]
    public async Task TestFilters_ReturnsListOf1WithCorrespondingId_WhenFilterBySingleId()
    {
        // Arrange
        var query = new TestKontragentsQuery { filters = ["id:3"] };

        var validator = new TestKontragentsQueryValidator(_mapper);
        var handler = new TestKontragentsQueryHandler(_mapper);

        // Act
        var validationResult = validator.Validate(query);
        var result = await handler.Handle(query, default);

        // Assert
        Assert.NotNull(validationResult);
        Assert.True(validationResult.IsValid);
        Assert.NotNull(result?.Items?[0]);
        Assert.Equal(3, result?.Items?[0].Id);
    }

    [Fact]
    public async Task TestFilters_ReturnsListWithCorrespondingId_WhenFilterByRangeOfIds()
    {
        // Arrange
        var query = new TestKontragentsQuery { filters = ["id:3..7"] };

        var validator = new TestKontragentsQueryValidator(_mapper);
        var handler = new TestKontragentsQueryHandler(_mapper);

        // Act
        var validationResult = validator.Validate(query);
        var result = await handler.Handle(query, default);

        // Assert
        Assert.NotNull(validationResult);
        Assert.True(validationResult.IsValid);
        Assert.NotNull(result?.Items);
        Assert.DoesNotContain(result.Items, x => x.Id < 3 || x.Id > 7);
    }

    [Fact]
    public async Task TestFilters_ReturnsListOf0_WhenFilterOutOfRange()
    {
        // Arrange
        var query = new TestKontragentsQuery { filters = ["id:133"] };

        var validator = new TestKontragentsQueryValidator(_mapper);
        var handler = new TestKontragentsQueryHandler(_mapper);

        // Act
        var validationResult = validator.Validate(query);
        var result = await handler.Handle(query, default);

        // Assert
        Assert.NotNull(validationResult);
        Assert.True(validationResult.IsValid);
        Assert.NotNull(result?.Items);
        Assert.Equal(0, result.Items.Count());
    }

    [Fact]
    public async Task TestFilters_ReturnsListOfCorrespondingId_WhenFilterWithoutUpperLimit()
    {
        // Arrange
        var query = new TestKontragentsQuery { filters = ["id:3.."] };

        var validator = new TestKontragentsQueryValidator(_mapper);
        var handler = new TestKontragentsQueryHandler(_mapper);

        // Act
        var validationResult = validator.Validate(query);
        var result = await handler.Handle(query, default);

        // Assert
        Assert.NotNull(validationResult);
        Assert.True(validationResult.IsValid);
        Assert.NotNull(result?.Items);
        Assert.DoesNotContain(result.Items, x => x.Id < 3);
    }

    [Fact]
    public async Task TestFilters_ReturnsListOfCorrespondingId_WhenFilterWithoutBottomLimit()
    {
        // Arrange
        var query = new TestKontragentsQuery { filters = ["id:..7"] };

        var validator = new TestKontragentsQueryValidator(_mapper);
        var handler = new TestKontragentsQueryHandler(_mapper);

        // Act
        var validationResult = validator.Validate(query);
        var result = await handler.Handle(query, default);

        // Assert
        Assert.NotNull(validationResult);
        Assert.True(validationResult.IsValid);
        Assert.NotNull(result?.Items);
        Assert.DoesNotContain(result.Items, x => x.Id > 7);
    }

    [Fact]
    public async Task TestFilters_ReturnsList_WhenFilterSingleEntityById()
    {
        // Arrange
        var query = new TestKontragentsQuery { filters = ["someInnerEntity:107"] };

        var validator = new TestKontragentsQueryValidator(_mapper);
        var handler = new TestKontragentsQueryHandler(_mapper);

        // Act
        var validationResult = validator.Validate(query);
        var result = await handler.Handle(query, default);

        // Assert
        Assert.NotNull(validationResult);
        Assert.True(validationResult.IsValid);
        Assert.NotNull(result?.Items);
        Assert.Equal(107, result.Items?[0]?.SomeInnerEntity?.Id);
    }
}