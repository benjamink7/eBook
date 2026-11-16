using eBook.Model.Messaging;

namespace eBook.Worker;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
