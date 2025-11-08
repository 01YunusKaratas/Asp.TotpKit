using System;

namespace Asp.TotpKit.Model;

public class TotpOptions<TUser>
{
    public string Issuer { get; set; } = "Asp.TotpKit";
    public string AppName { get; set; } = "Asp.TotpKit";
    public Func<TUser, string>? GetUserEmail { get; set; }
    public Func<TUser, string>? GetUserId { get; set; }
}
