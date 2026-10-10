# HoneyDrunk Conventions

This document owns shared engineering conventions. Apply language-specific rules only to that stack. A repository's verified boundaries, public contracts, existing tests and specific engineering documents refine these defaults; they do not authorize unrelated rewrites. The package enforces only its configured analyzer/build rules, not every architectural guideline in this document.

## Table of Contents
- [Naming Conventions](#naming-conventions)
- [Folder Structure](#folder-structure)
- [Namespace Policy](#namespace-policy)
- [Nullable Reference Types](#nullable-reference-types)
- [Warning Philosophy](#warning-philosophy)
- [Analyzer Rationale](#analyzer-rationale)
- [Commit Messages](#commit-messages)
- [Architectural Patterns](#architectural-patterns)
- [Agent workflow and instruction files](#agent-workflow-and-instruction-files)

---

## Naming Conventions

### General Rules
- **PascalCase** for types, methods, properties, events, constants
- **camelCase** for parameters and local variables; **_camelCase** for private fields
- **SCREAMING_CASE** — *Never use* (use PascalCase for constants)

### Specific Patterns

#### Interfaces
```csharp
public interface IUserRepository { } // ? Prefix with 'I'
public interface UserRepository { }  // ? Missing 'I'
```

#### Type Parameters
```csharp
public class List<T> { }         // ? Single letter for simple generics
public class Dictionary<TKey, TValue> { } // ? Descriptive for complex generics
```

#### Private Fields
```csharp
private readonly ILogger _logger;    // ? Underscore prefix for private fields
private readonly ILogger logger;  // ? Missing underscore
```

**Rationale:** While `SA1309` (no underscore prefix) exists, we suppress it in favor of C# community convention.

#### Boolean Properties/Methods
```csharp
public bool IsActive { get; set; }   // ? Prefix with 'Is', 'Has', 'Can'
public bool CanExecute() => true;  // ?
public bool Active { get; set; }     // ? Unclear
```

#### Async Methods
```csharp
public async Task<User> GetUserAsync(int id) { } // ? Suffix with 'Async'
public async Task<User> GetUser(int id) { }      // ? Missing 'Async'
```

#### Test Methods
```csharp
[Fact]
public void GetUser_WithValidId_ReturnsUser() { } // ? Pattern: MethodName_Condition_ExpectedResult
[Fact]
public void TestGetUser() { }      // ? Not descriptive
```

**Rationale:** This naming suppresses `CA1707` (no underscores) for test methods since readability is critical.

---

## Folder Structure

### Repository and feature layout

Keep the repository's actual solution/project layout. Many .NET repositories keep peer projects beside a solution under a repository-named directory; others keep the solution at the root. Do not create or remove `src/`, `tests/`, Domain or Application projects merely to match a universal template.

Within each layer, organize by the domain/responsibility being changed. For an application that already uses API, Contracts, Services, Data and Database projects, mirror relevant feature folders such as Accounts or Orders. Keep pure domain rules in an existing Domain project where that boundary is useful. Libraries may use Abstractions, runtime and provider packages; they do not need application layers.

Keep static validation and pure mapping in named files under the feature's `Validators` and `Mapping` folders. Services and data services have focused interfaces; pure static validators and mappers do not need artificial interfaces or injected wrapper services. Put tests beside or under the existing test layout and mirror the responsibilities they verify. Use `docs/` and `scripts/` for supporting material when those directories already fit the repository.

### File Naming
- **One type per file** (enforced by `SA1402`)
- **File name matches type name** (enforced by `SA1649`)
- **Use PascalCase** for filenames: `UserRepository.cs`, not `userRepository.cs`

---

## Namespace Policy

### Rules
1. **Namespaces match folder structure** (enforced by `IDE0130`)
2. **Use file-scoped namespaces** (enforced by `IDE0161`)
3. **No nested namespaces in a single file** (enforced by `SA1403`)

### Examples

#### ? Correct
```csharp
// File: ProjectName.Domain/Accounts/User.cs
namespace ProjectName.Domain.Accounts;

public class User { }
```

#### ? Incorrect
```csharp
// File: ProjectName.Domain/Accounts/User.cs
namespace ProjectName.Domain // ? Doesn't match folder
{
    public class User { }// ? Block-scoped namespace
}
```

### Global Usings
- **System namespaces** in `GlobalUsings.cs` (implicit via `ImplicitUsings=enable`)
- **Project-specific common usings** in `GlobalUsings.cs` at project root

```csharp
// GlobalUsings.cs
global using ProjectName.Domain.Accounts;
```

---

## Nullable Reference Types

### Policy
- **Enabled by default** (`Nullable=enable` in Directory.Build.props)
- **No exceptions** — All projects must be nullable-aware

### Handling Nullability

#### ? Correct Patterns
```csharp
public string GetName() => _name; // Non-nullable return

public string? GetOptionalName() => _optionalName; // Nullable return

public void SetName(string name) // Non-nullable parameter
{
    ArgumentNullException.ThrowIfNull(name);
    _name = name;
}

public void SetOptionalName(string? name) // Nullable parameter
{
    _optionalName = name;
}
```

#### ? Incorrect Patterns
```csharp
public string GetName() => _optionalName!; // ? Null-forgiving operator without justification

public void SetName(string name) // ? Missing null check
{
  _name = name; // Potential NullReferenceException
}
```

### Suppressing Warnings
Only use `!` (null-forgiving operator) when:
1. You have **proof** the value is non-null (e.g., framework guarantees)
2. You add a comment explaining why

```csharp
// Authentication does not guarantee this claim exists. Check the boundary explicitly.
var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
if (string.IsNullOrWhiteSpace(userId))
    throw new UnauthorizedAccessException("The required subject claim is missing.");
```

---

## Warning Philosophy

### Core Principles
1. **Warnings are errors** in CI builds (`TreatWarningsAsErrors=true`)
2. **Suppressions require justification** (enforced by `SA1404`)
3. **No blanket suppressions** — Suppress specific rules with rationale

### Acceptable Suppressions

#### Localization (Internal Tools)
```xml
<NoWarn>$(NoWarn);CA1303</NoWarn> <!-- Localization not required for internal tooling -->
```

#### General Exception Handling (Application Boundaries)
```xml
<NoWarn>$(NoWarn);CA1031</NoWarn> <!-- Catching general exceptions at API/UI boundaries is acceptable -->
```

#### Test Method Naming
```xml
<NoWarn>$(NoWarn);CA1707</NoWarn> <!-- Underscores allowed in test method names -->
```

### Unacceptable Suppressions

#### Security Rules
```xml
<!-- ? NEVER suppress security analyzers -->
<NoWarn>$(NoWarn);CA2100</NoWarn> <!-- SQL injection checks -->
<NoWarn>$(NoWarn);CA5*</NoWarn>   <!-- Cryptography/security rules -->
```

#### Nullable Warnings
```xml
<!-- ? Avoid suppressing nullable warnings globally -->
<Nullable>disable</Nullable>
<NoWarn>$(NoWarn);CS8600;CS8601;CS8602;CS8603;CS8604</NoWarn>
```

**Rationale:** Nullable warnings prevent `NullReferenceException` at runtime. Fix the code, don't suppress.

---

## Analyzer Rationale

### Why StyleCop?
- Enforces consistent ordering (usings, members, modifiers)
- Catches naming violations early
- Reduces PR review friction on style issues

### Why Microsoft.CodeAnalysis.NetAnalyzers?
- **Performance:** Catches inefficient LINQ, string operations, allocations
- **Reliability:** Prevents dispose issues, async pitfalls, threading bugs
- **Security:** Detects SQL injection, weak crypto, validation gaps

### Disabled Rules Justification

| Rule | Reason Disabled | Alternative |
|------|----------------|-------------|
| `CA1031` | General exception handling needed at boundaries | Catch specific exceptions in business logic |
| `CA1303` | Localization not required for internal tools | Enable for customer-facing applications |
| `CA1707` | Underscores improve test readability | Use `MethodName_Condition_Result` pattern |
| `CA1848` | LoggerMessage delegates add boilerplate | Opt-in for performance-critical paths |
| `CA2007` | `ConfigureAwait(false)` not required in modern .NET | Use in libraries if supporting .NET Framework |
| `SA1101` | `this.` prefix conflicts with modern C# style | Rely on IDE hints for clarity |
| `SA1309` | Underscore prefix is C# community convention | Use `_fieldName` for private fields |
| `SA1503` | Minimalist approach - braces only when multiline | Use braces for multi-line blocks, omit for single-line |
| `SA1633` | File headers add noise | Use where legally required |

---

## Commit Messages

### Format (Conventional Commits)

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Types
- **feat:** New feature
- **fix:** Bug fix
- **docs:** Documentation changes
- **style:** Code style changes (formatting, no logic change)
- **refactor:** Code refactoring (no feature or fix)
- **perf:** Performance improvement
- **test:** Adding or updating tests
- **chore:** Maintenance tasks (deps, build, config)
- **ci:** CI/CD changes

### Examples

```
feat(analyzers): add CA2016 CancellationToken forwarding rule

Enforce passing CancellationToken to async methods to prevent
orphaned operations.

Closes #42
```

```
fix(props): correct StyleCop path reference

The path to stylecop.json was incorrect in Directory.Build.props,
causing consumers to not apply StyleCop rules.

BREAKING CHANGE: Consumers must restore packages to pick up corrected path.
```

### Rules
- **Lowercase** `<type>` and `<scope>`
- **Imperative mood** in `<subject>` ("add", not "added" or "adds")
- **Max 72 characters** for `<subject>`
- **Reference existing issues when relevant**; use closing keywords only when closure is intended. No issue or packet is required for scoped work
- **Breaking changes** noted in `<footer>` with `BREAKING CHANGE:`

### Enforcement
Inspect the repository's current hooks, PR template and CI metadata checks. Do not infer enforcement or invent a hook package from this document.

---

## Architectural Patterns

### Simple layers and explicit ownership

Use ordinary C# with clear responsibilities. Endpoints own HTTP/authentication policy, transport adaptation and typed responses; delegate business decisions to named services. Services own use-case orchestration and business rules, calling pure domain rules where an existing Domain layer is appropriate. Data services own persistence queries, locking and transaction mechanics, not product outcomes. Contracts do not expose EF entities. Keep shared mechanics in real existing nodes and product policy in applications.

Use focused interfaces for services and data services. Scan current types, sibling features and existing shared nodes before adding helpers. Reuse demonstrated behavior; do not add speculative frameworks, service locators, event buses, CQRS, sagas, rich aggregates or another forwarding layer just to satisfy an architecture template. Existing domain models and provider patterns remain valid where they serve current behavior. This convention is not permission to change public contracts or migrate working code incidentally.

### Validation and mapping

Static validators are deterministic and free of I/O. Validate untrusted inputs and runtime null/error cases before mapping; nullable annotations do not validate external data. Name repeated limits/messages and preserve exception/status contracts. Service-level authorization, ownership and business decisions stay explicit.

Pure mapping lives in its own named file and translates already-selected data. It must not perform I/O, generate random IDs, read a host clock, choose business outcomes or hide retry/transaction policy. Services supply selected IDs, trusted ownership and operation times. Use the repository's established identity and history semantics rather than replacing them with a generic stamping mechanism.

### EF and SQL

For ordinary updates, load tracked entities, change intended properties and let EF detect changes. Do not add custom mutation bags, generic change bindings or `Update` calls on already tracked entities. Respect existing transaction ownership, immutable history, idempotency, rowversion and retry boundaries. EF tracking cannot decide which business facts need a new historical version.

Use only Fluent mappings that express a real contract or differ from conventions. Preserve keys, names, ownership relationships, delete behavior, lengths, precision, collation, indexes/filters, concurrency tokens, converters/comparers and immutability protections when required. Do not remove a mapping just because it looks redundant; verify the effective model and deployed schema.

Every application-owned SQL table and column needs accurate metadata describing meaning and relevant ownership/time semantics. Indexes need a demonstrated query or constraint. Keep schema, EF mappings and schema tests aligned. Where a SQL project/DACPAC owns schema, do not introduce EF migrations as a second owner. Preserve separately owned shared Audit and Outbox schema.

### Asynchrony and integration

Use asynchronous I/O and propagate cancellation. Avoid `.Result`, `.Wait()` and `async void` except framework event handlers. Use injected `TimeProvider` at host-facing boundaries when time affects behavior; capture operation time once and pass it into pure rules. Keep provider SDKs inside their owning adapters. Verify real shared-node contracts and implementation rather than inferring availability from a name or catalog.

### Tests and documentation

Use the repository's current tests and lockfiles. The shared .NET test package supplies xUnit, NSubstitute and AwesomeAssertions; do not introduce Moq or FluentAssertions from old examples. Preserve repository-specific naming and supported framework constraints. Test changed behavior, failure paths and meaningful contracts, without mirroring implementation or fabricating coverage thresholds. Existing required CI, coverage and security gates remain in force.

Use the pinned SDK and language settings; do not hardcode a remembered "latest" C# version into instructions. Follow `.editorconfig`, nullable annotations, public API XML documentation and the actual analyzer configuration. Comments explain non-obvious intent, constraints or trade-offs; do not add comments to every function or repeat obvious code.

Repository Markdown uses a descriptive H1 and hierarchical headings, fenced examples and valid links. Frontmatter is required only by a real consuming schema (for example a publishing pipeline), not for every README. Do not invent blog fields, a categories file or validator enforcement. Check paths and anchors when moving documents.

## Agent workflow and instruction files

Use one root `AGENTS.md` per repository as the shared entrypoint. Put scope, important boundaries, links to maintained standards and actual verification commands there. Keep deeper engineering detail in ordinary docs and current product specifications/ADRs/progress in their existing owners. Do not create a duplicate universal `SPEC.md`, `RULES.md` or roadmap. Such names are ordinary documents unless a specific tool is explicitly configured to read them.

HoneyDrunk deliberately does not keep separate `CLAUDE.md`, `.github/copilot-instructions.md`, `.github/instructions/*.instructions.md` or nested agent entrypoints. Tool compatibility is a trade-off, not proof that all clients load the root automatically:

- [Codex](https://developers.openai.com/codex/guides/agents-md/) discovers `AGENTS.md` with directory-based precedence; overrides and custom fallbacks require intentional configuration.
- [Claude Code](https://code.claude.com/docs/en/memory) supports AGENTS.md in current versions, but ancestor CLAUDE files and the user's project-instruction setting affect discovery. Check the actual session's loaded files. Do not silently recreate an adapter.
- [Copilot support varies by surface](https://docs.github.com/en/copilot/reference/custom-instructions-support). Some surfaces consume AGENTS.md; others rely on tool-specific files. In those clients, explicitly supply the root instructions. Removing the old adapters does not uninstall Copilot.
- [Greptile](https://www.greptile.com/docs/code-review/greptile-config) can use scoped `.greptile/rules.md` and `files.json`; an arbitrary root RULES.md has no equivalent discovery guarantee. Dashboard rules are additive. Keep reviewer config only where it is needed, and verify actual review coverage separately from saved configuration.

For authorized publication, preserve unrelated changes, review the intended diff explicitly, resolve actionable findings, and stage only intended files. Use Conventional Commits and ready-for-review PRs unless the user explicitly requests drafts. Follow the current PR template, including exactly one accurate `Authorship:` line and a `Request:` line; use the same authorship class in the commit trailer. Valid classes are `human`, `agent-codex`, `agent-claude-code`, `agent-copilot` and `mixed`.

Report the tested/reviewed revision, meaningful checks and limitations. For documentation-only changes, validate content, links, commands and instruction consistency; do not claim runtime tests ran. For applicable Sonar analysis, inspect actual latest-head new-code findings, not just a green upload job or gate. Fix new issues without suppressing findings or weakening checks; unavailable/stale analysis stays unverified.

Legacy Grid Review is retired. Do not restore its workers, schedules, queues, merge requirements or bypass labels. Explicit authoring/manual review, tests, security/Sonar and existing required checks remain. A saved replacement-reviewer rule is not evidence of a completed review or required merge check. Publishing instructions do not grant merge, deployment, paid review, credential or protection-change authorization.
