# Changelog

All notable changes to HoneyDrunk.Standards will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

[0.2.6]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/compare/v0.2.5...v0.2.6
[0.2.5]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/releases/tag/v0.2.5
