using AutoMapper;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Application.Services.Kontragents;

namespace MockEsu.Application.Tests.Services.Kontragents
{
    public class GetKontragentsQueryTests
    {
        // private readonly IAppDbContext _context = Substitute.For<IAppDbContext>();
        // private readonly IMapper _mapper = Substitute.For<IMapper>();

        [Fact]
        public async Task GetKontragentsQuery_ReturnList_WhenNothingProvided()
        {
            // // Arrange
            // var command = new GetKontragentsQuery();
            // var handler = new GetKontragentsQueryHandler(_context, _mapper);
            //
            // // Act
            // GetKontragentsResponse result = await handler.Handle(command, default);
            //
            // //Assert
            // Assert.NotNull(result);
        }
    }
}