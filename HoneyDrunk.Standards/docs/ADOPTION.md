# Adoption Guide

This document explains how to adopt HoneyDrunk.Standards in your project, customize settings, and test changes locally.

## Adding the Package

### For Production Use (NuGet)

Once published to NuGet.org or a private feed:

```xml
<ItemGroup>
  <PackageReference Include="HoneyDrunk.Standards" Version="0.1.0" PrivateAssets="all" />
</ItemGroup>
```

**Important:** Always include `PrivateAssets="all"` to prevent the package from becoming a transitive dependency.

### For Local Development (Project Reference)

While developing or testing standards changes:

```xml
<ItemGroup>
  <ProjectReference Include="..\HoneyDrunk.Standards\HoneyDrunk.Standards.csproj" PrivateAssets="all" />
</ItemGroup>
```

### For Local Testing (Local NuGet Package)

1. Pack the standards project:
   ```bash
   dotnet pack HoneyDrunk.Standards/HoneyDrunk.Standards.csproj -c Release -o ./artifacts
   ```

2. Create or update `nuget.config` in your consumer project:
   ```xml
   <?xml version="1.0" encoding="utf-8"?>
   <configuration>
     <packageSources>
     <clear />
       <add key="local" value="./artifacts" />
       <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
     </packageSources>
   </configuration>
   ```

3. Add the package reference:
   ```xml
   <PackageReference Include="HoneyDrunk.Standards" Version="0.1.0" PrivateAssets="all" />
   ```

4. Restore packages:
   ```bash
   dotnet restore
   ```

## Verifying Application

After adding the package, verify it's working:

```bash
# Should show analyzer warnings/errors if any violations exist
dotnet build

# Check that props are applied
dotnet build -v:detailed | Select-String "Nullable"
# Expected output: Nullable = enable

# Check that analyzers are loaded
dotnet build -v:detailed | Select-String "StyleCop"
# Expected output: Loading analyzer from StyleCop.Analyzers
```

## Understanding Build Modes

### ContinuousIntegrationBuild Behavior

HoneyDrunk.Standards automatically detects CI environments and enables deterministic builds:

**Local Development (Default):**
- `ContinuousIntegrationBuild` = `false`
- Source file paths are relative
- PDB files contain absolute paths for better debugging
- Faster incremental builds

**CI Environment (Auto-detected):**
- `ContinuousIntegrationBuild` = `true`
- Source file paths are embedded deterministically
- Reproducible builds across machines
- Optimized for build servers

**Detected CI Environments:**
- GitHub Actions (`GITHUB_ACTIONS=true`)
- Azure Pipelines (`TF_BUILD=true`)
- Generic CI (`CI=true`)

**Manual Override:**
```xml
<PropertyGroup>
  <!-- Force CI mode locally (for testing) -->
  <ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
</PropertyGroup>
```

**Why This Matters:**
- **Local Dev:** Absolute paths in PDBs allow debuggers to find source files
- **CI/CD:** Deterministic paths ensure identical outputs for the same source
- **NuGet Publishing:** Required for SourceLink to work correctly

## Customizing Settings

All HoneyDrunk.Standards behavior can be customized via MSBuild properties.

### Disabling All Analyzers

```xml
<PropertyGroup>
  <HD_EnableAnalyzers>false</HD_EnableAnalyzers>
</PropertyGroup>
```

**Use Case:** Legacy projects with high warning counts that need gradual adoption.

### Disabling StyleCop Only

```xml
<PropertyGroup>
  <HD_UseStyleCop>false</HD_UseStyleCop>
</PropertyGroup>
```

**Use Case:** Projects that only want Roslyn analyzers, not style enforcement.

### Allowing Warnings (Not Treating as Errors)

```xml
<PropertyGroup>
  <HD_TreatWarningsAsErrors>false</HD_TreatWarningsAsErrors>
</PropertyGroup>
```

