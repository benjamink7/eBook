using System.ComponentModel.DataAnnotations;

namespace eBook.Model.Reports;

public class PopularBooksReportRequest : ReportRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Žanr nije ispravan.")]
    public int? GenreId { get; set; }

    [Range(1, 100, ErrorMessage = "Broj knjiga mora biti između 1 i 100.")]
    public int Top { get; set; } = 20;
}
