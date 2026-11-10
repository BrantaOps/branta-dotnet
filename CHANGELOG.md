# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [4.2.0] - 2026-11-10

### Added
- `DestinationType.UsdtPolygon` for Polygon USDT. `AddPaymentAsync` encrypts the recipient with a random secret (same pattern as bitcoin). `VerifyAsync` parses `pol:` URIs with `branta_id` / `branta_secret`; other query params (`value`, `currency`, `user`) are ignored for binding. A bare `0x` address is `Unknown`. EVM address compare is case-insensitive.
- `DestinationType.UsdcPolygon` for Polygon USDC. Same secret-key encrypt / `pol:` verify path as `usdt_polygon`. Token is the destination type on the payment; `pol:` remains the Polygon EVM scheme for both.
- `VerifyAsync` `Match` includes `MatchedDestinations`: the rails this scan bound (plaintext + `Type`). Wallets should send from that list instead of filtering `Payments[].Destinations`.
- `DestinationTypes.PolygonEvm` / `IsPolygonEvm` / `IsSecretKeyAddress` helpers.

### Changed
- `VerifyAsync` of a `pol:` URI binds any Polygon EVM family member whose decrypted address matches (match-if-any).
- `pol:` parsing no longer stamps a token `DestinationType` on the QR address. The scheme maps to the Polygon EVM family for verify; the token still comes from the matched payment destination.

## [4.0.3] - 2026-11-01

### Fixed
- `VerifyAsync` of a bolt11, Ark, or silent payment returns `Tampered` when the decrypted plaintext is not the scanned value. Case differences still match.
- `AddPaymentAsync` encrypts a copy of the caller's `Payment`. A failed post leaves that object plaintext, so a retry encrypts the original destination again.

## [4.0.2] - 2026-11-01

### Changed
- `SetDescription` writes encrypted `metadata.description`. The SDK does not POST or GET Guardrail's plaintext `description` field. After metadata decrypt, `Payment.Description` is lifted from `metadata.description` for wallets.

## [4.0.1] - 2026-11-01

### Added
- Restored `Payment.BtcPayServerPluginVersion` / `PaymentBuilder.SetBtcPayServerPluginVersion()`. Guardrail still stores `btc_pay_server_plugin_version` from the BTCPay plugin.

## [4.0.0] - 2026-10-31

Port of `@branta-ops/branta` 4.0.2.

### Removed
- **BREAKING:** `PrivacyMode` and the `Privacy` client option. The SDK encrypts every destination: on-chain lookups need `branta_id` / `branta_secret`; bolt11, Ark, and silent payments still resolve from the value.
- **BREAKING:** `PaymentBuilder.SetZk()` and `Destination.IsZk` / `Destination.ZkId`.
- **BREAKING:** `GetPaymentsAsync` and `GetPaymentsByQrCodeAsync`. Wallets call `VerifyAsync(input)` for both QR scan and clipboard paste.
- **BREAKING:** `BrantaClient` and `IBrantaClient` are no longer public. A GET of `branta_id` without `VerifyAsync` skips decrypt and the address bind, so a swapped QR can still show a merchant card.
- **BREAKING:** `QRParser`, `AesEncryption`, `AesEncryptionService`, `IAesEncryption`, `GuidSecretGenerator`, `ISecretGenerator`, `BrantaExtensions`, and `UrlAttribute` are no longer public. Call `IBrantaService`.
- **BREAKING:** `VerifyAsync` returns `{ Status, VerifyUrl }` with `Status` of `Match`, `Unknown`, `Tampered`, or `Incomplete`. A swapped address is `Tampered`, not a thrown exception. `Payments` is present only on `Match`. `PaymentsResult` and `BrantaPaymentExceptionReason` are gone.
- **BREAKING:** `DestinationType.Bolt12`, `LnUrl`, `TetherAddress`, and `LnAddress`. `DestinationType` is the set of types `AddPaymentAsync` accepts: bitcoin, bolt11, Ark, and silent payments.
- **BREAKING:** `BrantaClientOptions` is `BrantaApiOptions`. `BaseUrl` is `Environment`; `BrantaServerBaseUrl` is `BrantaEnvironment`; `DefaultApiKey` is `ApiKey`.
- **BREAKING:** `QRParseException`. Nothing threw it.
- **BREAKING:** `Payment.BtcPayServerPluginVersion`.
- **BREAKING:** `Payment.GetDefaultValue()`.
- **BREAKING:** `Destination.ZkId` is `Destination.Id`. Guardrail still receives `zk_id`.
- **BREAKING:** Public types live in `namespace Branta`. `ConfigureBrantaServices` is in `Microsoft.Extensions.DependencyInjection`.
- **BREAKING:** `CreatePaymentBuilder()` is gone. Use `new PaymentBuilder()`.
- **BREAKING:** `AddPaymentAsync` returns `AddPaymentResult` (`Payment`, `Secret`, `VerifyUrl`) instead of a tuple.
- **BREAKING:** `Destination.Zk` and `Payment.IsMetadataDecrypted` are no longer public. POST still sends `zk: true`. Use `Destination.IsEncrypted` after verify.
- **BREAKING:** `Payment.TTL` is `Ttl`. `Payment.CreatedDate` is `CreatedAt`. `ConfigureBrantaServices` requires options.

