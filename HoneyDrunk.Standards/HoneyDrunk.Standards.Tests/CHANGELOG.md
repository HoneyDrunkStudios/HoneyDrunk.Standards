# Changelog

All notable changes to HoneyDrunk.Standards.Tests will be documented in this file.

## [0.3.0] - 2026-09-26

### Changed

- Update Microsoft.NET.Test.Sdk 18.5.1 to 18.10.1, xunit.runner.visualstudio 2.8.2 to 4.0.0, NSubstitute 5.3.0 to 6.2.0, and AwesomeAssertions 9.4.0 to 9.6.0.
- Consume HoneyDrunk.Standards 0.3.0. Keep xUnit v2 2.9.3 and coverlet.collector 10.0.1, which are already current stable releases for these package IDs.

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
