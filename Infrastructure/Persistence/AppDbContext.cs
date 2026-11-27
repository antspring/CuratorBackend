using Domain.Catalogs;
using Domain.Files;
using Domain.Mentees;
using Domain.Mentors;
using Domain.Mentorships;
using Domain.Users;
using Infrastructure.Authentication;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Skill> Skills => Set<Skill>();

    public DbSet<City> Cities => Set<City>();

    public DbSet<CatalogItem> CatalogItems => Set<CatalogItem>();

    public DbSet<StoredFile> Files => Set<StoredFile>();

    public DbSet<MenteeProfile> MenteeProfiles => Set<MenteeProfile>();

    public DbSet<MentorProfile> MentorProfiles => Set<MentorProfile>();

    public DbSet<MentorshipRequest> MentorshipRequests => Set<MentorshipRequest>();

    public DbSet<Mentorship> Mentorships => Set<Mentorship>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
