# Branta .NET SDK

Package contains functionality to assist .NET projects with making requests to Branta's server.

# Requirements

 * .NET 8.0 or higher

# Installation

Install via NuGet Package Manager:

```bash
dotnet add package Branta
```
Or via Package Manager Console:
```ps
Install-Package Branta
```

# Integration Guide

If you are an AI agent implementing this SDK, follow the steps below exactly. If you are a developer, use this as a reference for the recommended integration patterns.



**1. Ask which side(s) of a payment they are implementing:**
- **Send side** — the flow for outbound payments (e.g. a wallet sending funds).
- **Receive side** — the flow for receiving payment (e.g. a checkout, POS, invoicing platform). You post destinations (zero-knowledge encrypted) to Branta so wallets can verify them.
- **Both** — some apps do both (e.g. an exchange or self-custodial wallet with invoice generation). Implement each side independently.

If they are on the receive side, ask one follow-up:
- **Platform** — single-tenant, one API key.
- **Parent Platform** — multi-tenant, manages multiple child platforms each with their own API key and HMAC secret.

**2. Follow the matching Quick Start section below.**

**3. Apply these rules:**

General (all types):
- Always use `PrivacyMode.Strict`. Never switch to `Loose` unless there is no QR scanner and ZK is impossible.
- Never call `BrantaClient` directly — always go through `IBrantaService`.

Send side (wallets):
- Prefer `GetPaymentsByQrCodeAsync` over `GetPaymentsAsync` — it handles multi-value ZK QR payloads correctly.
- Only fall back to `GetPaymentsAsync` for copy/paste flows where there is no QR code.
- If QR scanning is not available, see the **No-QR-Code Flows** section for recommended options.
- If `Payments` is empty or an exception is thrown, render nothing. Never show an error or "not verified" message — an empty result means the destination is unknown to Branta, not that it is malicious.
- When `result.Payments` is non-empty, display: the platform logo, the payment description, and `result.VerifyUrl`.
- For the platform logo, use `payment.PlatformLogoUrl` on dark backgrounds and `payment.PlatformLogoLightUrl` on light backgrounds.
- Optionally display `payment.ParentPlatform.LogoUrl` / `payment.ParentPlatform.LogoLightUrl` as a small secondary badge (e.g. corner icon). This is not required.

Receive side (platforms):
- Always call `.SetZk()` on the `PaymentBuilder` before calling `AddPaymentAsync`. Plain-text destinations are rejected in `Strict` mode.
- Store the `secret` returned by `AddPaymentAsync` alongside the invoice — it is required to reconstruct the verify URL for the wallet.

Receive side (parent platforms), in addition to the platform rules:
- Include `HmacSecret` in `BrantaClientOptions`.
- Pass per-call `BrantaClientOptions` to scope requests to the correct child platform's API key.

# Quick Start

## For Wallets

Wallets should use `Strict` privacy mode. Two flows are supported:

- **Copy/paste**: call `GetPaymentsAsync` with the pasted text. Plain-text on-chain addresses will not return results in strict mode — they must be ZK-encoded. Self-encrypted destinations (bolt11, ark_address) work as plain text.
- **QR scan**: call `GetPaymentsByQrCodeAsync` with the raw QR text. This handles both on-chain (when the QR includes `branta_id` / `branta_secret`) and self-encrypted destinations.

Always catch errors and show nothing on not-found — a missing record just means the address was not posted to Branta.

### No-QR-Code Flows

When QR scanning is not available, three options exist. Choose one based on how much control you want to give users over privacy:

**Option 1 — Keep Strict mode (no code changes)**

Only self-encrypted destinations (bolt11, ark_address) will return results. Plain-text on-chain address lookups silently return empty. This is the safest default and requires no additional work.

**Option 2 — Opt-in Loose mode (Recommended)**

Add a user-facing setting (e.g. "Enable on-chain address verification"). Only switch to `PrivacyMode.Loose` when the user explicitly opts in — this sends on-chain addresses in plain text, so the choice should be theirs.

> **AI agents:** Before implementing this option, ask the developer which of the following approaches fits their codebase:
>
> 1. **Startup-time configuration** — the preference is loaded once at startup (e.g. from a config file or persisted user setting) and passed to `ConfigureBrantaServices`. Best when the setting rarely changes without a restart.
> 2. **Per-call runtime override** — `Strict` is registered as the DI default, and a `BrantaClientOptions` override is passed on each call when the user has opted in. Best when the preference can change while the app is running.
> 3. **Two named registrations** — register both a `Strict` and a `Loose` instance and inject the appropriate one based on context. Best for apps that already use keyed/named DI services.

If the preference is known at startup (approach 1):
```cs
services.ConfigureBrantaServices(new BrantaClientOptions {
    BaseUrl = BrantaServerBaseUrl.Production,
    Privacy = userOptedIn ? PrivacyMode.Loose : PrivacyMode.Strict
});
```

