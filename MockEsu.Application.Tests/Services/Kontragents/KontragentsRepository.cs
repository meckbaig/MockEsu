using MockEsu.Application.Common.Interfaces;
using MockEsu.Domain.Entities;
using Moq;

namespace MockEsu.Application.Tests.Services.Kontragents;

public static class KontragentsRepository
{
    public static Mock<IAppDbContext> GetKontragentsRepository()
    {
        City city = new City() { Name = "Алматы" };
        var kontragents = new List<Kontragent>
        {
           new Kontragent()
           {
               Address = new Address()
               {
                   Apartment = "1",
                   City = city,
                   HouseName = "1B",
                   PorchNumber = 1,
                   Region = new Region()
                   {
                       Name = "Алаутский"
                   },
                   Street = new Street()
                   {
                       City = city,
                       Name = "Толе би"
                   }
               }
           }
        };

        var mockRepo = new Mock<IAppDbContext>();
        return mockRepo;
    }
}