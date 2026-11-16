using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using DotNetEnv;
using eBook.API.Configuration;
using eBook.API.Middleware;
using eBook.Model.Constants;
using eBook.Model.Messaging;
using eBook.Model.Subscriptions;
using eBook.Services.Database;
using eBook.Services.Implementations;
using eBook.Services.Interfaces;
using eBook.Services.Messaging;
using eBook.Services.Payments;
using eBook.Services.Security;
using eBook.Services.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

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

var rabbitMqSettings = new RabbitMqSettings(
    GetRequired(configuration, "RABBITMQ_HOST"),
    int.Parse(GetRequired(configuration, "RABBITMQ_PORT")),
    GetRequired(configuration, "RABBITMQ_USER"),
    GetRequired(configuration, "RABBITMQ_PASSWORD"));

var stripeSettings = new StripeSettings(
    GetRequired(configuration, "STRIPE_SECRET_KEY"),
    GetRequired(configuration, "STRIPE_PUBLISHABLE_KEY"),
    configuration["STRIPE_WEBHOOK_SECRET"] ?? string.Empty);

var allowedOrigins = GetRequired(configuration, "CORS_ALLOWED_ORIGINS")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton(rabbitMqSettings);
builder.Services.AddSingleton(stripeSettings);
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();
builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICityService, CityService>();
builder.Services.AddScoped<IGenreService, GenreService>();
builder.Services.AddScoped<IAuthorService, AuthorService>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IPaymentGateway, StripePaymentGateway>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IReadingProgressService, ReadingProgressService>();
builder.Services.AddScoped<ISearchHistoryService, SearchHistoryService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<INewsService, NewsService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();

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

// Ogranicava broj pokusaja na osjetljivim endpointima (npr. pogadjanje koda za reset lozinke).
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(RateLimitPolicies.Auth, httpContext => RateLimitPartition.GetFixedWindowLimiter(
        httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1)
        }));
});

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
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
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await DbInitializer.InitializeAsync(app.Services);

app.Run();

static string GetRequired(IConfiguration configuration, string key) =>
    configuration[key] ?? throw new InvalidOperationException($"Konfiguracijska vrijednost '{key}' nije postavljena u .env fajlu.");
