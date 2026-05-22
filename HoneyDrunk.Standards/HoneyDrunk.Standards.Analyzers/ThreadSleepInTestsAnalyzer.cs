using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace HoneyDrunk.Standards.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ThreadSleepInTestsAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "HD0051";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Thread.Sleep is forbidden in HoneyDrunk test projects",
        "Thread.Sleep is forbidden in HoneyDrunk test projects; use await, polling with explicit timeout, or a synchronously-completing fake",
        "Testing",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "ADR-0047 / Invariant 51 forbids Thread.Sleep in test projects because it creates CI flakiness.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(RegisterIfTestProject);
    }

    private static void RegisterIfTestProject(CompilationStartAnalysisContext context)
    {
        if (!IsGridTestProject(context.Options.AnalyzerConfigOptionsProvider.GlobalOptions))
        {
            return;
        }

        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static bool IsGridTestProject(AnalyzerConfigOptions options)
    {
        return options.TryGetValue("build_property.HD_IsGridTestProject", out var value)
            && bool.TryParse(value, out var isTestProject)
            && isTestProject;
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        var invocation = (IInvocationOperation)context.Operation;
        var method = invocation.TargetMethod;

        if (method.Name != "Sleep" || method.ContainingType?.ToDisplayString() != "System.Threading.Thread")
        {
            return;
        }

        if (method.Parameters.Length != 1)
        {
            return;
        }

        var parameterType = method.Parameters[0].Type.ToDisplayString();
        if (parameterType is not ("int" or "System.Int32" or "System.TimeSpan"))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation()));
    }
}
