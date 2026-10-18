using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace HoneyDrunk.Standards.Analyzers.EntityFramework;

/// <summary>
/// Entities end with Entity (HD0101), are mapped without attributes (HD0103), each have a configuration
/// class (HD0104), and are never configured inline on the model builder (HD0105).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class EntityConventionsAnalyzer : DiagnosticAnalyzer
{
    private const string EntitySuffix = "Entity";

    // Attributes that change how EF maps a type. Validation attributes on contracts are fine; on an entity
    // they become hidden mapping, which belongs in the entity's configuration class.
    private static readonly ImmutableHashSet<string> MappingAttributes = ImmutableHashSet.Create(
        "System.ComponentModel.DataAnnotations.KeyAttribute",
        "System.ComponentModel.DataAnnotations.RequiredAttribute",
        "System.ComponentModel.DataAnnotations.MaxLengthAttribute",
        "System.ComponentModel.DataAnnotations.StringLengthAttribute",
        "System.ComponentModel.DataAnnotations.ConcurrencyCheckAttribute",
        "System.ComponentModel.DataAnnotations.TimestampAttribute",
        "System.ComponentModel.DataAnnotations.Schema.TableAttribute",
        "System.ComponentModel.DataAnnotations.Schema.ColumnAttribute",
        "System.ComponentModel.DataAnnotations.Schema.ForeignKeyAttribute",
        "System.ComponentModel.DataAnnotations.Schema.InversePropertyAttribute",
        "System.ComponentModel.DataAnnotations.Schema.NotMappedAttribute",
        "System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedAttribute",
        "System.ComponentModel.DataAnnotations.Schema.ComplexTypeAttribute",
        "Microsoft.EntityFrameworkCore.IndexAttribute",
        "Microsoft.EntityFrameworkCore.PrimaryKeyAttribute",
        "Microsoft.EntityFrameworkCore.PrecisionAttribute",
        "Microsoft.EntityFrameworkCore.UnicodeAttribute",
        "Microsoft.EntityFrameworkCore.KeylessAttribute",
        "Microsoft.EntityFrameworkCore.OwnedAttribute",
        "Microsoft.EntityFrameworkCore.BackingFieldAttribute",
        "Microsoft.EntityFrameworkCore.DeleteBehaviorAttribute",
        "Microsoft.EntityFrameworkCore.CommentAttribute",
        "Microsoft.EntityFrameworkCore.EntityTypeConfigurationAttribute");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(
        ArchitectureRules.EntitySuffix,
        ArchitectureRules.MappingAttribute,
        ArchitectureRules.MissingEntityConfiguration,
        ArchitectureRules.InlineModelConfiguration);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(Start);
    }

    private static void Start(CompilationStartAnalysisContext context)
    {
        var ef = EfSymbols.From(context.Compilation);
        if (ef is null || IsTestProject(context.Options.AnalyzerConfigOptionsProvider.GlobalOptions))
        {
            // Test fixtures build throwaway models, often inline; the rules are for production schema.
            return;
        }

        var entities = new ConcurrentDictionary<ITypeSymbol, byte>(SymbolEqualityComparer.Default);
        var configured = new ConcurrentDictionary<ITypeSymbol, byte>(SymbolEqualityComparer.Default);
        var dbSets = new ConcurrentBag<(ITypeSymbol Entity, Location Location)>();

        context.RegisterSymbolAction(
            symbolContext => CollectEntities(ef, (INamedTypeSymbol)symbolContext.Symbol, entities, configured, dbSets),
            SymbolKind.NamedType);
        context.RegisterOperationAction(
            operationContext => AnalyzeModelBuilderCall(ef, operationContext, entities),
            OperationKind.Invocation);
        context.RegisterCompilationEndAction(endContext => ReportEntities(endContext, entities, configured, dbSets));
    }

    private static void CollectEntities(
        EfSymbols ef,
        INamedTypeSymbol type,
        ConcurrentDictionary<ITypeSymbol, byte> entities,
        ConcurrentDictionary<ITypeSymbol, byte> configured,
        ConcurrentBag<(ITypeSymbol Entity, Location Location)> dbSets)
    {
        // A generic base configuration (BaseConfiguration<T>) configures no entity of its own.
        var configuredEntity = ef.ConfiguredEntity(type);
        if (configuredEntity is INamedTypeSymbol)
        {
            configured.TryAdd(configuredEntity, 0);
            entities.TryAdd(configuredEntity, 0);
        }

        if (!ef.IsDbContext(type))
        {
            return;
        }

        var dbSetProperties = type.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(property => ef.IsDbSet(property.Type) && property.Locations.FirstOrDefault() is { IsInSource: true });
        foreach (var property in dbSetProperties)
        {
            var entity = ((INamedTypeSymbol)property.Type).TypeArguments[0];
            entities.TryAdd(entity, 0);
            dbSets.Add((entity, property.Locations[0]));
        }
    }

    private static void AnalyzeModelBuilderCall(EfSymbols ef, OperationAnalysisContext context, ConcurrentDictionary<ITypeSymbol, byte> entities)
    {
        var method = ((IInvocationOperation)context.Operation).TargetMethod;
        if (method.Name != "Entity" || !SymbolEqualityComparer.Default.Equals(method.ContainingType, ef.ModelBuilder))
        {
            return;
        }

        var name = "the entity";
        if (method.TypeArguments.Length == 1)
        {
            if (method.TypeArguments[0] is INamedTypeSymbol)
            {
                entities.TryAdd(method.TypeArguments[0], 0);
            }

            name = method.TypeArguments[0].Name;
        }

        context.ReportDiagnostic(Diagnostic.Create(ArchitectureRules.InlineModelConfiguration, context.Operation.Syntax.GetLocation(), name));
    }

    private static void ReportEntities(
        CompilationAnalysisContext context,
        ConcurrentDictionary<ITypeSymbol, byte> entities,
        ConcurrentDictionary<ITypeSymbol, byte> configured,
        ConcurrentBag<(ITypeSymbol Entity, Location Location)> dbSets)
    {
        foreach (var entity in entities.Keys)
        {
            var location = entity.Locations.FirstOrDefault(candidate => candidate.IsInSource);
            if (location is null)
            {
                continue;
            }

            if (!entity.Name.EndsWith(EntitySuffix, System.StringComparison.Ordinal))
            {
                context.ReportDiagnostic(Diagnostic.Create(ArchitectureRules.EntitySuffix, location, entity.Name));
            }

            foreach (var (attribute, owner) in MappingAttributesOn(entity))
            {
                var attributeLocation = attribute.ApplicationSyntaxReference?.GetSyntax(context.CancellationToken).GetLocation() ?? owner;
                var attributeName = attribute.AttributeClass!.Name;
                if (attributeName.EndsWith("Attribute", System.StringComparison.Ordinal))
                {
                    attributeName = attributeName.Substring(0, attributeName.Length - "Attribute".Length);
                }

                context.ReportDiagnostic(Diagnostic.Create(ArchitectureRules.MappingAttribute, attributeLocation, attributeName, entity.Name));
            }
        }

        var reported = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var (entity, location) in dbSets)
        {
            // An entity from a referenced library is configured by that library.
            if (entity.Locations.Any(candidate => candidate.IsInSource) && !configured.ContainsKey(entity) && reported.Add(entity))
            {
                context.ReportDiagnostic(Diagnostic.Create(ArchitectureRules.MissingEntityConfiguration, location, entity.Name));
            }
        }
    }

    private static bool IsTestProject(AnalyzerConfigOptions options) =>
        options.TryGetValue("build_property.HD_IsGridTestProject", out var value) && bool.TryParse(value, out var isTest) && isTest;

    private static IEnumerable<(AttributeData Attribute, Location Owner)> MappingAttributesOn(ITypeSymbol entity)
    {
        // EF maps inherited members too, so walk the base types declared in this project.
        var owners = new List<ISymbol>();
        for (var type = entity; type is not null && type.Locations.Any(candidate => candidate.IsInSource); type = type.BaseType)
        {
            owners.Add(type);
            owners.AddRange(type.GetMembers().Where(member => member is IPropertySymbol or IFieldSymbol));
        }
        foreach (var owner in owners)
        {
            var ownerLocation = owner.Locations.FirstOrDefault() ?? Location.None;
            var mapping = owner.GetAttributes()
                .Where(attribute => attribute.AttributeClass is not null && MappingAttributes.Contains(attribute.AttributeClass.ToDisplayString()));
            foreach (var attribute in mapping)
            {
                yield return (attribute, ownerLocation);
            }
        }
    }
}
