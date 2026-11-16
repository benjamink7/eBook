using eBook.Model.Statistics;

namespace eBook.Services.Interfaces;

public interface IStatisticsService
{
    Task<StatisticsResponse> GetAsync();
}
