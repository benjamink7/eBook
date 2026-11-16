using System.Net;
using System.Net.Mail;
using eBook.Model.Messaging;

namespace eBook.Worker;

public class SmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _settings;

    public SmtpEmailSender(SmtpSettings settings)
    {
        _settings = settings;
    }

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        using var mail = new MailMessage(_settings.Sender, message.To, message.Subject, message.Body)
        {
            IsBodyHtml = true
        };

        using var client = new SmtpClient(_settings.Host, _settings.Port)
        {
            EnableSsl = _settings.UseSsl,
            Credentials = new NetworkCredential(_settings.Username, _settings.Password)
        };

        await client.SendMailAsync(mail, cancellationToken);
    }
}
