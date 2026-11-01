namespace eBook.Services.Database;

public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string PasswordSalt { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public byte[]? ProfilePicture { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    // Povecava se pri logout-u; JWT nosi verziju pa se stari tokeni invalidiraju na serveru.
    public int TokenVersion { get; set; }

    // Reset lozinke: kod se cuva samo kao hash i ima rok vazenja.
    public string? PasswordResetCodeHash { get; set; }
    public DateTime? PasswordResetCodeExpiry { get; set; }

    public int CityId { get; set; }
    public City City { get; set; } = null!;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<ReadingProgress> ReadingProgresses { get; set; } = new List<ReadingProgress>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<SearchHistory> SearchHistories { get; set; } = new List<SearchHistory>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
