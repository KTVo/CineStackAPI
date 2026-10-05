using BaseEFAPI.MVCS.Services.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BaseEFAPI.Database;

public static class IdentityDatabaseUpgrade
{
    // Explicit maintenance command only: stop writers and back up the database first.
    // Requires the columns from UpgradeApplicationUserToIdentity.sql.
    public static async Task RunAsync(RegistrationDbContext db, ILookupNormalizer normalizer)
    {
        await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            db.ChangeTracker.Clear();
            await using var transaction = await db.Database.BeginTransactionAsync();
            var users = await db.ApplicationUser.ToListAsync();
            foreach (var user in users)
            {
                user.NormalizedUserName = normalizer.NormalizeName(user.UserName);
                user.NormalizedEmail = normalizer.NormalizeEmail(user.Email);
                if (string.IsNullOrWhiteSpace(user.NormalizedUserName) ||
                    string.IsNullOrWhiteSpace(user.NormalizedEmail) ||
                    user.UserName!.Length > 256 || user.Email!.Length > 256 ||
                    user.NormalizedUserName.Length > 256 || user.NormalizedEmail.Length > 256)
                    throw new InvalidOperationException("Resolve missing or oversized user identifiers before upgrading.");
                user.SecurityStamp ??= Guid.NewGuid().ToString();
                user.ConcurrencyStamp ??= Guid.NewGuid().ToString();
                user.LockoutEnabled = true;
            }

            if (users.GroupBy(u => u.NormalizedUserName).Any(g => g.Count() > 1) ||
                users.GroupBy(u => u.NormalizedEmail).Any(g => g.Count() > 1))
                throw new InvalidOperationException("Resolve duplicate normalized usernames/emails before upgrading.");

            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync("""
                IF COL_LENGTH(N'dbo.ApplicationUser', N'UserName') <> 512
                    ALTER TABLE dbo.ApplicationUser ALTER COLUMN UserName nvarchar(256) NULL;
                IF COL_LENGTH(N'dbo.ApplicationUser', N'Email') <> 512
                    ALTER TABLE dbo.ApplicationUser ALTER COLUMN Email nvarchar(256) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ApplicationUser') AND name = N'UserNameIndex')
                    CREATE UNIQUE INDEX UserNameIndex ON dbo.ApplicationUser(NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ApplicationUser') AND name = N'EmailIndex')
                    CREATE UNIQUE INDEX EmailIndex ON dbo.ApplicationUser(NormalizedEmail) WHERE NormalizedEmail IS NOT NULL;
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.ApplicationUser') AND name IN (N'UserNameIndex', N'EmailIndex') AND is_unique = 0)
                    THROW 50002, 'Existing Identity indexes must be unique. Review them before upgrading.', 1;

                IF OBJECT_ID(N'dbo.AspNetUserClaims', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.AspNetUserClaims (
                        Id int IDENTITY PRIMARY KEY,
                        UserId nvarchar(450) NOT NULL,
                        ClaimType nvarchar(max) NULL,
                        ClaimValue nvarchar(max) NULL,
                        CONSTRAINT FK_AspNetUserClaims_ApplicationUser FOREIGN KEY(UserId) REFERENCES dbo.ApplicationUser(Id) ON DELETE CASCADE);
                    CREATE INDEX IX_AspNetUserClaims_UserId ON dbo.AspNetUserClaims(UserId);
                END;
                IF OBJECT_ID(N'dbo.AspNetUserLogins', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.AspNetUserLogins (
                        LoginProvider nvarchar(128) NOT NULL,
                        ProviderKey nvarchar(128) NOT NULL,
                        ProviderDisplayName nvarchar(max) NULL,
                        UserId nvarchar(450) NOT NULL,
                        PRIMARY KEY(LoginProvider, ProviderKey),
                        CONSTRAINT FK_AspNetUserLogins_ApplicationUser FOREIGN KEY(UserId) REFERENCES dbo.ApplicationUser(Id) ON DELETE CASCADE);
                    CREATE INDEX IX_AspNetUserLogins_UserId ON dbo.AspNetUserLogins(UserId);
                END;
                IF OBJECT_ID(N'dbo.AspNetUserTokens', N'U') IS NULL
                    CREATE TABLE dbo.AspNetUserTokens (
                        UserId nvarchar(450) NOT NULL,
                        LoginProvider nvarchar(128) NOT NULL,
                        Name nvarchar(128) NOT NULL,
                        Value nvarchar(max) NULL,
                        PRIMARY KEY(UserId, LoginProvider, Name),
                        CONSTRAINT FK_AspNetUserTokens_ApplicationUser FOREIGN KEY(UserId) REFERENCES dbo.ApplicationUser(Id) ON DELETE CASCADE);
                """);
            await transaction.CommitAsync();
        });
    }
}
