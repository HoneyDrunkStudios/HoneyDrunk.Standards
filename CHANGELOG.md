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

## [0.4.1] - 2026-11-01

- HoneyDrunk.Standards fixes missed EF dependency declarations, non-concrete entity configurations and untracked TODOs. HoneyDrunk.Standards.Tests consumes the corrected package.
- The separately pinned repository tools reject symlinked C# files and SARIF tool metadata that does not match its declared version.

## [0.4.0] - 2026-10-18

- HoneyDrunk.Standards 0.4.0 adds the architecture and comment analyzers (HD0101 to HD0106, HD0110, HD0111); HoneyDrunk.Standards.Tests 0.4.0 consumes it. See the package changelogs for details.
