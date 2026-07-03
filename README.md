# HoneyDrunk.Standards

[![Validate](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/actions/workflows/validate-pr.yml/badge.svg)](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/actions/workflows/validate-pr.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)

> **Build-transitive standards package** enforcing shared conventions, analyzers, and deterministic builds across HoneyDrunk Studios repositories.

## 📦 What Is This?

HoneyDrunk.Standards is a **zero-configuration** NuGet package that automatically applies:

- ✅ **StyleCop Analyzers** - Consistent naming and ordering conventions
- ✅ **Roslyn Analyzers** - Performance, reliability, and security checks
- ✅ **EditorConfig** - IDE-agnostic formatting rules
- ✅ **Deterministic Builds** - Reproducible builds across machines
- ✅ **Nullable Reference Types** - Enforced null safety
- ✅ **Warnings as Errors** - Quality gate in CI/CD
- ✅ **ADR-0047 Test Stack** - xUnit v2, NSubstitute, AwesomeAssertions, and coverlet for `*.Tests.*` projects
- ✅ **Coverage Templates** - Tiered coverlet runsettings for ADR-0047 / ADR-0036 coverage targets
- ✅ **Test Flake Guardrail** - `Thread.Sleep` is banned in test projects via `HD0051`

All of this happens **automatically** when you add the package. No manual configuration needed.

---

## 🚀 Quick Start

### Installation

Add the package to your project:

```xml
<ItemGroup>
  <PackageReference Include="HoneyDrunk.Standards" Version="0.2.9" PrivateAssets="all" />
</ItemGroup>
```

**Important:** Always include `PrivateAssets="all"` to prevent transitive dependencies.

### Verify It Works

```bash
dotnet build
```

You should see:
- ✅ Analyzer warnings/errors if code violates standards
- ✅ StyleCop rules enforced
- ✅ EditorConfig formatting applied

---

## 📚 Documentation

| Document | Description |
|----------|-------------|
| **[CONVENTIONS.md](HoneyDrunk.Standards/docs/CONVENTIONS.md)** | Coding standards, naming rules, and architectural patterns |
| **[ADOPTION.md](HoneyDrunk.Standards/docs/ADOPTION.md)** | How to adopt, customize, and troubleshoot |
| **[Consumer.Sample](HoneyDrunk.Standards/Consumer.Sample/)** | Working example with compliant and violation code |

---

## 🎯 Features

### 🔍 Analyzers Included

| Analyzer | Purpose | Rules |
|----------|---------|-------|
| **StyleCop.Analyzers** | Naming, ordering, documentation | 100+ SA rules |
| **Microsoft.CodeAnalysis.NetAnalyzers** | Performance, reliability, security | 200+ CA rules |
| **IDE Analyzers** | Code style, modernization | 50+ IDE rules |

### 📝 Enforced Standards

