namespace eBook.Services.Database;

public class ReadingProgress
{
    public int Id { get; set; }
    public int CurrentPage { get; set; }
    public bool IsFinished { get; set; }
    public DateTime LastReadAt { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int BookId { get; set; }
    public Book Book { get; set; } = null!;
}
