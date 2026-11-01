# Branta .NET SDK

If you are implementing this SDK in a consumer project, see `README.md`. Product rules live in the [developer docs](https://developer.branta.pro).

---

## Developer notes (SDK contributors)

### Solution layout
- `Branta/` — the SDK that ships as the `Branta` NuGet package. `Branta.csproj` targets `net8.0;net10.0`.
- `Branta.Tests/` — xUnit test project mirroring the SDK namespaces.
- `Branta.sln` — wires the two projects together.

Public API:
- `IBrantaService` / `BrantaService` (`VerifyAsync`, `AddPaymentAsync`, `IsApiKeyValidAsync`).
- `ConfigureBrantaServices(BrantaApiOptions)` is the DI entry point.
- `PaymentBuilder`, `Payment`, `Destination`, `Platform`, `VerifyResult` / `VerifyStatus`.
- `BrantaApiOptions`, `BrantaEnvironment`, `DestinationType`, `BrantaPaymentException`.

`BrantaClient`, `QRParser`, and AES helpers are internal. Do not re-export them.

### Build / test / release
- Build: `dotnet build Branta.sln`
- Test: `dotnet test Branta.sln`
- Live example-QR tests: `BRANTA_RUN_INTEGRATION=1 dotnet test --filter FullyQualifiedName~ExampleQRCodes`
- Release: bump `<AssemblyVersion>` in `Branta/Branta.csproj`, build in Release, upload `Branta/bin/Release/Branta.X.X.X.nupkg` to nuget.org.

### Key behaviors to preserve
- **Encrypt everything.** On-chain lookups need `branta_id` / `branta_secret`. bolt11, Ark, and silent payments resolve from the value.
- **`VerifyUrl` is always returned**, including on a miss, tamper, or incomplete lookup.
- **`VerifyAsync` statuses:** `Match` (show the card), `Unknown` (not in Branta / 404), `Tampered` (destination altered), `Incomplete` (5xx, 429, or unreachable). Do not treat incomplete as unknown.
- **Derived-key bind:** a Guardrail hit is not a match unless a destination of that type decrypts.
- Keep parity with `branta-js` 4.x.

### Conventions
- `BrantaApiOptions` can be passed per-call to override DI-registered defaults.
- Tests mirror SDK namespaces: `Branta.Tests/V2/Services/...` mirrors `Branta/V2/Services/...`.
