using eBook.Model.Common;
using eBook.Model.Constants;
using eBook.Model.Genres;
using eBook.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eBook.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GenresController : ControllerBase
{
    private readonly IGenreService _genreService;

    public GenresController(IGenreService genreService)
    {
        _genreService = genreService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<GenreResponse>>> Get([FromQuery] GenreSearchRequest request)
    {
        return Ok(await _genreService.GetAsync(request));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<GenreResponse>> GetById(int id)
    {
        return Ok(await _genreService.GetByIdAsync(id));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<GenreResponse>> Create(GenreUpsertRequest request)
    {
        var genre = await _genreService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = genre.Id }, genre);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<GenreResponse>> Update(int id, GenreUpsertRequest request)
    {
        return Ok(await _genreService.UpdateAsync(id, request));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _genreService.DeleteAsync(id);
        return NoContent();
    }
}
