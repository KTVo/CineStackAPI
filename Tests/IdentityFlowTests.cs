using BaseEFAPI.MVCS.Models.Authorization;
using BaseEFAPI.MVCS.Models.SignIn;
using BaseEFAPI.MVCS.Services.Authentication;
using BaseEFAPI.MVCS.Services.Authentication.Interfaces;
using BaseEFAPI.MVCS.Services.Context;
using BaseEFAPI.MVCS.Services.SignIn.Implementations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

public sealed class IdentityFlowTests : IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly ServiceProvider provider;
    private readonly TokenSpy tokens = new();
    private const string Password = "Valid123!";

    public IdentityFlowTests()
    {
        connection.Open();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthentication();
        services.AddDbContext<RegistrationDbContext>(o => o.UseSqlite(connection));
        services.AddApplicationIdentity();
        services.AddSingleton<IJwtTokenService>(tokens);
        services.AddScoped<RegistrationService>();
        services.AddScoped<SignInService>();
        provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<RegistrationDbContext>().Database.EnsureCreated();
    }

    [Theory]
    [InlineData("short")]
    [InlineData("lowercase123!")]
    [InlineData("UPPERCASE123!")]
    [InlineData("NoDigitsHere!")]
    [InlineData("NoSymbols123")]
    public async Task Weak_password_is_rejected_without_persisting_user(string password)
    {
        using var scope = provider.CreateScope();
        var response = await scope.ServiceProvider.GetRequiredService<RegistrationService>()
            .RegisterUserAsync(NewUser(), password);
        Assert.False(response.IsSuccess);
        Assert.NotEmpty(response.Errors!);
        Assert.Empty(await scope.ServiceProvider.GetRequiredService<RegistrationDbContext>().Users.ToListAsync());
    }

    [Fact]
    public async Task Registration_persists_identity_fields_and_normalized_login_works()
    {
        await Register();
        using var scope = provider.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUserModel>>();
        var user = (await manager.FindByNameAsync("ALICE"))!;
        Assert.True(user.LockoutEnabled);
        Assert.NotNull(user.SecurityStamp);
        Assert.NotEqual(Password, user.PasswordHash);
        Assert.True(await manager.CheckPasswordAsync(user, Password));
        var result = await Login(Password, email: true);
        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, tokens.LastSubject);
    }

    [Fact]
    public async Task Fifth_failure_locks_account_across_scopes_and_blocks_correct_password_until_expiry()
    {
        await Register();
        for (int i = 0; i < 5; i++)
            Assert.True((await Login("Wrong123!")).AuthenticationFailed);
        Assert.Equal(0, tokens.Calls);
        Assert.True((await Login(Password)).AuthenticationFailed);
        using (var scope = provider.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUserModel>>();
            var user = (await manager.FindByNameAsync("alice"))!;
            Assert.True(await manager.IsLockedOutAsync(user));
            Assert.True(user.LockoutEnd > DateTimeOffset.UtcNow.AddMinutes(4));
            Assert.True((await manager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddSeconds(-1))).Succeeded);
        }
        Assert.True((await Login(Password)).IsSuccess);
        Assert.Equal(1, tokens.Calls);
    }

    [Fact]
    public async Task Successful_login_resets_failed_count()
    {
        await Register();
        await Login("Wrong123!");
        Assert.True((await Login(Password)).IsSuccess);
        using var scope = provider.CreateScope();
        Assert.Equal(0, (await scope.ServiceProvider.GetRequiredService<RegistrationDbContext>().Users.SingleAsync()).AccessFailedCount);
    }

    [Fact]
    public async Task Confirmation_requirement_blocks_token_issuance()
    {
        await Register();
        provider.GetRequiredService<IOptions<IdentityOptions>>().Value.SignIn.RequireConfirmedEmail = true;
        Assert.True((await Login(Password)).AuthenticationFailed);
        Assert.Equal(0, tokens.Calls);
    }

    [Fact]
    public async Task Two_factor_account_cannot_receive_password_only_token()
    {
        await Register();
        using (var scope = provider.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUserModel>>();
            Assert.True((await manager.SetTwoFactorEnabledAsync((await manager.FindByNameAsync("alice"))!, true)).Succeeded);
        }
        Assert.True((await Login(Password)).AuthenticationFailed);
        Assert.Equal(0, tokens.Calls);
    }

    private async Task Register()
    {
        using var scope = provider.CreateScope();
        Assert.True((await scope.ServiceProvider.GetRequiredService<RegistrationService>()
            .RegisterUserAsync(NewUser(), Password)).IsSuccess);
    }

    private async Task<SignInResponseModel> Login(string password, bool email = false)
    {
        using var scope = provider.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<SignInService>().SignInAsync(new()
        {
            UserName = email ? null : "ALICE", Email = email ? "ALICE@EXAMPLE.COM" : null, Password = password
        });
    }

    private static ApplicationUserModel NewUser() => new()
    {
        UserName = "alice", Email = "alice@example.com", UserType = "USER", CreatedAt = DateTime.UtcNow
    };

    public void Dispose() { provider.Dispose(); connection.Dispose(); }

    private sealed class TokenSpy : IJwtTokenService
    {
        public int Calls { get; private set; }
        public string? LastSubject { get; private set; }
        public Task<JwtTokenResponse> GenerateJwtTokenAsync(JwtTokenModel model)
        {
            Calls++; LastSubject = model.Subject;
            return Task.FromResult(new JwtTokenResponse { IsSuccess = true, Token = "test-token" });
        }
        public DecryptedJweTokenResponse DecryptJWEToken(string tokenString) => throw new NotSupportedException();
    }
}
