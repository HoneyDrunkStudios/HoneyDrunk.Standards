using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HoneyDrunk.Standards.Analyzers.Services;

/// <summary>
/// HD0106: a class named *Service (business or data) stays small: at most a configured number of
/// public or internal methods and of lines across all its partial declarations.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ServiceSizeAnalyzer : DiagnosticAnalyzer
{
    public const string MaxMethodsOption = "honeydrunk.service_max_methods";
    public const string MaxLinesOption = "honeydrunk.service_max_lines";
    public const int DefaultMaxMethods = 10;
    public const int DefaultMaxLines = 300;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ArchitectureRules.ServiceTooLarge);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
    }

    private static void Analyze(SymbolAnalysisContext context)
    {
        var type = (INamedTypeSymbol)context.Symbol;
        if (type.TypeKind != TypeKind.Class || !type.Name.EndsWith("Service", System.StringComparison.Ordinal))
        {
            return;
        }

        var declarations = type.DeclaringSyntaxReferences;
        if (declarations.IsEmpty)
        {
            return;
        }

        var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(declarations[0].SyntaxTree);
        var location = type.Locations[0];

        var methods = type.GetMembers()
            .OfType<IMethodSymbol>()
            .Count(method => method.MethodKind == MethodKind.Ordinary
                && !method.IsImplicitlyDeclared
                && method.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal);
        var maxMethods = Read(options, MaxMethodsOption, DefaultMaxMethods);
        if (methods > maxMethods)
        {
            context.ReportDiagnostic(Diagnostic.Create(ArchitectureRules.ServiceTooLarge, location, type.Name, methods, "public or internal methods", maxMethods));
        }

        var lines = declarations.Sum(declaration =>
        {
            var span = declaration.SyntaxTree.GetLineSpan(declaration.Span, context.CancellationToken);
            return span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
        });
        var maxLines = Read(options, MaxLinesOption, DefaultMaxLines);
        if (lines > maxLines)
        {
            context.ReportDiagnostic(Diagnostic.Create(ArchitectureRules.ServiceTooLarge, location, type.Name, lines, "lines", maxLines));
        }
    }

    private static int Read(AnalyzerConfigOptions options, string key, int fallback) =>
        options.TryGetValue(key, out var text) && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value > 0
            ? value
            : fallback;
}
