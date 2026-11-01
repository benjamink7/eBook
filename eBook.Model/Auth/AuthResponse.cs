namespace eBook.Model.Auth;

public class AuthResponse
{
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = null!;
    public List<string> Roles { get; set; } = new();
}
