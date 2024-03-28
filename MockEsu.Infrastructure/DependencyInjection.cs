using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using MockEsu.Application.Common.Interfaces;
using MockEsu.Domain.Entities.Authentification;
using MockEsu.Infrastructure.AsyncMessaging;
using MockEsu.Infrastructure.Authentification;
using MockEsu.Infrastructure.Caching;
using MockEsu.Infrastructure.Data;
using MockEsu.Infrastructure.Interceptors;

namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();
        services.AddScoped<TransactionLoggingInterceptor>();

        string cachedKeysConnectionString = configuration.GetConnectionString("CachedKeysConnection");
        services.AddDbContext<CachedKeysContext>((sp, options) =>
        {
            options.UseNpgsql(cachedKeysConnectionString);
        });
        services.AddScoped<ICachedKeysContext>(provider => provider.GetRequiredService<CachedKeysContext>());

        string defaultConnectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.AddInterceptors(sp.GetServices<TransactionLoggingInterceptor>());
            options.UseNpgsql(defaultConnectionString);
        });
        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        services.AddStackExchangeRedisCache(options =>
            options.Configuration = configuration.GetConnectionString("Redis"));
        services.AddSingleton<IJwtProvider, JwtProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
        services.AddTransient<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<ICachedKeysProvider, AsyncSqlCachedKeysProvider>();
        services.AddMassTransit(configurator =>
        {
            configurator.SetKebabCaseEndpointNameFormatter();

            configurator.AddConsumer<SqlCachedKeysCachingHandler>();

            configurator.UsingRabbitMq((context, config) =>
            {
                config.Host(new Uri(configuration.GetConnectionString("RabbitMQ")), h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });
                config.ConfigureEndpoints(context);
            });
        });

        using (var scope = services.BuildServiceProvider())
        {
            var context = scope.GetRequiredService(typeof(IAppDbContext)) as IAppDbContext;
            context.ConfigurePermissions();
        }

        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
