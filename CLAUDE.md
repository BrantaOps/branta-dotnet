# Branta .NET SDK

If you are implementing this SDK in a consumer project, see `README.md`. Product rules live in the [developer docs](https://developer.branta.pro).

---

## Developer notes (SDK contributors)

### Solution layout
- `Branta/` — the SDK that ships as the `Branta` NuGet package. `Branta.csproj` targets `net8.0;net10.0`.
- `Branta.Tests/` — xUnit test project mirroring the SDK folders (`Services`, `Classes`, `Integration`).
- `Branta.sln` — wires the two projects together.

Public API lives in `namespace Branta` (plus DI extension under `Microsoft.Extensions.DependencyInjection`):
- `IBrantaService` / `BrantaService` (`VerifyAsync`, `AddPaymentAsync`, `IsApiKeyValidAsync`). Construct with `new BrantaService(BrantaEnvironment)` or DI.
- `PaymentBuilder`, `Payment`, `Destination`, `Platform`, `VerifyResult` / `VerifyStatus` (including `MatchedDestinations` on Match), `AddPaymentResult`.
- `BrantaApiOptions`, `BrantaEnvironment`, `DestinationType`, `BrantaPaymentException`.

Do **not** make public: `BrantaClient`, `QRParser`, AES helpers, `DestinationTypes` (family lists / `SecretKeyUriFamilies` / `IsSecretKeyAddress`), `IncompleteError`, or `TamperedError`. Prefer `internal`. `PublicApiTests` locks the exported surface with reflection.

`ConfigureBrantaServices(BrantaApiOptions)` is the DI entry point.

### Build / test / release
- Build: `dotnet build Branta.sln`
- Test + coverage gate: `dotnet test Branta.sln /p:CollectCoverage=true /p:Threshold=80 /p:ThresholdType=line /p:ThresholdStat=total /p:Exclude="[Branta.Tests]*"`
- Live multi-rail: `BRANTA_RUN_INTEGRATION=1 BRANTA_API_KEY=… dotnet test --filter FullyQualifiedName~MultiRailRoundTrip`
- Live example-QR lookups: `BRANTA_RUN_INTEGRATION=1 dotnet test --filter FullyQualifiedName~ExampleQRCodes`
- Release: bump `<AssemblyVersion>` in `Branta/Branta.csproj`, build in Release, upload `Branta/bin/Release/Branta.X.X.X.nupkg` to nuget.org.

### Key behaviors to preserve
- **Encrypt everything.** On-chain lookups need `branta_id` / `branta_secret`. bolt11, Ark, and silent payments resolve from the value. Stablecoin rails (`usdt_polygon` … `usdc_solana`) use chain URIs (`pol:`, `eth:`, `bsc:`, `tron:`, `ton:`, `sol:`; internal `DestinationTypes.SecretKeyUriFamilies`) with the same secret-key pattern as bitcoin; token is on `MatchedDestinations[].Type`, not the URI scheme. A bare address is `Unknown`. No `usdc_ton` yet.
- **`VerifyUrl` is always returned**, including on a miss, tamper, or incomplete lookup.
- **`VerifyAsync` statuses:** `Match` (show the card), `Unknown` (not in Branta / 404), `Tampered` (destination altered), `Incomplete` (5xx, 429, unreachable, timeout, or malformed GET body). Do not treat incomplete as unknown. Logo-domain mismatches still throw `BrantaPaymentException`.
- **`MatchedDestinations` on Match** — wallets send from that list, not by filtering `Payments[].Destinations`.
- **Derived-key bind:** a Guardrail hit is not a match unless a destination of that type decrypts.
- **`HmacSecret` is obsolete** — shared `ApiKey` + `SetChildPlatform` is the parent-platform path.
- Default HTTP timeout is 30s. CI gates line coverage at ≥80%.
- Keep parity with `@branta-ops/branta` / Swift / Kotlin 4.3.0.

### Conventions
- `BrantaApiOptions` can be passed per-call to override DI-registered defaults.
- Tests mirror SDK folders: `Branta.Tests/Services/...` mirrors `Branta/Services/...`. Public types are `namespace Branta` regardless of folder. Guardrail HTTP paths stay `/v2/...`.
