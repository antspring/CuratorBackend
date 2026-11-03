using Infrastructure.Persistence;
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
            .UseSnakeCaseNamingConvention());

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgresql");

        return services;
    }
}
