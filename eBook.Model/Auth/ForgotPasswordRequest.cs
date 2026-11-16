using System.ComponentModel.DataAnnotations;

namespace eBook.Model.Auth;

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "E-mail adresa je obavezna.")]
    [EmailAddress(ErrorMessage = "Unesite validnu e-mail adresu u formatu: ime@domena.ba")]
    public string Email { get; set; } = null!;
}
