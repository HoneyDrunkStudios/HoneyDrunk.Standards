using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace HoneyDrunk.Standards.Unity.Analyzers
{
    /// <summary>Unity test assemblies do not receive the server MSBuild test property.</summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ThreadSleepInTestsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "HD0051";
        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "Thread.Sleep is forbidden in HoneyDrunk test assemblies",
            "Thread.Sleep is forbidden in test assemblies; await completion or use a coroutine with an explicit timeout",
            "Testing",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(start =>
            {
                var name = start.Compilation.AssemblyName ?? string.Empty;
                if (name.EndsWith(".Tests", StringComparison.Ordinal)
                    || name.EndsWith(".Tests.EditMode", StringComparison.Ordinal)
                    || name.EndsWith(".Tests.PlayMode", StringComparison.Ordinal))
                    start.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
            });
        }

        private static void AnalyzeInvocation(OperationAnalysisContext context)
        {
            var invocation = (IInvocationOperation)context.Operation;
            var method = invocation.TargetMethod;
            var thread = context.Compilation.GetTypeByMetadataName("System.Threading.Thread");
            if (method.Name != "Sleep" || method.Parameters.Length != 1
                || !SymbolEqualityComparer.Default.Equals(method.ContainingType, thread))
                return;

            var path = invocation.Syntax.SyntaxTree.FilePath.Replace('\\', '/');
            // These are source ownership boundaries, not diagnostic-wide suppressions.
            foreach (var segment in path.Split('/'))
                if (segment.Equals("Generated", StringComparison.OrdinalIgnoreCase)
                    || segment.Equals("ThirdParty", StringComparison.OrdinalIgnoreCase)
                    || segment.Equals("Plugins", StringComparison.OrdinalIgnoreCase)
                    || segment.Equals("Packages", StringComparison.OrdinalIgnoreCase))
                    return;

            var parameter = method.Parameters[0].Type;
            if (parameter.SpecialType == SpecialType.System_Int32
                || SymbolEqualityComparer.Default.Equals(parameter, context.Compilation.GetTypeByMetadataName("System.TimeSpan")))
                context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.Syntax.GetLocation()));
        }
    }
}
