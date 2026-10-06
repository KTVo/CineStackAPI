using CineStackAPI.MVCS.Services.Authentication;
using System.Text;
using CineStackAPI.MVCS.Services.Authentication.Implementations;
using CineStackAPI.MVCS.Services.Authentication.Interfaces;
using CineStackAPI.MVCS.Services.Context;
using CineStackAPI.MVCS.Services.Registration.Interfaces;
using CineStackAPI.MVCS.Services.SignIn.Implementations;
using CineStackAPI.MVCS.Services.SignIn.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using CineStackAPI.MVCS.Repositories.Viewership.Cache.Interfaces;
using CineStackAPI.MVCS.Repositories.Viewership.Cache;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(builder => builder.AddConsole());

// ADD SERVICES TO THE CONTAINER
builder.Services.AddAuthorization();

string? issuer = builder.Configuration["Authentication:JwtSettings:Issuer"];
string? audience = builder.Configuration["Authentication:JwtSettings:Audience"];
string? secretKey = builder.Configuration["Authentication:JwtSettings:SecretKey"];
string? encryptionKey = builder.Configuration["Authentication:JwtSettings:EncryptionKey"];

if (string.IsNullOrEmpty(issuer))
{
    Console.WriteLine("JWT issuer is missing!");
    Environment.Exit(1);
}
if (string.IsNullOrEmpty(audience))
{
    Console.WriteLine("JWT audience is missing!");
    Environment.Exit(1);
}

if (string.IsNullOrEmpty(secretKey))
{
    Console.WriteLine("JWT secret key is missing!");
    Environment.Exit(1);
}

if (string.IsNullOrEmpty(encryptionKey))
{
    Console.WriteLine("JWT encryption key is missing!");
    Environment.Exit(1);
}

SymmetricSecurityKey signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
SymmetricSecurityKey encryptionSecurityKey = new SymmetricSecurityKey(Convert.FromBase64String(encryptionKey));

// ADD AUTHENTICATION SERVICES TO THE CONTAINER
builder.Services.AddApplicationIdentity();

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme
    )
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                // Decrypt JWE
                TokenDecryptionKey = encryptionSecurityKey,

                // Verify signed JWT inside
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,

                // Validate issuer
                ValidateIssuer = true,
                ValidIssuer = issuer,

                // Validate audience
                ValidateAudience = true,
                ValidAudience = audience,

                // Validate expiration
                ValidateLifetime = true,

                ClockSkew = TimeSpan.FromSeconds(30),

                RoleClaimType = "role"
            };
    });

string? conStrMain = builder.Configuration.GetConnectionString("DefaultPostgres");

if (string.IsNullOrEmpty(conStrMain) == true)
{
    Console.WriteLine("Connection string for main database not found!");
    Environment.Exit(0);
}

string? conStrCache = builder.Configuration.GetConnectionString("DefaultRedis");

if (string.IsNullOrEmpty(conStrCache) == true)
{
    Console.WriteLine("Connection string for database caching not found!");
    Environment.Exit(0);
}



//// MSSQL - REGISTER THE REGISTRATION API DBCONTEXT WITH THE CONTAINER
//builder.Services.AddDbContext<RegistrationDbContext>(options =>
//options.UseSqlServer(
//    connectionString: conStrMain, sqlServerOptionsAction: sqlOptions =>
//    {
//        sqlOptions.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
//    })
//);

// POSTGRES - REGISTER THE REGISTRATION API DBCONTEXT WITH THE CONTAINER
builder.Services.AddDbContext<RegistrationDbContext>(options =>
    options.UseNpgsql(conStrMain)
    );

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = conStrCache;
    options.InstanceName = "CineStackCache";
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins("http://localhost")
              .AllowAnyHeader()
              .AllowAnyMethod());
});


// LEARN MORE ABOUT CONFIGURING SWAGGER/OPENAPI AT HTTPS://AKA.MS/ASPNETCORE/SWASHBUCKLE
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ADD CONTROLLERS TO THE CONTAINER
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null;
});

// TODO: ADD SERVICES TO THE CONTAINER
builder.Services
    .AddScoped<IRegistrationService, RegistrationService>()
    .AddScoped<ISignInService, SignInService>()
    .AddTransient<IJwtTokenService, JwtTokenService>()
    .AddTransient<IUserRepository, UserRepository>()
    .AddTransient<IDbCacheRepository, DbCacheRepository>();

// Configure the HTTP request pipeline.
if (builder.Environment.IsDevelopment())
{
    Console.WriteLine("Development environment detected. Enabling CORS for all origins.");

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll",
                       bldr => bldr.AllowAnyMethod()
                                   .AllowAnyHeader()
                                   .AllowAnyOrigin());
    });

}
else
{
    builder.Services.AddCors(options =>
   {
       options.AddPolicy("AllowAppServices",
                                 bldr => bldr
                                 .AllowAnyMethod()
                                 .AllowAnyHeader()
                                 .WithOrigins());
   });
}

WebApplication app = builder.Build();

if (args.Contains("--initialize-database", StringComparer.Ordinal))
{
    if (args.Contains("--upgrade-identity", StringComparer.Ordinal))
        throw new InvalidOperationException("Run database initialization and the existing-database upgrade separately.");

    await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<RegistrationDbContext>();
    bool created = await db.Database.EnsureCreatedAsync();
    if (!created)
        throw new InvalidOperationException(
            "Database initialization was skipped because tables already exist. " +
            "Check DefaultConnection, the schema, and the case-sensitive ApplicationUser table name. " +
            "Existing databases require a reviewed schema migration; initialization does not upgrade them.");

    app.Logger.LogInformation("Database initialized with ApplicationUser and the Identity tables.");
    return;
}
if (args.Contains("--upgrade-identity", StringComparer.Ordinal))
{
    await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
    await CineStackAPI.Database.IdentityDatabaseUpgrade.RunAsync(
        scope.ServiceProvider.GetRequiredService<RegistrationDbContext>(),
        scope.ServiceProvider.GetRequiredService<ILookupNormalizer>());
    app.Logger.LogInformation("Identity database upgrade completed.");
    return;
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // ENABLE MIDDLEWARE TO SERVE GENERATED SWAGGER AS JSON ENDPOINT
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        // options.RoutePrefix = string.Empty;
    });

    

    // ENABLE CORS POLICY
    app.UseCors("AllowAll");
}
else
{   
    app.UseCors("AllowAppServices");
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
