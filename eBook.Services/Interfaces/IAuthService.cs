using eBook.Model.Auth;

namespace eBook.Services.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task LogoutAsync();
    Task<AuthResponse> ChangePasswordAsync(ChangePasswordRequest request);
    Task<bool> IsSessionValidAsync(int userId, int tokenVersion);
}
