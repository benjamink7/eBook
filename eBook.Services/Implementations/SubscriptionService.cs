using System.Linq.Expressions;
using eBook.Model.Common;
using eBook.Model.Constants;
using eBook.Model.Enums;
using eBook.Model.Exceptions;
using eBook.Model.Messaging;
using eBook.Model.Subscriptions;
using eBook.Services.Database;
using eBook.Services.Extensions;
using eBook.Services.Interfaces;
using eBook.Services.Messaging;
using eBook.Services.Payments;
using eBook.Services.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eBook.Services.Implementations;

public class SubscriptionService : ISubscriptionService
{
    private static readonly Expression<Func<Subscription, SubscriptionResponse>> ToResponse = s => new SubscriptionResponse
    {
        Id = s.Id,
        UserId = s.UserId,
        Username = s.User.Username,
        PlanName = s.SubscriptionPlan.Name,
        Status = s.Status,
        StartDate = s.StartDate,
        EndDate = s.EndDate,
        CreatedAt = s.CreatedAt,
        CancelledAt = s.CancelledAt,
        CancellationReason = s.CancellationReason,
        Amount = s.Payments.OrderByDescending(p => p.CreatedAt).Select(p => (decimal?)p.Amount).FirstOrDefault(),
        Currency = s.Payments.OrderByDescending(p => p.CreatedAt).Select(p => p.Currency).FirstOrDefault(),
        PaymentStatus = s.Payments.OrderByDescending(p => p.CreatedAt).Select(p => (PaymentStatus?)p.Status).FirstOrDefault()
    };

    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPaymentGateway _gateway;
    private readonly IMessagePublisher _publisher;
    private readonly StripeSettings _stripeSettings;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        IPaymentGateway gateway,
        IMessagePublisher publisher,
        StripeSettings stripeSettings,
        ILogger<SubscriptionService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _gateway = gateway;
        _publisher = publisher;
        _stripeSettings = stripeSettings;
        _logger = logger;
    }

    public Task<List<SubscriptionPlanResponse>> GetPlansAsync()
    {
        return _db.SubscriptionPlans
            .AsNoTracking()
            .Where(plan => plan.IsActive)
            .OrderBy(plan => plan.Price)
            .Select(plan => new SubscriptionPlanResponse
            {
                Id = plan.Id,
                Name = plan.Name,
                Description = plan.Description,
                Price = plan.Price,
                Currency = plan.Currency,
                DurationInMonths = plan.DurationInMonths
            })
            .ToListAsync();
    }

    public async Task<CheckoutResponse> CheckoutAsync(CheckoutRequest request)
    {
        var userId = _currentUser.UserId;
        var now = DateTime.UtcNow;

        var plan = await _db.SubscriptionPlans.SingleOrDefaultAsync(p => p.Id == request.PlanId && p.IsActive)
            ?? throw new NotFoundException("Plan pretplate nije pronađen.");

        var hasActive = await _db.Subscriptions.AnyAsync(s =>
            s.UserId == userId && s.Status == SubscriptionStatus.Active && s.EndDate > now);

        if (hasActive)
        {
            throw new BusinessException("Već imate aktivnu pretplatu.");
        }

        // Stari neplaceni pokusaji se zatvaraju, da ne ostanu otvorene uplate.
        var stalePending = await _db.Subscriptions
            .Include(s => s.Payments)
            .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Pending)
            .ToListAsync();

        foreach (var stale in stalePending)
        {
            foreach (var stalePayment in stale.Payments.Where(p => p.Status == PaymentStatus.Pending))
            {
                await _gateway.CancelPaymentIntentAsync(stalePayment.StripePaymentIntentId);
                stalePayment.Status = PaymentStatus.Failed;
            }

            SubscriptionStateMachine.Transition(
                stale, SubscriptionStatus.Cancelled, "Zamijenjeno novim pokušajem kupovine.", now, userId);
            stale.CancelledAt = now;
            stale.CancellationReason = "Zamijenjeno novim pokušajem kupovine.";
        }

        var subscription = new Subscription
        {
            UserId = userId,
            SubscriptionPlanId = plan.Id,
            Status = SubscriptionStatus.Pending,
            CreatedAt = now,
            StatusChangedAt = now,
            StatusChangeNote = "Pretplata kreirana, čeka se plaćanje."
        };

        subscription.StatusHistory.Add(new SubscriptionStatusHistory
        {
            FromStatus = null,
            ToStatus = SubscriptionStatus.Pending,
            Note = subscription.StatusChangeNote,
            ChangedAt = now,
            ChangedByUserId = userId
        });

        _db.Subscriptions.Add(subscription);
        await _db.SaveChangesAsync();

        // Cijena dolazi iskljucivo iz baze, nikad od klijenta.
        var intent = await _gateway.CreatePaymentIntentAsync(ToMinorUnits(plan.Price), plan.Currency, subscription.Id);

        _db.Payments.Add(new Payment
        {
            SubscriptionId = subscription.Id,
            UserId = userId,
            Amount = plan.Price,
            Currency = plan.Currency,
            Status = PaymentStatus.Pending,
            StripePaymentIntentId = intent.Id,
            CreatedAt = now
        });

        await _db.SaveChangesAsync();

        return new CheckoutResponse
        {
            SubscriptionId = subscription.Id,
            PaymentIntentClientSecret = intent.ClientSecret
                ?? throw new BusinessException("Plaćanje trenutno nije moguće. Pokušajte ponovo."),
            PublishableKey = _stripeSettings.PublishableKey,
            Amount = plan.Price,
            Currency = plan.Currency
        };
    }

    public async Task<SubscriptionResponse> ConfirmAsync(int subscriptionId)
    {
        var userId = _currentUser.UserId;

        var payment = await _db.Payments
            .Where(p => p.SubscriptionId == subscriptionId && p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync()
            ?? throw new NotFoundException("Pretplata nije pronađena.");

        if (payment.Status == PaymentStatus.Pending || payment.Status == PaymentStatus.Failed)
        {
            // Status uplate se uvijek provjerava kod Stripe-a, klijentu se ne vjeruje.
            var intent = await _gateway.GetPaymentIntentAsync(payment.StripePaymentIntentId);

            if (intent.Status == "succeeded")
            {
                await ActivateAsync(payment.StripePaymentIntentId, intent.AmountReceivedMinor, intent.Currency);
            }
        }

        return await GetResponseAsync(subscriptionId);
    }

    public async Task<SubscriptionResponse?> GetCurrentAsync()
    {
        var userId = _currentUser.UserId;
        var now = DateTime.UtcNow;

        return await _db.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active && s.EndDate > now)
            .OrderByDescending(s => s.EndDate)
            .Select(ToResponse)
            .FirstOrDefaultAsync();
    }

    public Task<PagedResult<SubscriptionResponse>> GetMineAsync(PagedRequest request)
    {
        var userId = _currentUser.UserId;

        return _db.Subscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(ToResponse)
            .ToPagedResultAsync(request);
    }

    public Task<PagedResult<SubscriptionResponse>> GetAllAsync(SubscriptionSearchRequest request)
    {
        var query = _db.Subscriptions.AsNoTracking().AsQueryable();

        if (request.Status.HasValue)
        {
            query = query.Where(s => s.Status == request.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(s => s.User.Username.Contains(term) || s.User.Email.Contains(term));
        }

        return query
            .OrderByDescending(s => s.CreatedAt)
            .Select(ToResponse)
            .ToPagedResultAsync(request);
    }

    public async Task<List<SubscriptionStatusHistoryResponse>> GetHistoryAsync(int subscriptionId)
    {
        var userId = _currentUser.UserId;
        var isAdmin = _currentUser.IsAdmin;

        var hasAccess = await _db.Subscriptions.AnyAsync(s =>
            s.Id == subscriptionId && (isAdmin || s.UserId == userId));

        if (!hasAccess)
        {
            throw new NotFoundException("Pretplata nije pronađena.");
        }

        return await _db.SubscriptionStatusHistories
            .AsNoTracking()
            .Where(h => h.SubscriptionId == subscriptionId)
            .OrderBy(h => h.ChangedAt)
            .ThenBy(h => h.Id)
            .Select(h => new SubscriptionStatusHistoryResponse
            {
                FromStatus = h.FromStatus,
                ToStatus = h.ToStatus,
                Note = h.Note,
                ChangedAt = h.ChangedAt,
                ChangedBy = h.ChangedByUser != null ? h.ChangedByUser.Username : "Sistem"
            })
            .ToListAsync();
    }

    public async Task<SubscriptionResponse> CancelAsync(int subscriptionId, SubscriptionReasonRequest request)
    {
        var userId = _currentUser.UserId;
        var now = DateTime.UtcNow;
        var reason = request.Reason.Trim();

        var subscription = await _db.Subscriptions
            .Include(s => s.SubscriptionPlan)
            .Include(s => s.User)
            .Include(s => s.Payments)
            .SingleOrDefaultAsync(s => s.Id == subscriptionId && s.UserId == userId)
            ?? throw new NotFoundException("Pretplata nije pronađena.");

        foreach (var payment in subscription.Payments.Where(p => p.Status == PaymentStatus.Pending))
        {
            await _gateway.CancelPaymentIntentAsync(payment.StripePaymentIntentId);
            payment.Status = PaymentStatus.Failed;
        }

        SubscriptionStateMachine.Transition(subscription, SubscriptionStatus.Cancelled, $"Otkazano: {reason}", now, userId);
        subscription.CancelledAt = now;
        subscription.CancellationReason = reason;

        AddNotification(
            subscription.UserId,
            "Pretplata otkazana",
            $"Vaša pretplata \"{subscription.SubscriptionPlan.Name}\" je otkazana.",
            NotificationType.Subscription,
            now);

        await _db.SaveChangesAsync();

        await PublishEmailAsync(
            subscription.User,
            EmailTemplates.SubscriptionCancelled(subscription.User.FirstName, subscription.SubscriptionPlan.Name, reason));

        return await GetResponseAsync(subscriptionId);
    }

    public async Task<SubscriptionResponse> RefundAsync(int subscriptionId, SubscriptionReasonRequest request)
    {
        var adminId = _currentUser.UserId;
        var reason = request.Reason.Trim();

        var payment = await _db.Payments
            .Include(p => p.Subscription).ThenInclude(s => s.SubscriptionPlan)
            .Include(p => p.User)
            .Where(p => p.SubscriptionId == subscriptionId && p.Status == PaymentStatus.Completed)
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync()
            ?? throw new BusinessException("Za ovu pretplatu ne postoji uplata koja se može refundirati.");

        // Vraca se tacno onaj iznos koji je stvarno naplacen, a ne trenutna cijena plana.
        var refundId = await _gateway.RefundAsync(payment.StripePaymentIntentId, ToMinorUnits(payment.Amount), payment.Id);

        await ApplyRefundAsync(payment, refundId, payment.Amount, $"Refund: {reason}", adminId);

        return await GetResponseAsync(subscriptionId);
    }

    public async Task HandleWebhookAsync(PaymentWebhookEvent webhookEvent)
    {
        switch (webhookEvent.Type)
        {
            case PaymentWebhookEventType.PaymentSucceeded:
                await ActivateAsync(webhookEvent.PaymentIntentId, webhookEvent.AmountMinor, webhookEvent.Currency);
                break;
            case PaymentWebhookEventType.PaymentFailed:
                await MarkPaymentFailedAsync(webhookEvent.PaymentIntentId);
                break;
            case PaymentWebhookEventType.PaymentRefunded:
                await HandleExternalRefundAsync(webhookEvent.PaymentIntentId, webhookEvent.AmountMinor);
                break;
        }
    }

    // Idempotentno: poziva se i iz confirm endpointa i iz webhook-a, drugi poziv ne mijenja nista.
    private async Task ActivateAsync(string paymentIntentId, long amountReceivedMinor, string currency)
    {
        var payment = await FindPaymentAsync(paymentIntentId);

        if (payment is null || payment.Status == PaymentStatus.Completed || payment.Status == PaymentStatus.Refunded)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var subscription = payment.Subscription;

        payment.Status = PaymentStatus.Completed;
        payment.PaidAt = now;
        payment.Amount = amountReceivedMinor / 100m;
        payment.Currency = currency;

        if (subscription.Status == SubscriptionStatus.Pending)
        {
            SubscriptionStateMachine.Transition(subscription, SubscriptionStatus.Active, "Plaćanje potvrđeno.", now, null);
            subscription.StartDate = now;
            subscription.EndDate = now.AddMonths(subscription.SubscriptionPlan.DurationInMonths);

            AddNotification(
                payment.UserId,
                "Pretplata aktivirana",
                $"Plaćanje je uspjelo. Pretplata \"{subscription.SubscriptionPlan.Name}\" važi do {subscription.EndDate:dd.MM.yyyy}.",
                NotificationType.Subscription,
                now);
        }
        else
        {
            _logger.LogError(
                "Uplata {PaymentIntentId} je stigla za pretplatu {SubscriptionId} u statusu {Status}; potreban je ručni refund.",
                paymentIntentId, subscription.Id, subscription.Status);
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Drugi tok (webhook ili confirm) je vec obradio istu uplatu.
            _logger.LogInformation("Uplata {PaymentIntentId} je već obrađena.", paymentIntentId);
            return;
        }

        if (subscription.Status == SubscriptionStatus.Active)
        {
            await PublishEmailAsync(
                payment.User,
                EmailTemplates.SubscriptionActivated(payment.User.FirstName, subscription.SubscriptionPlan.Name, subscription.EndDate!.Value));
        }
    }

    private async Task MarkPaymentFailedAsync(string paymentIntentId)
    {
        var payment = await FindPaymentAsync(paymentIntentId);

        if (payment is null || payment.Status != PaymentStatus.Pending)
        {
            return;
        }

        payment.Status = PaymentStatus.Failed;

        AddNotification(
            payment.UserId,
            "Plaćanje nije uspjelo",
            "Plaćanje pretplate nije uspjelo. Pokušajte ponovo s drugom karticom.",
            NotificationType.Payment,
            DateTime.UtcNow);

        await _db.SaveChangesAsync();
    }

    private async Task HandleExternalRefundAsync(string paymentIntentId, long amountRefundedMinor)
    {
        var payment = await FindPaymentAsync(paymentIntentId);

        if (payment is null || payment.Status == PaymentStatus.Refunded)
        {
            return;
        }

        await ApplyRefundAsync(payment, null, amountRefundedMinor / 100m, "Refund izvršen u Stripe-u.", null);
    }

    private async Task ApplyRefundAsync(Payment payment, string? refundId, decimal amount, string note, int? changedByUserId)
    {
        var now = DateTime.UtcNow;
        var subscription = payment.Subscription;

        payment.Status = PaymentStatus.Refunded;
        payment.StripeRefundId = refundId;
        payment.RefundedAmount = amount;
        payment.RefundedAt = now;

        if (SubscriptionStateMachine.CanTransition(subscription.Status, SubscriptionStatus.Cancelled))
        {
            SubscriptionStateMachine.Transition(subscription, SubscriptionStatus.Cancelled, note, now, changedByUserId);
            subscription.CancelledAt = now;
            subscription.CancellationReason = note;
        }

        AddNotification(
            payment.UserId,
            "Povrat novca",
            $"Vraćeno vam je {amount:0.00} {payment.Currency.ToUpperInvariant()} za pretplatu \"{subscription.SubscriptionPlan.Name}\".",
            NotificationType.Refund,
            now);

        await _db.SaveChangesAsync();

        await PublishEmailAsync(
            payment.User,
            EmailTemplates.PaymentRefunded(payment.User.FirstName, amount, payment.Currency));
    }

    private Task<Payment?> FindPaymentAsync(string paymentIntentId)
    {
        return _db.Payments
            .Include(p => p.Subscription).ThenInclude(s => s.SubscriptionPlan)
            .Include(p => p.User)
            .SingleOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId);
    }

    private Task<SubscriptionResponse> GetResponseAsync(int subscriptionId)
    {
        return _db.Subscriptions
            .AsNoTracking()
            .Where(s => s.Id == subscriptionId)
            .Select(ToResponse)
            .SingleAsync();
    }

    private void AddNotification(int userId, string title, string message, NotificationType type, DateTime now)
    {
        _db.Notifications.Add(new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            CreatedAt = now
        });
    }

    // Slanje maila nije kriticno za placanje, pa greska RabbitMQ-a samo ostaje u logu.
    private async Task PublishEmailAsync(User user, EmailContent content)
    {
        try
        {
            await _publisher.PublishAsync(QueueNames.Email, new EmailMessage(user.Email, content.Subject, content.Body));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Slanje e-mail poruke na RabbitMQ nije uspjelo za korisnika {UserId}.", user.Id);
        }
    }

    private static long ToMinorUnits(decimal amount) =>
        (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
}
