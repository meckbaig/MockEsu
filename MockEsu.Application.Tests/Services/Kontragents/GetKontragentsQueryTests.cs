using System.Reflection;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.Services.Kontragents;
using MockEsu.Infrastructure.Data;
using Moq;
using NSubstitute;

namespace MockEsu.Application.Tests.Services.Kontragents;

public class GetKontragentsQueryTests //: KontragentsApiFactory
{
    private readonly GetKontragentsQueryHandler _handler;

    public GetKontragentsQueryTests()
    {
        //_container.StartAsync().Wait();
        var dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(//_container.GetConnectionString())
                "Server=localhost;Port=5433;Database=MockEsu;User ID=postgres;Password=testtest;")
            .Options;
        IAppDbContext inMemoryContext = new AppDbContext(dbContextOptions, default);

        var config = new MapperConfiguration(c =>
            c.AddMaps(Assembly.GetAssembly(typeof(IAppDbContext))));
        var mapper = config.CreateMapper();
        var cache = Substitute.For<IDistributedCache>();

        _handler = new GetKontragentsQueryHandler(inMemoryContext, mapper, cache);
    }

    [Fact]
    public async Task GetKontragentsQuery_ReturnList_WhenNothingProvided()
    {
        // Arrange
        var command = new GetKontragentsQuery();

        // Act
        var result = await _handler.Handle(command, default);

        // Assert
        Assert.NotNull(result?.Items);
        Assert.Equal(result?.Items.Count, 13021);
    }
}