# Changelog

All notable changes to the HoneyDrunk.Standards repository are documented in the
package-level changelogs. This root file points to the canonical histories so it
cannot drift from them.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Canonical changelogs

- `HoneyDrunk.Standards/CHANGELOG.md` — the `HoneyDrunk.Standards` standards
  package (build-transitive analyzers, EditorConfig, deterministic builds).
- `src/HoneyDrunk.Standards.Tests/CHANGELOG.md` — the
  `HoneyDrunk.Standards.Tests` companion test-stack package.

## [Unreleased]

- See the canonical changelogs above for in-flight changes.

## [0.4.2] - 2026-11-05

### Changed

- Update embedded Microsoft.CodeAnalysis.NetAnalyzers from 10.0.401 to 10.0.403. Refresh the companion test SDK, runner and coverage collector.
- Preserve the server Roslyn 4.14 and separate Unity Roslyn 4.3 compiler API baselines. Retain the newer StyleCop 1.2 beta instead of downgrading to its older stable release.

## [0.4.1] - 2026-11-01

- HoneyDrunk.Standards fixes missed EF dependency declarations, non-concrete entity configurations and untracked TODOs. HoneyDrunk.Standards.Tests consumes the corrected package.
- The separately pinned repository tools reject symlinked C# files and SARIF tool metadata that does not match its declared version.

## [0.4.0] - 2026-10-18

- HoneyDrunk.Standards 0.4.0 adds the architecture and comment analyzers (HD0101 to HD0106, HD0110, HD0111); HoneyDrunk.Standards.Tests 0.4.0 consumes it. See the package changelogs for details.
