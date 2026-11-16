namespace eBook.Model.Users;

public class UserAdminResponse
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string CityName { get; set; } = null!;
    public string CountryName { get; set; } = null!;
    public bool IsActive { get; set; }
    public bool HasActiveSubscription { get; set; }
    public List<string> Roles { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