### Added
- `VerifyAsync` status `Incomplete` when the lookup does not finish (5xx, 429, or no network). A 404 is still `Unknown`.
- `new BrantaService(BrantaEnvironment)` and `new BrantaService(BrantaEnvironment, apiKey)` for apps that are not using DI. HMAC and other options still use `BrantaApiOptions`. `ConfigureBrantaServices(options)` still registers `IBrantaService`.

### Fixed
- `VerifyAsync` of a `bitcoin:` URI with `branta_id` / `branta_secret` and no payable destination is `Unknown`. Finding the record is not a match.
- `VerifyAsync` of a bolt11, Ark, or silent payment is `Unknown` unless a destination of that type decrypts. A Guardrail hit is not a match by itself.

### Changed
- README is the .NET surface (install, DI, samples). Product rules live in the [developer docs](https://developer.branta.pro).
- A bolt11 / Ark / silent-payment miss no longer retries the raw string.
- A raw on-chain address is not looked up. `VerifyAsync` returns `Unknown` for those destinations, including testnet `tb1` / `bcrt1`.
- `AddPaymentAsync` infers `bitcoin_address` from the value when `Type` is omitted, so `AddDestination(value)` still encrypts.
- POST always sends `zk: true` (Guardrail wire field).

## [[3.2.4](https://github.com/BrantaOps/branta-dotnet/compare/3.2.3...3.2.4)] - 2026-10-29

### Fixed
- `GetPaymentsByQrCodeAsync` returns a payment only when every payable instruction in the QR matches a decrypted destination on that record. A swapped address with a rewritten `branta_secret`, an added `lightning=`, `ark=`, or `sp=` leg, or a conflicting `bc=` / `tb=` no longer comes back as verified. `bolt12`, `lno=`, `pay=`, and LNURL return no payments (ported from `branta-js` [#78](https://github.com/BrantaOps/branta-js/pull/78)).

## [[3.2.3](https://github.com/BrantaOps/branta-dotnet/compare/3.2.2...3.2.3)] - 2026-10-28

### Added
- Recognize signet and regtest Ark addresses (`tark1`) as `ark_address`, alongside mainnet `ark1`. Strict-mode hash-ZK lookup and registration treat them the same way as `ark1`.

## [[3.2.2](https://github.com/BrantaOps/branta-dotnet/compare/3.2.1...3.2.2)] - 2026-09-30

### Fixed
- Fixed a same-origin validation gap: if the first payment in a `GetPaymentsAsync` response had no `PlatformLogoUrl`, validation silently stopped checking every later payment's logo URLs entirely. Logo-URL checks now run independently per payment, and now also cover `PlatformLogoLightUrl`, `ParentPlatform.LogoUrl`/`LogoLightUrl`, and `ChildPlatform.LogoUrl`/`LogoLightUrl` (previously only `PlatformLogoUrl` was checked)

## [[3.2.1](https://github.com/BrantaOps/branta-dotnet/compare/3.2.0...3.2.1)] - 2026-08-29

### Fixed
- `GetPaymentsByQrCodeAsync` now verifies that the plaintext Bitcoin address parsed from a scanned QR code matches the address decrypted via `branta_id`/`branta_secret`, throwing `BrantaPaymentException` with `Reason: BrantaPaymentExceptionReason.Tampered` on mismatch. Closes a gap where an attacker could swap the visible address in a `bitcoin:` URI while leaving a legitimate, verified `branta_id`/`branta_secret` pair untouched (ported from `branta-js` 3.2.1)

## [[3.2.0](https://github.com/BrantaOps/branta-dotnet/compare/3.1.6...3.2.0)] - 2026-07-17

### Added
- Child platform flow support ([#74](https://github.com/BrantaOps/branta-dotnet/pull/74))

## [[3.1.6](https://github.com/BrantaOps/branta-dotnet/compare/3.1.5...3.1.6)] - 2026-06-22

### Added
- Silent payment support ([#67](https://github.com/BrantaOps/branta-dotnet/pull/67))
- Encrypted metadata support ([#69](https://github.com/BrantaOps/branta-dotnet/pull/69))

## [[3.1.5](https://github.com/BrantaOps/branta-dotnet/compare/3.1.4...3.1.5)] - 2026-05-29

### Changed
- Updated release instructions

## [[3.1.4](https://github.com/BrantaOps/branta-dotnet/compare/3.1.3...3.1.4)] - 2026-05-29

### Added
- No-QR-Code Flows section in the integration guide with three options for copy/paste-only flows

## [[3.1.3](https://github.com/BrantaOps/branta-dotnet/compare/3.1.2...3.1.3)] - 2026-05-29

### Added
- Integration guide for developers and AI assistants ([#60](https://github.com/BrantaOps/branta-dotnet/pull/60))

## [[3.1.2](https://github.com/BrantaOps/branta-dotnet/compare/3.1.1...3.1.2)] - 2026-05-29

### Added
- Parent platform model included in payment response ([#58](https://github.com/BrantaOps/branta-dotnet/pull/58))
- Integration tests and expanded test coverage ([#59](https://github.com/BrantaOps/branta-dotnet/pull/59))

## [[3.1.1](https://github.com/BrantaOps/branta-dotnet/compare/3.1.0...3.1.1)] - 2026-05-19

### Fixed
- Allow encrypted bitcoin address lookups in strict mode when a secret is provided ([#54](https://github.com/BrantaOps/branta-dotnet/pull/54))

## [[3.1.0](https://github.com/BrantaOps/branta-dotnet/compare/3.0.4...3.1.0)] - 2026-05-15

### Changed
- **BREAKING:** Return both the list of payments and `verifyUrl`; removed `verifyUrl` from the payment model ([#51](https://github.com/BrantaOps/branta-dotnet/pull/51))

## [[3.0.4](https://github.com/BrantaOps/branta-dotnet/compare/3.0.3...3.0.4)] - 2026-05-15

### Changed
- Do not throw an exception when a payment destination cannot be decrypted; expose an `IsEncrypted` value instead

## [[3.0.3](https://github.com/BrantaOps/branta-dotnet/compare/3.0.2...3.0.3)] - 2026-05-14

### Fixed
- Hashing of uppercase values ([#47](https://github.com/BrantaOps/branta-dotnet/pull/47))

## [[3.0.2](https://github.com/BrantaOps/branta-dotnet/compare/3.0.0...3.0.2)] - 2026-05-06

### Changed
- QR code query params are now URL-encoded by default ([#44](https://github.com/BrantaOps/branta-dotnet/pull/44))

## [[3.0.1](https://github.com/BrantaOps/branta-dotnet/compare/3.0.0...3.0.2)] - 2026-05-06

### Fixed
- Register all required services and move services into the correct folders ([#41](https://github.com/BrantaOps/branta-dotnet/pull/41))

## [[3.0.0](https://github.com/BrantaOps/branta-dotnet/compare/2.1.1...3.0.0)] - 2026-05-02

### Changed
- **BREAKING:** Remove business logic from `BrantaClient` and move it into a dedicated service class ([#38](https://github.com/BrantaOps/branta-dotnet/pull/38))

## [[2.1.1](https://github.com/BrantaOps/branta-dotnet/compare/2.1.0...2.1.1)] - 2026-04-29

### Fixed
- Properly set keys in the verify link when both on-chain and lightning addresses are posted ([#36](https://github.com/BrantaOps/branta-dotnet/pull/36))

## [[2.1.0](https://github.com/BrantaOps/branta-dotnet/compare/2.0.0...2.1.0)] - 2026-04-28

### Added
- bolt11 ZK support using hash ([#34](https://github.com/BrantaOps/branta-dotnet/pull/34))
- QR scan tests ([#34](https://github.com/BrantaOps/branta-dotnet/pull/34))

### Changed
- Each ZK item now gets its own secret; ZK is only allowed for bitcoin addresses ([#34](https://github.com/BrantaOps/branta-dotnet/pull/34))
- Move QR parsing logic into a separate class and tidy tests ([#31](https://github.com/BrantaOps/branta-dotnet/pull/31))

### Removed
- Unused methods ([#31](https://github.com/BrantaOps/branta-dotnet/pull/31))
- URI parsing in QR scan path ([#31](https://github.com/BrantaOps/branta-dotnet/pull/31))

## [[2.0.0](https://github.com/BrantaOps/branta-dotnet/compare/1.0.3...2.0.0)] - 2026-04-20

### Added
- `Privacy` config option ([#26](https://github.com/BrantaOps/branta-dotnet/pull/26))

### Fixed
- Properly serialize JSON ([#28](https://github.com/BrantaOps/branta-dotnet/pull/28))

### Changed
- Clarify confusing privacy wording ([#26](https://github.com/BrantaOps/branta-dotnet/pull/26))

## [[1.0.3](https://github.com/BrantaOps/branta-dotnet/compare/1.0.2...1.0.3)] - 2026-04-14

### Added
- `PlatformLogoLightUrl` field ([#23](https://github.com/BrantaOps/branta-dotnet/pull/23))

## [[1.0.2](https://github.com/BrantaOps/branta-dotnet/compare/1.0.1...1.0.2)] - 2026-04-04

### Added
- `ln` and `ark` destination types ([#20](https://github.com/BrantaOps/branta-dotnet/pull/20))

## [[1.0.1](https://github.com/BrantaOps/branta-dotnet/compare/1.0.0...1.0.1)] - 2026-04-02

### Added
- .NET 10 as a target framework ([#19](https://github.com/BrantaOps/branta-dotnet/pull/19))

## [[1.0.0](https://github.com/BrantaOps/branta-dotnet/compare/0.0.6...1.0.1)] - 2026-04-02

### Added
- Destination type support on payments ([#17](https://github.com/BrantaOps/branta-dotnet/pull/17))
- SDK parity with sibling SDKs ([#16](https://github.com/BrantaOps/branta-dotnet/pull/16))
- CI/CD workflow ([#16](https://github.com/BrantaOps/branta-dotnet/pull/16))
- README documentation of feature support ([#16](https://github.com/BrantaOps/branta-dotnet/pull/16))

## [[0.0.6](https://github.com/BrantaOps/branta-dotnet/compare/0.0.1...0.0.6)] - 2026-01-26

### Added
- GitHub CI ([#14](https://github.com/BrantaOps/branta-dotnet/pull/14))
- API key health-check endpoint ([#11](https://github.com/BrantaOps/branta-dotnet/pull/11))
- BTCPay Server version on payment ([#8](https://github.com/BrantaOps/branta-dotnet/pull/8))
- Optional cancellation tokens ([#6](https://github.com/BrantaOps/branta-dotnet/pull/6))
- Test project and initial tests ([#5](https://github.com/BrantaOps/branta-dotnet/pull/5))
- Server base URL enum ([#3](https://github.com/BrantaOps/branta-dotnet/pull/3))
- Options for `GetZK` ([#3](https://github.com/BrantaOps/branta-dotnet/pull/3))
- Remaining client methods ([#3](https://github.com/BrantaOps/branta-dotnet/pull/3))

### Changed
- Migrate `staging.branta.pro` to `staging.guardrail.branta.pro` ([#13](https://github.com/BrantaOps/branta-dotnet/pull/13))
- Move `Branta.csproj` into its own folder ([#5](https://github.com/BrantaOps/branta-dotnet/pull/5))

### Fixed
- Don't throw an error when the API key is null; let the unauthorized response surface naturally ([#9](https://github.com/BrantaOps/branta-dotnet/pull/9))
- Throw an exception when POST status code is not success ([#8](https://github.com/BrantaOps/branta-dotnet/pull/8))

### Removed
- Unimplemented timeout option ([#6](https://github.com/BrantaOps/branta-dotnet/pull/6))

## [[0.0.1](https://github.com/BrantaOps/branta-dotnet/releases/tag/0.0.1)] - 2025-12-09

### Added
- Initial release
- NuGet packaging
- README and license

