# Changelog

All notable changes to HoneyDrunk.Standards.Tests will be documented in this file.

## [0.2.8] - 2026-05-22

### Added
- Initial ADR-0047 test-stack package for Grid test projects.
- Declares xUnit v2, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, NSubstitute, AwesomeAssertions, and `coverlet.collector` for consuming test projects.
- Marks consuming projects as test projects and non-packable through build-transitive props.
