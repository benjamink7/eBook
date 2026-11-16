using System.ComponentModel.DataAnnotations;

namespace eBook.Model.ReadingProgress;

public class ReadingProgressUpsertRequest
{
    [Range(1, int.MaxValue, ErrorMessage = "Knjiga nije ispravna.")]
    public int BookId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Broj stranice mora biti najmanje 1.")]
    public int CurrentPage { get; set; }
}
