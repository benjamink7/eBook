using eBook.Model.Cities;
using eBook.Model.Common;
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
}
