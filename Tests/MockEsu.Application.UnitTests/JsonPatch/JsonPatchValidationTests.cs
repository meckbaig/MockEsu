using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using MockEsu.Application.Services.Tariffs;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;
using static MockEsu.Application.UnitTests.ValidationTestsEntites;

namespace MockEsu.Application.UnitTests.JsonPatch;

public class JsonPatchValidationTests
{
    private readonly IMapper _mapper;

    public JsonPatchValidationTests()
    {
        var config = new MapperConfiguration(c =>
        {
            c.AddProfile<TestEntityDto.Mapping>();
            c.AddProfile<TestNestedEntityDto.Mapping>();
            c.AddProfile<TestEntityEditDto.Mapping>();
            c.AddProfile<TestNestedEntityEditDto.Mapping>();
            c.AddProfile<TestEditDtoWithLongNameMapping.Mapping>();
        });
        _mapper = config.CreateMapper();
    }

    [Fact]
    public async Task Validate_ReturnsOk_WhenDefault()
    {
        // Arrange
        var command = new TestJsonPatchCommand
        {
            Patch = new JsonPatchDocument<TestEntityEditDto>()
        };

        var validator = new TestJsonPatchCommandValidator(_mapper);

        // Act
        var validationResult = validator.Validate(command);

        // Assert
        Assert.NotNull(validationResult);
        Assert.True(validationResult.IsValid);
    }

    [Fact]
    public async Task ValidateReplace_ReturnsOk_WhenStringProperty()
    {
        // Arrange
        string newEntityName = "NewValue1";

        List<Operation<TestEntityEditDto>> operations = new()
        {
            new Operation<TestEntityEditDto>
            {
                op = "replace",
                path = "/1/entityName",
                value = newEntityName
            }
        };

        var command = new TestJsonPatchCommand
        {
            Patch = new JsonPatchDocument<TestEntityEditDto>(
                operations,
                new CamelCasePropertyNamesContractResolver())
        };

        var validator = new TestJsonPatchCommandValidator(_mapper);

        // Act
        var validationResult = validator.Validate(command);

        // Assert
        Assert.NotNull(validationResult);
        Assert.True(validationResult.IsValid);
    }

    [Fact]
    public async Task ValidateReplace_ReturnsOk_WhenNestedStringPropertyInModel()
    {
        // Arrange
        string newEntityName = "NewValue1";

        List<Operation<TestEntityEditDto>> operations = new()
        {
            new Operation<TestEntityEditDto>
            {
                op = "replace",
                path = "/1/someInnerEntity/nestedName",
                value = newEntityName
            }
        };

        var command = new TestJsonPatchCommand
        {
            Patch = new JsonPatchDocument<TestEntityEditDto>(
                operations,
                new CamelCasePropertyNamesContractResolver())
        };

        var validator = new TestJsonPatchCommandValidator(_mapper);

        // Act
        var validationResult = validator.Validate(command);

        // Assert
        Assert.NotNull(validationResult);
        Assert.True(validationResult.IsValid);
    }

    [Fact]
    public async Task ValidateReplace_ReturnsOk_WhenNestedStringPropertyInCollection()
    {
        // Arrange
        string newEntityName = "NewValue1";

        List<Operation<TestEntityEditDto>> operations = new()
        {
            new Operation<TestEntityEditDto>
            {
                op = "replace",
                path = "/1/nestedThings/2/nestedName",
                value = newEntityName
            }
        };

        var command = new TestJsonPatchCommand
        {
            Patch = new JsonPatchDocument<TestEntityEditDto>(
                operations,
                new CamelCasePropertyNamesContractResolver())
        };

        var validator = new TestJsonPatchCommandValidator(_mapper);

        // Act
        var validationResult = validator.Validate(command);

        // Assert
        Assert.NotNull(validationResult);
        Assert.True(validationResult.IsValid);
    }

    [Fact]
    public async Task ValidateReplace_ReturnsOk_WhenPropertyWithComplexMapping()
    {
        // Arrange
        string newDate = "31 декабря 2020 г.";

        List<Operation<TestEntityEditDto>> operations = new()
        {
            new Operation<TestEntityEditDto>
            {
                op = "replace",
                path = "/1/dateString",
                value = newDate
            }
        };

        var command = new TestJsonPatchCommand
        {
            Patch = new JsonPatchDocument<TestEntityEditDto>(
                operations,
                new CamelCasePropertyNamesContractResolver())
        };

        var validator = new TestJsonPatchCommandValidator(_mapper);

        // Act
        var validationResult = validator.Validate(command);

        // Assert
        Assert.NotNull(validationResult);
        Assert.True(validationResult.IsValid);
    }

