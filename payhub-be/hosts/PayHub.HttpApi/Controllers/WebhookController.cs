namespace PayHub.HttpApi.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

[ApiController]
[Route("api/webhook")]
public class WebhookController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public WebhookController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("sipay")]
    public IActionResult Sipay([FromForm] IFormCollection form)
    {
        var hashKey = form["hash_key"].FirstOrDefault() ?? form["hashKey"].FirstOrDefault();
        if (string.IsNullOrEmpty(hashKey))
            return BadRequest("hash_key is required");

        var secret = _configuration["Sipay:AppSecret"];
        if (string.IsNullOrEmpty(secret))
            return StatusCode(StatusCodes.Status500InternalServerError, "Sipay app secret not configured");

        var parsed = ValidateHashKey(hashKey, secret);
        if (parsed == null)
            return BadRequest("Invalid hash_key");

        return Ok(parsed);
    }

    private object? ValidateHashKey(string hashKey, string secretKey)
    {
        try
        {
            // The incoming hash may use '__' inplace of '/'
            hashKey = hashKey.Replace("__", "/");
            var password = ComputeSha1Hash(secretKey);

            var components = hashKey.Split(':');
            if (components.Length <= 2) return null;

            var iv = components[0];
            var salt = components[1];
            var encryptedMsg = components[2];

            var saltHash = ComputeSha256Hash(password + salt);

            // Build key/iv similar to the encryption implementation used elsewhere
            var keyBytes = Encoding.UTF8.GetBytes(saltHash.PadRight(32).Substring(0, 32));
            var ivBytes = Encoding.UTF8.GetBytes(iv.PadRight(16).Substring(0, 16));

            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = keyBytes;
            aes.IV = ivBytes;

            var encryptedBytes = Convert.FromBase64String(encryptedMsg);
            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
            var decrypted = Encoding.UTF8.GetString(decryptedBytes);

            if (!decrypted.Contains("|")) return null;
            var parts = decrypted.Split('|');

            return new
            {
                status = parts.Length > 0 ? parts[0] : "",
                total = parts.Length > 1 ? parts[1] : "",
                invoiceId = parts.Length > 2 ? parts[2] : "",
                orderId = parts.Length > 3 ? parts[3] : "",
                currencyCode = parts.Length > 4 ? parts[4] : ""
            };
        }
        catch
        {
            return null;
        }
    }

    private static string ComputeSha1Hash(string input)
    {
        using var sha1 = SHA1.Create();
        var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLower();
    }

    private static string ComputeSha256Hash(string input)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLower();
    }
}
