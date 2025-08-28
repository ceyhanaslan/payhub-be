namespace PayHub.Application.Services;
using System.Security.Cryptography;
using System.Text;

public static class HmacHelper
{
    public static string ComputeHmacSha256(string secret, string data)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToBase64String(hash);
    }
    public static bool ValidateHmac(string secret, string data, string signature)
    {
        var computed = ComputeHmacSha256(secret, data);
        return computed == signature;
    }
}
