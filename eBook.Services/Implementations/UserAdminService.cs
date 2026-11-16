using System.Linq.Expressions;
using eBook.Model.Common;
using eBook.Model.Enums;
using eBook.Model.Exceptions;
using eBook.Model.Users;
using eBook.Services.Database;
using eBook.Services.Extensions;
using eBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace eBook.Services.Implementations;

public class UserAdminService : IUserAdminService
{
    private static readonly Expression<Func<User, UserAdminResponse>> ToResponse = user => new UserAdminResponse
    {
        Id = user.Id,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Username = user.Username,
        PhoneNumber = user.PhoneNumber,
        CityName = user.City.Name,
        CountryName = user.City.Country.Name,
        IsActive = user.IsActive,
        HasActiveSubscription = user.Subscriptions.Any(s => s.Status == SubscriptionStatus.Active),
        Roles = user.UserRoles.Select(userRole => userRole.Role.Name).ToList(),
        CreatedAt = user.CreatedAt
    };

    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<UserAdminService> _logger;

    public UserAdminService(ApplicationDbContext db, ICurrentUserService currentUser, ILogger<UserAdminService> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public Task<PagedResult<UserAdminResponse>> GetAsync(UserSearchRequest request)
    {
        var query = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(user =>
                user.FirstName.Contains(term)
                || user.LastName.Contains(term)
                || user.Username.Contains(term)
                || user.Email.Contains(term));
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(user => user.IsActive == request.IsActive.Value);
        }

        return query
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .Select(ToResponse)
            .ToPagedResultAsync(request);
    }

    public async Task<UserAdminResponse> GetByIdAsync(int id)
    {
        return await _db.Users.AsNoTracking().Where(user => user.Id == id).Select(ToResponse).SingleOrDefaultAsync()
            ?? throw new NotFoundException("Korisnik nije pronađen.");
    }

    public async Task<UserAdminResponse> SetActiveAsync(int id, bool isActive)
    {
        if (id == _currentUser.UserId && !isActive)
        {
            throw new BusinessException("Ne možete deaktivirati vlastiti račun.");
        }

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == id)
            ?? throw new NotFoundException("Korisnik nije pronađen.");

        user.IsActive = isActive;

        // Deaktivirani korisnik gubi postojecu sesiju jer se verzija tokena povecava.
        if (!isActive)
        {
            user.TokenVersion++;
        }

        await _db.SaveChangesAsync();

        _logger.LogInformation("Administrator {AdminId} je postavio aktivnost korisnika {UserId} na {IsActive}.",
            _currentUser.UserId, id, isActive);

        return await GetByIdAsync(id);
    }
}
