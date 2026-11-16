namespace eBook.Model.Authors;

public class AuthorResponse
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string FullName => $"{FirstName} {LastName}";
    public string? Biography { get; set; }
    public int? BirthYear { get; set; }
    public int BookCount { get; set; }
}
