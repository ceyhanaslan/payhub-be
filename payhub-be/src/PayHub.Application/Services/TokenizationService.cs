namespace PayHub.Application.Services;
using System;
using System.Collections.Concurrent;

public interface ITokenizationService
{
    string TokenizeCard(string cardNumber, string expiry, string cvv);
    // Kart bilgisini geri döndürmez! Sadece token'dan doğrulama yapılabilir.
    bool ValidateToken(string token);
}

public class TokenizationService : ITokenizationService
{
    private static readonly ConcurrentDictionary<string, string> _tokenStore = new();
    public string TokenizeCard(string cardNumber, string expiry, string cvv)
    {
        // Gerçek PCI DSS için harici bir vault/token provider gerekir!
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        _tokenStore[token] = "MASKED"; // Kartı asla saklama!
        return token;
    }
    public bool ValidateToken(string token)
    {
        return _tokenStore.ContainsKey(token);
    }
}
