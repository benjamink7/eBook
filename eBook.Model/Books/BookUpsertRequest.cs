using System.ComponentModel.DataAnnotations;
using eBook.Model.Enums;

namespace eBook.Model.Books;

public class BookUpsertRequest
{
    [Required(ErrorMessage = "Naslov je obavezan.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Naslov može imati najviše 200 znakova.")]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "Opis je obavezan.")]
    [StringLength(4000, ErrorMessage = "Opis može imati najviše 4000 znakova.")]
    public string Description { get; set; } = null!;

    [Range(1000, 2100, ErrorMessage = "Godina izdanja mora biti između 1000 i 2100.")]
    public int PublishedYear { get; set; }

    [EnumDataType(typeof(BookAccessType), ErrorMessage = "Odaberite tip pristupa (Free ili Premium).")]
    public BookAccessType AccessType { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Odaberite autora.")]
    public int AuthorId { get; set; }

    [Required(ErrorMessage = "Odaberite barem jedan žanr.")]
    [MinLength(1, ErrorMessage = "Odaberite barem jedan žanr.")]
    public List<int> GenreIds { get; set; } = new();

    public bool IsActive { get; set; } = true;

    // Obavezno pri dodavanju knjige; pri izmjeni se mijenja samo ako je poslano.
    [StringLength(5_000_000, ErrorMessage = "Sadržaj knjige može imati najviše 5 miliona znakova.")]
    public string? Content { get; set; }
}
