using eBook.Model.Exceptions;
using eBook.Model.Subscriptions;
using Microsoft.Extensions.Logging;
using Stripe;

namespace eBook.Services.Payments;

public class StripePaymentGateway : IPaymentGateway
{
    private const string PaymentUnavailableMessage = "Usluga plaćanja trenutno nije dostupna. Pokušajte ponovo kasnije.";

    private readonly StripeSettings _settings;
    private readonly StripeClient _client;
    private readonly ILogger<StripePaymentGateway> _logger;

    public StripePaymentGateway(StripeSettings settings, ILogger<StripePaymentGateway> logger)
    {
        _settings = settings;
        _logger = logger;
        _client = new StripeClient(settings.SecretKey);
    }

    public async Task<PaymentIntentInfo> CreatePaymentIntentAsync(long amountMinor, string currency, int subscriptionId)
    {
        try
        {
            var options = new PaymentIntentCreateOptions
            {
                Amount = amountMinor,
                Currency = currency,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
                Metadata = new Dictionary<string, string> { ["subscriptionId"] = subscriptionId.ToString() }
            };

            var requestOptions = new RequestOptions { IdempotencyKey = $"ebook-subscription-{subscriptionId}" };
            var intent = await new PaymentIntentService(_client).CreateAsync(options, requestOptions);

            return Map(intent);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe: kreiranje PaymentIntent-a nije uspjelo za pretplatu {SubscriptionId}.", subscriptionId);
            throw new BusinessException(PaymentUnavailableMessage);
        }
    }

    public async Task<PaymentIntentInfo> GetPaymentIntentAsync(string paymentIntentId)
    {
        try
        {
            var intent = await new PaymentIntentService(_client).GetAsync(paymentIntentId);
            return Map(intent);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe: dohvat PaymentIntent-a {PaymentIntentId} nije uspio.", paymentIntentId);
            throw new BusinessException(PaymentUnavailableMessage);
        }
    }

    // Otkazivanje je "best effort": ako Stripe odbije (npr. uplata je vec prosla), stanje rjesava webhook.
    public async Task CancelPaymentIntentAsync(string paymentIntentId)
    {
        try
        {
            await new PaymentIntentService(_client).CancelAsync(paymentIntentId);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe: otkazivanje PaymentIntent-a {PaymentIntentId} nije uspjelo.", paymentIntentId);
        }
    }

    public async Task<string> RefundAsync(string paymentIntentId, long amountMinor, int paymentId)
    {
        try
        {
            var options = new RefundCreateOptions { PaymentIntent = paymentIntentId, Amount = amountMinor };
            var requestOptions = new RequestOptions { IdempotencyKey = $"ebook-refund-{paymentId}" };
            var refund = await new RefundService(_client).CreateAsync(options, requestOptions);

            return refund.Id;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe: refund za PaymentIntent {PaymentIntentId} nije uspio.", paymentIntentId);
            throw new BusinessException("Povrat novca nije uspio. Provjerite stanje uplate u Stripe-u.");
        }
    }

    public PaymentWebhookEvent? ParseWebhook(string payload, string signatureHeader)
    {
        if (string.IsNullOrWhiteSpace(_settings.WebhookSecret))
        {
            throw new BusinessException("Stripe webhook nije konfigurisan.");
        }

        Event stripeEvent;

        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                payload, signatureHeader, _settings.WebhookSecret, throwOnApiVersionMismatch: false);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe webhook: neispravan potpis.");
            throw new BusinessException("Neispravan potpis webhook zahtjeva.");
        }

        return stripeEvent.Type switch
        {
            "payment_intent.succeeded" when stripeEvent.Data.Object is PaymentIntent intent =>
                new PaymentWebhookEvent(PaymentWebhookEventType.PaymentSucceeded, intent.Id, intent.AmountReceived, intent.Currency),
            "payment_intent.payment_failed" when stripeEvent.Data.Object is PaymentIntent intent =>
                new PaymentWebhookEvent(PaymentWebhookEventType.PaymentFailed, intent.Id, intent.Amount, intent.Currency),
            "charge.refunded" when stripeEvent.Data.Object is Charge charge && charge.PaymentIntentId is not null =>
                new PaymentWebhookEvent(PaymentWebhookEventType.PaymentRefunded, charge.PaymentIntentId, charge.AmountRefunded, charge.Currency),
            _ => null
        };
    }

    private static PaymentIntentInfo Map(PaymentIntent intent) =>
        new(intent.Id, intent.Status, intent.AmountReceived, intent.Currency, intent.ClientSecret);
}
