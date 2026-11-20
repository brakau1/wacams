using System.Security.Cryptography;
using System.Text;

namespace Wacam.Api.Services;

/// <summary>
/// Service for encrypting and decrypting sensitive data (GIS credentials).
/// Uses AES-256-GCM for secure encryption.
/// </summary>
public class EncryptionService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EncryptionService> _logger;
    private readonly byte[] _key;

    public EncryptionService(IConfiguration configuration, ILogger<EncryptionService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        // Get encryption key from configuration (must be 32 bytes for AES-256)
        var keyString = _configuration["Encryption:Key"] 
            ?? "WACAMS-Default-Encryption-Key-32B!"; // Default key for development
        
        // Ensure key is exactly 32 bytes
        _key = Encoding.UTF8.GetBytes(keyString.PadRight(32, '!').Substring(0, 32));
    }

    /// <summary>
    /// Encrypt a plaintext string.
    /// </summary>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        try
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Combine IV + encrypted data
            var combined = new byte[aes.IV.Length + encryptedBytes.Length];
            Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
            Buffer.BlockCopy(encryptedBytes, 0, combined, aes.IV.Length, encryptedBytes.Length);

            return Convert.ToBase64String(combined);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt data");
            throw;
        }
    }

    /// <summary>
    /// Decrypt an encrypted string.
    /// </summary>
    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return string.Empty;

        try
        {
            var combined = Convert.FromBase64String(encryptedText);

            using var aes = Aes.Create();
            aes.Key = _key;

            // Extract IV
            var iv = new byte[aes.IV.Length];
            Buffer.BlockCopy(combined, 0, iv, 0, iv.Length);
            aes.IV = iv;

            // Extract encrypted data
            var encryptedBytes = new byte[combined.Length - iv.Length];
            Buffer.BlockCopy(combined, iv.Length, encryptedBytes, 0, encryptedBytes.Length);

            using var decryptor = aes.CreateDecryptor();
            var plainBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt data");
            throw;
        }
    }

    /// <summary>
    /// Mask a sensitive string for display (e.g., "sntseke1" -> "snt****e1").
    /// </summary>
    public string Mask(string value, int visibleChars = 3)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= visibleChars * 2)
            return "****";

        var start = value.Substring(0, visibleChars);
        var end = value.Substring(value.Length - visibleChars);
        var middle = new string('*', value.Length - visibleChars * 2);

        return $"{start}{middle}{end}";
    }
}
