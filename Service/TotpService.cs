using System;
using System.Security.Cryptography;
using System.Text;
using Asp.TotpKit.Interface;
using Asp.TotpKit.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QRCoder;

namespace Asp.TotpKit.Service;
//Both of AppUser and TUser should implement ITotpUserInfo
public class TotpService<TUser> : ITotpService<TUser> where TUser : class,ITotpUserInfo
{
    private readonly UserManager<TUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TotpService<TUser>> _logger;
    private readonly TotpOptions<TUser> _options;
    public TotpService(
        UserManager<TUser> userManager,
        IConfiguration configuration,
        ILogger<TotpService<TUser>> logger,
        IOptions<TotpOptions<TUser>> options)
    {
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
        _options = options.Value;
    }
    #region Public Methods
    public async Task<string> GenerateSecretKeyAsync(TUser user)
    {
        try
        {
            if (string.IsNullOrEmpty(user.ToptSecret))
            {
                // 32 karakter base32 secret key oluştur
                var key = new byte[20];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(key);
                }

                user.ToptSecret = Base32Encode(key);
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("TOTP secret key başarılı bir şekilde oluşturuldu");
            }

            return user.ToptSecret;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TOTP secret key oluşturma hatası");
            throw;
        }
    }

    public async Task<string> GenerateQrCodeUriAsync(TUser user, string secretKey)
    {
        try
        {
            var appName = _options.AppName?? _configuration["AppSettings:AppName"] ?? "Asp.TotpKit";
            var issuer = _options.Issuer?? _configuration["AppSettings:TotpIssuer"] ?? appName;

            var email = _options.GetUserEmail?.Invoke(user) ?? "unknown@user";
            var userId = _options.GetUserId?.Invoke(user) ?? Guid.NewGuid().ToString();

            var accountTitle = $"{issuer}:{email}";
            var qrCodeUri = 
            $"otpauth://totp/{Uri.EscapeDataString(accountTitle)}?secret={secretKey}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits=6&period=30";

            _logger.LogInformation("QR Code URI oluşturuldu: {UserId}", userId);

            return qrCodeUri;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QR Code URI oluşturma hatası");
            throw;
        }
    }

    public async Task<byte[]> GenerateQrCodeImageAsync(string qrCodeUri)
    {
        try
        {
            // QRCoder kütüphanesi ile QR Code oluştur
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(qrCodeUri, QRCodeGenerator.ECCLevel.Q);

            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(20);

            _logger.LogInformation("QR Code resmi oluşturuldu");

            return qrCodeBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QR Code resmi oluşturma hatası");
            var fallbackText = $"QR Code oluşturulamadı. Manuel kod: {qrCodeUri}";
            return Encoding.UTF8.GetBytes(fallbackText);
        }
    }

    public async Task<bool> ValidateTotpCodeAsync(TUser user, string code)
    {
        try
        {
            if (string.IsNullOrEmpty(user.ToptSecret) || string.IsNullOrEmpty(code))
            {
                _logger.LogWarning("TOTP validasyon - eksik parametre: {UserId}",_options.GetUserId?.Invoke(user) ?? "unknown");
                return false;
            }

            if (code.Length != 6 || !code.All(char.IsDigit))
            {
                _logger.LogWarning("TOTP validasyon - geçersiz kod formatı: {UserId}", _options.GetUserId?.Invoke(user) ?? "unknown");
                return false;
            }

            var secretKeyBytes = Base32Decode(user.ToptSecret);
            var currentTimeStep = GetCurrentTimeStep();

            // Zaman toleransı için önceki ve sonraki time step'leri de kontrol et
            for (int i = -1; i <= 1; i++)
            {
                var timeStep = currentTimeStep + i;
                var expectedCode = GenerateTotpCode(secretKeyBytes, timeStep);

                if (expectedCode == code)
                {
                    _logger.LogInformation("TOTP validasyon başarılı: {UserId}",_options.GetUserId?.Invoke(user) ?? "unknown");
                    return true;
                }
            }

            _logger.LogWarning("TOTP validasyon başarısız: {UserId}", _options.GetUserId?.Invoke(user) ?? "unknown");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TOTP validasyon hatası: {UserId}", _options.GetUserId?.Invoke(user) ?? "unknown");
            return false;
        }
    }
    //LOGİN SIRASINDA TOTP KODUNU DOĞRULARIZ
    public async Task<bool> ValidateLoginCodeAsync(TUser user, string code)
    {
        return await ValidateTotpCodeAsync(user, code);
    }

    public bool HasTotpSetup(TUser user)
    {
        return user.IsToptEnabled && !string.IsNullOrEmpty(user.ToptSecret);
    }
    //Kurulum var mı yok mu diye kontrol ederiz
    public async Task<TotpSetupResponse> GenerateSetupAsync(TUser user)
    {
        try
        {
            var secretKey = await GenerateSecretKeyAsync(user);
            var qrCodeUri = await GenerateQrCodeUriAsync(user, secretKey);

            return new TotpSetupResponse
            {
                Success = true,
                SecretKey = secretKey,
                QrCodeUri = qrCodeUri,
                ManualEntryKey = secretKey.Replace(" ", "").Replace("-", ""),
                Instructions = new TotpInstructions
                {
                    Web = "QR kodu Google Authenticator ile tarayın",
                    Mobile = "32 haneli kodu Google Authenticator'a manuel olarak girin",
                    General = "Kurulum tamamlandıktan sonra 6 haneli kodu girerek doğrulayın"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TOTP setup oluşturma hatası: {UserId}", _options.GetUserId?.Invoke(user) ?? "unknown");
            return new TotpSetupResponse
            {
                Success = false,
                Message = "TOTP kurulum bilgileri oluşturulamadı"
            };
        }
    }
    //TOTP KODUNU DOĞRULA VE KURULUMU TAMAMLA
    public async Task<TotpVerifyResult> VerifyCodeAsync(TUser user, string code)
    {
        try
        {
            var isValid = await ValidateTotpCodeAsync(user, code);

            if (isValid)
            {
                var success = await EnableTotpAsync(user, code);
                if (success)
                {
                    return new TotpVerifyResult
                    {
                        Success = true,
                        Message = "TOTP başarıyla aktif edildi!",
                        Warning = "TOTP kurulumu tamamlandı"
                    };
                }
            }
            return new TotpVerifyResult
            {
                Success = false,
                Message = "Geçersiz TOTP kodu",
                Warning = ""
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TOTP verify hatası: {UserId}", _options.GetUserId?.Invoke(user) ?? "unknown");
            return new TotpVerifyResult
            {
                Success = false,
                Message = "TOTP doğrulama sırasında hata"
            };
        }
    }

    //KULLANICI İÇİN TOTP'Yİ AKTİF EDER
    public async Task<bool> EnableTotpAsync(TUser user, string code)
    {
        try
        {
            if (await ValidateTotpCodeAsync(user, code))
            {
                user.IsToptEnabled = true;
                user.ToptVerifiedDate = DateTime.Now;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("TOTP aktif edildi: {UserId}", _options.GetUserId?.Invoke(user) ?? "unknown");
                return true;
            }

            _logger.LogWarning("TOTP aktifleştirme başarısız - kod geçersiz: {UserId}",_options.GetUserId?.Invoke(user) ?? "unknown");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TOTP aktifleştirme hatası: {UserId}", _options.GetUserId?.Invoke(user) ?? "unknown");
            return false;
        }
    }
    //KULLANICI İÇİN TOTP'Yİ DEVRE Dışı Bırakır
    public async Task<bool> DisableTotpAsync(TUser user)
    {
        try
        {
            user.IsToptEnabled = false;
            user.ToptSecret = null;
            user.ToptVerifiedDate = null;

            await _userManager.UpdateAsync(user);

            _logger.LogInformation("TOTP devre dışı bırakıldı: {UserId}", _options.GetUserId?.Invoke(user) ?? "unknown");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TOTP devre dışı bırakma hatası: {UserId}", _options.GetUserId?.Invoke(user) ?? "unknown");
            return false;
        }
    }
    #endregion
    #region Cookie Operations

    public bool httpOnlyCookie { get; set; } = false;
    public bool secureCookie { get; set; } = false;
    public SameSiteMode sameSiteMode { get; set; } = SameSiteMode.Lax;


    public void SetTotpTempCookie(HttpResponse response, string userId, int expiryMinutes = 10)
    {
        try
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = httpOnlyCookie,
                Secure = secureCookie,
                SameSite = sameSiteMode,
                Expires = DateTime.Now.AddMinutes(expiryMinutes),
                Path = "/",
                IsEssential = true
            };

            response.Cookies.Append("TotpTemp", userId, cookieOptions);

            _logger.LogInformation("TOTP geçici cookie set edildi: {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TOTP geçici cookie set etme hatası: {UserId}", userId);
        }
    }

    public string GetTotpTempCookie(HttpRequest request)
    {
        try
        {
            return request.Cookies["TotpTemp"] ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TOTP geçici cookie alma hatası");
            return string.Empty;
        }
    }

    public void ClearTotpTempCookie(HttpResponse response)
    {
        try
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = httpOnlyCookie,
                Secure = secureCookie,
                SameSite = sameSiteMode,
                Expires = DateTime.Now.AddDays(-1),
                Path = "/",
                 IsEssential = true
            };

            response.Cookies.Append("TotpTemp", "", cookieOptions);

            _logger.LogInformation("TOTP geçici cookie temizlendi");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TOTP geçici cookie temizleme hatası");
        }
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Mevcut zaman adımını alır (30 saniye)
    /// </summary>
    private long GetCurrentTimeStep()
    {
        var unixTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        return unixTime / 30; // 30 saniye time step
    }

    /// <summary>
    /// TOTP kodu oluşturur
    /// </summary>
    private string GenerateTotpCode(byte[] secretKey, long timeStep)
    {
        var timeStepBytes = BitConverter.GetBytes(timeStep);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(timeStepBytes);

        using var hmac = new HMACSHA1(secretKey);
        var hash = hmac.ComputeHash(timeStepBytes);

        var offset = hash[hash.Length - 1] & 0x0F;
        var binaryCode = (hash[offset] & 0x7F) << 24 |
                        (hash[offset + 1] & 0xFF) << 16 |
                        (hash[offset + 2] & 0xFF) << 8 |
                        (hash[offset + 3] & 0xFF);

        var code = binaryCode % 1000000;
        return code.ToString("D6");
    }

    /// <summary>
    /// Base32 encoding
    /// </summary>
    private string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var result = new StringBuilder();

        int bits = 0;
        int value = 0;

        foreach (byte b in data)
        {
            value = (value << 8) | b;
            bits += 8;

            while (bits >= 5)
            {
                result.Append(alphabet[(value >> (bits - 5)) & 0x1F]);
                bits -= 5;
            }
        }

        if (bits > 0)
        {
            result.Append(alphabet[(value << (5 - bits)) & 0x1F]);
        }

        return result.ToString();
    }

    /// <summary>
    /// Base32 decoding
    /// </summary>
    private byte[] Base32Decode(string encoded)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var result = new List<byte>();

        encoded = encoded.ToUpper().Replace(" ", "").Replace("-", "");

        int bits = 0;
        int value = 0;

        foreach (char c in encoded)
        {
            int index = alphabet.IndexOf(c);
            if (index < 0) continue;

            value = (value << 5) | index;
            bits += 5;

            if (bits >= 8)
            {
                result.Add((byte)((value >> (bits - 8)) & 0xFF));
                bits -= 8;
            }
        }

        return result.ToArray();
    }

    #endregion
}