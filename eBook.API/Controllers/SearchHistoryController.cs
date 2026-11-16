using eBook.Model.SearchHistory;
using eBook.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eBook.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SearchHistoryController : ControllerBase
{
    private readonly ISearchHistoryService _searchHistoryService;

    public SearchHistoryController(ISearchHistoryService searchHistoryService)
    {
        _searchHistoryService = searchHistoryService;
    }

    [HttpGet]
    public async Task<ActionResult<List<SearchHistoryResponse>>> GetRecent()
    {
        return Ok(await _searchHistoryService.GetRecentAsync());
    }

    [HttpDelete]
    public async Task<IActionResult> Clear()
    {
        await _searchHistoryService.ClearAsync();
        return NoContent();
    }
}
