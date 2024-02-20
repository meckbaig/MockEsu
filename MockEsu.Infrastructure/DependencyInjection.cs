using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Domain.Entities.Authentification;
using MockEsu.Infrastructure.Authentification;
using MockEsu.Infrastructure.Data;
using MockEsu.Infrastructure.Interceptors;

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
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
        services.AddTransient<IPasswordHasher<User>, PasswordHasher<User>>();

        //services.AddScoped<AppDbContextInitialiser>();

        using (var scope = services.BuildServiceProvider())
        {
            var context = scope.GetRequiredService(typeof(IAppDbContext)) as IAppDbContext;
            context.ConfigurePermissions();
        }

        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
