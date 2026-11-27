using Application.Accounts;
using Application.Catalogs;
using Application.Files;
using Application.Mentees;
using Application.Mentors;
using Application.Mentorships;
using Application.Profiles;
using Application.Sessions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<AccountService>();
        services.AddScoped<SessionService>();
        services.AddScoped<CatalogService>();
        services.AddScoped<FileService>();
        services.AddScoped<ProfileInputValidator>();
        services.AddScoped<MenteeProfileService>();
        services.AddScoped<MentorProfileService>();
        services.AddScoped<MentorCardService>();
        services.AddScoped<MentorshipService>();

        return services;
    }
}
