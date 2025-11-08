using System;
namespace Asp.TotpKit.Model;
//TOTP kurulum talimatları
public class TotpInstructions
{
    // Web için talimat
    public string Web { get; set; } = "QR kodu Google Authenticator ile tarayın";
    
    // Mobil için talimat
    public string Mobile { get; set; } = "32 haneli kodu Google Authenticator'a manuel olarak girin";
    // Genel bilgi
    public string General { get; set; } = "Kurulum tamamlandıktan sonra 6 haneli kodu girerek doğrulayın";
}
