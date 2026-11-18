using eBook.Model.Messaging;
using eBook.Services.Database;
using eBook.Services.Messaging;
using eBook.Worker;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
var configuration = builder.Configuration;

var connectionString = new SqlConnectionStringBuilder
{
    DataSource = $"{GetRequired(configuration, "DB_HOST")},{GetRequired(configuration, "DB_PORT")}",
    InitialCatalog = GetRequired(configuration, "DB_NAME"),
    UserID = GetRequired(configuration, "DB_USER"),
    Password = GetRequired(configuration, "SQL_SA_PASSWORD"),
    TrustServerCertificate = true
}.ConnectionString;

builder.Services.AddSingleton(new RabbitMqSettings(
    GetRequired(configuration, "RABBITMQ_HOST"),
    int.Parse(GetRequired(configuration, "RABBITMQ_PORT")),
    GetRequired(configuration, "RABBITMQ_USER"),
    GetRequired(configuration, "RABBITMQ_PASSWORD")));

builder.Services.AddSingleton(new SmtpSettings(
    GetRequired(configuration, "SMTP_HOST"),
    int.Parse(GetRequired(configuration, "SMTP_PORT")),
    GetRequired(configuration, "SMTP_USERNAME"),
    GetRequired(configuration, "SMTP_PASSWORD"),
    bool.Parse(GetRequired(configuration, "SMTP_USE_SSL")),
    GetRequired(configuration, "SMTP_SENDER")));

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddHostedService<EmailConsumerService>();
builder.Services.AddHostedService<SubscriptionMaintenanceService>();

await builder.Build().RunAsync();

static string GetRequired(IConfiguration configuration, string key) =>
    configuration[key] ?? throw new InvalidOperationException($"Konfiguracijska vrijednost '{key}' nije postavljena u .env fajlu.");
