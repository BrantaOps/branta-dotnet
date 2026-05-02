# Branta .NET SDK

Package contains functionality to assist .NET projects with making requests to Branta's server.

## Requirements

 * .NET 8.0 or higher

## Installation

Install via NuGet Package Manager:

```bash
dotnet add package Branta
```
Or via Package Manager Console:
```ps
Install-Package Branta
```

## Quick Start

### For Wallets

```cs
using Branta.V2.Extensions;
using Branta.V2.Interfaces;

services.ConfigureBrantaServices(new BrantaClientOptions() {
    BaseUrl = BrantaServerBaseUrl.Production,
    Privacy = PrivacyMode.Loose
});
```

```cs
public class Example(IBrantaService brantaService)
{
    public async Task ExampleMethod(string qrCodeText)
    {
        // recommended — handles multiple ZK values
        var payments = await brantaService.GetPaymentsByQrCodeAsync(qrCodeText);

        // OR
        var payments = await brantaService.GetPaymentsAsync("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa");
    }
}
```

### For Platforms

```cs
services.ConfigureBrantaServices(new BrantaClientOptions() {
    BaseUrl = BrantaServerBaseUrl.Production,
    DefaultApiKey = "<api-key>",
    Privacy = PrivacyMode.Loose
});
```

```cs
await _brantaService.AddPaymentAsync(new Payment {
    Description = "Testing description",
    Destinations =
    [
        new Destination { Value = "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", IsZk = false }
    ],
    TTL = 600
});
```

### For Parent Platforms

```cs
services.ConfigureBrantaServices(new BrantaClientOptions() {
    BaseUrl = BrantaServerBaseUrl.Production,
    DefaultApiKey = "<api-key>",
    HmacSecret = "<hmac-secret>",
    Privacy = PrivacyMode.Loose
});
```

```cs
await _brantaService.AddPaymentAsync(new Payment {
    Description = "Testing description",
    Destinations =
    [
        new Destination { Value = "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", IsZk = false }
    ],
    TTL = 600
});
```

## Privacy

`PrivacyMode` controls whether plain-text on-chain lookups are allowed.

| Value | Behavior |
|-------|----------|
| `Strict` | Only ZK (zero-knowledge / encrypted) lookups are permitted. `GetPaymentsAsync` throws `BrantaPaymentException` for plain addresses; `GetPaymentsByQrCodeAsync` returns an empty list. `AddPaymentAsync` requires all destinations to have `IsZk = true`. |
| `Loose` | Both plain and ZK lookups are allowed. No restrictions enforced. |

→ [`Branta/Enums/PrivacyMode.cs`](Branta/Enums/PrivacyMode.cs)

## IBrantaService

The primary service interface registered by `ConfigureBrantaServices()`.

**Prefer `GetPaymentsByQrCodeAsync` for integrations.** It parses the raw QR text and correctly resolves multiple ZK values in a single scan. `GetPaymentsAsync` only handles a single destination value and does not support multi-value ZK lookups.

```cs
Task<List<Payment>> GetPaymentsByQrCodeAsync(string qrText); // recommended
Task<List<Payment>> GetPaymentsAsync(string destinationValue, string? destinationEncryptionKey = null);
Task<(Payment, string)> AddPaymentAsync(Payment payment);
Task<bool> IsApiKeyValidAsync(BrantaClientOptions? options = null);
```

→ [`Branta/V2/Interfaces/IBrantaService.cs`](Branta/V2/Interfaces/IBrantaService.cs)

## Release

 - Open .sln file in Visual Studio
 - Update version in `Branta/Branta.csproj`
 - Change Configuration from Debug to Release
 - Run Build
 - Package can be found at `Branta/bin/Release/Branta.X.X.X.nupkg`
 - Upload this file to the new release on nuget.org
