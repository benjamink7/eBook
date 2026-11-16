using eBook.Model.Enums;
using eBook.Model.Exceptions;
using eBook.Services.Database;

namespace eBook.Services.Subscriptions;

// Jedino mjesto na kojem se mijenja status pretplate; svaka promjena ostavlja trag u historiji.
public static class SubscriptionStateMachine
{
    private static readonly Dictionary<SubscriptionStatus, SubscriptionStatus[]> AllowedTransitions = new()
    {
        [SubscriptionStatus.Pending] = new[] { SubscriptionStatus.Active, SubscriptionStatus.Cancelled },
        [SubscriptionStatus.Active] = new[] { SubscriptionStatus.Cancelled, SubscriptionStatus.Expired },
        [SubscriptionStatus.Cancelled] = Array.Empty<SubscriptionStatus>(),
        [SubscriptionStatus.Expired] = Array.Empty<SubscriptionStatus>()
    };

    public static bool CanTransition(SubscriptionStatus from, SubscriptionStatus to) =>
        AllowedTransitions[from].Contains(to);

    public static void Transition(
        Subscription subscription, SubscriptionStatus to, string note, DateTime now, int? changedByUserId)
    {
        if (!CanTransition(subscription.Status, to))
        {
            throw new BusinessException(
                $"Promjena statusa pretplate iz '{subscription.Status}' u '{to}' nije dozvoljena.");
        }

        subscription.StatusHistory.Add(new SubscriptionStatusHistory
        {
            FromStatus = subscription.Status,
            ToStatus = to,
            Note = note,
            ChangedAt = now,
            ChangedByUserId = changedByUserId
        });

        subscription.Status = to;
        subscription.StatusChangedAt = now;
        subscription.StatusChangeNote = note;
    }
}
