using eBook.Model.Exceptions;
using eBook.Model.Profile;
using eBook.Services.Database;
using eBook.Services.Files;
using eBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eBook.Services.Implementations;

public class ProfileService : IProfileService
{
    public const int MaxPictureSizeBytes = 2 * 1024 * 1024;

    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ProfileService(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ProfileResponse> GetAsync()
    {
        var userId = _currentUser.UserId;

        return await _db.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new ProfileResponse
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Username = user.Username,
                PhoneNumber = user.PhoneNumber,
                CityId = user.CityId,
                CityName = user.City.Name,
                CountryName = user.City.Country.Name,
                HasPicture = user.ProfilePicture != null,
                CreatedAt = user.CreatedAt
            })
            .SingleOrDefaultAsync()
            ?? throw new NotFoundException("Korisnik nije pronađen.");
    }

    public async Task<ProfileResponse> UpdateAsync(ProfileUpdateRequest request)
    {
        var userId = _currentUser.UserId;
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == userId)
            ?? throw new NotFoundException("Korisnik nije pronađen.");

        if (await _db.Users.AnyAsync(u => u.Email == email && u.Id != userId))
        {
            throw new BusinessException("Korisnik s ovom e-mail adresom već postoji.");
        }

        if (!await _db.Cities.AnyAsync(city => city.Id == request.CityId))
        {
            throw new BusinessException("Odabrani grad ne postoji.");
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Email = email;
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
        user.CityId = request.CityId;

        await _db.SaveChangesAsync();

        return await GetAsync();
    }

    public async Task SetPictureAsync(byte[] image)
    {
        if (image.Length == 0 || image.Length > MaxPictureSizeBytes)
        {
            throw new BusinessException("Slika profila mora imati najviše 2 MB.");
        }

        if (ImageValidator.DetectContentType(image) is null)
        {
            throw new BusinessException("Dozvoljeni formati slike su PNG i JPEG.");
        }

        var userId = _currentUser.UserId;

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == userId)
            ?? throw new NotFoundException("Korisnik nije pronađen.");

        user.ProfilePicture = image;
        await _db.SaveChangesAsync();
    }

    public async Task<(byte[] Content, string ContentType)> GetPictureAsync()
    {
        var userId = _currentUser.UserId;

        var picture = await _db.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => user.ProfilePicture)
            .SingleOrDefaultAsync()
            ?? throw new NotFoundException("Slika profila nije postavljena.");

        return (picture, ImageValidator.DetectContentType(picture) ?? "application/octet-stream");
    }
}
