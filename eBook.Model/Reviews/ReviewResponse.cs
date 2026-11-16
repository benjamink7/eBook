namespace eBook.Model.Reviews;

public class ReviewResponse
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string Username { get; set; } = null!;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsMine { get; set; }
}
