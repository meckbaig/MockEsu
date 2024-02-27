using MockEsu.Application.Services.Kontragents;
using MockEsu.Domain.Enums;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MockEsu.Application.IntegrationTests.Controllers.Kontragents
{
    public class GetListTest
    {
        [Fact]
        public async Task GetList_ReturnsListOf10_WhenTake10()
        {
            // Arrange
            var app = new MockesuWebApplicationFactory();

            var client = app.CreateClient();

            // Act
            client.DefaultRequestHeaders.Authorization
                = new AuthenticationHeaderValue("Bearer", TestAuth.GetToken(Permission.ReadMember));

            var response = await client.GetAsync("api/v1/Kontragents/Get?take=10");

            // Assert
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<GetKontragentsResponse>();
            Assert.Equal(content.Items.Count, 10);
        }

        [Fact]
        public async Task GetList_ReturnsListOfKontragentsWithPositiveBalanse_WhenFilterBalanseGreaterThanOrEquals0()
        {
            // Arrange
            var app = new MockesuWebApplicationFactory();

            var client = app.CreateClient();

            // Act
            client.DefaultRequestHeaders.Authorization
                = new AuthenticationHeaderValue("Bearer", TestAuth.GetToken(Permission.ReadMember));

            var response = await client.GetAsync("api/v1/Kontragents/Get?filters=balance:0..");

            // Assert
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<GetKontragentsResponse>();
            Assert.True(!content.Items.Any(k => k.Balance < 0));
        }

        [Fact]
        public async Task GetList_ReturnsListOfSingle_WhenFilterIdEquals10()
        {
            // Arrange
            var app = new MockesuWebApplicationFactory();

            var client = app.CreateClient();

            // Act
            client.DefaultRequestHeaders.Authorization
                = new AuthenticationHeaderValue("Bearer", TestAuth.GetToken(Permission.ReadMember));

            var response = await client.GetAsync("api/v1/Kontragents/Get?filters=id:10");

            // Assert
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadFromJsonAsync<GetKontragentsResponse>();
            Assert.True(content.Items.Count() == 1);
            Assert.True(content.Items[0].Id == 10);
        }

        [Fact]
        public async Task GetList_Returns401_WhenAuthorizationIsMissing()
        {
            // Arrange
            var app = new MockesuWebApplicationFactory();

            var client = app.CreateClient();

            // Act
            var response = await client.GetAsync("api/v1/Kontragents/Get");

            // Assert
            Assert.True(response.StatusCode == System.Net.HttpStatusCode.Unauthorized);
        }
    }
}