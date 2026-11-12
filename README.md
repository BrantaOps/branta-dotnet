# Branta .NET SDK

.NET SDK for [Branta](https://developer.branta.pro). Targets `net8.0` and `net10.0`.

Use `VerifyAsync` on send and `AddPaymentAsync` on receive. Status handling and parent platforms are in the [developer docs](https://developer.branta.pro).

## Install

```bash
dotnet add package Branta --version 4.3.0
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
        // e.g. UsdtEthereum / UsdcSolana / BitcoinAddress. Do not infer token from the URI scheme.
        break;
    case VerifyStatus.Unknown:
        // Not in Branta — show nothing.
        break;
    case VerifyStatus.Tampered:
        // Destination was altered. Warn. Do not show a verified card.
        break;
    case VerifyStatus.Incomplete:
        // 5xx, 429, unreachable, timeout, or malformed lookup. Tell the user.
        // Do not treat as unknown.
        break;
}
```

On-chain Bitcoin needs a `bitcoin:` URI with `branta_id` / `branta_secret`. Stablecoin rails need a chain URI with the same params — `pol:` (`usdt_polygon`, `usdc_polygon`), `eth:` (`usdt_ethereum`, `usdc_ethereum`), `bsc:` (`usdt_bsc`, `usdc_bsc`), `tron:` (`usdt_tron`), `ton:` (`usdt_ton`), `sol:` (`usdt_solana`, `usdc_solana`). Token is on `MatchedDestinations[].Type`, not the URI scheme; verify is match-if-any within the scheme's family. bolt11, Ark, and silent payments resolve from the value. A bare address (including bare `0x`) is `unknown`.

A 404 is `unknown`. 5xx, 429, no network, a timeout, or a malformed GET body is `incomplete`. A logo on the wrong origin throws.

## Receive

```csharp
using Branta;

var service = new BrantaService(BrantaEnvironment.Production, apiKey: "<api-key>");

var payment = new PaymentBuilder()
    .SetDescription("Invoice #12345")
    .AddDestination("bc1q…", DestinationType.BitcoinAddress)
    // Stablecoin: pass the type explicitly — bare addresses are not auto-detected.
    // .AddDestination("0x…", DestinationType.UsdtEthereum)
    .SetTtl(600) // seconds
    .Build();

var posted = await service.AddPaymentAsync(payment);
// posted.Secret, posted.VerifyUrl, posted.Payment
```

Store `secret` with the invoice. Put `branta_id` and `branta_secret` on the invoice QR (`bitcoin:…`, `pol:…`, `eth:…`, …).

`HmacSecret` is obsolete. Prefer a shared API key and `SetChildPlatform()`.

ASP.NET: `services.ConfigureBrantaServices(options)` registers `IBrantaService` with a 30s HTTP timeout.

[Changelog](CHANGELOG.md)
