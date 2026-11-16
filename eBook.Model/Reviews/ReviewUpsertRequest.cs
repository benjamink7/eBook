using System.ComponentModel.DataAnnotations;

namespace eBook.Model.Reviews;

public class ReviewUpsertRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Knjiga nije ispravna.")]
    public int BookId { get; set; }

    [Range(1, 5, ErrorMessage = "Ocjena mora biti između 1 i 5.")]
    public int Rating { get; set; }

    [StringLength(2000, ErrorMessage = "Komentar može imati najviše 2000 znakova.")]
    public string? Comment { get; set; }
}
