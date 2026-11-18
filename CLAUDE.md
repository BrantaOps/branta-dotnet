# Branta .NET SDK

.NET SDK published as the `Branta` NuGet package. Consumers (wallets, platforms, parent platforms) call `BrantaService` to look up or post payments to Branta. `README.md` is the install, constructor, and samples guide.

## Behavior contract

What `verify` returns and how ERC-681 is parsed: [Status rules](https://developer.branta.pro/setup/send/status-rules). Which rails encrypt with what key: [Encryption](https://developer.branta.pro/setup/receive/encryption). Send handling is on [Send](https://developer.branta.pro/setup/send), and the type list on [Destination types](https://developer.branta.pro/destination-types). Do not restate them here. Change the docs first, then the SDK.

Invariants that break silently. Check them before you change `verify` or `addPayment`:

- A raw on-chain address (including `tb1` / `bcrt1` / bare `0x`) is `unknown` and does not GET.
- A swapped address is `tampered`. `unknown`, `tampered`, and `incomplete` are returned, never thrown.
- The token is `matchedDestinations[].type`, never the URI scheme.
- A logo-domain mismatch throws. Every other lookup failure is a status.
- `addPayment` encrypts a copy and leaves the caller's `Payment` unchanged.
- Keep parity with every 4.x Branta SDK. A public method added or changed here is mirrored in each of them.

## .NET specifics

- The contract's names are `VerifyAsync`, `AddPaymentAsync`, `VerifyStatus` (`Match`, `Unknown`, `Tampered`, `Incomplete`), `MatchedDestinations[].Type`, and `VerifyUrl`.
- A logo-domain mismatch throws `BrantaPaymentException`.
- `DestinationType` is only what `AddPaymentAsync` accepts. It also includes `usdt_tron`, `usdt_ton`, `usdt_solana`, and `usdc_solana`. They can be posted but no URI verifies them, and they are not on the public Destination types page. There is no `usdc_ton`.
- `HmacSecret` is obsolete. A shared `ApiKey` plus `SetChildPlatform` is the parent-platform path.
- Default HTTP timeout is 30s.

## Solution layout

- `Branta/` — the SDK that ships as the `Branta` NuGet package. `Branta.csproj` targets `net8.0;net10.0`.
- `Branta.Tests/` — xUnit test project mirroring the SDK folders (`Services`, `Classes`, `Integration`).
- `Branta.sln` — wires the two projects together.

Public API lives in `namespace Branta` (plus the DI extension under `Microsoft.Extensions.DependencyInjection`):

- `IBrantaService` / `BrantaService` (`VerifyAsync`, `AddPaymentAsync`, `IsApiKeyValidAsync`). Construct with `new BrantaService(BrantaEnvironment)` or DI.
- `PaymentBuilder`, `Payment`, `Destination`, `Platform`, `VerifyResult` / `VerifyStatus` (including `MatchedDestinations` on Match), `AddPaymentResult`.
- `BrantaApiOptions`, `BrantaEnvironment`, `DestinationType`, `BrantaPaymentException`.

Do **not** make public: `BrantaClient`, `QRParser`, AES helpers, `DestinationTypes` (the rail table `EvmTokenRails` / `EvmTokenType` / `IsSecretKeyAddress`), `IncompleteError`, or `TamperedError`. Prefer `internal`. `PublicApiTests` locks the exported surface with reflection.

`ConfigureBrantaServices(BrantaApiOptions)` is the DI entry point.

## Build / test / release

- Build: `dotnet build Branta.sln`
- Test + coverage gate (line coverage ≥80% in CI): `dotnet test Branta.sln /p:CollectCoverage=true /p:Threshold=80 /p:ThresholdType=line /p:ThresholdStat=total /p:Exclude="[Branta.Tests]*"`
- Live multi-rail: `BRANTA_RUN_INTEGRATION=1 BRANTA_API_KEY=… dotnet test --filter FullyQualifiedName~MultiRailRoundTrip`
- Live example-QR lookups: `BRANTA_RUN_INTEGRATION=1 dotnet test --filter FullyQualifiedName~ExampleQRCodes`
- Release: bump `<AssemblyVersion>` in `Branta/Branta.csproj`, build in Release, upload `Branta/bin/Release/Branta.X.X.X.nupkg` to nuget.org, then tag the release commit `vX.X.X` and push the tag.

## Conventions

- `BrantaApiOptions` can be passed per-call to override DI-registered defaults.
- Tests mirror SDK folders: `Branta.Tests/Services/...` mirrors `Branta/Services/...`. Public types are `namespace Branta` regardless of folder. Guardrail HTTP paths stay `/v2/...`.
