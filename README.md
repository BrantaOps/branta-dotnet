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

# Quick Start

## For Wallets

Wallets should use `Strict` privacy mode. Two flows are supported:

- **Copy/paste**: call `GetPaymentsAsync` with the pasted text. Plain-text on-chain addresses will not return results in strict mode — they must be ZK-encoded. Lightning destinations (bolt11, bolt12, ln_url, ln_address) work as plain text.
- **QR scan**: call `GetPaymentsByQrCodeAsync` with the raw QR text. This handles both on-chain (when the QR includes `branta_id` / `branta_secret`) and lightning destinations.

Always catch errors and show nothing on not-found — a missing record just means the address was not posted to Branta.

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
