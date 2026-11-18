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
        // What to send is result.MatchedDestinations — each has Type + plaintext Value.
        // e.g. UsdtBase / UsdcArbitrum / BitcoinAddress.
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

Pass the scanned or pasted text to `VerifyAsync`. A Bitcoin or EVM stablecoin lookup needs `branta_id` and `branta_secret` in the URI. bolt11, Ark, and silent payments resolve from the value. A bare address is `Unknown`. The full rules are on [Status rules](https://developer.branta.pro/setup/send/status-rules), and supported chains and contracts are on [Destination types](https://developer.branta.pro/destination-types).

## Receive

```csharp
using Branta;

var service = new BrantaService(BrantaEnvironment.Production, apiKey: "<api-key>");

var payment = new PaymentBuilder()
    .SetDescription("Invoice #12345")
    .AddDestination("bc1q…", DestinationType.BitcoinAddress)
    // Stablecoin: pass the type explicitly — bare addresses are not auto-detected.
    // .AddDestination("0x…", DestinationType.UsdtBase)
    // .AddDestination("0x…", DestinationType.UsdcArbitrum)
    .SetTtl(600) // seconds
    .Build();

var posted = await service.AddPaymentAsync(payment);
// posted.Secret, posted.VerifyUrl, posted.Payment
```

Store `secret` with the invoice. Put `branta_id` and `branta_secret` on the invoice QR (`bitcoin:…` or an ERC-681 `ethereum:…/transfer` URI).

`AddPaymentAsync` accepts the types on [Destination types](https://developer.branta.pro/destination-types), and [Encryption](https://developer.branta.pro/setup/receive/encryption) says which key each uses. `UsdtTron`, `UsdtTon`, `UsdtSolana`, and `UsdcSolana` also encrypt and post, but no URI verifies them.

ASP.NET: `services.ConfigureBrantaServices(options)` registers `IBrantaService`.

[Changelog](CHANGELOG.md)
