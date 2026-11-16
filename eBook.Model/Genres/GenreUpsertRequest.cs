using System.ComponentModel.DataAnnotations;

namespace eBook.Model.Genres;

public class GenreUpsertRequest
{
    [Required(ErrorMessage = "Naziv žanra je obavezan.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Naziv žanra mora imati između 2 i 100 znakova.")]
    public string Name { get; set; } = null!;

    [StringLength(500, ErrorMessage = "Opis može imati najviše 500 znakova.")]
    public string? Description { get; set; }
}
