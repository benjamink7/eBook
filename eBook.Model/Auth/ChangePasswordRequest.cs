using System.ComponentModel.DataAnnotations;

namespace eBook.Model.Auth;

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Trenutna lozinka je obavezna.")]
    public string OldPassword { get; set; } = null!;

    [Required(ErrorMessage = "Nova lozinka je obavezna.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,100}$", ErrorMessage = "Lozinka mora imati 8 do 100 znakova, uključujući veliko slovo, malo slovo i broj.")]
    public string NewPassword { get; set; } = null!;

    [Required(ErrorMessage = "Potvrda nove lozinke je obavezna.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Lozinke se ne podudaraju.")]
    public string ConfirmNewPassword { get; set; } = null!;
}
