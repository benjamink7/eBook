using DotNetEnv;
using eBook.API.Middleware;
using eBook.Services.Database;
using eBook.Services.Seed;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

// Lokalno (izvan Dockera) konfiguracija se cita iz .env fajla; u kontejneru je ubacuje docker-compose.
if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
{
    Env.TraversePath().Load();
}

var builder = WebApplication.CreateBuilder(args);

var connectionString = new SqlConnectionStringBuilder
{
    DataSource = $"{GetRequired(builder.Configuration, "DB_HOST")},{GetRequired(builder.Configuration, "DB_PORT")}",
    InitialCatalog = GetRequired(builder.Configuration, "DB_NAME"),
    UserID = GetRequired(builder.Configuration, "DB_USER"),
    Password = GetRequired(builder.Configuration, "SQL_SA_PASSWORD"),
    TrustServerCertificate = true
}.ConnectionString;

var allowedOrigins = GetRequired(builder.Configuration, "CORS_ALLOWED_ORIGINS")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

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
app.MapControllers();

await DbInitializer.InitializeAsync(app.Services);

app.Run();

static string GetRequired(IConfiguration configuration, string key) =>
    configuration[key] ?? throw new InvalidOperationException($"Konfiguracijska vrijednost '{key}' nije postavljena u .env fajlu.");
