using System.ComponentModel.DataAnnotations;

namespace eBook.Model.News;

public class NewsUpsertRequest
{
    [Required(ErrorMessage = "Naslov je obavezan.")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Naslov mora imati između 3 i 200 znakova.")]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "Tekst novosti je obavezan.")]
    [StringLength(4000, MinimumLength = 10, ErrorMessage = "Tekst mora imati između 10 i 4000 znakova.")]
    public string Content { get; set; } = null!;
}
