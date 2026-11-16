namespace eBook.Worker;

public record SmtpSettings(string Host, int Port, string Username, string Password, bool UseSsl, string Sender);
