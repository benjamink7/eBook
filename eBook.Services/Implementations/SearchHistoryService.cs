using eBook.Model.SearchHistory;
using eBook.Services.Database;
using eBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eBook.Services.Implementations;

public class SearchHistoryService : ISearchHistoryService
{
    private const int MaxRecentSearches = 10;

    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public SearchHistoryService(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Task<List<SearchHistoryResponse>> GetRecentAsync()
    {
        var userId = _currentUser.UserId;

        return _db.SearchHistories
            .AsNoTracking()
            .Where(history => history.UserId == userId)
            .GroupBy(history => history.Query)
            .Select(group => new SearchHistoryResponse
            {
                Query = group.Key,
                LastSearchedAt = group.Max(history => history.CreatedAt)
            })
            .OrderByDescending(response => response.LastSearchedAt)
            .Take(MaxRecentSearches)
            .ToListAsync();
    }

    public async Task ClearAsync()
    {
        var userId = _currentUser.UserId;

        await _db.SearchHistories.Where(history => history.UserId == userId).ExecuteDeleteAsync();
    }
}
