# Changelog

## Unreleased

## 0.2.9 - test-stack alignment

### Added

- Initial build-transitive standards package with StyleCop and Roslyn analyzers, EditorConfig, global analyzer config, and deterministic builds.
- Embedded StyleCop and NetAnalyzers DLLs so analyzers load in `dotnet build` and CI, not only Visual Studio.
- Alphabetical using-directive ordering and `var`-usage enforcement.
- Kept Standards aligned with the fixed ADR-0047 `HoneyDrunk.Standards.Tests` package.
