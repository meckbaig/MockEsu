using AutoMapper;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using MockEsu.Application.Common.BaseRequests.ListQuery;
using MockEsu.Application.Common.Dtos;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.DTOs.Kontragents;
using MockEsu.Domain.Common;
using MockEsu.Domain.Entities;
using System.Reflection;
using Xunit;
using static MockEsu.Application.UnitTests.ListFilters.ListFiltersValidationTestsClass;

namespace MockEsu.Application.UnitTests.ListFilters;

public class ListFiltersValidationTests
{
    private readonly IMapper _mapper;

    public ListFiltersValidationTests()
    {
        var config = new MapperConfiguration(c =>
            c.AddMaps(Assembly.GetAssembly(typeof(IAppDbContext))));
        _mapper = config.CreateMapper();
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
    public async Task ValidateSkipValue_ReturnsError_WhenTakeIsBelow0()
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
    public async Task ValidateSkipValue_ReturnsOk_WhenTakeAndSkipDefault()
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


}