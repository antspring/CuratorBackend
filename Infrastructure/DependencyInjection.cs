using Application.Accounts;
using Application.Catalogs;
using Application.Mentees;
using Application.Mentors;
using Application.Sessions;
using Infrastructure.Authentication;
using Infrastructure.Files;
using Infrastructure.Identity;
using Infrastructure.Mentees;
using Infrastructure.Mentors;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Seeding;
using Infrastructure.Profiles;
using Infrastructure.Registration;
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

        services.AddOptions<SmtpOptions>().Bind(configuration.GetSection("Smtp"))
            .ValidateDataAnnotations()
            .Validate(options => string.IsNullOrWhiteSpace(options.Username) == string.IsNullOrWhiteSpace(options.Password),
                "Укажите оба параметра SMTP-аутентификации.")
            .Validate(options => string.IsNullOrEmpty(options.Username) || options.StartTls,
                "SMTP-аутентификация требует STARTTLS.")
            .ValidateOnStart();
        services.AddSingleton<VerificationCodes>();
        services.AddHostedService<RegistrationMailWorker>();
        services.AddScoped<IRegistrationStore, RegistrationStore>();
        services.AddScoped<IAccountStore, AccountStore>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<CatalogReader>();
        services.AddScoped<ICatalogReader>(provider => provider.GetRequiredService<CatalogReader>());
        services.AddScoped<ICatalogLookup>(provider => provider.GetRequiredService<CatalogReader>());
        services.AddScoped<PersonalProfileStore>();
        services.AddScoped<IMenteeProfileStore, MenteeProfileStore>();
        services.AddScoped<IMentorProfileStore, MentorProfileStore>();

        services.AddJwtAuthentication(configuration);
        services.AddFileStorage(configuration);

        services.AddHealthChecks()
            .AddNpgSql(connectionString, name: "postgresql");

        return services;
    }
}
