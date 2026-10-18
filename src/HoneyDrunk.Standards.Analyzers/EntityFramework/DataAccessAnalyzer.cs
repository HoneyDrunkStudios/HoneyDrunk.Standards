using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HoneyDrunk.Standards.Analyzers.EntityFramework;

/// <summary>
/// HD0102: only types named *DataService or *UnitOfWork (and EF's own infrastructure) use a DbContext,
/// DbSet or DbContext factory. Business services call data services; endpoints call services.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DataAccessAnalyzer : DiagnosticAnalyzer
{
    private static readonly string[] DataAccessSuffixes = { "DataService", "UnitOfWork" };

    private static readonly ImmutableArray<OperationKind> References = ImmutableArray.Create(
        OperationKind.FieldReference,
        OperationKind.PropertyReference,
        OperationKind.ParameterReference,
        OperationKind.LocalReference);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ArchitectureRules.DataAccessOutsideDataService);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(Start);
    }

    private static void Start(CompilationStartAnalysisContext context)
    {
        var ef = EfSymbols.From(context.Compilation);
        var options = context.Options.AnalyzerConfigOptionsProvider.GlobalOptions;
        if (ef is null || IsSet(options, "build_property.HD_IsGridTestProject") || IsSet(options, "build_property.HD_IsDataInfrastructure"))
        {
            return;
        }

        // One diagnostic per offending member keeps a long method from drowning the build output.
        var reported = new ConcurrentDictionary<ISymbol, byte>(SymbolEqualityComparer.Default);
        context.RegisterOperationAction(operationContext => Analyze(ef, operationContext, reported), References);
    }

    private static void Analyze(EfSymbols ef, OperationAnalysisContext context, ConcurrentDictionary<ISymbol, byte> reported)
    {
        var type = context.Operation.Type;
        if (!ef.IsDataAccessType(type) || IsAllowed(ef, context.ContainingSymbol.ContainingType))
        {
            return;
        }

        if (reported.TryAdd(context.ContainingSymbol, 0))
        {
            var owner = context.ContainingSymbol.ContainingType?.Name ?? context.ContainingSymbol.Name;
            context.ReportDiagnostic(Diagnostic.Create(
                ArchitectureRules.DataAccessOutsideDataService,
                context.Operation.Syntax.GetLocation(),
                owner,
                type!.Name));
        }
    }

    private static bool IsAllowed(EfSymbols ef, INamedTypeSymbol? type)
    {
        for (var current = type; current is not null; current = current.ContainingType)
        {
            if (DataAccessSuffixes.Any(suffix => current.Name.EndsWith(suffix, System.StringComparison.Ordinal)) || ef.IsInfrastructure(current))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSet(AnalyzerConfigOptions options, string key) =>
        options.TryGetValue(key, out var value) && bool.TryParse(value, out var isSet) && isSet;
}
