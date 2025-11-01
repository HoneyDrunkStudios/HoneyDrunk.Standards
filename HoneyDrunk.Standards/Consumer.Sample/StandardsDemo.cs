namespace Consumer.Sample;

/// <summary>
/// Example class demonstrating HoneyDrunk.Standards analyzer enforcement.
/// </summary>
public sealed class StandardsDemo
{
    private readonly string _name;
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StandardsDemo"/> class.
    /// </summary>
    /// <param name="name">The name value.</param>
    /// <param name="logger">Optional logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when name is null.</exception>
    public StandardsDemo(string name, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        _name = name;
        _logger = logger;
    }

    /// <summary>
    /// Gets the name value.
    /// </summary>
    public string Name => _name;

    /// <summary>
    /// Creates a greeting message asynchronously.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the greeting operation.</returns>
    public async Task<string> GetGreetingAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Generating greeting for {Name}", _name);

        // Simulate async work
        await Task.Delay(100, cancellationToken);

        return $"Hello, {_name}!";
    }

    /// <summary>
    /// Validates the name format.
    /// </summary>
    /// <returns>True if the name is valid; otherwise, false.</returns>
    public bool IsValidName() => !string.IsNullOrWhiteSpace(_name) && _name.Length >= 2;
}

/// <summary>
/// Interface demonstrating proper naming conventions.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Gets a user by their unique identifier.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user if found; otherwise, null.</returns>
    Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Saves a user to the repository.
    /// </summary>
    /// <param name="user">The user to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the save operation.</returns>
    Task SaveAsync(User user, CancellationToken cancellationToken);
}

/// <summary>
/// Represents a user in the system.
/// </summary>
public sealed class User
{
    /// <summary>
    /// Initializes a new instance of the <see cref="User"/> class.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="email">The user's email address.</param>
    public User(int id, string email)
    {
        ArgumentNullException.ThrowIfNull(email);

        if (id <= 0)
        {
            throw new ArgumentException("User ID must be positive", nameof(id));
        }

        Id = id;
        Email = email;
    }

    /// <summary>
    /// Gets the user identifier.
    /// </summary>
    public int Id { get; }

    /// <summary>
    /// Gets the user's email address.
    /// </summary>
    public string Email { get; }
}

/// <summary>
/// Minimal logger interface for demonstration purposes.
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Logs an informational message.
    /// </summary>
    /// <param name="message">The message template.</param>
    /// <param name="args">Message arguments.</param>
    void LogInformation(string message, params object[] args);
}


// ============================================
// INTENTIONAL VIOLATIONS (COMMENTED OUT)
// Uncomment these to see analyzer warnings
// ============================================
/// <summary>
/// Example class with intentional violations for testing.
/// </summary>
//public class ViolationDemo
//{
//    // ❌ CA1051: Do not declare visible instance fields
//    public string PublicField = string.Empty;
//    // ❌ CA1822: Member can be made static
//    public string GetConstantValue() { return "constant"; }
//    // ❌ CA1031: Do not catch general exception types (in business logic)
//    public void CatchAllExceptions()
//    {
//        try
//        {
//            throw new InvalidOperationException();
//        }
//        catch (Exception)
//        {
//            // Should catch specific exceptions
//        }
//    }
//    // ❌ CA1849: Call async methods when in an async method
//    public async Task BadAsyncPattern()
//    {
//        Task.Delay(100).Wait(); // Should use await
//    }
//    // ❌ CA2007: ConfigureAwait(disabled in our rules, but shown for reference)
//    public async Task MissingConfigureAwait()
//    {
//        await Task.Delay(100); // In library code, might want ConfigureAwait(false)
//    }

//    // ❌ IDE0161: Use file-scoped namespace
//    // This file uses file-scoped namespace, but block-scoped would trigger
//    // warning

//    // ❌ CA2227: Collection properties should be read only
//    public List<string> MutableCollection { get; set; } = new();

//    //  ❌ CA1062: Validate arguments of public methods
//    public void NoNullCheck(string value)
//    {
//        Console.WriteLine(value.Length); // Missing null check
//    }

//    // ❌ CA2000: Dispose objects before losing scope
//    public void MissingDispose()
//    {
//        var stream = new MemoryStream();
//        // Missing using or Dispose call
//    }
//}

//❌ SA1649: File name should match first type name
//If this file was named differently, StyleCop would warn

//❌ SA1633: File should have header
//We've disabled this rule, but if enabled, would require copyright header

//❌ IDE0005: Remove unnecessary using directives
//using System.Linq; // If unused, would trigger warning
