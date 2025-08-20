using System.Text;
using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Infrastructure.Authentication;
using Infrastructure.Authorization;
using Infrastructure.Database;
using Infrastructure.DomainEvents;
using Infrastructure.Time;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SharedKernel;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .AddServices()
            .AddDatabase(configuration)
            .AddHealthChecks(configuration)
            .AddAuthenticationInternal(configuration)
            .AddAuthorizationInternal();
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddTransient<IDomainEventsDispatcher, DomainEventsDispatcher>();

        // Register repositories
        services.AddScoped<Application.Abstractions.Repositories.IUserRepository, Repositories.UserRepository>();
        services.AddScoped<Application.Abstractions.Repositories.IMeetingUserRepository, Repositories.MeetingUserRepository>();
        services.AddScoped<Application.Abstractions.Repositories.IMeetingRepository, Repositories.MeetingRepository>();



        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        string? connectionString = configuration.GetConnectionString("Database");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                // Use in-memory database for development
                options.UseInMemoryDatabase("MeetingSchedulerDb");
            }
            else
            {
                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.Default);
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
                })
                .UseSnakeCaseNamingConvention()
                .EnableSensitiveDataLogging(false)
                .EnableServiceProviderCaching()
                .EnableDetailedErrors(false);
            }
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        return services;
    }

    private static IServiceCollection AddHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        IHealthChecksBuilder healthChecksBuilder = services.AddHealthChecks();

        string? connectionString = configuration.GetConnectionString("Database");
        if (!string.IsNullOrEmpty(connectionString))
        {
            healthChecksBuilder.AddNpgSql(connectionString, name: "meeting-scheduler-db");
        }
        else
        {
            // Add a simple health check for in-memory database
            healthChecksBuilder.AddCheck("meeting-scheduler-inmemory", static () =>
                Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("In-memory database is available"));
        }

        return services;
    }

    private static IServiceCollection AddAuthenticationInternal(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.RequireHttpsMetadata = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!)),
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenProvider, TokenProvider>();

        return services;
    }

    private static IServiceCollection AddAuthorizationInternal(this IServiceCollection services)
    {
        services.AddAuthorization();

        services.AddScoped<PermissionProvider>();

        services.AddTransient<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddTransient<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();

        return services;
    }
}
