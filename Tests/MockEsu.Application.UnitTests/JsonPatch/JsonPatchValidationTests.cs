using AutoMapper;
using DotNet.Testcontainers.Builders;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;
using MockEsu.Application.Services.Tariffs;
using Renci.SshNet;
using Testcontainers.PostgreSql;
using Xunit;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using static MockEsu.Application.UnitTests.ValidationTestsEntites;

namespace MockEsu.Application.UnitTests.JsonPatch;

public class JsonPatchValidationTests
{
    protected readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .WithDatabase("MockEsu")
        .WithUsername("postgres")
        .WithPassword("testtest")
        .WithPortBinding(5555, 5432)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
        .Build();
    private readonly IMapper _mapper;

    public JsonPatchValidationTests()
    {
        var config = new MapperConfiguration(c =>
        {
            c.AddProfile<TestEntityDto.Mapping>();
            c.AddProfile<TestNestedEntityDto.Mapping>();
            //c.AddProfile<TestEntityEditDto.Mapping>();
            //c.AddProfile<TestNestedEntityEditDto.Mapping>();
        });
        _mapper = config.CreateMapper();
    }

    private async Task<TestDbContext> CreateAndSeedContext()
    {
        await _container.StartAsync();
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        optionsBuilder.UseNpgsql(_container.GetConnectionString());
        var context = new TestDbContext(optionsBuilder.Options);
        TestMigration(context);
        return context;
    }

    [Fact]
    public async Task Validate_ReturnsOk_WhenDefault()
    {
        // Arrange
        using TestDbContext context = await CreateAndSeedContext();

        var command = new TestJsonPatchCommand 
        { 
            Patch = new JsonPatchDocument<TestEntityEditDto>()
        };

        var validator = new TestJsonPatchTariffsCommandValidator(_mapper);
        var handler = new TestJsonPatchCommandHandler(context, _mapper);

        // Act
        //var validationResult = validator.Validate(command);
        var result = await handler.Handle(command, default);

        // Assert
        //Assert.NotNull(validationResult);
        //Assert.True(validationResult.IsValid);
        //Assert.Equal(0, validationResult.Errors.Count);

        //await _container.StopAsync();
    }
}
