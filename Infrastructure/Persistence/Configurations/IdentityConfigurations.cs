using Domain.Users;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("users");

        builder.HasIndex(user => user.NormalizedUserName)
            .HasDatabaseName("ix_users_normalized_user_name")
            .IsUnique();

        builder.HasIndex(user => user.NormalizedEmail)
            .HasDatabaseName("ix_users_normalized_email")
            .IsUnique();
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    private static readonly IdentityRole<Guid>[] SeedRoles =
    [
        CreateRole("0b6b4f0e-5f1a-4c2e-9d3a-1f0c6a7e2b01", Roles.Mentee),
        CreateRole("0b6b4f0e-5f1a-4c2e-9d3a-1f0c6a7e2b02", Roles.Mentor),
        CreateRole("0b6b4f0e-5f1a-4c2e-9d3a-1f0c6a7e2b03", Roles.Curator),
        CreateRole("0b6b4f0e-5f1a-4c2e-9d3a-1f0c6a7e2b04", Roles.Administrator),
    ];

    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> builder)
    {
        builder.ToTable("roles");

        builder.HasIndex(role => role.NormalizedName)
            .HasDatabaseName("ix_roles_normalized_name")
            .IsUnique();

        builder.HasData(SeedRoles);
    }

    private static IdentityRole<Guid> CreateRole(string id, string name) => new()
    {
        Id = Guid.Parse(id),
        Name = name,
        NormalizedName = name.ToUpperInvariant(),
        ConcurrencyStamp = id,
    };
}

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<IdentityUserRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> builder) => builder.ToTable("user_roles");
}

internal sealed class UserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> builder) => builder.ToTable("user_claims");
}

internal sealed class UserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> builder) => builder.ToTable("user_logins");
}

internal sealed class UserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> builder) => builder.ToTable("user_tokens");
}

internal sealed class RoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> builder) => builder.ToTable("role_claims");
}
