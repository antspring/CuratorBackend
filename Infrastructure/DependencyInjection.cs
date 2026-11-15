using Application.Accounts;
using Application.Catalogs;
using Application.Sessions;
using Infrastructure.Authentication;
using Infrastructure.Files;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Seeding;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    private const string DatabaseConnectionStringName = "Database";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(DatabaseConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Не задана строка подключения 'ConnectionStrings:{DatabaseConnectionStringName}'. " +
                "Локально задайте её через dotnet user-secrets, в Docker — через .env.");

        services.AddDbContext<AppDbContext>(options => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .UseSeeding(CatalogSeeder.Seed)
            .UseAsyncSeeding(CatalogSeeder.SeedAsync));

        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = string.Empty;

                options.Password.RequiredLength = 12;
                options.Password.RequiredUniqueChars = 1;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>();

        services.AddScoped<IAccountStore, AccountStore>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICatalogReader, CatalogReader>();

        services.AddJwtAuthentication(configuration);
        services.AddFileStorage(configuration);

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgresql");

        return services;
    }
}
