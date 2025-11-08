using System;

namespace Asp.TotpKit.Interface;

public interface ITotpUserInfo
{
    string? ToptSecret { get; set; }
    bool IsToptEnabled { get; set; }
    DateTime? ToptVerifiedDate { get; set; }
    bool IsTotpResetRequested { get; set; }
    DateTime? TotpResetRequestedAt { get; set; }
}
