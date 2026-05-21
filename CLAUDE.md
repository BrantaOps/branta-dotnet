# Branta .NET SDK

.NET 8/10 library that wraps Branta's V2 payment-verification API. Consumers (wallets, platforms, parent platforms) call `IBrantaService` to look up or post payments.

## Solution layout
- `Branta/` — the SDK that ships as the `Branta` NuGet package. `Branta.csproj` targets `net8.0;net10.0` and sets `GeneratePackageOnBuild`.
- `Branta.Tests/` — xUnit-style test project mirroring the SDK namespaces.
- `Branta.sln` — wires the two projects together.

Inside `Branta/`, V2 is the only supported API surface:
- `V2/Interfaces/IBrantaService.cs` — public entry point (`GetPaymentsAsync`, `GetPaymentsByQrCodeAsync`, `AddPaymentAsync`, `IsApiKeyValidAsync`).
- `V2/Services/BrantaService.cs` — orchestrates ZK encrypt/decrypt around HTTP calls.
- `V2/Services/BrantaClient.cs` — raw HTTP client; do not call directly from consumer code.
- `V2/Extensions/` — `ConfigureBrantaServices(BrantaClientOptions)` is the DI entry point.
- `Enums/PrivacyMode.cs`, `Enums/DestinationType.cs`, `Enums/BrantaServerBaseUrl.cs`.

## Build / test / release
- Build: `dotnet build Branta.sln`
- Test: `dotnet test Branta.sln`
- Release a NuGet package: bump `<AssemblyVersion>` in `Branta/Branta.csproj`, build in Release, upload `Branta/bin/Release/Branta.X.X.X.nupkg` to nuget.org. (Full steps in `README.md`.)

## Key behaviors to preserve when editing the SDK
- **`PrivacyMode.Strict` is the default.** It forbids plain-text on-chain lookups (`GetPaymentsAsync` throws `BrantaPaymentException`, `GetPaymentsByQrCodeAsync` returns an empty `PaymentsResult` with a populated `VerifyUrl`) and forbids non-ZK destinations on `AddPaymentAsync`. `Loose` removes those restrictions.
- **`VerifyUrl` is always returned**, including on a miss. Format is `{baseUrl}/v2/verify/{lookup}` plus a fragment built from per-destination encryption keys (see `BuildVerifyUrl` in `BrantaService.cs`).
- **ZK destinations.** Bitcoin addresses are encrypted with a caller-supplied secret (random GUID via `GuidSecretGenerator`); hash-ZK types (bolt11/bolt12/ln_url/ln_address/ark/tether) are encrypted with a deterministic key derived from a normalized hash of the value, so the same input always produces the same lookup token. `BrantaService.AddPaymentAsync` mutates `payment.Destinations[*].Value` to the encrypted form before POSTing.
- **Prefer `GetPaymentsByQrCodeAsync`** for any QR-driven flow — it parses multi-value QR payloads (`branta_id`/`branta_secret` fragments) via `QRParser`. `GetPaymentsAsync` only handles a single destination string.
- **Never surface lookup failures to the user.** README documents the consumer contract: swallow errors and an empty `Payments` list both mean "show nothing."

## Conventions
- Public API lives under `Branta.V2.*`; types outside `V2/` (in `Branta/Classes`, `Branta/Extensions`, `Branta/Enums`) are shared primitives reused by V2.
- `BrantaClientOptions` can be passed per-call to override the DI-registered defaults — every public method accepts an optional `options` parameter; respect that pattern when adding new methods.
- Tests live alongside the namespace they cover (`Branta.Tests/V2/Services/...` mirrors `Branta/V2/Services/...`).
