# Changelog

All notable changes to HoneyDrunk.Standards will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

### Fixed

- HD0102 checks unused EF dependency declarations as well as operations, with one diagnostic per member.
- HD0104 requires a concrete, non-generic entity configuration class; inherited concrete implementations remain supported.
- HD0111 no longer treats arbitrary documentation URLs as tracked deferred work.

## [0.4.0] - 2026-10-18

### Added

- HoneyDrunk architecture analyzers, warnings that the package's TreatWarningsAsErrors default makes build errors (see CONVENTIONS.md, "Enforced Architecture Rules"):
  - HD0101: EF Core entity types end with `Entity`.
  - HD0102: only types named `*DataService` or `*UnitOfWork` (and EF infrastructure) use a DbContext, DbSet or DbContext factory. Test projects are exempt, and so is a project that sets `HD_IsDataInfrastructure` to true.
  - HD0103: no mapping attributes (`[Key]`, `[Table]`, `[Column]`, `[Required]`, `[MaxLength]`, `[Index]` and the rest) on entities.
  - HD0104: every entity in a DbSet has an `IEntityTypeConfiguration<T>` class.
  - HD0105: no inline `ModelBuilder.Entity` configuration. HD0101 to HD0105 do not apply to test projects.
  - HD0106: a `*Service` class has at most 10 public or internal methods and 300 lines across its partials (configurable with `honeydrunk.service_max_methods` and `honeydrunk.service_max_lines`).
  - HD0110: no commented-out code.
  - HD0111: `TODO`, `FIXME`, `HACK` and `XXX` comments link the issue that tracks them.
- Repository tools (not part of the NuGet package): `tools/check_file_layout.py` checks owned C# file sizes (warn over 400 lines, fail over 800) against a ratcheting baseline, and the folder layout (no code in a layer root, advisory warning over 12 C# files in one folder). `tools/check_new_diagnostics.py` fails chosen analyzer diagnostics (`SA1402`, `SA1649`, `IDE0130` by default) only in new files and on changed lines.
- CONVENTIONS.md: file size and folder layout limits, a game and Unity layout section, and a short "where new code goes" rule for agents.

### Migration

- Projects keep `TreatWarningsAsErrors`, so existing violations fail the build after upgrading. Fix them in the upgrade change; a repository that cannot do that in one change lists the rule in `WarningsNotAsErrors` with a dated reason and gates new violations with `tools/check_new_diagnostics.py`.

## [0.3.0] - 2026-09-26

### Changed

- Refresh embedded Microsoft.CodeAnalysis.NetAnalyzers to 10.0.401 and build-time Microsoft.CodeAnalysis.Analyzers to 5.9.0.
- Release the refreshed companion test stack as 0.3.0, including major runner and mocking-library updates.
- Preserve the Roslyn compiler API reference at 4.14.0 so the custom analyzer remains loadable in earlier .NET 10 SDK hosts. Compiling it against 5.9.0 would require those consumers to upgrade their compiler host.
- Retain the existing StyleCop 1.2.0-beta.556 baseline: the latest stable 1.1.118 would be a downgrade. No new preview dependencies or target-framework changes.

### Internal

- Enabled ADR-0044 Grid Review request workflow and repo-local OpenClaw/Codex review configuration.

---

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

[0.2.9]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/compare/v0.2.8...v0.2.9
[0.2.8]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/compare/v0.2.7...v0.2.8
[0.2.7]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/compare/v0.2.6...v0.2.7
[0.2.6]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/compare/v0.2.5...v0.2.6
[0.2.5]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/releases/tag/v0.2.5
