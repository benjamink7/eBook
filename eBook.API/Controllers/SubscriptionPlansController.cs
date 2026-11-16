using eBook.Model.Common;
using eBook.Model.Constants;
using eBook.Model.Subscriptions;
using eBook.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eBook.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Admin)]
public class SubscriptionPlansController : ControllerBase
{
    private readonly ISubscriptionPlanService _planService;

    public SubscriptionPlansController(ISubscriptionPlanService planService)
    {
        _planService = planService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<PlanAdminResponse>>> Get([FromQuery] PlanSearchRequest request)
    {
        return Ok(await _planService.GetAsync(request));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PlanAdminResponse>> GetById(int id)
    {
        return Ok(await _planService.GetByIdAsync(id));
    }

    [HttpPost]
    public async Task<ActionResult<PlanAdminResponse>> Create(PlanUpsertRequest request)
    {
        var plan = await _planService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = plan.Id }, plan);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<PlanAdminResponse>> Update(int id, PlanUpsertRequest request)
    {
        return Ok(await _planService.UpdateAsync(id, request));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _planService.DeleteAsync(id);
        return NoContent();
    }
}
