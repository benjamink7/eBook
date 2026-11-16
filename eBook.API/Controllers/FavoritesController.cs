using eBook.Model.Books;
using eBook.Model.Common;
using eBook.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eBook.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FavoritesController : ControllerBase
{
    private readonly IFavoriteService _favoriteService;

    public FavoritesController(IFavoriteService favoriteService)
    {
        _favoriteService = favoriteService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<BookListItemResponse>>> Get([FromQuery] PagedRequest request)
    {
        return Ok(await _favoriteService.GetAsync(request));
    }

    [HttpPut("{bookId:int}")]
    public async Task<IActionResult> Add(int bookId)
    {
        await _favoriteService.AddAsync(bookId);
        return NoContent();
    }

    [HttpDelete("{bookId:int}")]
    public async Task<IActionResult> Remove(int bookId)
    {
        await _favoriteService.RemoveAsync(bookId);
        return NoContent();
    }
}
