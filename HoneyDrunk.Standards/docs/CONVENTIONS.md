# HoneyDrunk Conventions

This document defines coding, organizational, and process conventions for all HoneyDrunk projects.

## Table of Contents
- [Naming Conventions](#naming-conventions)
- [Folder Structure](#folder-structure)
- [Namespace Policy](#namespace-policy)
- [Nullable Reference Types](#nullable-reference-types)
- [Warning Philosophy](#warning-philosophy)
- [Analyzer Rationale](#analyzer-rationale)
- [Commit Messages](#commit-messages)
- [Architectural Patterns](#architectural-patterns)

---

## Naming Conventions

### General Rules
- **PascalCase** for types, methods, properties, events, constants
- **camelCase** for parameters, local variables, private fields
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

### Standard Project Layout

```
Solution.sln
/ProjectName.Domain/
  /Aggregates/
  /ValueObjects/
  /DomainEvents/
/Specifications/
/ProjectName.Application/
  /Commands/
  /Queries/
  /DTOs/
  /Validators/
/ProjectName.Infrastructure/
  /Persistence/
  /Repositories/
  /EventBus/
  /ExternalServices/
/ProjectName.Api/ or /ProjectName.Web/
  /Controllers/ or /Endpoints/
  /Middleware/
  /Filters/
/tests/
  /ProjectName.Domain.Tests/
  /ProjectName.Application.Tests/
  /ProjectName.Infrastructure.Tests/
  /ProjectName.Api.Tests/
/docs/
  README.md
  ARCHITECTURE.md
  API.md
/samples/
  /SampleConsumer/
```

### Key Principles
- **Solution root** contains all projects directly
- **`/tests`** for test projects (mirrors main project structure)
- **`/docs`** for documentation
- **`/samples`** for example usage (optional)
- **`/scripts`** for build/deployment scripts (optional)

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
// File: ProjectName.Domain/Aggregates/User.cs
namespace ProjectName.Domain.Aggregates;

public class User { }
```

#### ? Incorrect
```csharp
// File: ProjectName.Domain/Aggregates/User.cs
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
global using FluentValidation;
global using MediatR;
global using ProjectName.Domain.Aggregates;
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
// The framework guarantees HttpContext is non-null in middleware
var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
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
- **Reference issues** in `<footer>` (`Closes #123`, `Fixes #456`)
- **Breaking changes** noted in `<footer>` with `BREAKING CHANGE:`

### Enforcement
Currently **not enforced** by tooling. Future package (`HoneyDrunk.GitHooks`) may add commit-msg validation.

---

## Architectural Patterns

### Domain-Driven Design (DDD)

HoneyDrunk projects follow DDD principles where applicable.

#### Aggregate Boundaries
```csharp
// ? Aggregate root enforces invariants
public class Order
{
    private readonly List<OrderLine> _lines = new();
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    public void AddLine(Product product, int quantity)
    {
        if (quantity <= 0)
    throw new ArgumentException("Quantity must be positive", nameof(quantity));

    _lines.Add(new OrderLine(product, quantity));
    }
}

// ? Exposed mutable collection
public class Order
{
    public List<OrderLine> Lines { get; set; } = new(); // Violates encapsulation
}
```

#### Value Objects
```csharp
// ? Immutable value object
public sealed record EmailAddress(string Value)
{
    public EmailAddress(string value) : this(value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be empty", nameof(value));
        if (!value.Contains('@'))
            throw new ArgumentException("Invalid email format", nameof(value));
    }
}

// ? Mutable value object
public class EmailAddress
{
    public string Value { get; set; } // Should be immutable
}
```

### SOLID Principles

#### Single Responsibility
```csharp
// ? Single responsibility
public class UserRepository
{
    public User GetById(int id) { /* ... */ }
    public void Save(User user) { /* ... */ }
}

public class UserValidator
{
    public ValidationResult Validate(User user) { /* ... */ }
}

// ? Multiple responsibilities
public class UserService
{
    public User GetById(int id) { /* ... */ }
    public void Save(User user) { /* ... */ }
    public ValidationResult Validate(User user) { /* ... */ } // Should be separate
    public void SendWelcomeEmail(User user) { /* ... */ } // Should be separate
}
```

#### Dependency Inversion
```csharp
// ? Depend on abstraction
public class OrderService
{
    private readonly IOrderRepository _repository;
    
    public OrderService(IOrderRepository repository)
    {
        _repository = repository;
    }
}

// ? Depend on concretion
public class OrderService
{
    private readonly SqlOrderRepository _repository = new(); // Tightly coupled
}
```

### Async/Await

#### ? Correct Patterns
```csharp
public async Task<User> GetUserAsync(int id, CancellationToken cancellationToken)
{
    return await _dbContext.Users
        .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
}

public async Task ProcessOrdersAsync(CancellationToken cancellationToken)
{
    var orders = await GetPendingOrdersAsync(cancellationToken);
    
    foreach (var order in orders)
    {
        await ProcessOrderAsync(order, cancellationToken);
    }
}
```

#### ? Incorrect Patterns
```csharp
// ? Blocking async code
public User GetUser(int id)
{
    return GetUserAsync(id, CancellationToken.None).Result; // Deadlock risk
}

// ? Async void (except event handlers)
public async void ProcessOrder(Order order) // Should return Task
{
    await _repository.SaveAsync(order);
}

// ? Missing CancellationToken
public async Task<User> GetUserAsync(int id) // Should accept CancellationToken
{
    return await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
}
```

---

## Summary

These conventions are enforced by:
- **HoneyDrunk.Standards** package (analyzers, .editorconfig, ruleset)
- **Code review** (architectural patterns, DDD principles)
- **CI builds** (warnings as errors, deterministic builds)

For questions or proposed changes, see [CONTRIBUTING.md](../CONTRIBUTING.md) or open a [GitHub Discussion](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/discussions).