If the preference can change at runtime (approach 2), keep `Strict` as the registered default and pass a per-call override when the user has opted in:
```cs
var options = userPreferences.OnChainVerificationEnabled
    ? new BrantaClientOptions { Privacy = PrivacyMode.Loose }
    : null;

var result = await brantaService.GetPaymentsAsync(input, options: options);
```

**Option 3 — Always Loose mode**

Configure with `PrivacyMode.Loose` globally. All lookups including plain-text on-chain addresses are sent to Branta. Simplest, but gives users no privacy control.

```cs
services.ConfigureBrantaServices(new BrantaClientOptions {
    BaseUrl = BrantaServerBaseUrl.Production,
    Privacy = PrivacyMode.Loose
});
```

---

```cs
using Branta.V2.Extensions;
using Branta.V2.Interfaces;

services.ConfigureBrantaServices(new BrantaClientOptions() {
    BaseUrl = BrantaServerBaseUrl.Production,
    Privacy = PrivacyMode.Strict
});
```

```cs
public class Example(IBrantaService brantaService)
{
    public async Task LookupAsync(string input, bool isQrCode)
    {
        try
        {
            var result = isQrCode
                ? await brantaService.GetPaymentsByQrCodeAsync(input)
                : await brantaService.GetPaymentsAsync(input);

            if (result.Payments.Count == 0)
            {
                // Not found — show nothing. The address may simply not exist in Branta.
                return;
            }

            // Render result.Payments and result.VerifyUrl
        }
        catch
        {
            // Swallow errors — never surface a "not found" or lookup failure to the user.
        }
    }
}
```

## For Platforms

Platforms post payments to Branta so wallets can verify them. Use `Strict` privacy mode and mark each destination ZK via `SetZk()` on the `PaymentBuilder`.

```cs
services.ConfigureBrantaServices(new BrantaClientOptions() {
    BaseUrl = BrantaServerBaseUrl.Production,
    DefaultApiKey = "<api-key>",
    Privacy = PrivacyMode.Strict
});
```

```cs
var payment = new PaymentBuilder()
    .SetDescription("Testing description")
    .AddDestination("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", DestinationType.BitcoinAddress)
    .SetZk()
    .SetTtl(600)
    .Build();

var (response, secret, verifyUrl) = await _brantaService.AddPaymentAsync(payment);
// `secret` is the encryption key needed to look the payment up later.
```

## For Parent Platforms

Parent Platforms sign requests with HMAC in addition to the API key. Use `Strict` privacy mode and ZK destinations.

```cs
services.ConfigureBrantaServices(new BrantaClientOptions() {
    BaseUrl = BrantaServerBaseUrl.Production,
    DefaultApiKey = "<api-key>",
    HmacSecret = "<hmac-secret>",
    Privacy = PrivacyMode.Strict
});
```

```cs
var payment = new PaymentBuilder()
    .SetDescription("Testing description")
    .AddDestination("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", DestinationType.BitcoinAddress)
    .SetZk()
    .SetTtl(600)
    .Build();

var (response, secret, verifyUrl) = await _brantaService.AddPaymentAsync(payment);
```

# Privacy

`PrivacyMode` controls whether plain-text on-chain lookups are allowed.

| Value | Behavior |
|-------|----------|
| `Strict` | Only ZK (zero-knowledge / encrypted) lookups are permitted. `GetPaymentsAsync` throws `BrantaPaymentException` for plain addresses; `GetPaymentsByQrCodeAsync` returns an empty list. `AddPaymentAsync` requires all destinations to have `IsZk = true`. |
| `Loose` | Both plain and ZK lookups are allowed. No restrictions enforced. |

→ [`Branta/Enums/PrivacyMode.cs`](Branta/Enums/PrivacyMode.cs)

# IBrantaService

The primary service interface registered by `ConfigureBrantaServices()`.

**Prefer `GetPaymentsByQrCodeAsync` for integrations.** It parses the raw QR text and correctly resolves multiple ZK values in a single scan. `GetPaymentsAsync` only handles a single destination value and does not support multi-value ZK lookups.

```cs
Task<PaymentsResult> GetPaymentsByQrCodeAsync(string qrText); // recommended
Task<PaymentsResult> GetPaymentsAsync(string destinationValue, string? destinationEncryptionKey = null);
Task<(Payment Payment, string Secret, string VerifyUrl)> AddPaymentAsync(Payment payment);
Task<bool> IsApiKeyValidAsync(BrantaClientOptions? options = null);
```

`PaymentsResult` contains the list of matching `Payments` and the `VerifyUrl` to display to the user — `VerifyUrl` is always returned, even when `Payments` is empty.

→ [`Branta/V2/Interfaces/IBrantaService.cs`](Branta/V2/Interfaces/IBrantaService.cs)

# Release

 - Open .sln file in Visual Studio
 - Update version in `Branta/Branta.csproj`
 - Change Configuration from Debug to Release
 - Run Build
 - Package can be found at `Branta/bin/Release/Branta.X.X.X.nupkg`
 - Upload this file to the new release on nuget.org

# Responsible Disclosure

Found critical bugs/vulnerabilities? Please email them to support@branta.pro. Thanks!
