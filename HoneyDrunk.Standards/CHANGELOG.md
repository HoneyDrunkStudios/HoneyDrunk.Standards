# Changelog

All notable changes to HoneyDrunk.Standards will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.2.9] - 2026-05-22

### Fixed

- Fixed HoneyDrunk.Standards.Tests packaging so test-stack dependencies flow to consumer compile and test runtime output.
- Added a package-consumer smoke project for HoneyDrunk.Standards.Tests.

---

## [0.2.8] - 2026-05-22

### Added
- `HoneyDrunk.Standards.Tests` companion package carrying ADR-0047 test-stack dependencies for Grid test projects matching `*.Tests.*`: xUnit v2, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, NSubstitute, AwesomeAssertions, and `coverlet.collector`.
- Tiered coverlet runsettings templates for Tier 0 (85% line / 80% branch), Tier 1 (75% / 70%), and Tier 2 (60% / 55% warn-only via CI).
- Test-only banned-API enforcement for `Thread.Sleep(int)` and `Thread.Sleep(TimeSpan)` using HoneyDrunk analyzer rule `HD0051`.

### Changed
- Build-transitive standards now detect Grid test projects and set `IsPackable=false`, `IsTestProject=true`, and `HD_ExcludeFromSolutionVersion=true`.
- Documentation now covers ADR-0047 test package defaults, coverage template adoption, and approved alternatives to `Thread.Sleep` in tests.

## [0.2.7] - 2026-04-11

### Fixed
- StyleCop SA* analyzer DLLs now load in `dotnet build` and CI pipelines by embedding the analyzer assemblies in the package and wiring them through `HoneyDrunk.Standards.targets`

### Breaking
- Consumer repositories will see newly surfaced SA* and CA* diagnostics on CLI builds after upgrading because the analyzers now run outside Visual Studio too

### Removed
- Broken analyzer `PackageReference` injection from `buildTransitive/HoneyDrunk.Standards.props`

## [0.2.6] - 2025-11-22

### Changed
- **Using Directives Ordering**: Changed to purely alphabetical ordering instead of System namespaces first
  - Set `systemUsingDirectivesFirst: false` in `stylecop.json`
  - Disabled SA1208 (System using directives should be placed before others)

## [0.2.5] - 2025-11-20

### Changed
- Enforce using `var` over explicit built-in types (IDE0007 set to error)

## [0.2.0-0.2.4]

### Added
- Initial standards package with build-transitive configuration
- StyleCop.Analyzers integration
- Microsoft.CodeAnalysis.NetAnalyzers integration
- EditorConfig for consistent coding styles
- Global analyzer configuration
- StyleCop JSON configuration
- Support for .NET 10 and C# 14 features

### Configuration
- File-scoped namespaces enforced
- Modern C# pattern matching preferences
- Collection expressions support
- Primary constructors support
- Deterministic builds enabled

[0.2.8]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/compare/v0.2.7...v0.2.8
[0.2.7]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/compare/v0.2.6...v0.2.7
[0.2.6]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/compare/v0.2.5...v0.2.6
[0.2.5]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/releases/tag/v0.2.5
