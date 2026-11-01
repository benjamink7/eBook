namespace eBook.Services.Database;

public class News
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public byte[] Image { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public int CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;
}
