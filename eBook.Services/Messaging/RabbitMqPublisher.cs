using System.Text.Json;
using eBook.Model.Messaging;
using RabbitMQ.Client;

namespace eBook.Services.Messaging;

// Jedna dijeljena konekcija i kanal za cijelu aplikaciju (ne otvara se nova konekcija po poruci).
public sealed class RabbitMqPublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public RabbitMqPublisher(RabbitMqSettings settings)
    {
        _factory = new ConnectionFactory
        {
            HostName = settings.Host,
            Port = settings.Port,
            UserName = settings.User,
            Password = settings.Password
        };
    }

    public async Task PublishAsync<T>(string queue, T message)
    {
        await _lock.WaitAsync();

        try
        {
            var channel = await GetChannelAsync();

            await channel.QueueDeclareAsync(queue: queue, durable: true, exclusive: false, autoDelete: false);

            var body = JsonSerializer.SerializeToUtf8Bytes(message);
            var properties = new BasicProperties { Persistent = true, ContentType = "application/json" };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queue,
                mandatory: false,
                basicProperties: properties,
                body: body);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IChannel> GetChannelAsync()
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        if (_connection is not { IsOpen: true })
        {
            if (_connection is not null)
            {
                await _connection.DisposeAsync();
            }

            _connection = await _factory.CreateConnectionAsync();
        }

        _channel = await _connection.CreateChannelAsync();
        return _channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _lock.Dispose();
    }
}
