using System;
namespace Asp.TotpKit.Model;
//TotP kurulum yanıt modeli
public class TotpSetupResponse
{
    // Başarılı mı?
    public bool Success { get; set; } = true;

    // Hata mesajı (varsa)
    public string Message { get; set; } = string.Empty;

    //Secret key (Base32)
    public string SecretKey { get; set; } = string.Empty;

    // QR Code URI
    public string QrCodeUri { get; set; } = string.Empty;

    // Manuel giriş için key (boşluksuz)
    public string ManualEntryKey { get; set; } = string.Empty;

    // Kurulum talimatları
    public TotpInstructions Instructions { get; set; } = new();
}
