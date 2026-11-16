namespace eBook.Model.Messaging;

public record EmailMessage(string To, string Subject, string Body);
