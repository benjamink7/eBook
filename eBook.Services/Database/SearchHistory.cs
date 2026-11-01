namespace eBook.Services.Database;

public class SearchHistory
{
    public int Id { get; set; }
    public string Query { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int? GenreId { get; set; }
    public Genre? Genre { get; set; }
}
