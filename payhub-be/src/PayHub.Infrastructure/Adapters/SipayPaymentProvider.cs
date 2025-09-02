namespace PayHub.Infrastructure.Adapters;

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using PayHub.Application.Interfaces;
using PayHub.Domain;

public class SipayPaymentProvider : IPaymentProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SipayPaymentProvider> _logger;

    private readonly string _baseUrl;
    private readonly string _appId;
    private readonly string _appSecret;
    private readonly string _merchantKey;
    private readonly string _merchantId;

    public SipayPaymentProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<SipayPaymentProvider> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        _baseUrl = _configuration["Sipay:BaseUrl"] ?? "https://provisioning.sipay.com.tr/ccpayment";
        _appId = _configuration["Sipay:AppId"] ?? throw new ArgumentNullException("Sipay:AppId");
        _appSecret = _configuration["Sipay:AppSecret"] ?? throw new ArgumentNullException("Sipay:AppSecret");
        _merchantKey = _configuration["Sipay:MerchantKey"] ?? throw new ArgumentNullException("Sipay:MerchantKey");
        _merchantId = _configuration["Sipay:MerchantId"] ?? throw new ArgumentNullException("Sipay:MerchantId");
    }

    public string ProviderName => "Sipay";

    public async Task<bool> ProcessPaymentAsync(PayHub.Application.Interfaces.PaymentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing Sipay payment for TransactionId: {TransactionId}", request.TransactionId);

            // Get token first
            var token = await GetTokenAsync(cancellationToken);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Failed to get Sipay token");
                return false;
            }

            // Process payment based on 3D configuration
            var result = await ProcessPaymentWithTokenAsync(request, token, cancellationToken);

            _logger.LogInformation("Sipay payment result for TransactionId: {TransactionId}, Success: {Success}",
                request.TransactionId, result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Sipay payment for TransactionId: {TransactionId}", request.TransactionId);
            return false;
        }
    }

    private async Task<string?> GetTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            var tokenRequest = new
            {
                app_id = _appId,
                app_secret = _appSecret
            };

            var json = JsonSerializer.Serialize(tokenRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_baseUrl}/api/token", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var tokenResponse = JsonSerializer.Deserialize<SipayTokenResponse>(responseContent);
                if (tokenResponse?.StatusCode == 100)
                {
                    return tokenResponse.Data?.Token;
                }
            }

            _logger.LogError("Failed to get Sipay token. Response: {Response}", responseContent);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception getting Sipay token");
            return null;
        }
    }

    private async Task<bool> ProcessPaymentWithTokenAsync(PayHub.Application.Interfaces.PaymentRequest request, string token, CancellationToken cancellationToken)
    {
        try
        {
            // Generate hash key for payment
            var hashKey = GenerateHashKey(
                request.Amount.ToString("F2"),
                "1", // installments
                request.Currency,
                _merchantKey,
                request.TransactionId,
                _appSecret
            );

            var paymentRequest = new
            {
                cc_holder_name = "Card Holder", // This should come from tokenized data
                cc_no = "4508034508034509", // Demo card - in real implementation use tokenized data
                expiry_month = "12", // This should come from tokenized data
                expiry_year = "2025", // This should come from tokenized data
                cvv = "000", // This should come from tokenized data (if allowed)
                currency_code = request.Currency,
                installments_number = 1,
                invoice_id = request.TransactionId,
                invoice_description = $"Payment for transaction {request.TransactionId}",
                name = "Customer", // This should come from customer data
                surname = "Name", // This should come from customer data
                total = request.Amount,
                merchant_key = _merchantKey,
                items = JsonSerializer.Serialize(new[]
                {
                    new
                    {
                        name = "Payment",
                        price = request.Amount.ToString("F2"),
                        quantity = 1,
                        description = "Payment transaction"
                    }
                }),
                cancel_url = _configuration["Sipay:CancelUrl"] ?? "https://yourdomain.com/payment/cancel",
                return_url = _configuration["Sipay:ReturnUrl"] ?? "https://yourdomain.com/payment/success",
                hash_key = hashKey,
                bill_email = request.CustomerEmail ?? "customer@example.com",
                bill_phone = request.CustomerPhone ?? "5551234567",
                ip = request.CustomerIp ?? "127.0.0.1",
                response_method = "POST"
            };

            var json = JsonSerializer.Serialize(paymentRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Add authorization header
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            // Use Non-Secure payment for direct processing
            var response = await _httpClient.PostAsync($"{_baseUrl}/api/paySmart2D", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var paymentResponse = JsonSerializer.Deserialize<SipayPaymentResponse>(responseContent);
                if (paymentResponse?.StatusCode == 100 && paymentResponse.Data?.PaymentStatus == 1)
                {
                    _logger.LogInformation("Sipay payment successful for TransactionId: {TransactionId}, OrderId: {OrderId}",
                        request.TransactionId, paymentResponse.Data.OrderId);
                    return true;
                }
            }

            _logger.LogError("Sipay payment failed for TransactionId: {TransactionId}. Response: {Response}",
                request.TransactionId, responseContent);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception processing Sipay payment for TransactionId: {TransactionId}", request.TransactionId);
            return false;
        }
    }

    private string GenerateHashKey(string total, string installment, string currencyCode,
        string merchantKey, string invoiceId, string appSecret)
    {
        try
        {
            var data = $"{total}|{installment}|{currencyCode}|{merchantKey}|{invoiceId}";

            var iv = GenerateRandomString(16);
            var password = ComputeSha1Hash(appSecret);
            var salt = GenerateRandomString(4);
            var saltWithPassword = ComputeSha256Hash(password + salt);

            var encrypted = EncryptAes256Cbc(data, saltWithPassword, iv);
            var msgEncryptedBundle = $"{iv}:{salt}:{encrypted}";
            var hashKey = msgEncryptedBundle.Replace("/", "__");

            return hashKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating hash key");
            return string.Empty;
        }
    }

    private static string GenerateRandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        var result = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            result.Append(chars[random.Next(chars.Length)]);
        }
        return result.ToString();
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

    private static string EncryptAes256Cbc(string plainText, string key, string iv)
    {
        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
        aes.IV = Encoding.UTF8.GetBytes(iv.PadRight(16).Substring(0, 16));

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        return Convert.ToBase64String(encryptedBytes);
    }
}

public class SipayTokenResponse
{
    public int StatusCode { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public SipayTokenData? Data { get; set; }
}

public class SipayTokenData
{
    public string Token { get; set; } = string.Empty;
    public int Is3D { get; set; }
}

public class SipayPaymentResponse
{
    public int StatusCode { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public SipayPaymentData? Data { get; set; }
}

public class SipayPaymentData
{
    public int PaymentStatus { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string InvoiceId { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public string AuthCode { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}
