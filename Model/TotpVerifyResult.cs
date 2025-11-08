using System;
namespace Asp.TotpKit.Model;

// TOTP doğrulama sonucu
public class TotpVerifyResult
{

    // Başarılı mı?
    public bool Success { get; set; }
    
    // Mesaj
    public string Message { get; set; } = string.Empty;
    
    // Uyarı mesajı
    public string Warning { get; set; } = string.Empty;
}
