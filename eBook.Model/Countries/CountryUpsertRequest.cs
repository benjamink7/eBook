using System.ComponentModel.DataAnnotations;

namespace eBook.Model.Countries;

public class CountryUpsertRequest
{
    [Required(ErrorMessage = "Naziv države je obavezan.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Naziv države mora imati između 2 i 100 znakova.")]
    public string Name { get; set; } = null!;
}
