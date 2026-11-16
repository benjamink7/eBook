using System.Text.Json;
using eBook.Model.Constants;
using eBook.Model.Messaging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace eBook.Worker;

public class EmailConsumerService : BackgroundService
{
    private const int MaxSendAttempts = 5;
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);

    private readonly RabbitMqSettings _rabbitMqSettings;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailConsumerService> _logger;

    public EmailConsumerService(
        RabbitMqSettings rabbitMqSettings,
        IEmailSender emailSender,
        ILogger<EmailConsumerService> logger)
    {
        _rabbitMqSettings = rabbitMqSettings;
        _emailSender = emailSender;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunConsumerAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker nije uspio ostvariti vezu s RabbitMQ-om. Ponovni pokušaj za {Delay} s.",
                    ReconnectDelay.TotalSeconds);
            }

            try
            {
                await Task.Delay(ReconnectDelay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("Email worker zaustavljen.");
    }

    private async Task RunConsumerAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _rabbitMqSettings.Host,
            Port = _rabbitMqSettings.Port,
            UserName = _rabbitMqSettings.User,
            Password = _rabbitMqSettings.Password
        };

        await using var connection = await factory.CreateConnectionAsync(stoppingToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await channel.QueueDeclareAsync(
            queue: QueueNames.Email, durable: true, exclusive: false, autoDelete: false,
            cancellationToken: stoppingToken);

        // Jedna poruka po radniku istovremeno.
        await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

        var connectionClosed = new TaskCompletionSource();

        connection.ConnectionShutdownAsync += (_, args) =>
        {
            _logger.LogWarning("Veza s RabbitMQ-om je zatvorena: {Reason}", args.ReplyText);
            connectionClosed.TrySetResult();
            return Task.CompletedTask;
        };

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) => await HandleMessageAsync(channel, ea, stoppingToken);

        await channel.BasicConsumeAsync(queue: QueueNames.Email, autoAck: false, consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("Email worker sluša red {Queue}.", QueueNames.Email);

        using var cancellationRegistration = stoppingToken.Register(() => connectionClosed.TrySetResult());
        await connectionClosed.Task;
    }

    private async Task HandleMessageAsync(IChannel channel, BasicDeliverEventArgs ea, CancellationToken stoppingToken)
    {
        EmailMessage? message;

        try
        {
            message = JsonSerializer.Deserialize<EmailMessage>(ea.Body.Span);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Poruka u redu nije ispravan JSON i bit će odbačena.");
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, CancellationToken.None);
            return;
        }

        if (message is null)
        {
            _logger.LogError("Poruka u redu je prazna i bit će odbačena.");
            await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, CancellationToken.None);
            return;
        }

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await _emailSender.SendAsync(message, stoppingToken);
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, CancellationToken.None);

                _logger.LogInformation("E-mail \"{Subject}\" je uspješno poslan.", message.Subject);
                return;
            }
            catch (Exception ex) when (attempt < MaxSendAttempts && !stoppingToken.IsCancellationRequested)
            {
                // Eksponencijalni backoff: 1 s, 2 s, 4 s, 8 s.
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));

                _logger.LogWarning(ex, "Slanje e-maila \"{Subject}\" nije uspjelo (pokušaj {Attempt}/{Max}). Ponovo za {Delay} s.",
                    message.Subject, attempt, MaxSendAttempts, delay.TotalSeconds);

                await Task.Delay(delay, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "E-mail \"{Subject}\" nije poslan nakon {Max} pokušaja i poruka se odbacuje.",
                    message.Subject, MaxSendAttempts);

                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false, CancellationToken.None);
                return;
            }
        }
    }
}
