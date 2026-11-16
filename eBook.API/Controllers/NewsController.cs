using eBook.Model.Common;
using eBook.Model.Constants;
using eBook.Model.Exceptions;
using eBook.Model.News;
using eBook.Services.Implementations;
using eBook.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eBook.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NewsController : ControllerBase
{
    private readonly INewsService _newsService;

    public NewsController(INewsService newsService)
    {
        _newsService = newsService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<NewsResponse>>> Get([FromQuery] NewsSearchRequest request)
    {
        return Ok(await _newsService.GetAsync(request));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<NewsResponse>> GetById(int id)
    {
        return Ok(await _newsService.GetByIdAsync(id));
    }

    [HttpGet("{id:int}/image")]
    public async Task<IActionResult> GetImage(int id)
    {
        var (content, contentType) = await _newsService.GetImageAsync(id);
        return File(content, contentType);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<NewsResponse>> Create([FromForm] NewsUpsertRequest request, IFormFile? image)
    {
        var news = await _newsService.CreateAsync(request, await ReadImageAsync(image));
        return CreatedAtAction(nameof(GetById), new { id = news.Id }, news);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{id:int}")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<NewsResponse>> Update(int id, [FromForm] NewsUpsertRequest request, IFormFile? image)
    {
        return Ok(await _newsService.UpdateAsync(id, request, await ReadImageAsync(image)));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _newsService.DeleteAsync(id);
        return NoContent();
    }

    private static async Task<byte[]?> ReadImageAsync(IFormFile? image)
    {
        if (image is null)
        {
            return null;
        }

        if (image.Length > NewsService.MaxImageSizeBytes)
        {
            throw new BusinessException("Slika novosti mora imati najviše 2 MB.");
        }

        using var memory = new MemoryStream();
        await image.CopyToAsync(memory);

        return memory.ToArray();
    }
}