    [Fact]
    public async Task ValidateReplace_ReturnsOk_WhenPropertyWithModelId()
    {
        // Arrange
        List<Operation<TestEntityEditDto>> operations = new()
        {
            new Operation<TestEntityEditDto>
            {
                op = "replace",
                path = "/1/someInnerEntityId",
                value = 2
            }
        };

        var command = new TestJsonPatchCommand
        {
            Patch = new JsonPatchDocument<TestEntityEditDto>(
                operations,
                new CamelCasePropertyNamesContractResolver())
        };

        var validator = new TestJsonPatchCommandValidator(_mapper);

        // Act
        var validationResult = validator.Validate(command);

        // Assert
        Assert.NotNull(validationResult);
        Assert.True(validationResult.IsValid);
    }

    [Fact]
    public async Task ValidateReplace_ReturnsOk_WhenPropertyWithLongMapping()
    {
        // Arrange
        string newName = "NewNestedEntityNameValue1";

        List<Operation<TestEditDtoWithLongNameMapping>> operations = new()
        {
            new Operation<TestEditDtoWithLongNameMapping>
            {
                op = "replace",
                path = "/1/nestedName",
                value = newName
            }
        };

        var command = new TestJsonPatchLongMappingCommand
        {
            Patch = new JsonPatchDocument<TestEditDtoWithLongNameMapping>(
                operations,
                new CamelCasePropertyNamesContractResolver())
        };

        var validator = new TestJsonPatchLongMappingCommandValidator(_mapper);

        // Act
        var validationResult = validator.Validate(command);

        // Assert
        Assert.NotNull(validationResult);
        Assert.True(validationResult.IsValid);
    }

    [Fact]
    public async Task ValidateAdd_ReturnsOk_WhenModelIntoCollectionWithManyToMany()
    {
        // Arrange
        var newModel = new TestNestedEntityEditDto
        {
            Id = 4,
            NestedName = "qqqqqqqqqqqq",
            Number = 111111
        };

        List<Operation<TestEntityEditDto>> operations = new()
        {
            new Operation<TestEntityEditDto>
            {
                op = "add",
                path = "/1/nestedThings/-",
                value = newModel
            }
        };


        var command = new TestJsonPatchCommand
        {
            Patch = new JsonPatchDocument<TestEntityEditDto>(
                operations,
                new CamelCasePropertyNamesContractResolver())
        };

        var validator = new TestJsonPatchCommandValidator(_mapper);

        // Act
        var validationResult = validator.Validate(command);

        // Assert
        Assert.NotNull(validationResult);
        Assert.True(validationResult.IsValid);
    }

    [Fact]
    public async Task ValidateAdd_ReturnsOk_WhenModelIntoDB()
    {
        // Arrange
        var nestedThings = new List<TestNestedEntityEditDto>
        {
            new () { Id = 4, NestedName = "12343567", Number = 12344 },
            new () { Id = 6, NestedName = "12343567", Number = 12344  },
            new () { Id = 8, NestedName = "12343567", Number = 12344  }
        };

        var newModel = new TestEntityEditDto
        {
            Id = 11,
            EntityName = "NewAddedTestEntity For ValidateAdd_ReturnsOk_WhenModelIntoDB",
            OriginalDescription = "New Description",
            DateString = new DateOnly(2022, 1, 2).ToLongDateString(),
            SomeInnerEntityId = 10,
            NestedThings = nestedThings
        };

        List<Operation<TestEntityEditDto>> operations = new()
        {
            new Operation<TestEntityEditDto>
            {
                op = "add",
                path = "/-",
                value = newModel
            }
        };

        var command = new TestJsonPatchCommand
        {
            Patch = new JsonPatchDocument<TestEntityEditDto>(
                operations,
                new CamelCasePropertyNamesContractResolver())
        };

        var validator = new TestJsonPatchCommandValidator(_mapper);

        // Act
        var validationResult = validator.Validate(command);

        // Assert
        Assert.NotNull(validationResult);
        Assert.True(validationResult.IsValid);
    }
}