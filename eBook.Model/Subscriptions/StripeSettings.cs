namespace eBook.Model.Subscriptions;

public record StripeSettings(string SecretKey, string PublishableKey, string WebhookSecret);