- **File-scoped namespaces** - `namespace MyApp;` (C# 10+)
- **Private field prefixes** - `_fieldName` convention
- **var usage** - Prefer `var` everywhere
- **Minimal braces** - Only required for multi-line blocks
- **Async suffix** - `GetUserAsync` naming
- **XML documentation** - Required for public APIs
- **Nullable annotations** - Explicit `?` for nullability

### ⚙️ Build-Time Features

- **Deterministic builds** - Same source = same binary
- **CI mode auto-detection** - Optimized for GitHub Actions, Azure Pipelines
- **Warnings as errors** - Fail builds on analyzer warnings
- **Nullable reference types** - Catch null bugs at compile time

### 🧪 ADR-0047 Test Defaults

Test projects should reference both packages:

```xml
<ItemGroup>
  <PackageReference Include="HoneyDrunk.Standards" Version="0.2.9" PrivateAssets="all" />
  <PackageReference Include="HoneyDrunk.Standards.Tests" Version="0.2.9" PrivateAssets="all" />
</ItemGroup>
```

`HoneyDrunk.Standards.Tests` carries the NuGet dependencies because package `buildTransitive` props cannot reliably add `PackageReference` items during restore. Keep the reference simple: use `PrivateAssets="all"`, but do not copy the analyzer-package `IncludeAssets` pattern onto this metapackage because consumers need compile/runtime assets from the test-stack dependencies. Projects whose names match the Grid test convention (`*.Tests.Unit`, `*.Tests.Integration`, `*.Tests.Integration.Containers`, `*.Tests.E2E`, `*.Tests.Benchmarks`) receive:

- `IsPackable=false`
- `IsTestProject=true`
- xUnit `2.9.3`
- `xunit.runner.visualstudio` `2.8.2`
- `Microsoft.NET.Test.Sdk` `18.5.1`
- `NSubstitute` `5.3.0`
- `AwesomeAssertions` `9.4.0`
- `coverlet.collector` `10.0.1`

Runtime projects must reference only `HoneyDrunk.Standards`, so they do not receive these test packages. Moq and FluentAssertions are intentionally absent per ADR-0047 D2.

### 📊 Coverage Runsettings Templates

Copy the matching template from the package's `buildTransitive` assets to the Node repo root as `coverlet.runsettings`, then run:

```bash
dotnet test --settings coverlet.runsettings
```

| Node tier | Template | Line | Branch | Behavior |
|-----------|----------|------|--------|----------|
| Tier 0 | `coverlet.tier0.runsettings` | 85% | 80% | Hard gate |
| Tier 1 | `coverlet.tier1.runsettings` | 75% | 70% | Hard gate |
| Tier 2 | `coverlet.tier2.runsettings` | 60% | 55% | Warn-only; CI emits the warning |
| Untiered | none | none | none | No coverage gate |

Tier 2 omits coverlet thresholds because coverlet thresholds fail the test run; the CI job owns the warning posture. All first adoptions have ADR-0047's 30-day advisory grace period before hard gates become blocking. Forward-declared Nodes such as Notify Cloud, Memory, Knowledge, Flow, and Evals use the template for their tier when scaffolded.

### 🚫 Test `Thread.Sleep` Ban

`Thread.Sleep` is forbidden in `*.Tests.*` projects by the HoneyDrunk analyzer rule `HD0051`. The rule is build-breaking for test projects only and covers both `Thread.Sleep(int)` and `Thread.Sleep(TimeSpan)`.

Use one of these instead:

- `await` the async operation
- polling primitives with explicit timeouts
- synchronously-completing fakes

Runtime projects are not flagged by this rule.

---

## 🛠️ Configuration

### Opt-Out of Features

All behavior can be customized via MSBuild properties:

```xml
<PropertyGroup>
  <!-- Disable all analyzers -->
  <HD_EnableAnalyzers>false</HD_EnableAnalyzers>
  
  <!-- Disable StyleCop only (keep Roslyn) -->
  <HD_UseStyleCop>false</HD_UseStyleCop>
  
  <!-- Allow warnings (don't treat as errors) -->
  <HD_TreatWarningsAsErrors>false</HD_TreatWarningsAsErrors>
</PropertyGroup>
```

### Suppress Specific Rules

```xml
<PropertyGroup>
  <!-- Suppress specific analyzer rules -->
  <NoWarn>$(NoWarn);CA1062;SA1633</NoWarn>
</PropertyGroup>
```

### Override Language Features

```xml
<PropertyGroup>
  <!-- Use specific C# version instead of 'latest' -->
  <LangVersion>12.0</LangVersion>
  
  <!-- Disable nullable reference types -->
  <Nullable>disable</Nullable>
</PropertyGroup>
```

---

## 📋 Included Rules

### Enabled by Default

| Category | Example Rules | Severity |
|----------|---------------|----------|
| **Naming** | CA1715 (interface prefix), SA1309 (field prefix) | Warning |
| **Performance** | CA1827 (use Any()), CA1846 (use AsSpan()) | Warning |
| **Reliability** | CA2000 (dispose objects), CA2016 (forward CancellationToken) | Warning |
| **Security** | CA2100 (SQL injection), CA5* (crypto) | Warning |
| **Style** | SA1200 (using order), IDE0161 (file-scoped namespace) | Warning |

### Disabled Rules (With Rationale)

| Rule | Why Disabled | When to Enable |
|------|--------------|----------------|
| **CA1031** | General exception handling needed at boundaries | Enable in business logic |
| **CA1303** | Localization not required for internal tools | Enable for customer-facing apps |
| **CA1707** | Underscores improve test method readability | Keep disabled for tests |
| **CA1848** | LoggerMessage delegates add boilerplate | Opt-in for performance-critical paths |
| **CA2007** | ConfigureAwait not needed in modern .NET | Enable for libraries supporting .NET Framework |
| **SA1503** | Minimize braces (only when multi-line) | Override if you prefer always-braces |

See [CONVENTIONS.md](HoneyDrunk.Standards/docs/CONVENTIONS.md) for full rationale.

---

## 🧪 Testing & Validation

### Sample Project

The [Consumer.Sample](HoneyDrunk.Standards/Consumer.Sample/) project demonstrates:

- ✅ Compliant code examples
- ❌ Commented-out violations you can uncomment to test
- 📖 README with step-by-step testing instructions

### Local Testing

```bash
# Clone the repository
git clone https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards.git
cd HoneyDrunk.Standards/HoneyDrunk.Standards

# Build and pack
dotnet pack -c Release -o ./artifacts

# Test in your project
dotnet add package HoneyDrunk.Standards --source ./artifacts --version 0.2.9
```

---

## 🔄 CI/CD Integration

### GitHub Actions

The package auto-detects CI environments:

```yaml
- name: Build with standards
  run: dotnet build -c Release
  # ContinuousIntegrationBuild=true automatically enabled
  # TreatWarningsAsErrors=true enforced
```

### Explicit CI Mode

Force CI mode locally:

```bash
dotnet build /p:ContinuousIntegrationBuild=true /p:TreatWarningsAsErrors=true
```

---

## 🤝 Contributing

Contributions are welcome! Please:

1. Read [CONVENTIONS.md](HoneyDrunk.Standards/docs/CONVENTIONS.md) for coding standards
2. Open an issue for discussion before major changes
3. Include tests for new rules or features
4. Update documentation

### Development Workflow

```bash
# Restore dependencies
dotnet restore HoneyDrunk.Standards.sln

# Build
dotnet build HoneyDrunk.Standards.sln -c Release

# Pack
dotnet pack HoneyDrunk.Standards/HoneyDrunk.Standards.csproj -c Release -o ./artifacts

# Test in Consumer.Sample
cd Consumer.Sample
dotnet build
```

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).

---

## 🐝 About HoneyDrunk Studios

HoneyDrunk.Standards is part of the **Hive** ecosystem - a collection of tools, libraries, and standards for building high-quality .NET applications.

**Other Projects:**
- 🚧 HoneyDrunk.Build *(coming soon)* - Build tooling and SourceLink automation
- 🚧 HoneyDrunk.GitHooks *(coming soon)* - Git hooks for commit message validation

---

## 📞 Support

- **Questions:** Email us at [contact@honeydrunkstudios.com](mailto:contact@honeydrunkstudios.com)
- **Bugs:** File an [issue](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/issues)
- **Feature Requests:** Open an [issue](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/issues) with the `enhancement` label

---

<div align="center">

**Built with 🍯 by HoneyDrunk Studios**

[Documentation](HoneyDrunk.Standards/docs/) • [Sample Project](HoneyDrunk.Standards/Consumer.Sample/) • [Issues](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/issues) • [Contact](mailto:contact@honeydrunkstudios.com)

</div>
