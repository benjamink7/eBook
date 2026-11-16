using System.Security.Cryptography;
using eBook.Model.Auth;
using eBook.Model.Constants;
using eBook.Model.Exceptions;
using eBook.Model.Messaging;
using eBook.Services.Database;
using eBook.Services.Interfaces;
using eBook.Services.Messaging;
using eBook.Services.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eBook.Services.Implementations;

public class AuthService : IAuthService
{
    private const int ResetCodeValidityMinutes = 15;

    private readonly ApplicationDbContext _db;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly ICurrentUserService _currentUser;
    private readonly IMessagePublisher _publisher;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        ApplicationDbContext db,
        IJwtTokenService jwtTokenService,
        ICurrentUserService currentUser,
        IMessagePublisher publisher,
        ILogger<AuthService> logger)
    {
        _db = db;
        _jwtTokenService = jwtTokenService;
        _currentUser = currentUser;
        _publisher = publisher;
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

        await PublishEmailAsync(user.Id, user.Email, EmailTemplates.Welcome(user.FirstName));

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

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email && u.IsActive);

        // Odgovor je isti bez obzira postoji li racun, da se ne otkriva koje adrese su registrovane.
        if (user is null)
        {
            _logger.LogInformation("Zahtjev za reset lozinke za nepostojecu ili neaktivnu adresu.");
            return;
        }

        var code = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();
        var (hash, salt) = PasswordHasher.Hash(code);

        user.PasswordResetCodeHash = $"{salt}:{hash}";
        user.PasswordResetCodeExpiry = DateTime.UtcNow.AddMinutes(ResetCodeValidityMinutes);
        await _db.SaveChangesAsync();

        await PublishEmailAsync(
            user.Id, user.Email, EmailTemplates.PasswordReset(user.FirstName, code, ResetCodeValidityMinutes));
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == email && u.IsActive);

        if (user is null || !IsResetCodeValid(user, request.Code))
        {
            throw new BusinessException("Kod za reset lozinke nije ispravan ili je istekao.");
        }

        var (hash, salt) = PasswordHasher.Hash(request.NewPassword);
        user.PasswordHash = hash;
        user.PasswordSalt = salt;
        user.PasswordResetCodeHash = null;
        user.PasswordResetCodeExpiry = null;
        user.TokenVersion++;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Lozinka resetovana za korisnika {UserId}.", user.Id);
    }

    public Task<bool> IsSessionValidAsync(int userId, int tokenVersion)
    {
        return _db.Users.AnyAsync(user =>
            user.Id == userId && user.IsActive && user.TokenVersion == tokenVersion);
    }

    private static bool IsResetCodeValid(User user, string code)
    {
        if (user.PasswordResetCodeHash is null
            || user.PasswordResetCodeExpiry is null
            || user.PasswordResetCodeExpiry < DateTime.UtcNow)
        {
            return false;
        }

        var parts = user.PasswordResetCodeHash.Split(':');

        return parts.Length == 2 && PasswordHasher.Verify(code, parts[1], parts[0]);
    }

    // Slanje maila nije kriticno za registraciju, pa greska RabbitMQ-a samo ostaje u logu.
    private async Task PublishEmailAsync(int userId, string to, EmailContent content)
    {
        try
        {
            await _publisher.PublishAsync(QueueNames.Email, new EmailMessage(to, content.Subject, content.Body));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Slanje e-mail poruke na RabbitMQ nije uspjelo za korisnika {UserId}.", userId);
        }
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
