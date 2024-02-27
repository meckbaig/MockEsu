using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MockEsu.Infrastructure.Data;

namespace MockEsu.Application.IntegrationTests;

internal class MockesuWebApplicationFactory : WebApplicationFactory<Program>
{
    const string TestConnectionString = "Server=localhost;Port=5433;Database=MockEsu;User ID=postgres;Password=testtest;";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
                options.UseNpgsql(TestConnectionString);
            });
        });

        base.ConfigureWebHost(builder);
    }
}
