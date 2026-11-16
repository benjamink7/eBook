using System.ComponentModel.DataAnnotations;

namespace eBook.Model.Profile;

public class ProfileUpdateRequest
{
    [Required(ErrorMessage = "Ime je obavezno.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Ime mora imati između 2 i 50 znakova.")]
    public string FirstName { get; set; } = null!;

    [Required(ErrorMessage = "Prezime je obavezno.")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Prezime mora imati između 2 i 50 znakova.")]
    public string LastName { get; set; } = null!;

    [Required(ErrorMessage = "E-mail adresa je obavezna.")]
    [EmailAddress(ErrorMessage = "Unesite validnu e-mail adresu u formatu: ime@domena.ba")]
    [StringLength(150, ErrorMessage = "E-mail adresa može imati najviše 150 znakova.")]
    public string Email { get; set; } = null!;

    [RegularExpression(@"^\+?[0-9 ]{6,20}$", ErrorMessage = "Unesite telefon u formatu: +387 61 123 456")]
    public string? PhoneNumber { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Odaberite grad.")]
    public int CityId { get; set; }
}
