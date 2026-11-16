using eBook.Model.Common;
using eBook.Model.Subscriptions;

namespace eBook.Services.Interfaces;

public interface ISubscriptionPlanService
{
    Task<PagedResult<PlanAdminResponse>> GetAsync(PlanSearchRequest request);
    Task<PlanAdminResponse> GetByIdAsync(int id);
    Task<PlanAdminResponse> CreateAsync(PlanUpsertRequest request);
    Task<PlanAdminResponse> UpdateAsync(int id, PlanUpsertRequest request);
    Task DeleteAsync(int id);
}
