# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

