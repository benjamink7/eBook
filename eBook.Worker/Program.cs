using eBook.Model.Messaging;
using eBook.Worker;

var builder = Host.CreateApplicationBuilder(args);
var configuration = builder.Configuration;

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

builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddHostedService<EmailConsumerService>();

await builder.Build().RunAsync();

static string GetRequired(IConfiguration configuration, string key) =>
    configuration[key] ?? throw new InvalidOperationException($"Konfiguracijska vrijednost '{key}' nije postavljena u .env fajlu.");
