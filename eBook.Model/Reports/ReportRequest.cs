using System.ComponentModel.DataAnnotations;

namespace eBook.Model.Reports;

public class ReportRequest
{
    [Required(ErrorMessage = "Datum početka je obavezan.")]
    public DateTime? From { get; set; }

    [Required(ErrorMessage = "Datum završetka je obavezan.")]
    public DateTime? To { get; set; }
}
