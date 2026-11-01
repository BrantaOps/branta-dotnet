# Branta .NET SDK

.NET SDK for [Branta](https://developer.branta.pro). Targets `net8.0` and `net10.0`.

Use `VerifyAsync` on send and `AddPaymentAsync` on receive. Status handling and parent platforms are in the [developer docs](https://developer.branta.pro).

## Install

```bash
dotnet add package Branta
```

## Send

```csharp
using Branta;

var service = new BrantaService(BrantaEnvironment.Production); // Staging everywhere else

var result = await service.VerifyAsync(input); // scanned QR or pasted text

switch (result.Status)
{
    case VerifyStatus.Match:
        // Show the card for result.Payments[0]. Link it to result.VerifyUrl.
        break;
    case VerifyStatus.Unknown:
        // Not in Branta — show nothing.
        break;
    case VerifyStatus.Tampered:
        // Destination was altered. Warn. Do not show a verified card.
        break;
    case VerifyStatus.Incomplete:
        // 5xx, 429, or unreachable. Tell the user. Do not treat as unknown.
        break;
}
```

On-chain Bitcoin needs a `bitcoin:` URI with `branta_id` / `branta_secret`. bolt11, Ark, and silent payments resolve from the value. A bare address is `unknown`.

A 404 is `unknown`. 5xx, 429, or no network is `incomplete`.

## Receive

```csharp
using Branta;

var service = new BrantaService(BrantaEnvironment.Production, apiKey: "<api-key>");

var payment = new PaymentBuilder()
    .SetDescription("Invoice #12345")
    .AddDestination("bc1q…", DestinationType.BitcoinAddress)
    .SetTtl(600) // seconds
    .Build();

var posted = await service.AddPaymentAsync(payment);
// posted.Secret, posted.VerifyUrl, posted.Payment
```

Store `secret` with the invoice. Put `branta_id` and `branta_secret` on the invoice QR.

Parent platforms: prefer a shared API key and `SetChildPlatform()`. Per-client HMAC (`HmacSecret` on `BrantaApiOptions`) is still supported.

ASP.NET: `services.ConfigureBrantaServices(options)` registers `IBrantaService`.

[Changelog](CHANGELOG.md)
