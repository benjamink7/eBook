using eBook.Services.Interfaces;
using eBook.Services.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eBook.API.Controllers;

[ApiController]
[Route("api/stripe/webhook")]
[AllowAnonymous]
public class StripeWebhookController : ControllerBase
{
    private readonly IPaymentGateway _gateway;
    private readonly ISubscriptionService _subscriptionService;

    public StripeWebhookController(IPaymentGateway gateway, ISubscriptionService subscriptionService)
    {
        _gateway = gateway;
        _subscriptionService = subscriptionService;
    }

    // Stripe zove ovaj endpoint; potpis se provjerava prije bilo kakve obrade.
    [HttpPost]
    public async Task<IActionResult> Receive()
    {
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();

        var webhookEvent = _gateway.ParseWebhook(payload, Request.Headers["Stripe-Signature"].ToString());

        if (webhookEvent is not null)
        {
            await _subscriptionService.HandleWebhookAsync(webhookEvent);
        }

        return Ok();
    }
}
