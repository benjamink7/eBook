using DotNetEnv;
using eBook.API.Middleware;
using eBook.Model.Constants;
using eBook.Services.Database;
using eBook.Services.Implementations;
using eBook.Services.Interfaces;
using eBook.Services.Security;
using eBook.Services.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

// Lokalno (izvan Dockera) konfiguracija se cita iz .env fajla; u kontejneru je ubacuje docker-compose.
if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
{
    Env.TraversePath().Load();
}

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;

var connectionString = new SqlConnectionStringBuilder
{
    DataSource = $"{GetRequired(configuration, "DB_HOST")},{GetRequired(configuration, "DB_PORT")}",
    InitialCatalog = GetRequired(configuration, "DB_NAME"),
    UserID = GetRequired(configuration, "DB_USER"),
    Password = GetRequired(configuration, "SQL_SA_PASSWORD"),
    TrustServerCertificate = true
}.ConnectionString;

var jwtSettings = new JwtSettings(
    GetRequired(configuration, "JWT_KEY"),
    GetRequired(configuration, "JWT_ISSUER"),
    GetRequired(configuration, "JWT_AUDIENCE"),
    int.Parse(GetRequired(configuration, "JWT_EXPIRY_MINUTES")));

if (Encoding.UTF8.GetByteCount(jwtSettings.Key) < 32)
{
    throw new InvalidOperationException("JWT_KEY mora imati najmanje 32 znaka.");
}

var allowedOrigins = GetRequired(configuration, "CORS_ALLOWED_ORIGINS")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICityService, CityService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = JwtClaimNames.Name,
            RoleClaimType = JwtClaimNames.Role
        };

        options.Events = new JwtBearerEvents
        {
            // Provjera na serveru: logout, deaktivacija korisnika i promjena lozinke ponistavaju stare tokene.
            OnTokenValidated = async context =>
            {
                var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
                var subClaim = context.Principal?.FindFirst(JwtClaimNames.Sub)?.Value;
                var versionClaim = context.Principal?.FindFirst(JwtClaimNames.TokenVersion)?.Value;

                if (!int.TryParse(subClaim, out var userId)
                    || !int.TryParse(versionClaim, out var tokenVersion)
                    || !await authService.IsSessionValidAsync(userId, tokenVersion))
                {
                    context.Fail("Sesija više nije važeća.");
                }
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await DbInitializer.InitializeAsync(app.Services);

app.Run();

static string GetRequired(IConfiguration configuration, string key) =>
    configuration[key] ?? throw new InvalidOperationException($"Konfiguracijska vrijednost '{key}' nije postavljena u .env fajlu.");
