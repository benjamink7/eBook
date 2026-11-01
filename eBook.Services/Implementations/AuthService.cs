using eBook.Model.Auth;
using eBook.Model.Constants;
using eBook.Model.Exceptions;
using eBook.Services.Database;
using eBook.Services.Interfaces;
using eBook.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eBook.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext db,
        IJwtTokenService jwtTokenService,
        ICurrentUserService currentUser,
        ILogger<AuthService> logger)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim().ToLowerInvariant();

        if (await _db.Users.AnyAsync(user => user.Username == username))
        {
            throw new BusinessException("Korisničko ime je već zauzeto.");
        }

        if (await _db.Users.AnyAsync(user => user.Email == email))
        {
            throw new BusinessException("Korisnik s ovom e-mail adresom već postoji.");
        }

        if (!await _db.Cities.AnyAsync(city => city.Id == request.CityId))
        {
            throw new BusinessException("Odabrani grad ne postoji.");
        }

        // Rola se nikada ne uzima od klijenta; svaki novi korisnik dobija rolu User.
        var role = await _db.Roles.SingleAsync(r => r.Name == AppRoles.User);
        var (hash, salt) = PasswordHasher.Hash(request.Password);

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            Username = username,
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            PasswordHash = hash,
            PasswordSalt = salt,
            CityId = request.CityId,
            CreatedAt = DateTime.UtcNow,
            UserRoles = { new UserRole { Role = role } }
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Registrovan novi korisnik {UserId}.", user.Id);

        return _jwtTokenService.CreateAuthResponse(user, [role.Name]);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var username = request.Username.Trim();

        var user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(u => u.Username == username);

        if (user is null
            || !user.IsActive
            || !PasswordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            _logger.LogWarning("Neuspjesna prijava za korisnicko ime {Username}.", username);
            throw new UnauthorizedException("Neispravno korisničko ime ili lozinka.");
        }

        return _jwtTokenService.CreateAuthResponse(user, GetRoleNames(user));
    }

    public async Task LogoutAsync()
    {
        var user = await GetCurrentUserAsync();

        // Povecanje verzije invalidira sve izdate tokene tog korisnika na serveru.
        user.TokenVersion++;
        await _db.SaveChangesAsync();
    }

    public async Task<AuthResponse> ChangePasswordAsync(ChangePasswordRequest request)
    {
        var userId = _currentUser.UserId;

        var user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .SingleOrDefaultAsync(u => u.Id == userId)
            ?? throw new NotFoundException("Korisnik nije pronađen.");

        if (!PasswordHasher.Verify(request.OldPassword, user.PasswordHash, user.PasswordSalt))
        {
            throw new BusinessException("Trenutna lozinka nije ispravna.");
        }

        if (request.OldPassword == request.NewPassword)
        {
            throw new BusinessException("Nova lozinka mora biti različita od trenutne.");
        }

        var (hash, salt) = PasswordHasher.Hash(request.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.TokenVersion++;

        await _db.SaveChangesAsync();

        return _jwtTokenService.CreateAuthResponse(user, GetRoleNames(user));
    }

    public Task<bool> IsSessionValidAsync(int userId, int tokenVersion)
    {
        return _db.Users.AnyAsync(user =>
            user.Id == userId && user.IsActive && user.TokenVersion == tokenVersion);
    }

    private async Task<User> GetCurrentUserAsync()
    {
        var userId = _currentUser.UserId;

        return await _db.Users.SingleOrDefaultAsync(u => u.Id == userId)
            ?? throw new NotFoundException("Korisnik nije pronađen.");
    }

    private static List<string> GetRoleNames(User user) =>
        user.UserRoles.Select(userRole => userRole.Role.Name).ToList();
}
