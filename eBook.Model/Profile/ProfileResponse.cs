namespace eBook.Model.Profile;

public class ProfileResponse
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public int CityId { get; set; }
    public string CityName { get; set; } = null!;
    public string CountryName { get; set; } = null!;
    public bool HasPicture { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? PictureUrl => HasPicture ? "/api/Profile/picture" : null;
}