**Use Case:** Development/prototyping environments, or projects with technical debt.

### Disabling Deterministic Builds

```xml
<PropertyGroup>
  <Deterministic>false</Deterministic>
</PropertyGroup>
```

**Use Case:** Rare edge cases where build timestamps are required.

**Warning:** Disabling deterministic builds breaks reproducibility and SourceLink integration.

### Overriding Language Version

```xml
<PropertyGroup>
  <LangVersion>11.0</LangVersion> <!-- Override 'latest' -->
</PropertyGroup>
```

**Use Case:** Projects constrained to older C# versions for compatibility.

### Suppressing Specific Analyzer Rules

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);CA1062;SA1101</NoWarn>
</PropertyGroup>
```

**Use Case:** Targeted suppression of specific rules without disabling entire analyzer packages.

**Example Rules to Consider Suppressing:**
- `CA1062` — Validate arguments of public methods (can be noisy with nullable enabled)
- `SA1101` — Prefix local calls with `this` (conflicts with modern C# style)
- `SA1633` — File should have header (if you don't use file headers)

### Combining Customizations

```xml
<PropertyGroup>
  <!-- Enable analyzers but allow warnings -->
  <HD_EnableAnalyzers>true</HD_EnableAnalyzers>
  <HD_TreatWarningsAsErrors>false</HD_TreatWarningsAsErrors>
  
  <!-- Suppress specific noisy rules -->
  <NoWarn>$(NoWarn);CA1062;SA1633</NoWarn>
</PropertyGroup>
```

## Gradual Adoption Strategy

For large existing codebases, we recommend a phased approach:

### Phase 1: Discovery (Warnings Allowed)

```xml
<PropertyGroup>
  <HD_TreatWarningsAsErrors>false</HD_TreatWarningsAsErrors>
</PropertyGroup>
```

**Actions:**
1. Add the package
2. Build and review warnings
3. Document high-frequency violations
4. Prioritize critical rules (security, reliability)

### Phase 2: Triage (Selective Suppression)

```xml
<PropertyGroup>
  <!-- Suppress non-critical rules temporarily -->
  <NoWarn>$(NoWarn);SA1101;SA1309;SA1633</NoWarn>
</PropertyGroup>
```

**Actions:**
1. Fix critical violations
2. Suppress style-only rules causing noise
3. Document suppression rationale in commits
4. Enable `TreatWarningsAsErrors` once critical rules pass

### Phase 3: Remediation (Progressive Rule Re-enablement)

```xml
<PropertyGroup>
  <HD_TreatWarningsAsErrors>true</HD_TreatWarningsAsErrors>
  <!-- Remove suppressions one at a time -->
  <NoWarn>$(NoWarn);SA1633</NoWarn> <!-- Only file headers remaining -->
</PropertyGroup>
```

**Actions:**
1. Pick one suppressed rule
2. Fix all violations
3. Remove from `NoWarn`
4. Repeat until fully compliant

### Phase 4: Full Compliance

```xml
<PropertyGroup>
  <!-- All defaults (no overrides) -->
</PropertyGroup>
```

**Actions:**
1. No suppressions or overrides
2. All analyzer rules enforced as errors
3. Maintain compliance via CI checks

## Troubleshooting

### Analyzers Not Running

**Symptoms:** No analyzer warnings even with violations.

**Solutions:**
1. Verify package is restored:
   ```bash
   dotnet restore --force
   ```

2. Check if analyzers are disabled:
   ```bash
   dotnet build -v:detailed | Select-String "HD_EnableAnalyzers"
   ```

3. Clear build artifacts:
   ```bash
   dotnet clean
   dotnet build
   ```

### Build Fails with SDK Version Error

**Error Message:**
```
error : HoneyDrunk.Standards requires .NET SDK 8.0 or higher. Current: 7.0.x. Please upgrade your SDK.
```

**Solution:**
1. Download and install .NET SDK 8.0+ from https://dot.net
2. Verify installation:
   ```bash
   dotnet --version
   ```

### Different Build Outputs Between Local and CI

**Symptoms:** Assemblies differ between local builds and CI builds.

**Cause:** `ContinuousIntegrationBuild` mode affects path embedding.

**Solution:**
This is **expected behavior**. To test CI-like builds locally:
```bash
dotnet build /p:ContinuousIntegrationBuild=true
```

Or add to your project:
```xml
<PropertyGroup>
  <ContinuousIntegrationBuild>true</ContinuousIntegrationBuild>
