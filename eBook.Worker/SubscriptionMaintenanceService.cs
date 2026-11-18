using eBook.Model.Constants;
using eBook.Model.Enums;
using eBook.Model.Messaging;
using eBook.Services.Database;
using eBook.Services.Messaging;
using eBook.Services.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace eBook.Worker;

// Periodicni posao: istekle pretplate prelaze u status Expired, a korisnici dobiju podsjetnik prije isteka.
public class SubscriptionMaintenanceService : BackgroundService
{
    private const string ReminderTitle = "Pretplata uskoro ističe";
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan ReminderWindow = TimeSpan.FromDays(3);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<SubscriptionMaintenanceService> _logger;

    public SubscriptionMaintenanceService(
        IServiceScopeFactory scopeFactory,
        IMessagePublisher publisher,
        ILogger<SubscriptionMaintenanceService> logger)
    {
        _scopeFactory = scopeFactory;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Obrada pretplata nije uspjela. Ponovni pokušaj za {Interval} min.", Interval.TotalMinutes);
            }
        }
        while (await WaitForNextTickAsync(timer, stoppingToken));

        _logger.LogInformation("Posao za održavanje pretplata zaustavljen.");
    }

    private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var now = DateTime.UtcNow;

        var expired = await ExpireSubscriptionsAsync(db, now, stoppingToken);
        var reminded = await SendRemindersAsync(db, now, stoppingToken);

        if (expired > 0 || reminded > 0)
        {
            _logger.LogInformation("Obrada pretplata: isteklo {Expired}, podsjetnika poslano {Reminded}.", expired, reminded);
        }
    }

    private async Task<int> ExpireSubscriptionsAsync(ApplicationDbContext db, DateTime now, CancellationToken stoppingToken)
    {
        var subscriptions = await db.Subscriptions
            .Include(s => s.User)
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.Status == SubscriptionStatus.Active && s.EndDate != null && s.EndDate <= now)
            .ToListAsync(stoppingToken);

        var count = 0;

        foreach (var subscription in subscriptions)
        {
            SubscriptionStateMachine.Transition(subscription, SubscriptionStatus.Expired, "Pretplata je istekla.", now, null);

            db.Notifications.Add(new Notification
            {
                UserId = subscription.UserId,
                Title = "Pretplata je istekla",
                Message = $"Pretplata \"{subscription.SubscriptionPlan.Name}\" je istekla. Obnovite je za pristup Premium knjigama.",
                Type = NotificationType.Subscription,
                CreatedAt = now
            });

            try
            {
                await db.SaveChangesAsync(stoppingToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                // Pretplatu je u međuvremenu promijenio drugi proces (npr. otkazivanje); preskace se.
                db.ChangeTracker.Clear();
                continue;
            }

            count++;

            await PublishEmailAsync(
                subscription.User,
                EmailTemplates.SubscriptionExpired(subscription.User.FirstName, subscription.SubscriptionPlan.Name));
        }

        return count;
    }

    private async Task<int> SendRemindersAsync(ApplicationDbContext db, DateTime now, CancellationToken stoppingToken)
    {
        var windowEnd = now.Add(ReminderWindow);

        var subscriptions = await db.Subscriptions
            .Include(s => s.User)
            .Include(s => s.SubscriptionPlan)
            .Where(s => s.Status == SubscriptionStatus.Active
                && s.EndDate != null && s.EndDate > now && s.EndDate <= windowEnd
                && !db.Notifications.Any(n => n.UserId == s.UserId
                    && n.Title == ReminderTitle
                    && n.CreatedAt >= (s.StartDate ?? s.CreatedAt)))
            .ToListAsync(stoppingToken);

        foreach (var subscription in subscriptions)
        {
            var endDate = subscription.EndDate!.Value;

            db.Notifications.Add(new Notification
            {
                UserId = subscription.UserId,
                Title = ReminderTitle,
                Message = $"Pretplata \"{subscription.SubscriptionPlan.Name}\" ističe {endDate:dd.MM.yyyy}.",
                Type = NotificationType.Subscription,
                CreatedAt = now
            });

            await db.SaveChangesAsync(stoppingToken);

            await PublishEmailAsync(
                subscription.User,
                EmailTemplates.SubscriptionExpiringSoon(subscription.User.FirstName, subscription.SubscriptionPlan.Name, endDate));
        }

        return subscriptions.Count;
    }

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
}
