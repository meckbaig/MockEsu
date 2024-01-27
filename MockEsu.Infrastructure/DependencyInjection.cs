using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Infrastructure.Interceptors;
using MockEsu.Infrastructure.Data;
using MockEsu.Infrastructure.Authentification;
using Microsoft.AspNetCore.Identity;
using MockEsu.Domain.Entities;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());
        services.AddSingleton<IJwtProvider, JwtProvider>();
        services.AddTransient<IPasswordHasher<User>, PasswordHasher<User>>();

        //services.AddScoped<AppDbContextInitialiser>();

        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
