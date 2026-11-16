using System.Linq.Expressions;
using eBook.Model.Common;
using eBook.Model.Exceptions;
using eBook.Model.Subscriptions;
using eBook.Services.Database;
using eBook.Services.Extensions;
using eBook.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace eBook.Services.Implementations;

public class SubscriptionPlanService : ISubscriptionPlanService
{
    private static readonly Expression<Func<SubscriptionPlan, PlanAdminResponse>> ToResponse = plan => new PlanAdminResponse
    {
        Id = plan.Id,
        Name = plan.Name,
        Description = plan.Description,
        Price = plan.Price,
        Currency = plan.Currency.ToUpper(),
        DurationInMonths = plan.DurationInMonths,
        IsActive = plan.IsActive,
        SubscriptionCount = plan.Subscriptions.Count
    };

    private readonly ApplicationDbContext _db;

    public SubscriptionPlanService(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<PagedResult<PlanAdminResponse>> GetAsync(PlanSearchRequest request)
    {
        var query = _db.SubscriptionPlans.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(plan => plan.Name.Contains(term));
        }

        return query.OrderBy(plan => plan.Price).Select(ToResponse).ToPagedResultAsync(request);
    }

    public async Task<PlanAdminResponse> GetByIdAsync(int id)
    {
        return await _db.SubscriptionPlans.AsNoTracking().Where(plan => plan.Id == id).Select(ToResponse).SingleOrDefaultAsync()
            ?? throw new NotFoundException("Plan pretplate nije pronađen.");
    }

    public async Task<PlanAdminResponse> CreateAsync(PlanUpsertRequest request)
    {
        var name = request.Name.Trim();
        await EnsureNameIsUniqueAsync(name, null);

        var plan = new SubscriptionPlan();
        Apply(plan, request, name);

        _db.SubscriptionPlans.Add(plan);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(plan.Id);
    }

    public async Task<PlanAdminResponse> UpdateAsync(int id, PlanUpsertRequest request)
    {
        var plan = await _db.SubscriptionPlans.SingleOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException("Plan pretplate nije pronađen.");

        var name = request.Name.Trim();
        await EnsureNameIsUniqueAsync(name, id);

        Apply(plan, request, name);
        await _db.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    public async Task DeleteAsync(int id)
    {
        var plan = await _db.SubscriptionPlans.SingleOrDefaultAsync(p => p.Id == id)
            ?? throw new NotFoundException("Plan pretplate nije pronađen.");

        if (await _db.Subscriptions.AnyAsync(subscription => subscription.SubscriptionPlanId == id))
        {
            throw new BusinessException("Plan nije moguće obrisati jer postoje pretplate na njega. Deaktivirajte ga umjesto brisanja.");
        }

        _db.SubscriptionPlans.Remove(plan);
        await _db.SaveChangesAsync();
    }

    private static void Apply(SubscriptionPlan plan, PlanUpsertRequest request, string name)
    {
        plan.Name = name;
        plan.Description = request.Description?.Trim();
        plan.Price = request.Price;
        plan.Currency = request.Currency.Trim().ToLowerInvariant();
        plan.DurationInMonths = request.DurationInMonths;
        plan.IsActive = request.IsActive;
    }

    private async Task EnsureNameIsUniqueAsync(string name, int? currentId)
    {
        if (await _db.SubscriptionPlans.AnyAsync(p => p.Name == name && p.Id != currentId))
        {
            throw new BusinessException("Plan s tim nazivom već postoji.");
        }
    }
}
