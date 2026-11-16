using eBook.Model.Common;
using eBook.Model.Constants;
using eBook.Model.Countries;
using eBook.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eBook.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CountriesController : ControllerBase
{
    private readonly ICountryService _countryService;

    public CountriesController(ICountryService countryService)
    {
        _countryService = countryService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<CountryResponse>>> Get([FromQuery] CountrySearchRequest request)
    {
        return Ok(await _countryService.GetAsync(request));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CountryResponse>> GetById(int id)
    {
        return Ok(await _countryService.GetByIdAsync(id));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<CountryResponse>> Create(CountryUpsertRequest request)
    {
        var country = await _countryService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = country.Id }, country);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CountryResponse>> Update(int id, CountryUpsertRequest request)
    {
        return Ok(await _countryService.UpdateAsync(id, request));
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _countryService.DeleteAsync(id);
        return NoContent();
    }
}
