namespace eBook.Model.Messaging;

public record RabbitMqSettings(string Host, int Port, string User, string Password);
