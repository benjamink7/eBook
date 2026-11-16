using eBook.Model.Books;
using eBook.Model.Common;
using eBook.Model.Constants;
using eBook.Model.Exceptions;
using eBook.Services.Implementations;
using eBook.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eBook.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BooksController : ControllerBase
{
    private static readonly string[] AllowedCoverContentTypes = ["image/png", "image/jpeg"];

    private readonly IBookService _bookService;

    public BooksController(IBookService bookService)
    {
        _bookService = bookService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<BookListItemResponse>>> Get([FromQuery] BookSearchRequest request)
    {
        return Ok(await _bookService.GetAsync(request));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BookDetailsResponse>> GetById(int id)
    {
        return Ok(await _bookService.GetByIdAsync(id));
    }

    [HttpGet("{id:int}/pages/{pageNumber:int}")]
    public async Task<ActionResult<BookPageResponse>> GetPage(int id, int pageNumber)
    {
        return Ok(await _bookService.GetPageAsync(id, pageNumber));
    }

    [HttpGet("{id:int}/cover")]
    public async Task<IActionResult> GetCover(int id)
    {
        var (content, contentType) = await _bookService.GetCoverAsync(id);
        return File(content, contentType);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<BookDetailsResponse>> Create(BookUpsertRequest request)
    {
        var book = await _bookService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = book.Id }, book);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<BookDetailsResponse>> Update(int id, BookUpsertRequest request)
    {
        return Ok(await _bookService.UpdateAsync(id, request));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _bookService.DeleteAsync(id);
        return NoContent();
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{id:int}/cover")]
    public async Task<IActionResult> SetCover(int id, IFormFile file)
    {
        if (file.Length == 0)
        {
            throw new BusinessException("Odaberite sliku naslovnice.");
        }

        if (file.Length > BookService.MaxCoverSizeBytes)
        {
            throw new BusinessException("Slika naslovnice može imati najviše 2 MB.");
        }

        if (!AllowedCoverContentTypes.Contains(file.ContentType))
        {
            throw new BusinessException("Dozvoljeni formati slike su PNG i JPEG.");
        }

        using var memory = new MemoryStream();
        await file.CopyToAsync(memory);

        await _bookService.SetCoverAsync(id, memory.ToArray());
        return NoContent();
    }
}
