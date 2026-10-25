# HoneyDrunk Conventions

This document owns shared engineering conventions. Apply language-specific rules only to that stack. A repository's verified boundaries, public contracts, existing tests and specific engineering documents refine these defaults; they do not authorize unrelated rewrites. The package enforces only its configured analyzer/build rules, not every architectural guideline in this document.

## Table of Contents
- [Naming Conventions](#naming-conventions)
- [Folder Structure](#folder-structure)
- [File Size and Folder Layout](#file-size-and-folder-layout)
- [Game and Unity Layout](#game-and-unity-layout)
- [Where New Code Goes](#where-new-code-goes)
- [Namespace Policy](#namespace-policy)
- [Nullable Reference Types](#nullable-reference-types)
- [Warning Philosophy](#warning-philosophy)
- [Analyzer Rationale](#analyzer-rationale)
- [Commit Messages](#commit-messages)
- [Architectural Patterns](#architectural-patterns)
- [Enforced Architecture Rules](#enforced-architecture-rules)
- [Code review](#code-review)
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

For .NET repositories, keep the solution and shared build configuration at the repository root, production projects under `src/`, test and canary projects under `tests/`, and examples under `samples/`. Published packages belong in `src/` even when their names contain `Tests` (for example, `HoneyDrunk.Standards.Tests`). Keep existing project names, namespaces, public contracts and dependency directions when relocating files. Update solution/project references, packaging assets, CI, Docker contexts and local tooling together.

Use ecosystem layouts for other repository types: Unity retains `Assets/`, `Packages/` and `ProjectSettings/`; app/package workspaces may retain `apps/` and `packages/` with their own source folders. Documentation, infrastructure and scaffolding repositories do not need an empty `src/`. Do not create Domain or Application layers merely to match a folder template.

Within each layer, organize by the domain/responsibility being changed. For an application that already uses API, Contracts, Services, Data and Database projects, mirror relevant feature folders such as Accounts or Orders. Keep pure domain rules in an existing Domain project where that boundary is useful. Libraries may use Abstractions, runtime and provider packages; they do not need application layers.

Keep static validation and pure mapping in named files under the feature's `Validators` and `Mapping` folders. Services and data services have focused interfaces; pure static validators and mappers do not need artificial interfaces or injected wrapper services. Put tests beside or under the existing test layout and mirror the responsibilities they verify. Use `docs/` and `scripts/` for supporting material when those directories already fit the repository.

### File Naming
- **One type per file** (reported by `SA1402`)
- **File name matches type name** (reported by `SA1649`; a partial class part adds its system, as in `ShopSimulation.Board.cs`)
- **Use PascalCase** for filenames: `UserRepository.cs`, not `userRepository.cs`

Where a repository has existing warnings for these rules, it blocks new ones with the [new-code analyzer gate](#analyzer-rules-on-new-code) instead of waiting for the backlog to be paid down.

---

## File Size and Folder Layout

Large files and flat folders grow because adding to an existing file is the easiest change to make. These limits make the easy change the right one: new code goes in a new file, in the folder of the system it belongs to.

### Limits

| Measure | Advisory (warns) | Fails |
|---|---|---|
| Lines in one owned C# file | over 400 | over 800, unless baselined |
| C# files directly in one folder | over 12 | never |
| C# files directly in a layer root | | any file not in the baseline |

**Owned** means handwritten C# under the source folders the repository's config lists. Lines are physical lines, comments and blank lines included. The check skips build output (`bin`, `obj`), Unity caches (`Library`, `Temp`), vendor code (`Packages`, `Plugins`, `ThirdParty`), generated code (a `Generated` folder, `*.g.cs`, `*.Designer.cs`, `*.generated.cs`, or an `<auto-generated>` header in the first ten lines) and any directory names the config adds. Do not put handwritten code in those paths to get around the check.

A **layer root** is the top folder of an assembly or layer that the config names, for example a Unity game's `Core` and `Runtime` folders. It holds the assembly definition, its ruleset and folders, and no code of its own.

The folder warning means the folder holds several systems side by side. Give each system its own subfolder. The warning stays advisory because moving existing files is separate work; new files for a system go in its subfolder from the start.

### The baseline

A repository adopting the limits commits a baseline that lists every owned file over the fail limit, with its line count when the baseline was made, and any existing files in a layer root.

- A baselined file may shrink but never grow. To add behavior to the system it holds, put the new code in a new file.
- When a baselined file shrinks, lock the smaller size by running `--update-baseline` in the same change. When it drops to the fail limit or below, the same command removes it.
- The baseline only ever loses entries or lowers counts. On a pull request the check compares the baseline with the one the base branch's config names (moving the baseline or the config with `git mv` does not skip the comparison, and switching to a config at another path fails) and fails if an entry was added or a count raised. Split the file instead. Only the change that first adds a layout config, when the base has none anywhere, is not compared.
- The config only tightens too. Against the base, raising `warnLines`, `failLines` or `maxFilesPerFolder`, removing a source or layer root, or excluding another directory fails. Loosening the rules is an owner decision, merged deliberately past the check.
- To move a baselined file into its system's folder, change its path in the baseline in the same change. The comparison accepts the new path when git detects the move as a rename.
- `--update-baseline` never adds or raises an entry. `--init-baseline` creates the first baseline once and refuses to run when one exists.

### Running the check

The check is [`tools/check_file_layout.py`](../../tools/check_file_layout.py) (Python 3.9 or later, standard library only). A consuming repository pins a reviewed Standards commit, fetches that commit and runs the script with its own config:

```json
{
    "tool": "honeydrunk-file-layout",
    "schemaVersion": 1,
    "sources": ["Assets/MagicShop", "Tests"],
    "layerRoots": ["Assets/MagicShop/Core", "Assets/MagicShop/Runtime"],
    "warnLines": 400,
    "failLines": 800,
    "maxFilesPerFolder": 12,
    "baseline": "Tools/file-layout-baseline.json"
}
```

```sh
python3 <standards>/tools/check_file_layout.py --config Tools/file-layout.json
python3 <standards>/tools/check_file_layout.py --config Tools/file-layout.json --update-baseline
```

`tool` identifies the file as a layout config and is required; a repository has exactly one. `warnLines`, `failLines` and `maxFilesPerFolder` default to the limits above; `excludeDirectories` adds directory names to skip. In CI, pass the commit the change is measured against as `--base-ref`. On a pull request, check out with `fetch-depth: 2` and use `HEAD^1`: the checked-out commit is the merge into the base branch, so its first parent is the base. On a push, use the event's `before` SHA and fetch enough history to have it, since one push can add several commits. Errors fail the run; advisory warnings print as warnings (GitHub annotations in Actions).

### Analyzer rules on new code

[`tools/check_new_diagnostics.py`](../../tools/check_new_diagnostics.py) reads the SARIF log a build writes (`-p:ErrorLog=<path>`) and the diff against a base ref. A chosen diagnostic fails when its file is new or when it sits on an added or changed line; existing warnings on untouched lines stay warnings. A pure move or rename changes no lines and fails nothing, so a later refactoring change can relocate files without fixing them in the same change.

```sh
python3 <standards>/tools/check_new_diagnostics.py --sarif TestResults/analysis.sarif --base-ref HEAD^1 --rules SA1402,SA1649
```

The default rules are `SA1402`, `SA1649` and `IDE0130`. Pass `--rules` to leave out `IDE0130` where namespaces deliberately do not follow folders (see [Game and Unity Layout](#game-and-unity-layout)). The gate covers only code the build compiled: a Unity assembly that CI does not compile is not checked by it.

---

## Game and Unity Layout

Unity games keep the ecosystem layout (`Assets/`, `Packages/`, `ProjectSettings/`) with everything the game owns under one folder such as `Assets/MagicShop`. Inside it:

### Core: engine-free, one folder per system

`Core` is an assembly with `noEngineReferences` and no dependencies. It owns every rule, piece of saved state, tuning value, save format and player-facing text, so presentation never decides an outcome and the rules run in fast host tests.

Organize Core by system, not by kind. A system (the healer, the board, the lockers, trading) keeps its rules, its texts and its state together in one folder:

```text
Core/
  Healer/
    ShopSimulation.Healer.cs        rules
    ShopSimulation.HealerTexts.cs   player-facing lines
    HealerState.cs                  saved state
  Board/
    ShopSimulation.Board.cs
    ShopSimulation.BoardTexts.cs
    Contract.cs
  Persistence/                      save format, migrations, load validation
  State/                            the root state that holds each system's state
```

Shared foundations, such as the root of the saved state and the save format, keep folders of their own. A game whose Core is still organized by kind (`State`, `Data`, `Simulation`) moves to system folders one system at a time in separate refactoring changes; new systems get their own folder from the start.

### Partial classes split by system

When one class is split into partial parts by system, as a simulation facade often is, each part goes in its system's folder: `Core/Board/ShopSimulation.Board.cs`, not `Core/Simulation/ShopSimulation.Board.cs` beside every other part. C# allows the parts of a partial class in any folder of the same assembly. The file name is the class name plus the system (`ShopSimulation.Board.cs`), and the class's root part stays where the class is introduced.

### Runtime: by area, then by system

`Runtime` holds Unity presentation only, in folders by area (`Session`, `Player`, `World`, `Input`, `UI`, `Audio`, `Localization`, `Persistence`). When an area grows past the folder guideline, its systems get subfolders inside it (`World/Board/WorldSync.Board.cs`, `UI/Staff/GameUI.Staff.cs`). Presentation shows Core's outcomes; it does not decide them.

### Everything else

- Layer roots (`Core`, `Runtime`) hold their assembly definition, ruleset and folders only.
- Tests mirror the system folders they verify, in host test projects and Unity EditMode and PlayMode assemblies alike.
- Generated code stays in a `Generated` folder and is never edited by hand.
- Unity assemblies use one namespace per assembly (for example `HoneyDrunk.MagicShop.Core`), not one per folder, so moving a file between folders never changes its namespace or its callers. `IDE0130` (namespace matches folder) does not apply to them; leave it out of the new-code gate there.
- Diagnostics, editor tooling and tests are separate assemblies that the player assembly never references.

---

## Where New Code Goes

Rules for agents, and for people, adding code:

1. **Find the system first.** Put the change in the folder of the system it belongs to. A new system gets a new folder: in Core for its rules, texts and state, and in the matching Runtime area for its presentation.
2. **Never add a file to a layer root**, and never add another file to a flat folder already over 12 C# files. Create the system's subfolder and put the new file there.
3. **Do not grow a large file.** If a file is over 400 lines, new members go in a new file (a new partial part in the system's folder, or a new type), not at its end. A baselined file must not grow at all.
4. **One type per file**, named for the type. A partial part is named `Class.System.cs`.
5. **Do not move or split existing code inside an unrelated change.** Moving files is its own pull request with no behavior change, so review sees pure moves.
6. **Run the layout check before opening a pull request**, and lower the baseline in the same change when you shrink a baselined file.

---

## Namespace Policy

### Rules
1. **Namespaces match folder structure** (reported by `IDE0130`) in .NET projects. Unity assemblies use one namespace per assembly instead; see [Game and Unity Layout](#game-and-unity-layout)
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

Map every entity explicitly in its own Fluent configuration class: table, key, column types, lengths, precision, nullability, relationships, delete behavior, indexes/filters, concurrency tokens, converters/comparers and immutability protections. Do not leave any of these to an EF convention or an attribute (see [Entity Framework and SQL](#entity-framework-and-sql)). Do not remove a mapping because it looks redundant; verify the effective model and deployed schema.

Every application-owned SQL table and column needs accurate metadata describing meaning and relevant ownership/time semantics. Indexes need a demonstrated query or constraint. Keep schema, EF mappings and schema tests aligned. Where a SQL project/DACPAC owns schema, do not introduce EF migrations as a second owner. Preserve separately owned shared Audit and Outbox schema.

### Asynchrony and integration

Use asynchronous I/O and propagate cancellation. Avoid `.Result`, `.Wait()` and `async void` except framework event handlers. Use injected `TimeProvider` at host-facing boundaries when time affects behavior; capture operation time once and pass it into pure rules. Keep provider SDKs inside their owning adapters. Verify real shared-node contracts and implementation rather than inferring availability from a name or catalog.

### Tests and documentation

Use the repository's current tests and lockfiles. The shared .NET test package supplies xUnit, NSubstitute and AwesomeAssertions; do not introduce Moq or FluentAssertions from old examples. Preserve repository-specific naming and supported framework constraints. Test changed behavior, failure paths and meaningful contracts, without mirroring implementation or fabricating coverage thresholds. Existing required CI, coverage and security gates remain in force.

Use the pinned SDK and language settings; do not hardcode a remembered "latest" C# version into instructions. Follow `.editorconfig`, nullable annotations, public API XML documentation and the actual analyzer configuration. Comments explain non-obvious intent, constraints or trade-offs; do not add comments to every function or repeat obvious code.

Repository Markdown uses a descriptive H1 and hierarchical headings, fenced examples and valid links. Frontmatter is required only by a real consuming schema (for example a publishing pipeline), not for every README. Do not invent blog fields, a categories file or validator enforcement. Check paths and anchors when moving documents.

## Enforced Architecture Rules

The package's analyzers enforce the parts of the [architectural patterns](#architectural-patterns) that a compiler can check. They report warnings, which the package's `TreatWarningsAsErrors` default makes build errors. Review still owns the rest: what a service decides, whether a mapping is complete, whether a comment is useful.

| Rule | Checks |
|---|---|
| `HD0101` | An EF Core entity type's name ends with `Entity` |
| `HD0102` | Only a type named `*DataService` or `*UnitOfWork` (or EF infrastructure) uses a `DbContext`, `DbSet` or `IDbContextFactory` |
| `HD0103` | No mapping attributes on an entity or its members |
| `HD0104` | Every entity in a `DbSet` has an `IEntityTypeConfiguration<T>` class |
| `HD0105` | No inline `ModelBuilder.Entity` configuration |
| `HD0106` | A `*Service` class has at most 10 public or internal methods and 300 lines across its partials |
| `HD0110` | No commented-out code |
| `HD0111` | A `TODO`, `FIXME`, `HACK` or `XXX` comment links the issue that tracks it |

### Services and data services

Endpoints call services; services call data services; only data services reach the database.

- **Services** (`OrderService`) own one use case's rules and orchestration. They take and return contracts or domain values, call data services and other services through focused interfaces, and never hold a `DbContext`.
- **Data services** (`OrderDataService`) own queries, commands, locking and transactions through EF Core. They make no business decisions. A `*UnitOfWork` type that owns the transaction boundary counts as data access too.
- `HD0102` reports field/property types and method returns/parameters even when unused, plus operations on a `DbContext`, `DbSet` or `IDbContextFactory` type used outside a `*DataService` or `*UnitOfWork` type (a type nested inside one counts as inside). EF's own infrastructure is exempt: the `DbContext` itself, `IEntityTypeConfiguration` classes, the design-time factory, migrations and the model snapshot, and interceptors. Test projects are exempt, and so is a project whose whole purpose is EF plumbing (such as HoneyDrunk.Data), which sets `<HD_IsDataInfrastructure>true</HD_IsDataInfrastructure>`. Startup code that migrates the database, and health checks that query it, call a data service too.
- `HD0106` keeps services small: split a service by use case before it passes 10 public or internal methods or 300 lines. Private helpers do not count toward the method limit. A repository may lower the limits, or raise them with a written reason, in its `.globalconfig`:

```ini
honeydrunk.service_max_methods = 8
honeydrunk.service_max_lines = 250
```

### Entity Framework and SQL

Entity Framework mapping is explicit code, reviewed like any other contract, and never left to conventions or attributes.

- These rules cover production schema; test projects, whose fixtures build throwaway models, are exempt. An entity is any type in a `DbSet<T>`, configured by an `IEntityTypeConfiguration<T>`, or passed to `ModelBuilder.Entity<T>()`. Its name ends with `Entity` (`HD0101`), so a persistence type is never mistaken for a contract or a domain model. Contracts do not expose entities.
- Each entity declared in the project has its own concrete, non-generic configuration class (`HD0104`; an entity from a referenced library is configured there), applied in `OnModelCreating` with `ApplyConfigurationsFromAssembly` or `ApplyConfiguration`. `OnModelCreating` does not configure entities inline (`HD0105`).
- The configuration states the table and schema, the key, each column's type, length, precision and nullability, relationships with their delete behavior, indexes and concurrency tokens. Do not rely on an EF convention to choose any of these.
- Entities carry no mapping attributes (`HD0103`): `[Key]`, `[Table]`, `[Column]`, `[Required]`, `[MaxLength]`, `[StringLength]`, `[ForeignKey]`, `[NotMapped]`, `[DatabaseGenerated]`, `[Index]`, `[Precision]` and the rest of the EF and data-annotation mapping set. Validation attributes on request contracts are fine.
- The schema has one owner. Where a SQL project (DACPAC) owns it, keep the EF configuration aligned with it and do not add EF migrations; see [EF and SQL](#ef-and-sql).

### Comments

Comments stay allowed: a comment that records why (a constraint, a trade-off, a provider's quirk) is information the code cannot carry. What is forbidden is the comment used as a shortcut:

- **No commented-out code** (`HD0110`). Delete it; version control keeps the history.
- **No untracked deferred work** (`HD0111`). A `TODO`, `FIXME`, `HACK` or `XXX` names the issue that tracks it (`TODO(#42)`, an issue URL or a `KEY-123` key), or the work is finished before merge. URLs must identify a numbered issue, pull request or ticket (`/issues/42`, `/pull/42`, `/tickets/42`) or contain a tracking key; documentation and tracker home pages do not count.
- **No unexplained suppression.** A `#pragma warning disable` or `[SuppressMessage]` carries its justification (`SA1404`), and a rule is never switched off for a whole project to get a build through.
- **No narration.** A comment that restates the next line, or a public member without its XML documentation, is a review finding.

### Adopting the rules in an existing repository

Upgrade the package and fix the violations in the same change. Rename entities and add configuration classes without changing table or column names (configure `ToTable` and `HasColumnName` where a convention chose them), and move data access into data services. Where one change cannot fix every violation, list the rule in the project's `WarningsNotAsErrors` with a dated reason in the same file, and gate new violations with [`tools/check_new_diagnostics.py`](#analyzer-rules-on-new-code) so the backlog only shrinks. Never set a rule's severity to `none` or `NoWarn` it to pass a build.

---

## Code review

Review the changed behavior against its stated purpose, current repository contracts and the relevant standards above. Follow callers and state transitions far enough to explain a concrete failure. Prioritize correctness, security/data isolation, persistence, reliability/concurrency and delivery defects before design polish. Cite the affected path/line, trigger, consequence and smallest safe correction. Distinguish demonstrated defects from hypotheses, existing debt and unverified evidence; do not inflate severity to get an architectural suggestion reported.

### Ownership, contracts and reuse

Check layer and package dependency direction, domain ownership, public API/event/schema compatibility and real downstream consumers. Apply [simple layers, validation/mapping and persistence rules](#architectural-patterns) to applicable .NET code; retain other repositories' documented stacks and exceptions. Keep business decisions out of transport, mapping and persistence mechanics, and provider implementations behind their actual contracts.

Before proposing consolidation, name the existing helper, contract or owning node and show that its implemented behavior fits. Flag a bypass or duplication when it creates a concrete consistency, correctness or maintenance risk. Prefer a focused extension or explicit justified difference over speculative shared services, generic mutation bindings, extra forwarding layers or frameworks for hypothetical consumers. Missing access to another repository is a verification limit, not proof that reuse exists or is absent.

### Failure, trust and state

Trace null/empty/invalid inputs, precision/time semantics, cancellation, async work, partial failure and recovery. Check authentication/authorization and tenant predicates across queries, caches, background work and provider callbacks; protect secrets and personal data in errors, logs and artifacts. For agent features, treat prompts, retrieved content and model output as untrusted data; preserve tool/approval scope, context isolation and human override.

Inspect transactions, immutable history, concurrency tokens/leases, idempotency and duplicate/out-of-order delivery. A retry must be bounded and safe for the side effect; a non-transient denial or budget stop must not be retried into success. Check migration/backfill/rollback compatibility with the actual schema owner and preserve audit attribution and correlation across boundaries.

### Performance and operations

Use a real changed execution path and expected workload to identify N+1 queries, missing pagination, unbounded queues/fan-out/retries, blocking I/O, hot-path allocations, resource leaks or unsafe cache invalidation. Recommend indexes or caching from demonstrated access patterns, not blanket rules. Keep useful diagnostic context without sensitive payloads or unbounded telemetry cardinality. Check actual caller permissions, workflow events/conditions, artifact provenance, rollout/rollback targets and resource ownership where delivery changes. Source validation does not establish live deployment, recovery or cost approval.

### Evidence and reporting

Ask for tests that would catch the changed behavior's realistic failure, authorization, compatibility or concurrency risk. Prefer observable contract/integration behavior over tests that mirror implementation, tautological assertions or mocks that bypass the affected boundary. Use repository-specific frameworks and meaningful test tiers; do not invent coverage thresholds or demand runtime tests for prose edits. State what mocks, browser checks or smoke runs cannot establish.

Formatting, lint, deterministic schema checks and analyzer policy belong in CI. Review their relevant results and missing coverage without duplicating machine comments. Inspect actual applicable Sonar findings for the latest analyzed head; a successful upload or green gate is not a zero-new-issue count. Fix introduced issues without suppressing findings or weakening gates; unavailable or stale analysis remains unverified.

[Native Codex GitHub review](https://learn.chatgpt.com/docs/third-party/github) follows applicable `## Code Review Rules` in `AGENTS.md`; the official page checked October 10, 2026 describes reporting P0/P1 issues. Keep lower-severity standards work in authoring/manual review and CI rather than promising every criterion will produce a native comment. A quiet review is not exhaustive compliance, test execution, a required status check, merge permission or access to linked repositories. Automatic triggers, final-head review evidence and enforcement must be verified separately. Keep one root entrypoint; do not add client adapters or restore retired review machinery.

## Agent workflow and instruction files

Use one root `AGENTS.md` per repository as the shared entrypoint. Put scope, important boundaries, links to maintained standards and actual verification commands there. Keep deeper engineering detail in ordinary docs and current product specifications/ADRs/progress in their existing owners. Do not create a duplicate universal `SPEC.md`, `RULES.md` or roadmap. Such names are ordinary documents unless a specific tool is explicitly configured to read them.

HoneyDrunk deliberately does not keep separate `CLAUDE.md`, `.github/copilot-instructions.md`, `.github/instructions/*.instructions.md` or nested agent entrypoints. Tool compatibility is a trade-off, not proof that all clients load the root automatically:

- [Codex](https://developers.openai.com/codex/guides/agents-md/) discovers `AGENTS.md` with directory-based precedence; overrides and custom fallbacks require intentional configuration.
- [Claude Code](https://code.claude.com/docs/en/memory) supports AGENTS.md in current versions, but ancestor CLAUDE files and the user's project-instruction setting affect discovery. Check the actual session's loaded files. Do not silently recreate an adapter.
- [Copilot support varies by surface](https://docs.github.com/en/copilot/reference/custom-instructions-support). Some surfaces consume AGENTS.md; others rely on tool-specific files. In those clients, explicitly supply the root instructions. Removing the old adapters does not uninstall Copilot.
- Native Codex review uses the root [Code Review Rules](#code-review). The selected workflow adds no Greptile dependency or separate reviewer rule file; review instructions and required-check enforcement are distinct.

For authorized publication, preserve unrelated changes, review the intended diff explicitly, resolve actionable findings, and stage only intended files. Use Conventional Commits and ready-for-review PRs unless the user explicitly requests drafts. Follow the current PR template, including exactly one accurate `Authorship:` line and a `Request:` line; use the same authorship class in the commit trailer. Valid classes are `human`, `agent-codex`, `agent-claude-code`, `agent-copilot` and `mixed`.

Report the tested/reviewed revision, meaningful checks and limitations. For documentation-only changes, validate content, links, commands and instruction consistency; do not claim runtime tests ran. For applicable Sonar analysis, inspect actual latest-head new-code findings, not just a green upload job or gate. Fix new issues without suppressing findings or weakening checks; unavailable/stale analysis stays unverified.

Legacy Grid Review is retired. Do not restore its workers, schedules, queues, merge requirements or bypass labels. Explicit authoring/manual review, tests, security/Sonar and existing required checks remain. A saved replacement-reviewer rule is not evidence of a completed review or required merge check. Publishing instructions do not grant merge, deployment, paid review, credential or protection-change authorization.
