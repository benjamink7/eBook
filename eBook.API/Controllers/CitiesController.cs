using eBook.Model.Cities;
using eBook.Model.Common;
using eBook.Model.Constants;
using eBook.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eBook.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CitiesController : ControllerBase
{
    private readonly ICityService _cityService;

    public CitiesController(ICityService cityService)
    {
        _cityService = cityService;
    }

    // Anonimno je dozvoljeno jer se gradovi biraju u formi za registraciju.
    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult<PagedResult<CityResponse>>> Get([FromQuery] CitySearchRequest request)
    {
        return Ok(await _cityService.GetAsync(request));
    }

    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CityResponse>> GetById(int id)
    {
        return Ok(await _cityService.GetByIdAsync(id));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<CityResponse>> Create(CityUpsertRequest request)
    {
        var city = await _cityService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = city.Id }, city);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CityResponse>> Update(int id, CityUpsertRequest request)
    {
        return Ok(await _cityService.UpdateAsync(id, request));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _cityService.DeleteAsync(id);
        return NoContent();
    }
}
