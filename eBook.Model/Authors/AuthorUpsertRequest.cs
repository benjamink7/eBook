using System.ComponentModel.DataAnnotations;

namespace eBook.Model.Authors;

public class AuthorUpsertRequest
{
    [Required(ErrorMessage = "Ime autora je obavezno.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Ime mora imati između 2 i 100 znakova.")]
    public string FirstName { get; set; } = null!;

    [Required(ErrorMessage = "Prezime autora je obavezno.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Prezime mora imati između 2 i 100 znakova.")]
    public string LastName { get; set; } = null!;

    [StringLength(4000, ErrorMessage = "Biografija može imati najviše 4000 znakova.")]
    public string? Biography { get; set; }

    [Range(1000, 2100, ErrorMessage = "Godina rođenja mora biti između 1000 i 2100.")]
    public int? BirthYear { get; set; }
}