</PropertyGroup>
```

**Note:** Local debugging may be affected with CI mode enabled due to different path resolution.

### SourceLink Warning Appears

**Warning Message:**
```
warning : PublishRepositoryUrl is not set. Consider adding HoneyDrunk.Build for automatic SourceLink configuration.
```

**Solution:**
This is informational. Either:
- **Ignore** (if not publishing NuGet packages)
- **Add HoneyDrunk.Build** (when available) for automatic SourceLink setup
- **Suppress** if not relevant:
```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);HD_SourceLink</NoWarn>
</PropertyGroup>
```

### Conflicts with Existing .editorconfig

**Symptoms:** Conflicting formatting or style rules.

**Solution:**
1. HoneyDrunk.Standards' `.editorconfig` has `root = true` — it wins by default
2. To use your own root config, suppress the package's config:
   ```xml
   <ItemGroup>
     <None Remove="**\.editorconfig" />
   </ItemGroup>
   ```

3. Alternatively, merge both configs (place HoneyDrunk settings in a global .editorconfig, project-specific in local files)

### StyleCop Rules Conflict with Roslyn IDE Rules

**Example:** `SA1101` (require `this.`) vs. `IDE0003` (remove `this.`)

**Solution:**
Suppress the conflicting rule:
```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);SA1101</NoWarn>
</PropertyGroup>
```

HoneyDrunk.Standards already disables `SA1101` by default in the ruleset.

## Configuration Precedence

MSBuild properties are evaluated in this order (last wins):

1. **HoneyDrunk.Standards** `Directory.Build.props` (package defaults)
2. **Your project's** `Directory.Build.props` (overrides)
3. **Your `.csproj`** `<PropertyGroup>` (final override)
4. **Command-line** `/p:PropertyName=Value` (highest priority)

**Example:**

```xml
<!-- HoneyDrunk.Standards sets: -->
<HD_TreatWarningsAsErrors>true</HD_TreatWarningsAsErrors>

<!-- Your Directory.Build.props overrides: -->
<HD_TreatWarningsAsErrors>false</HD_TreatWarningsAsErrors>

<!-- Final result: false (your override wins) -->
```

## Best Practices

### ✅ DO
- Use `PrivateAssets="all"` on the package reference
- Test standards changes locally with project references before packing
- Document any suppressions with rationale in comments
- Adopt standards early in new projects (not retroactively)
- File issues for rules that don't make sense in your context
- Let `ContinuousIntegrationBuild` auto-detect (don't override unless needed)

### ❌ DON'T
- Suppress entire analyzer packages unless absolutely necessary
- Override defaults without understanding the rationale
- Disable `TreatWarningsAsErrors` in production CI builds
- Ignore security-related analyzers (CA2100, CA5* rules)
- Copy `.editorconfig` or `stylecop.json` into consuming projects (rely on transitive files)
- Force `ContinuousIntegrationBuild=true` in local dev (breaks debugging)

## Getting Help

- **Questions:** Open a [GitHub Discussion](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/discussions)
- **Bugs:** File an [issue](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/issues)
- **Rule Changes:** Submit a [pull request](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/pulls) with rationale

---

**Next Steps:** Review [CONVENTIONS.md](./CONVENTIONS.md) for HoneyDrunk coding standards and architectural guidelines.
