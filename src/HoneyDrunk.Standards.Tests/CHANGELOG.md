# Changelog

All notable changes to HoneyDrunk.Standards.Tests will be documented in this file.

## [0.4.4] - 2026-11-05

### Changed

- Refresh the existing StyleCop prerelease dependency from 1.2.0-beta.556 to 1.3.171-beta, including upstream reliability and collection-expression fixes. Preserve the server Roslyn 4.14 and separate Unity Roslyn 4.3 profiles and existing diagnostic severities.
- Embed StyleCop analyzer/code-fix DLLs from the direct package's generated path property. The newer package ships the DLLs directly instead of using the old Unstable dependency, so the previous cache-path assumption would package stale or missing binaries.
- Keep the companion test-stack version aligned at 0.4.4. No runtime library API or Unity profile payload is changed.

## [0.4.3] - 2026-11-05

### Fixed

- HD0106 counts authored partial declarations and methods using Roslyn generated-code classification. Generated logging partials no longer inflate service size; oversized authored partials still produce diagnostics.
- The test-stack package consumes the corrected Standards package; test-tool versions remain unchanged from 0.4.2.

## [0.4.2] - 2026-11-05

### Changed

- Consume Standards 0.4.2 and update Microsoft.NET.Test.Sdk to 18.11.0, xunit.runner.visualstudio to 4.0.1, and coverlet.collector to 10.1.0. Retain xUnit v2 2.9.3, NSubstitute 6.2.0, and AwesomeAssertions 9.6.0.

## [0.4.1] - 2026-11-01

### Changed

- Consume HoneyDrunk.Standards 0.4.1 for the HD0102, HD0104 and HD0111 analyzer fixes.

## [0.4.0] - 2026-10-18

### Changed

- Consume HoneyDrunk.Standards 0.4.0, which adds the architecture analyzers. Test projects are exempt from the EF rules (HD0101 to HD0105); the service-size and comment rules apply. The test stack is unchanged.

## [0.3.0] - 2026-09-26

### Changed

- Update Microsoft.NET.Test.Sdk 18.5.1 to 18.10.1, xunit.runner.visualstudio 2.8.2 to 4.0.0, NSubstitute 5.3.0 to 6.2.0, and AwesomeAssertions 9.4.0 to 9.6.0.
- Consume HoneyDrunk.Standards 0.3.0. Keep xUnit v2 2.9.3 and coverlet.collector 10.0.1, which are already current stable releases for these package IDs.

### Migration

- NSubstitute 6 removes obsolete APIs and adds public nullable annotations on .NET 8+. Consumers using removed APIs must update those calls; nullable annotations may introduce diagnostics in existing tests. See the [upstream breaking changes](https://github.com/nsubstitute/NSubstitute/blob/v6.0.0/BreakingChanges.md).

## [0.2.9] - 2026-05-22

### Fixed

- Removed package DevelopmentDependency metadata so xUnit, AwesomeAssertions, NSubstitute, and test SDK assets flow correctly to test-stack consumers.
- Added a consumer smoke test proving the package compiles and runs with only HoneyDrunk.Standards.Tests referenced.

---

## [0.2.8] - 2026-05-22

### Added
- Initial ADR-0047 test-stack package for Grid test projects.
- Declares xUnit v2, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, NSubstitute, AwesomeAssertions, and `coverlet.collector` for consuming test projects.
- Marks consuming projects as test projects and non-packable through build-transitive props.
