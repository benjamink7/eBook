using System.ComponentModel.DataAnnotations;

namespace eBook.Model.Cities;

public class CityUpsertRequest
{
    [Required(ErrorMessage = "Naziv grada je obavezan.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Naziv grada mora imati između 2 i 100 znakova.")]
    public string Name { get; set; } = null!;

    [Range(1, int.MaxValue, ErrorMessage = "Odaberite državu.")]
    public int CountryId { get; set; }
}
