namespace eBook.Services.Interfaces;

public interface ICurrentUserService
{
    int UserId { get; }
    bool IsAdmin { get; }
}
