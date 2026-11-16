using eBook.Model.Constants;
using eBook.Model.Reports;
using eBook.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace eBook.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = AppRoles.Admin)]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("revenue")]
    public async Task<ActionResult<RevenueReportResponse>> GetRevenue([FromQuery] ReportRequest request)
    {
        return Ok(await _reportService.GetRevenueAsync(request));
    }

    [HttpGet("popular-books")]
    public async Task<ActionResult<PopularBooksReportResponse>> GetPopularBooks([FromQuery] PopularBooksReportRequest request)
    {
        return Ok(await _reportService.GetPopularBooksAsync(request));
    }
}
