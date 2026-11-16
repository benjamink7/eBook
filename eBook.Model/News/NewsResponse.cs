namespace eBook.Model.News;

public class NewsResponse
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string ImageUrl => $"/api/News/{Id}/image";
}
