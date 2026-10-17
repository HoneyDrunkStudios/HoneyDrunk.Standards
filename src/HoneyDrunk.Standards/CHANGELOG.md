# Changelog

All notable changes to HoneyDrunk.Standards will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

The canonical, full history for this package lives at
`HoneyDrunk.Standards/CHANGELOG.md`. The most recent entries are mirrored below.

## [Unreleased]

## [0.3.0] - 2026-09-26

### Changed

- Refresh embedded .NET analyzers to 10.0.401 and preserve their packaged load paths after the upstream directory-layout change.
- Preserve the custom analyzer compiler API baseline for compatibility with earlier .NET 10 SDK hosts.

## [0.2.9] - 2026-05-22

### Fixed

- Fixed HoneyDrunk.Standards.Tests packaging so test-stack dependencies flow to consumer compile and test runtime output.
- Added a package-consumer smoke project for HoneyDrunk.Standards.Tests.

## [0.2.8] - 2026-05-22

### Added

- `HoneyDrunk.Standards.Tests` companion package carrying ADR-0047 test-stack dependencies for Grid test projects.
- Tiered coverlet runsettings templates for Tier 0/1/2 coverage.
- Test-only banned-API enforcement for `Thread.Sleep` via HoneyDrunk analyzer rule `HD0051`.

## [0.2.7] - 2026-04-11

### Fixed

- StyleCop SA* analyzer DLLs now load in `dotnet build` and CI by embedding the analyzer assemblies in the package.
