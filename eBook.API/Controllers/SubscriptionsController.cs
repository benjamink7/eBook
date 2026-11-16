using eBook.Model.Common;
using eBook.Model.Constants;
using eBook.Model.Subscriptions;
using eBook.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eBook.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpGet("plans")]
    public async Task<ActionResult<List<SubscriptionPlanResponse>>> GetPlans()
    {
        return Ok(await _subscriptionService.GetPlansAsync());
    }

    [HttpGet("current")]
    public async Task<ActionResult<SubscriptionResponse>> GetCurrent()
    {
        var current = await _subscriptionService.GetCurrentAsync();
        return current is null ? NoContent() : Ok(current);
    }

    [HttpGet("my")]
    public async Task<ActionResult<PagedResult<SubscriptionResponse>>> GetMine([FromQuery] PagedRequest request)
    {
        return Ok(await _subscriptionService.GetMineAsync(request));
    }

    [HttpPost("checkout")]
    public async Task<ActionResult<CheckoutResponse>> Checkout(CheckoutRequest request)
    {
        return Ok(await _subscriptionService.CheckoutAsync(request));
    }

    [HttpPost("{id:int}/confirm")]
    public async Task<ActionResult<SubscriptionResponse>> Confirm(int id)
    {
        return Ok(await _subscriptionService.ConfirmAsync(id));
    }

    [HttpPost("{id:int}/cancel")]
    public async Task<ActionResult<SubscriptionResponse>> Cancel(int id, SubscriptionReasonRequest request)
    {
        return Ok(await _subscriptionService.CancelAsync(id, request));
    }

    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<List<SubscriptionStatusHistoryResponse>>> GetHistory(int id)
    {
        return Ok(await _subscriptionService.GetHistoryAsync(id));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpGet]
    public async Task<ActionResult<PagedResult<SubscriptionResponse>>> GetAll([FromQuery] SubscriptionSearchRequest request)
    {
        return Ok(await _subscriptionService.GetAllAsync(request));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("{id:int}/refund")]
    public async Task<ActionResult<SubscriptionResponse>> Refund(int id, SubscriptionReasonRequest request)
    {
        return Ok(await _subscriptionService.RefundAsync(id, request));
    }
}
