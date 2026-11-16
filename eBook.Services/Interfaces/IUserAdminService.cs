using eBook.Model.Common;
using eBook.Model.Users;

namespace eBook.Services.Interfaces;

public interface IUserAdminService
{
    Task<PagedResult<UserAdminResponse>> GetAsync(UserSearchRequest request);
    Task<UserAdminResponse> GetByIdAsync(int id);
    Task<UserAdminResponse> SetActiveAsync(int id, bool isActive);
}
