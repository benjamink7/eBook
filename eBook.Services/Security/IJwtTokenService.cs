using eBook.Model.Auth;
using eBook.Services.Database;

namespace eBook.Services.Security;

public interface IJwtTokenService
{
    AuthResponse CreateAuthResponse(User user, IReadOnlyCollection<string> roles);
}
