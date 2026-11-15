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

Do **not** make public: `BrantaClient`, `QRParser`, AES helpers, `DestinationTypes` (the rail table `EvmTokenRails` / `EvmTokenType` / `IsSecretKeyAddress`), `IncompleteError`, or `TamperedError`. Prefer `internal`. `PublicApiTests` locks the exported surface with reflection.

`ConfigureBrantaServices(BrantaApiOptions)` is the DI entry point.

### Build / test / release
- Build: `dotnet build Branta.sln`
- Test + coverage gate: `dotnet test Branta.sln /p:CollectCoverage=true /p:Threshold=80 /p:ThresholdType=line /p:ThresholdStat=total /p:Exclude="[Branta.Tests]*"`
- Live multi-rail: `BRANTA_RUN_INTEGRATION=1 BRANTA_API_KEY=… dotnet test --filter FullyQualifiedName~MultiRailRoundTrip`
- Live example-QR lookups: `BRANTA_RUN_INTEGRATION=1 dotnet test --filter FullyQualifiedName~ExampleQRCodes`
- Release: bump `<AssemblyVersion>` in `Branta/Branta.csproj`, build in Release, upload `Branta/bin/Release/Branta.X.X.X.nupkg` to nuget.org.

### Key behaviors to preserve
- **Encrypt everything.** On-chain lookups need `branta_id` / `branta_secret`. bolt11, Ark, and silent payments resolve from the value. EVM USDT/USDC (`usdt_polygon` … `usdc_arbitrum`, including `usdt_base`, `usdc_base`, `usdt_arbitrum`, `usdc_arbitrum`) verify as ERC-681: `ethereum:<contract>@<chainId>/transfer?address=<deposit>` plus `branta_id` / `branta_secret`. Chain id plus contract select one destination type (internal `DestinationTypes.EvmTokenRails`). `uint256` is not bound. `pol:`, `eth:`, `bsc:`, `tron:`, `ton:`, and `sol:` do not verify and do not GET. `AddPaymentAsync` still encrypts `usdt_tron`, `usdt_ton`, `usdt_solana`, and `usdc_solana`. A scan binds only the destination type its chain id and contract name. Token is on `MatchedDestinations[].Type`. A bare address is `Unknown`. No `usdc_ton` yet.
- **`VerifyUrl` is always returned**, including on a miss, tamper, or incomplete lookup.
- **`VerifyAsync` statuses:** `Match` (show the card), `Unknown` (not in Branta / 404), `Tampered` (destination altered), `Incomplete` (5xx, 429, unreachable, timeout, or malformed GET body). Do not treat incomplete as unknown. Logo-domain mismatches still throw `BrantaPaymentException`.
- **`MatchedDestinations` on Match** — wallets send from that list, not by filtering `Payments[].Destinations`.
- **Derived-key bind:** a Guardrail hit is not a match unless a destination of that type decrypts.
- **`HmacSecret` is obsolete** — shared `ApiKey` + `SetChildPlatform` is the parent-platform path.
- Default HTTP timeout is 30s. CI gates line coverage at ≥80%.
- Keep parity with `@branta-ops/branta` / Swift / Kotlin / branta-go / branta-rust 4.4.0.

### Conventions
- `BrantaApiOptions` can be passed per-call to override DI-registered defaults.
- Tests mirror SDK folders: `Branta.Tests/Services/...` mirrors `Branta/Services/...`. Public types are `namespace Branta` regardless of folder. Guardrail HTTP paths stay `/v2/...`.
