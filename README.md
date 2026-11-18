# Branta .NET SDK

.NET SDK for [Branta](https://developer.branta.pro). Targets `net8.0` and `net10.0`.

Status handling and parent platforms are in the [developer docs](https://developer.branta.pro).

## Install

```bash
dotnet add package Branta
```

## Send

```csharp
using Branta;

var service = new BrantaService(BrantaEnvironment.Production);

var result = await service.VerifyAsync(input); // scanned QR or pasted text

switch (result.Status)
{
    case VerifyStatus.Match:
        // Show the card for result.Payments[0]. Link it to result.VerifyUrl.
        // What to send is result.MatchedDestinations — each has Type + plaintext Value.
        break;
    case VerifyStatus.Unknown:
        // Not in Branta — show nothing.
        break;
    case VerifyStatus.Tampered:
        // Destination was altered. Warn. Do not show a verified card.
        break;
    case VerifyStatus.Incomplete:
        // Branta could not complete the check. Tell the user. Do not block send.
        break;
}
```

Rules for each status: [Status rules](https://developer.branta.pro/setup/send/status-rules). Supported types: [Destination types](https://developer.branta.pro/destination-types).

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
// Store posted.Secret with the invoice. Put branta_id and branta_secret on the QR.
// Link the sender to posted.VerifyUrl.
```

`AddPaymentAsync` accepts the types on [Destination types](https://developer.branta.pro/destination-types). Pass the type for stablecoins. A bare address is not auto-detected. `UsdtTron`, `UsdtTon`, `UsdtSolana`, and `UsdcSolana` also encrypt and post, but no URI verifies them.

ASP.NET: `services.ConfigureBrantaServices(options)` registers `IBrantaService`.

[Changelog](CHANGELOG.md)
