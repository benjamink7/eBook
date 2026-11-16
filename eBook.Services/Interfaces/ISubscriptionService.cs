using eBook.Model.Common;
using eBook.Model.Subscriptions;
using eBook.Services.Payments;

namespace eBook.Services.Interfaces;

public interface ISubscriptionService
{
    Task<List<SubscriptionPlanResponse>> GetPlansAsync();

    Task<CheckoutResponse> CheckoutAsync(CheckoutRequest request);

    Task<SubscriptionResponse> ConfirmAsync(int subscriptionId);

    Task<SubscriptionResponse?> GetCurrentAsync();

    Task<PagedResult<SubscriptionResponse>> GetMineAsync(PagedRequest request);

    Task<PagedResult<SubscriptionResponse>> GetAllAsync(SubscriptionSearchRequest request);

    Task<List<SubscriptionStatusHistoryResponse>> GetHistoryAsync(int subscriptionId);

    Task<SubscriptionResponse> CancelAsync(int subscriptionId, SubscriptionReasonRequest request);

    Task<SubscriptionResponse> RefundAsync(int subscriptionId, SubscriptionReasonRequest request);

    Task HandleWebhookAsync(PaymentWebhookEvent webhookEvent);
}
