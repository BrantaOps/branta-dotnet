# Branta .NET SDK

If you are implementing this SDK in a consumer project, see the **Integration Guide** section at the top of `README.md` — it covers integration types, recommended flows, and rules.

---

## Developer notes (SDK contributors)

### Solution layout
- `Branta/` — the SDK that ships as the `Branta` NuGet package. `Branta.csproj` targets `net8.0;net10.0`.
- `Branta.Tests/` — xUnit test project mirroring the SDK namespaces.
- `Branta.sln` — wires the two projects together.

Inside `Branta/`, V2 is the only supported API surface:
- `V2/Interfaces/IBrantaService.cs` — public entry point (`GetPaymentsAsync`, `GetPaymentsByQrCodeAsync`, `AddPaymentAsync`, `IsApiKeyValidAsync`).
- `V2/Services/BrantaService.cs` — orchestrates ZK encrypt/decrypt around HTTP calls.
- `V2/Services/BrantaClient.cs` — raw HTTP client; do not call directly from consumer code.
- `V2/Extensions/` — `ConfigureBrantaServices(BrantaClientOptions)` is the DI entry point.
- `Enums/PrivacyMode.cs`, `Enums/DestinationType.cs`, `Enums/BrantaServerBaseUrl.cs`.

### Build / test / release
- Build: `dotnet build Branta.sln`
- Test: `dotnet test Branta.sln`
- Release: bump `<AssemblyVersion>` in `Branta/Branta.csproj`, build in Release, upload `Branta/bin/Release/Branta.X.X.X.nupkg` to nuget.org.

### Key behaviors to preserve
- **`PrivacyMode.Strict` is the default.** It forbids plain-text on-chain lookups (`GetPaymentsAsync` throws `BrantaPaymentException`, `GetPaymentsByQrCodeAsync` returns an empty `PaymentsResult` with a populated `VerifyUrl`) and forbids non-ZK destinations on `AddPaymentAsync`. `Loose` removes those restrictions.
- **`VerifyUrl` is always returned**, including on a miss.
- **ZK destinations.** Bitcoin addresses are encrypted with a caller-supplied secret (random GUID via `GuidSecretGenerator`); hash-ZK types (bolt11/bolt12/ln_url/ln_address/ark/tether) use a deterministic key from a normalized hash of the value.
- **Prefer `GetPaymentsByQrCodeAsync`** for QR-driven flows.
- **Never surface lookup failures to the user.**

### Conventions
- Public API lives under `Branta.V2.*`.
- `BrantaClientOptions` can be passed per-call to override DI-registered defaults.
- Tests mirror SDK namespaces: `Branta.Tests/V2/Services/...` mirrors `Branta/V2/Services/...`.
- Keep parity with `branta-js` and `branta-dart`: mirror public method changes across all three SDKs.
