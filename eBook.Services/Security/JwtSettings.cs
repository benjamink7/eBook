namespace eBook.Services.Security;

public record JwtSettings(string Key, string Issuer, string Audience, int ExpiryMinutes);
