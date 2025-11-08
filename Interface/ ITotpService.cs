using System;
using Microsoft.AspNetCore.Http;
using Asp.TotpKit.Model;
namespace Asp.TotpKit.Interface;

public interface ITotpService<TUser> where TUser : class
{
    //Kullanıcı için TOTP secret key oluşturur
    Task<string> GenerateSecretKeyAsync(TUser user);

    //QR Code URI oluşturur (Google Authenticator için)
    Task<string> GenerateQrCodeUriAsync(TUser user, string secretKey);

    //QR Code PNG resmi oluşturur
    Task<byte[]> GenerateQrCodeImageAsync(string qrCodeUri);
    // TOTP kodunu doğrular
    Task<bool> ValidateTotpCodeAsync(TUser user, string code);
    
    //Login sırasında TOTP kodunu doğrularız
    Task<bool> ValidateLoginCodeAsync(TUser user, string code);

    // Kullanıcının TOTP kurulumu var mı kontrol ederiz
    bool HasTotpSetup(TUser user);

    // TOTP kurulum bilgilerini oluşturur (QR Code ile)
    Task<TotpSetupResponse> GenerateSetupAsync(TUser user);
    // TOTP kodunu doğrula ve kurulumu tamamla
    Task<TotpVerifyResult> VerifyCodeAsync(TUser user, string code);
    // Kullanıcı için TOTP'yi aktif eder
    Task<bool> EnableTotpAsync(TUser user, string code);
    //Kullanıcı için TOTP'yi devre dışı bırakır
    Task<bool> DisableTotpAsync(TUser user);
    // TOTP geçici cookie set eder (login sırasında)
    void SetTotpTempCookie(HttpResponse response, string userId, int expiryMinutes = 10);
    // TOTP geçici cookie'den user ID alır
    string GetTotpTempCookie(HttpRequest request);
    // TOTP geçici cookie'yi temizler
    void ClearTotpTempCookie(HttpResponse response);
}






