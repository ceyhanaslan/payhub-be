namespace PayHub.Infrastructure.Adapters;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using PayHub.Application.Interfaces;

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
                // Sipay API bazen array içinde döndürebilir, bazen de direkt obje olarak
                // Case-insensitive olarak deserialize edip snake_case/PascalCase eşleştirmesi sağlanır
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                
                try 
                {
                    // Önce düz obje olarak deneyeceğiz
                    var tokenResponse = JsonSerializer.Deserialize<SipayTokenResponse>(responseContent, options);
                    if (tokenResponse?.StatusCode == 100)
                    {
                        return tokenResponse.Data?.Token;
                    }
                }
                catch
                {
                    // Obje olarak deserialize edilemezse, array olabilir
                    try
                    {
                        var tokenResponseArray = JsonSerializer.Deserialize<SipayTokenResponse[]>(responseContent, options);
                        if (tokenResponseArray?.Length > 0 && tokenResponseArray[0]?.StatusCode == 100)
                        {
                            return tokenResponseArray[0].Data?.Token;
                        }
                    }
                    catch
                    {
                        // Son çare olarak JsonDocument ile ayrıştırmayı deneyelim
                        try
                        {
                            using var doc = JsonDocument.Parse(responseContent);
                            
                            // Array kontrolü yapalım
                            if (doc.RootElement.ValueKind == JsonValueKind.Array)
                            {
                                var firstItem = doc.RootElement[0];
                                if (TryGetTokenFromElement(firstItem, out var token))
                                    return token;
                            }
                            // Tekil obje kontrolü
                            else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                            {
                                if (TryGetTokenFromElement(doc.RootElement, out var token))
                                    return token;
                            }
                        }
                        catch (Exception docEx)
                        {
                            _logger.LogError(docEx, "Error parsing token response with JsonDocument");
                        }
                    }
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
    
    private bool TryGetTokenFromElement(JsonElement element, out string? token)
    {
        token = null;
        
        // status_code veya StatusCode kontrolü
        if ((element.TryGetProperty("status_code", out var statusCodeElement) || 
             element.TryGetProperty("StatusCode", out statusCodeElement)) && 
            statusCodeElement.TryGetInt32(out var statusCode) && 
            statusCode == 100)
        {
            // data veya Data özelliğini kontrol et
            if (element.TryGetProperty("data", out var dataElement) || 
                element.TryGetProperty("Data", out dataElement))
            {
                // token veya Token özelliğini kontrol et
                if ((dataElement.TryGetProperty("token", out var tokenElement) || 
                     dataElement.TryGetProperty("Token", out tokenElement)) && 
                    tokenElement.ValueKind == JsonValueKind.String)
                {
                    token = tokenElement.GetString();
                    return true;
                }
            }
        }
        
        return false;
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
                // Case-insensitive deserialization
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                
                try
                {
                    // Önce düz obje olarak deneyeceğiz
                    var paymentResponse = JsonSerializer.Deserialize<SipayPaymentResponse>(responseContent, options);
                    if (paymentResponse?.StatusCode == 100 && paymentResponse.Data?.PaymentStatus == 1)
                    {
                        _logger.LogInformation("Sipay payment successful for TransactionId: {TransactionId}, OrderId: {OrderId}",
                            request.TransactionId, paymentResponse.Data.OrderId);
                        return true;
                    }
                }
                catch
                {
                    // Obje olarak deserialize edilemezse, array olabilir
                    try
                    {
                        var paymentResponseArray = JsonSerializer.Deserialize<SipayPaymentResponse[]>(responseContent, options);
                        if (paymentResponseArray?.Length > 0 && 
                            paymentResponseArray[0]?.StatusCode == 100 && 
                            paymentResponseArray[0].Data?.PaymentStatus == 1)
                        {
                            _logger.LogInformation("Sipay payment successful (array response) for TransactionId: {TransactionId}, OrderId: {OrderId}",
                                request.TransactionId, paymentResponseArray[0].Data?.OrderId);
                            return true;
                        }
                    }
                    catch (Exception arrayEx)
                    {
                        _logger.LogError(arrayEx, "Error deserializing payment array response");
                    }
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
        // Kriptografik olarak güvenli rastgele sayı üreteci kullan
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        byte[] data = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(data);
        }

        var result = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            // Byte değerini chars dizisindeki bir karaktere dönüştür
            var index = data[i] % chars.Length;
            result.Append(chars[index]);
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

    public async Task<(string Content, int StatusCode)> PostProxyRawAsync(string path, string json, CancellationToken cancellationToken = default)
    {
        // Ensure we have a token
        var token = await GetTokenAsync(cancellationToken);
        if (string.IsNullOrEmpty(token))
        {
            _logger.LogError("Failed to get Sipay token for proxy request");
            throw new InvalidOperationException("Unable to obtain Sipay token");
        }

        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

        var response = await _httpClient.PostAsync($"{_baseUrl}{path}", content, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        return (responseContent, (int)response.StatusCode);
    }

    public async Task<(string Content, int StatusCode)> PostProxyAsync(string path, object body, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(body);
        return await PostProxyRawAsync(path, json, cancellationToken);
    }

    public async Task<(string Content, int StatusCode)> GetPosAsync(string jsonBody, CancellationToken cancellationToken = default)
    {
        return await PostProxyRawAsync("/api/getpos", jsonBody, cancellationToken);
    }

    public async Task<(string Content, int StatusCode)> InstallmentsAsync(string jsonBody, CancellationToken cancellationToken = default)
    {
        return await PostProxyRawAsync("/api/installments", jsonBody, cancellationToken);
    }

    public async Task<(string Content, int StatusCode)> CommissionsAsync(string jsonBody, CancellationToken cancellationToken = default)
    {
        return await PostProxyRawAsync("/api/commissions", jsonBody, cancellationToken);
    }

    public async Task<(string Content, int StatusCode)> PaySmart3DAsync(string jsonBody, CancellationToken cancellationToken = default)
    {
        return await PostProxyRawAsync("/api/paySmart3D", jsonBody, cancellationToken);
    }

    public async Task<(string Content, int StatusCode)> CompletePaymentAsync(string jsonBody, CancellationToken cancellationToken = default)
    {
        return await PostProxyRawAsync("/api/payment/complete", jsonBody, cancellationToken);
    }

    public async Task<(string Content, int StatusCode)> CheckStatusAsync(string jsonBody, CancellationToken cancellationToken = default)
    {
        return await PostProxyRawAsync("/api/checkstatus", jsonBody, cancellationToken);
    }

    public async Task<(string Content, int StatusCode)> ConfirmPaymentAsync(string jsonBody, CancellationToken cancellationToken = default)
    {
        return await PostProxyRawAsync("/api/confirmPayment", jsonBody, cancellationToken);
    }

    public async Task<(string Content, int StatusCode)> RefundAsync(string jsonBody, CancellationToken cancellationToken = default)
    {
        return await PostProxyRawAsync("/api/refund", jsonBody, cancellationToken);
    }

    public async Task<(string Content, int StatusCode)> SaveCardAsync(string jsonBody, CancellationToken cancellationToken = default)
    {
        return await PostProxyRawAsync("/api/saveCard", jsonBody, cancellationToken);
    }

    public async Task<(string Content, int StatusCode)> PayByCardTokenAsync(string jsonBody, CancellationToken cancellationToken = default)
    {
        return await PostProxyRawAsync("/api/payByCardToken", jsonBody, cancellationToken);
    }

    public async Task<(string Content, int StatusCode)> PayByCardTokenNonSecureAsync(string jsonBody, CancellationToken cancellationToken = default)
    {
        return await PostProxyRawAsync("/api/payByCardTokenNonSecure", jsonBody, cancellationToken);
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
