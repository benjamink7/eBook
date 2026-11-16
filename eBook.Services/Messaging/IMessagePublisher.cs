namespace eBook.Services.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<T>(string queue, T message);
}
