using System.ComponentModel.DataAnnotations;

namespace eBook.Model.Subscriptions;

public class PlanUpsertRequest
{
    [Required(ErrorMessage = "Naziv plana je obavezan.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Naziv plana mora imati između 2 i 100 znakova.")]
    public string Name { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Opis može imati najviše 500 znakova.")]
    public string? Description { get; set; }

    [Range(0.5, 10000, ErrorMessage = "Cijena mora biti između 0.50 i 10000.")]
    public decimal Price { get; set; }

    [Required(ErrorMessage = "Valuta je obavezna.")]
    [RegularExpression("^[a-zA-Z]{3}$", ErrorMessage = "Valuta mora imati 3 slova, npr. EUR.")]
    public string Currency { get; set; } = null!;

    [Range(1, 60, ErrorMessage = "Trajanje mora biti između 1 i 60 mjeseci.")]
    public int DurationInMonths { get; set; }

    public bool IsActive { get; set; } = true;
}
