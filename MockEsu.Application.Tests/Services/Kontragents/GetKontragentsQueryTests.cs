using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.DTOs.Kontragents;
using MockEsu.Application.Extensions.DataBaseProvider;
using MockEsu.Application.Services.Kontragents;
using MockEsu.Domain.Entities;
using MockEsu.Infrastructure.Data;
using MockEsu.Infrastructure.Interceptors;
using Moq;
using NSubstitute;
using System.Reflection;

namespace MockEsu.Application.Tests.Services.Kontragents;

public class GetKontragentsQueryTests //: KontragentsApiFactory
{
    IAppDbContext _inMemoryContext;
    IMapper _mapper;
    IDistributedCache _cache;

    public GetKontragentsQueryTests()
    {
        //_container.StartAsync().Wait();
        var dbContextOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(//_container.GetConnectionString())
                "Server=localhost;Port=5433;Database=MockEsu;User ID=postgres;Password=testtest;")
            .Options;
        _inMemoryContext = new AppDbContext(dbContextOptions, Substitute.For<ILogger<TransactionLoggingInterceptor>>());

        var config = new MapperConfiguration(c =>
            c.AddMaps(Assembly.GetAssembly(typeof(IAppDbContext))));
        _mapper = config.CreateMapper();
        _cache = Substitute.For<IDistributedCache>();
    }

    //[Fact]
    //public async Task GetKontragentsQuery_ReturnList_WhenNothingProvided()
    //{
    //    // Arrange
    //    var query = new GetKontragentsQuery();

    //    // Act
    //    var result = await _handler.Handle(query, default);

    //    // Assert
    //    Assert.NotNull(result?.Items);
    //    Assert.Equal(result?.Items.Count, 13021);
    //}


    [Fact]
    public async Task GetKontragentsQuery_ReturnSingle_WhenTake1_DtoCheck()
    {
        // Arrange
        List<Kontragent> kontragents = new List<Kontragent>()
        {
            new()
            {
                Id = 5,
                Address = new()
                {
                    City = new() { Name = "������" },
                    Street = new() { Name = "���-������" },
                    HouseName = "101",
                    PorchNumber = 1,
                    Apartment = "4",
                    Region = new() { Name = "������������� �����" }
                },
                PhoneNumber = "",
                KontragentAgreement = new()
                {
                    Balance = -620.00M,
                    DocumentNumber = "0041807",
                    PersonalAccount = "5379687",
                    ContractDate = DateOnly.FromDayNumber(733923)
                },
                Name = "������������"
            }
        };
        KonragentPreviewDto previewDto = new()
        {
            AddressString = "�. ������, ��. ���-������, �. 101, ���. 1, ��. 4",
            Balance = -620.00M,
            ContractDate = DateOnly.FromDayNumber(733923),
            DocumentNumber = "0041807",
            Id = 5,
            Name = "������������",
            PersonalAccount = "5379687",
            PhoneNumber = "",
            RegionString = "������������� �����"
        };

        //var contextMock = new Mock<IAppDbContext>();
        //contextMock.Setup(x => x.Kontragents).Returns(kontragents);

        var handler = new GetKontragentsQueryHandler(_inMemoryContext, _mapper, _cache);
        var query = new GetKontragentsQuery { take = 1 };

        // Act
        var result = await handler.Handle(query, default);

        // Assert
        Assert.NotNull(result?.Items);
        Assert.Equal(result?.Items.Count, 1);

        foreach (var prop in typeof(KonragentPreviewDto).GetProperties())
        {
            object v1 = prop.GetValue(previewDto);
            object v2 = prop.GetValue(result.Items[0]);
            Assert.Equal(v1, v2);
        }
    }
}