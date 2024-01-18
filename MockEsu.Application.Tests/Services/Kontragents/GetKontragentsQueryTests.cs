using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.DTOs.Kontragents;
using MockEsu.Application.Services.Kontragents;
using MockEsu.Infrastructure.Data;

namespace MockEsu.Application.Tests.Services.Kontragents
{
    public class GetKontragentsQueryTests //: KontragentsApiFactory
    {
        private readonly IAppDbContext _inMemoryContext;

        public GetKontragentsQueryTests()
        {
            //_container.StartAsync().Wait();
            var dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(connectionString: //_container.GetConnectionString())
                    "Server=localhost;Port=5433;Database=MockEsu;User ID=postgres;Password=testtest;")
                .Options;
            _inMemoryContext = new AppDbContext(dbContextOptions);
        }


        [Fact]
        public async Task GetKontragentsQuery_ReturnList_WhenNothingProvided()
        {
            // Arrange
            var config = new MapperConfiguration(c => c.AddProfile(KonragentPreviewDto.GetMapping()));
            var mapper = config.CreateMapper();

            var command = new GetKontragentsQuery();
            var handler = new GetKontragentsQueryHandler(_inMemoryContext, mapper);

            // Act
            var result = await handler.Handle(command, default);

            //Assert
            Assert.NotNull(result?.Items);
            Assert.Equal(result?.Items.Count, 13021);
        }
    }
}