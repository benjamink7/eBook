using eBook.Model.Authors;
using eBook.Model.Common;
using eBook.Model.Constants;
using eBook.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eBook.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuthorsController : ControllerBase
{
    private readonly IAuthorService _authorService;

    public AuthorsController(IAuthorService authorService)
    {
        _authorService = authorService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<AuthorResponse>>> Get([FromQuery] AuthorSearchRequest request)
    {
        return Ok(await _authorService.GetAsync(request));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AuthorResponse>> GetById(int id)
    {
        return Ok(await _authorService.GetByIdAsync(id));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<AuthorResponse>> Create(AuthorUpsertRequest request)
    {
        var author = await _authorService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = author.Id }, author);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<AuthorResponse>> Update(int id, AuthorUpsertRequest request)
    {
        return Ok(await _authorService.UpdateAsync(id, request));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _authorService.DeleteAsync(id);
        return NoContent();
    }
}
