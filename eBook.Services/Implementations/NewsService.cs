using System.Linq.Expressions;
using eBook.Model.Common;
using eBook.Model.Exceptions;
using eBook.Model.News;
using eBook.Services.Database;
using eBook.Services.Extensions;
using eBook.Services.Files;
using eBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eBook.Services.Implementations;

public class NewsService : INewsService
{
    public const int MaxImageSizeBytes = 2 * 1024 * 1024;

    private static readonly Expression<Func<News, NewsResponse>> ToResponse = news => new NewsResponse
    {
        Id = news.Id,
        Title = news.Title,
        Content = news.Content,
        CreatedAt = news.CreatedAt,
        CreatedBy = news.CreatedByUser.Username
    };

    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public NewsService(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<PagedResult<NewsResponse>> GetAsync(NewsSearchRequest request)
    {
        var query = _db.News.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(news => news.Title.Contains(term) || news.Content.Contains(term));
        }

        return query
            .OrderByDescending(news => news.CreatedAt)
            .Select(ToResponse)
            .ToPagedResultAsync(request);
    }

    public async Task<NewsResponse> GetByIdAsync(int id)
    {
        return await _db.News.AsNoTracking().Where(news => news.Id == id).Select(ToResponse).SingleOrDefaultAsync()
            ?? throw new NotFoundException("Novost nije pronađena.");
    }

    public async Task<NewsResponse> CreateAsync(NewsUpsertRequest request, byte[]? image)
    {
        if (image is null)
        {
            throw new BusinessException("Slika novosti je obavezna.");
        }

        ValidateImage(image);

        var news = new News
        {
            Title = request.Title.Trim(),
            Content = request.Content.Trim(),
            Image = image,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUser.UserId
        };

        _db.News.Add(news);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(news.Id);
    }

    public async Task<NewsResponse> UpdateAsync(int id, NewsUpsertRequest request, byte[]? image)
    {
        var news = await _db.News.SingleOrDefaultAsync(n => n.Id == id)
            ?? throw new NotFoundException("Novost nije pronađena.");

        news.Title = request.Title.Trim();
        news.Content = request.Content.Trim();

        if (image is not null)
        {
            ValidateImage(image);
            news.Image = image;
        }

        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var news = await _db.News.SingleOrDefaultAsync(n => n.Id == id)
            ?? throw new NotFoundException("Novost nije pronađena.");

        _db.News.Remove(news);
        await _db.SaveChangesAsync();
    }

    public async Task<(byte[] Content, string ContentType)> GetImageAsync(int id)
    {
        var image = await _db.News.AsNoTracking()
            .Where(news => news.Id == id)
            .Select(news => news.Image)
            .SingleOrDefaultAsync()
            ?? throw new NotFoundException("Novost nije pronađena.");

        return (image, ImageValidator.DetectContentType(image) ?? "application/octet-stream");
    }

    private static void ValidateImage(byte[] image)
    {
        if (image.Length == 0 || image.Length > MaxImageSizeBytes)
        {
            throw new BusinessException("Slika novosti mora imati najviše 2 MB.");
        }

        if (ImageValidator.DetectContentType(image) is null)
        {
            throw new BusinessException("Dozvoljeni formati slike su PNG i JPEG.");
        }
    }
}
