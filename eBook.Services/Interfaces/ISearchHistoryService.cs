using eBook.Model.SearchHistory;

namespace eBook.Services.Interfaces;

public interface ISearchHistoryService
{
    Task<List<SearchHistoryResponse>> GetRecentAsync();
    Task ClearAsync();
}
