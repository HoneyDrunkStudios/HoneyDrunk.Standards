# Consumer.Sample

This sample project demonstrates how HoneyDrunk.Standards applies build-transitive analyzer and style rules to consuming projects.

## Purpose

- **Validate** that HoneyDrunk.Standards package works correctly
- **Demonstrate** how analyzers catch common issues
- **Provide examples** of compliant code patterns

## What's Included

### `Class1.cs` - Compliant Examples

#### `StandardsDemo` Class
A fully compliant class demonstrating:
- ? File-scoped namespaces (`namespace Consumer.Sample;`)
- ? XML documentation for all public members
- ? Private fields with underscore prefix (`_name`, `_logger`)
- ? Proper null checks (`ArgumentNullException.ThrowIfNull`)
- ? Async methods with `CancellationToken` parameter
- ? Readonly fields where appropriate
- ? Sealed classes for performance
- ? Nullable reference types properly annotated

#### `IUserRepository` Interface
Shows proper interface naming:
- ? Prefix with `I`
- ? Async method signatures with cancellation tokens
- ? Nullable return types where appropriate

#### `User` Class
Demonstrates immutable entity:
- ? Init-only properties
- ? Constructor validation
- ? Proper exception handling

### Commented-Out `ViolationDemo` Class

Example violations you can uncomment to see analyzer warnings:

| Violation | Rule | Severity |
|-----------|------|----------|
| Public fields | `CA1051` | Warning |
| Non-static methods | `CA1822` | Suggestion |
| Catching general exceptions | `CA1031` | None (disabled) |
| Blocking async code | `CA1849` | Warning |
| Mutable collections | `CA2227` | Warning |
| Missing null checks | `CA1062` | Suggestion |
| Missing dispose | `CA2000` | Warning |

## Testing the Standards

### 1. Build the Project (Clean Build)

```bash
dotnet build Consumer.Sample/Consumer.Sample.csproj
```

**Expected:** Clean build with no warnings or errors.

### 2. View Applied Settings

Check that HoneyDrunk.Standards settings are applied:

```bash
dotnet build Consumer.Sample/Consumer.Sample.csproj -v:detailed | Select-String "Nullable"
```

**Expected Output:**
```
Nullable = enable
```

```bash
dotnet build Consumer.Sample/Consumer.Sample.csproj -v:detailed | Select-String "StyleCop"
```

**Expected Output:**
```
Loading analyzer from StyleCop.Analyzers...
```

### 3. Test Analyzer Detection

1. Open `Class1.cs`
2. Uncomment the `ViolationDemo` class
3. Rebuild:

```bash
dotnet build Consumer.Sample/Consumer.Sample.csproj
```

**Expected:** Build fails with multiple analyzer errors:

```
error CA1051: Do not declare visible instance fields [Consumer.Sample]
error CA1822: Member 'GetConstantValue' does not access instance data and can be marked as static [Consumer.Sample]
error CA1849: Call async methods when in an async method [Consumer.Sample]
error CA2227: Collection properties should be read only [Consumer.Sample]
error CA2000: Dispose objects before losing scope [Consumer.Sample]
```

### 4. Fix Violations

Apply the suggested fixes:

```csharp
// Fix CA1051: Make field private
private string _publicField = string.Empty;

// Fix CA1822: Make method static
public static string GetConstantValue() => "constant";

// Fix CA1849: Use await instead of Wait()
public async Task GoodAsyncPattern()
{
    await Task.Delay(100);
}

// Fix CA2227: Make collection read-only
public IReadOnlyList<string> ReadOnlyCollection { get; } = new List<string>();

// Fix CA2000: Use using statement
public void ProperDispose()
{
    using var stream = new MemoryStream();
    // stream is automatically disposed
}
```

### 5. Verify Clean Build

```bash
dotnet build Consumer.Sample/Consumer.Sample.csproj
```

**Expected:** Clean build again with no warnings.

## Customizing Standards for Testing

To test standard overrides, add properties to `Consumer.Sample.csproj`:

### Allow Warnings (Don't Treat as Errors)

```xml
<PropertyGroup>
  <HD_TreatWarningsAsErrors>false</HD_TreatWarningsAsErrors>
</PropertyGroup>
```

### Disable StyleCop

```xml
<PropertyGroup>
  <HD_UseStyleCop>false</HD_UseStyleCop>
</PropertyGroup>
```

### Disable All Analyzers

```xml
<PropertyGroup>
  <HD_EnableAnalyzers>false</HD_EnableAnalyzers>
</PropertyGroup>
```

### Suppress Specific Rules

```xml
<PropertyGroup>
  <NoWarn>$(NoWarn);CA1051;CA2227</NoWarn>
</PropertyGroup>
```

## EditorConfig in Action

The `.editorconfig` from HoneyDrunk.Standards enforces:

- **Indentation:** 4 spaces for C#
- **Line endings:** LF (Unix-style)
- **File-scoped namespaces:** Enforced with warnings
- **Using directives:** Outside namespace, System first
- **Braces:** Required for all blocks
- **Modifier order:** Specific order enforced (public, private, protected, etc.)

Try violating these rules in your editor and observe the squiggles!

## StyleCop Rules in Action

StyleCop enforces:

- **Ordering:** Elements ordered by access (public ? private), then by type (fields, properties, methods)
- **Naming:** Interfaces start with `I`, private fields with `_`
- **Documentation:** XML docs required for public members
- **Spacing:** Consistent spacing around operators, braces, keywords

## Roslyn Analyzers in Action

Microsoft.CodeAnalysis.NetAnalyzers catch:

- **Performance issues:** Inefficient LINQ, string allocations
- **Reliability problems:** Missing dispose, async deadlocks
- **Security vulnerabilities:** SQL injection, weak crypto
- **API design:** Proper exception types, nullable annotations

## Integration with IDEs

### Visual Studio 2022
- Analyzer warnings appear in Error List
- Quick fixes available via lightbulb (Ctrl+.)
- Code cleanup profiles respect .editorconfig

### VS Code
- Install C# Dev Kit extension
- Analyzer warnings appear inline
- Format on save respects .editorconfig

### JetBrains Rider
- Native .editorconfig support
- Analyzer warnings in Solution-wide Analysis
- Code cleanup follows EditorConfig rules

## Next Steps

1. **Review** [docs/ADOPTION.md](../../docs/ADOPTION.md) for adopting HoneyDrunk.Standards in your projects
2. **Learn** [docs/CONVENTIONS.md](../../docs/CONVENTIONS.md) for HoneyDrunk coding standards
3. **Experiment** with the violation examples to understand each analyzer rule
4. **Apply** these patterns in your own projects

## Troubleshooting

### Analyzers Not Running

**Solution:**
```bash
dotnet restore --force
dotnet clean
dotnet build
```

### Standards Not Applied

Check that the project reference is correct:
```xml
<ProjectReference Include="..\HoneyDrunk.Standards\HoneyDrunk.Standards.csproj" PrivateAssets="all" />
```

Verify path is relative to the `.csproj` file location.

### Too Many Warnings

For gradual adoption:
```xml
<PropertyGroup>
  <HD_TreatWarningsAsErrors>false</HD_TreatWarningsAsErrors>
</PropertyGroup>
```

Then fix warnings incrementally before re-enabling errors.

---

**Questions?** See the main [README](../../docs/README.md) or open an issue.
