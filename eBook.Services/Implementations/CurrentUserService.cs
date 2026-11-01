using eBook.Model.Constants;
using eBook.Model.Exceptions;
using eBook.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace eBook.Services.Implementations;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirst(JwtClaimNames.Sub)?.Value;

            return int.TryParse(value, out var userId)
                ? userId
                : throw new UnauthorizedException("Korisnik nije prijavljen.");
        }
    }

    public bool IsAdmin => _httpContextAccessor.HttpContext?.User.IsInRole(AppRoles.Admin) ?? false;
}
