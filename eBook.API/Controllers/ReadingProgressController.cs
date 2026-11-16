using eBook.Model.Common;
using eBook.Model.ReadingProgress;
using eBook.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eBook.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReadingProgressController : ControllerBase
{
    private readonly IReadingProgressService _progressService;

    public ReadingProgressController(IReadingProgressService progressService)
    {
        _progressService = progressService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ReadingProgressResponse>>> Get([FromQuery] PagedRequest request)
    {
        return Ok(await _progressService.GetAsync(request));
    }

    [HttpGet("{bookId:int}")]
    public async Task<ActionResult<ReadingProgressResponse>> GetByBook(int bookId)
    {
        var progress = await _progressService.GetByBookAsync(bookId);
        return progress is null ? NoContent() : Ok(progress);
    }

    [HttpPut]
    public async Task<ActionResult<ReadingProgressResponse>> Upsert(ReadingProgressUpsertRequest request)
    {
        return Ok(await _progressService.UpsertAsync(request));
    }
}
