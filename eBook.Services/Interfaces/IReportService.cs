using eBook.Model.Reports;

namespace eBook.Services.Interfaces;

public interface IReportService
{
    Task<RevenueReportResponse> GetRevenueAsync(ReportRequest request);
    Task<PopularBooksReportResponse> GetPopularBooksAsync(PopularBooksReportRequest request);
}
