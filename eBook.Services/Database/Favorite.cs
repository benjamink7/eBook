namespace eBook.Services.Database;

public class Favorite
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int BookId { get; set; }
    public Book Book { get; set; } = null!;
}
