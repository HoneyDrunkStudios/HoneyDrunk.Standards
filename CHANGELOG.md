# Changelog

## Unreleased

## 0.2.9 - test-stack packaging fix

### Added

- Build-transitive `HoneyDrunk.Standards` package enforcing shared StyleCop and Roslyn analyzers, EditorConfig, deterministic builds, and warnings-as-errors across HoneyDrunk repositories.
- Companion `HoneyDrunk.Standards.Tests` package carrying the ADR-0047 test stack (xUnit v2, NSubstitute, AwesomeAssertions, coverlet) for `*.Tests.*` projects.
- Tiered coverlet runsettings templates and the `HD0051` `Thread.Sleep` test-flake guardrail.
- Fixed `HoneyDrunk.Standards.Tests` packaging so test-stack dependencies flow to consumer compile and test runtime output, plus a package-consumer smoke fixture.
