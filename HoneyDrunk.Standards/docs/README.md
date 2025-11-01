# HoneyDrunk.Standards

Build-transitive standards package that applies analyzers, style conventions, and deterministic build defaults across all HoneyDrunk repositories.

## Purpose

This package serves as the **source of truth** for:
- C# coding style and conventions
- Roslyn and StyleCop analyzer configurations
- Deterministic build settings
- Nullable reference type enforcement

When referenced by any HoneyDrunk project, it automatically applies these standards without requiring manual configuration.

## Features

? **Build-Transitive** — Settings flow automatically to consuming projects  
? **Deterministic Builds** — Reproducible outputs regardless of environment  
? **Modern C#** — Enforces C# 12+ features (file-scoped namespaces, primary constructors)  
? **Nullable-Aware** — Nullable reference types enabled by default  
? **Comprehensive Analyzers** — 200+ Roslyn and StyleCop rules with rationale  
? **Opt-Out Toggles** — Flexible HD_* properties for legacy projects

## Installation

Add the package reference to your `.csproj`:

```xml
<PackageReference Include="HoneyDrunk.Standards" Version="0.1.0" PrivateAssets="all" />
```

**Note:** The `PrivateAssets="all"` attribute ensures the package is not exposed as a transitive dependency.

## What Gets Applied

### Build Properties
- `Nullable=enable` — Nullable reference types
- `LangVersion=latest` — Latest C# language features
- `Deterministic=true` — Reproducible builds
- `ContinuousIntegrationBuild=true` — CI-optimized builds
- `TreatWarningsAsErrors=true` — Enforce code quality

### Analyzers
- **StyleCop.Analyzers** (v1.2.0-beta.556) — Naming, ordering, documentation
- **Microsoft.CodeAnalysis.NetAnalyzers** (v9.0.0) — Performance, security, reliability

### Configuration Files
- `.editorconfig` — Editor settings (spacing, formatting, language conventions)
- `stylecop.json` — StyleCop behavior (docs requirements, ordering rules)
- `HoneyDrunk.Style.ruleset` — Granular analyzer severity levels

## Customization

See [ADOPTION.md](./ADOPTION.md) for detailed guidance on:
- Overriding default settings
- Disabling specific analyzers
- Local testing with project references
- Suppressing warnings for legacy code

See [CONVENTIONS.md](./CONVENTIONS.md) for:
- Naming conventions
- Folder structure standards
- Commit message guidelines
- Architectural patterns

## Requirements

- **.NET SDK 8.0+** — Earlier versions will fail with a clear error message
- **C# 12+** — Features like file-scoped namespaces are enforced

## Non-Goals (Current Phase)

This package intentionally **does not** include:
- ? NuGet publishing workflows
- ? SourceLink provider configuration (will be in `HoneyDrunk.Build`)
- ? Reusable CI/CD workflows
- ? Package versioning automation

These will be addressed in future packages or via separate GitHub Actions workflows.

## Repository Structure

```
HoneyDrunk.Standards/
??? buildTransitive/    # MSBuild props/targets
?   ??? Directory.Build.props # Default properties
?   ??? Directory.Build.targets # Validation guardrails
??? rulesets/       # Analyzer rulesets
?   ??? HoneyDrunk.Style.ruleset
??? .editorconfig   # Editor configuration
??? stylecop.json         # StyleCop settings
```

## Contributing

See [CONTRIBUTING.md](../CONTRIBUTING.md) for guidelines on:
- Proposing rule changes
- Testing analyzer updates
- Documenting breaking changes
- Semantic versioning policy

## License

Copyright © HoneyDrunk Studios  
Licensed under the MIT License. See [LICENSE](../LICENSE) for details.

## Related Packages

- **HoneyDrunk.Build** *(Coming Soon)* — SourceLink, packaging, versioning
- **HoneyDrunk.Testing** *(Coming Soon)* — xUnit conventions, test utilities

---

**Questions?** Open an issue at [HoneyDrunkStudios/HoneyDrunk.Standards](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/issues)
