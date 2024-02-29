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
        TestKontragentsResponse result = null;
        if (validationResult.IsValid)
            result = await handler.Handle(query, default);

        // Assert
        Assert.True(validationResult.IsValid);
        Assert.NotNull(result);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal(10, result.Items[0].Id);
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

    [Fact]
    public async Task TestOrderBy_ReturnsError_WhenOrderByDateStringWhichIsNotSimpleMapping()
    {
        // Arrange
        var query = new TestKontragentsQuery { orderBy = ["dateString"] };

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
}