using System.Linq;
using Microsoft.CodeAnalysis;

namespace HoneyDrunk.Standards.Analyzers.EntityFramework;

/// <summary>
/// The Entity Framework Core types the architecture rules look for, resolved once per compilation.
/// </summary>
internal sealed class EfSymbols
{
    private EfSymbols(Compilation compilation, INamedTypeSymbol dbContext)
    {
        DbContext = dbContext;
        DbSet = compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.DbSet`1");
        DbContextFactory = compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.IDbContextFactory`1");
        EntityTypeConfiguration = compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.IEntityTypeConfiguration`1");
        DesignTimeFactory = compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory`1");
        ModelBuilder = compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.ModelBuilder");
        Migration = compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.Migrations.Migration");
        ModelSnapshot = compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.Infrastructure.ModelSnapshot");
        Interceptor = compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor");
    }

    public INamedTypeSymbol DbContext { get; }

    public INamedTypeSymbol? DbSet { get; }

    public INamedTypeSymbol? DbContextFactory { get; }

    public INamedTypeSymbol? EntityTypeConfiguration { get; }

    public INamedTypeSymbol? DesignTimeFactory { get; }

    public INamedTypeSymbol? ModelBuilder { get; }

    public INamedTypeSymbol? Migration { get; }

    public INamedTypeSymbol? ModelSnapshot { get; }

    public INamedTypeSymbol? Interceptor { get; }

    /// <summary>The symbols, or null when the compilation does not reference Entity Framework Core.</summary>
    public static EfSymbols? From(Compilation compilation)
    {
        var dbContext = compilation.GetTypeByMetadataName("Microsoft.EntityFrameworkCore.DbContext");
        return dbContext is null ? null : new EfSymbols(compilation, dbContext);
    }

    public bool IsDbContext(ITypeSymbol? type) => DerivesFrom(type, DbContext);

    public bool IsDbSet(ITypeSymbol? type) =>
        type is INamedTypeSymbol named && DbSet is not null && SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, DbSet);

    /// <summary>A DbContext, a DbSet or a DbContext factory: the handles through which code reaches the database.</summary>
    public bool IsDataAccessType(ITypeSymbol? type) =>
        IsDbContext(type) || IsDbSet(type) || IsGenericInterface(type, DbContextFactory);

    /// <summary>The entity a type configures through IEntityTypeConfiguration&lt;T&gt;, if it does.</summary>
    public ITypeSymbol? ConfiguredEntity(INamedTypeSymbol type)
    {
        if (EntityTypeConfiguration is null)
        {
            return null;
        }

        foreach (var contract in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(contract.OriginalDefinition, EntityTypeConfiguration))
            {
                return contract.TypeArguments[0];
            }
        }

        return null;
    }

    /// <summary>
    /// Entity Framework infrastructure that legitimately holds a DbContext: the context itself, its mapping,
    /// design-time factory, migrations and interceptors.
    /// </summary>
    public bool IsInfrastructure(INamedTypeSymbol type) =>
        IsDbContext(type)
        || ConfiguredEntity(type) is not null
        || ImplementsGeneric(type, DesignTimeFactory)
        || DerivesFrom(type, Migration)
        || DerivesFrom(type, ModelSnapshot)
        || (Interceptor is not null && type.AllInterfaces.Contains(Interceptor, SymbolEqualityComparer.Default));

    private static bool DerivesFrom(ITypeSymbol? type, INamedTypeSymbol? baseType)
    {
        if (baseType is null)
        {
            return false;
        }

        for (var current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ImplementsGeneric(INamedTypeSymbol type, INamedTypeSymbol? contract) =>
        contract is not null
        && type.AllInterfaces.Any(candidate => SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, contract));

    private static bool IsGenericInterface(ITypeSymbol? type, INamedTypeSymbol? contract) =>
        type is INamedTypeSymbol named
        && contract is not null
        && (SymbolEqualityComparer.Default.Equals(named.OriginalDefinition, contract) || ImplementsGeneric(named, contract));
}
