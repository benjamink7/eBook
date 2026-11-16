using eBook.Model.Common;
using eBook.Model.Reviews;
using eBook.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eBook.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReviewsController : ControllerBase
{
    private readonly IReviewService _reviewService;

    public ReviewsController(IReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ReviewResponse>>> Get([FromQuery] ReviewSearchRequest request)
    {
        return Ok(await _reviewService.GetAsync(request));
    }

    [HttpPut]
    public async Task<ActionResult<ReviewResponse>> Upsert(ReviewUpsertRequest request)
    {
        return Ok(await _reviewService.UpsertAsync(request));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _reviewService.DeleteAsync(id);
        return NoContent();
    }
}
