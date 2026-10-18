using Microsoft.CodeAnalysis;

namespace HoneyDrunk.Standards.Analyzers;

/// <summary>
/// The HoneyDrunk architecture and maintainability rules. They are warnings, which the package's
/// TreatWarningsAsErrors default turns into build errors; CONVENTIONS.md explains each one.
/// </summary>
internal static class ArchitectureRules
{
    private const string Architecture = "Architecture";
    private const string Maintainability = "Maintainability";
    private const string HelpRoot = "https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards/blob/main/HoneyDrunk.Standards/docs/CONVENTIONS.md";

    public static readonly DiagnosticDescriptor EntitySuffix = Create(
        "HD0101",
        "Entity type names end with Entity",
        "Entity type '{0}' must be named '{0}Entity' so persistence types are never mistaken for contracts or domain models",
        Architecture,
        "#entity-framework-and-sql",
        WellKnownDiagnosticTags.CompilationEnd);

    public static readonly DiagnosticDescriptor DataAccessOutsideDataService = Create(
        "HD0102",
        "Only data services touch the database",
        "'{0}' uses {1} directly; move this query or command into a type named *DataService (or *UnitOfWork for a transaction boundary) and call that instead",
        Architecture,
        "#services-and-data-services");

    public static readonly DiagnosticDescriptor MappingAttribute = Create(
        "HD0103",
        "Entity mapping is explicit, not by attribute",
        "Remove [{0}] from entity '{1}' and configure it in the entity's IEntityTypeConfiguration class",
        Architecture,
        "#entity-framework-and-sql",
        WellKnownDiagnosticTags.CompilationEnd);

    public static readonly DiagnosticDescriptor MissingEntityConfiguration = Create(
        "HD0104",
        "Every entity has a configuration class",
        "Entity '{0}' has no IEntityTypeConfiguration<{0}> class; add one that maps its table, key and columns explicitly",
        Architecture,
        "#entity-framework-and-sql",
        WellKnownDiagnosticTags.CompilationEnd);

    public static readonly DiagnosticDescriptor InlineModelConfiguration = Create(
        "HD0105",
        "No inline entity configuration on the model builder",
        "Configure '{0}' in its IEntityTypeConfiguration class and apply it, instead of calling ModelBuilder.Entity",
        Architecture,
        "#entity-framework-and-sql");

    public static readonly DiagnosticDescriptor ServiceTooLarge = Create(
        "HD0106",
        "Services stay small",
        "Service '{0}' has {1} {2} (limit {3}); split it by use case into smaller services",
        Architecture,
        "#services-and-data-services");

    public static readonly DiagnosticDescriptor CommentedOutCode = Create(
        "HD0110",
        "No commented-out code",
        "Delete this commented-out code; version control keeps the history",
        Maintainability,
        "#comments");

    public static readonly DiagnosticDescriptor UntrackedTodo = Create(
        "HD0111",
        "Deferred work is tracked",
        "This {0} comment has no issue reference; finish the work, or link the issue that tracks it (#123 or a URL)",
        Maintainability,
        "#comments");

    private static DiagnosticDescriptor Create(string id, string title, string message, string category, string anchor, params string[] tags) =>
        new(id, title, message, category, DiagnosticSeverity.Warning, isEnabledByDefault: true, helpLinkUri: HelpRoot + anchor, customTags: tags);
}
