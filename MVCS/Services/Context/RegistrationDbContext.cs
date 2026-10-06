using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CineStackAPI.MVCS.Services.Context;

public sealed class RegistrationDbContext(DbContextOptions<RegistrationDbContext> options)
    : IdentityUserContext<ApplicationUserModel>(options)
{
    public DbSet<ApplicationUserModel> ApplicationUser => Set<ApplicationUserModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<IdentityUserLogin<string>>().Property(x => x.LoginProvider).HasMaxLength(128);
        modelBuilder.Entity<IdentityUserLogin<string>>().Property(x => x.ProviderKey).HasMaxLength(128);
        modelBuilder.Entity<IdentityUserToken<string>>().Property(x => x.LoginProvider).HasMaxLength(128);
        modelBuilder.Entity<IdentityUserToken<string>>().Property(x => x.Name).HasMaxLength(128);
        modelBuilder.Entity<ApplicationUserModel>().ToTable("ApplicationUser");
        modelBuilder.Entity<ApplicationUserModel>()
            .HasIndex(user => user.NormalizedEmail).HasDatabaseName("EmailIndex").IsUnique();
    }
}
