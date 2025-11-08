# **Asp.TotpKit**  
**TOTP (Time-based One-Time Password) Authentication Library for .NET**  
> Secure, plug-and-play 2FA (Google Authenticator compatible) system built for ASP.NET developers.  
> NuGet: [https://www.nuget.org/packages/Asp.TotpKit](https://www.nuget.org/packages/Asp.TotpKit)

---

## Purpose
I was tired of rebuilding the same **TOTP (Time-Based One-Time Password)** systems over and over — QR code generation, secret key encoding, validation, time window handling, you name it.

**Asp.TotpKit** solves that problem.

It provides a clean, extensible, and Identity-ready 2FA layer compatible with Google Authenticator and similar apps, without extra dependencies or complex setup.

> Works with or without ASP.NET Identity.  
> Just inject the service, call the methods, and you’ve got production-grade 2FA in minutes.

---

## ⚙️ Features
- Time-based one-time password generation (RFC 6238)
- QR Code creation for Google Authenticator setup
- 30-second rotating codes
- Code verification with tolerance window
- Optional cookie-based temporary validation support
- Works with `AppUser` (Identity) or any custom model
- Simple DI registration — plug & play
- No external dependencies beyond `QRCoder` & `.NET`

---

## 📦 Installation

### NuGet
```bash
dotnet add package Asp.TotpKit
```
## Documentation
- 🇹🇷 [Türkçe PDF Dokümanı](https://github.com/01YunusKaratas/Asp.TotpKit/blob/main/Docs/Asp.TotpKit-Tr.pdf)
- 🇬🇧 [English PDF Documentation](https://github.com/01YunusKaratas/Asp.TotpKit/blob/main/Docs/Asp.TotpKit-En.pdf) 

## Design Philosophy
Yunus.TotpKit was built with the same philosophy as Yunus.JwtKit:
a clean, decoupled design that favors simplicity and clarity.

✅ Dependency-Injection friendly

✅ Customizable via TotpOptions<TUser>

✅ Minimal configuration

✅ Fully async, exception-safe, and logged

It can serve as a standalone 2FA service or a drop-in extension to your existing identity layer.

