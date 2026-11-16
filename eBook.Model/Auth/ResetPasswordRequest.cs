using System.ComponentModel.DataAnnotations;

namespace eBook.Model.Auth;

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "E-mail adresa je obavezna.")]
    [EmailAddress(ErrorMessage = "Unesite validnu e-mail adresu u formatu: ime@domena.ba")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Kod je obavezan.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "Kod mora imati tačno 6 cifara, npr. 123456.")]
    public string Code { get; set; } = null!;

    [Required(ErrorMessage = "Nova lozinka je obavezna.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,100}$", ErrorMessage = "Lozinka mora imati 8 do 100 znakova, uključujući veliko slovo, malo slovo i broj.")]
    public string NewPassword { get; set; } = null!;

    [Required(ErrorMessage = "Potvrda nove lozinke je obavezna.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Lozinke se ne podudaraju.")]
    public string ConfirmNewPassword { get; set; } = null!;
}
