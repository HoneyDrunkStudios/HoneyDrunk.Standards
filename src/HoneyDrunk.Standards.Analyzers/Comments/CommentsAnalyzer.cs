using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HoneyDrunk.Standards.Analyzers.Comments;

/// <summary>
/// Comments explain intent; they never hold dead code (HD0110) or untracked deferred work (HD0111).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CommentsAnalyzer : DiagnosticAnalyzer
{
    private static readonly Regex DeferredWork = new(@"\b(TODO|FIXME|HACK|XXX)\b", RegexOptions.CultureInvariant);
    private static readonly Regex TrackingReference = new(@"#\d+|https?://|\b[A-Z][A-Z0-9]+-\d+\b", RegexOptions.CultureInvariant);

    // Prose rarely contains these; a comment line without any of them is not treated as code.
    private static readonly string[] CodeMarkers = { "(", "=", ".", "[", "=>", "return", "await", "new ", "var " };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(ArchitectureRules.CommentedOutCode, ArchitectureRules.UntrackedTodo);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxTreeAction(Analyze);
    }

    private static void Analyze(SyntaxTreeAnalysisContext context)
    {
        var root = context.Tree.GetRoot(context.CancellationToken);
        var block = new List<SyntaxTrivia>();
        var previousLine = -2;

        foreach (var trivia in root.DescendantTrivia(descendIntoTrivia: true))
        {
            ReportUntrackedWork(context, trivia);
            if (trivia.IsKind(SyntaxKind.MultiLineCommentTrivia))
            {
                ReportBlockCommentCode(context, trivia);
            }

            if (!trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
            {
                continue;
            }

            var line = trivia.GetLocation().GetLineSpan().StartLinePosition.Line;
            if (line != previousLine + 1 && block.Count > 0)
            {
                ReportBlock(context, block);
                block.Clear();
            }

            block.Add(trivia);
            previousLine = line;
        }

        if (block.Count > 0)
        {
            ReportBlock(context, block);
        }
    }

    private static void ReportBlockCommentCode(SyntaxTreeAnalysisContext context, SyntaxTrivia trivia)
    {
        var text = trivia.ToString();
        var body = text.Substring(2, System.Math.Max(0, text.Length - 4));
        var lines = body.Split('\n')
            .Select(line => line.Trim().TrimStart('*').Trim())
            .Where(line => line.Length > 0)
            .ToList();
        if (lines.Any(LooksLikeCodeLine) || LooksLikeCodeBlock(lines))
        {
            context.ReportDiagnostic(Diagnostic.Create(ArchitectureRules.CommentedOutCode, trivia.GetLocation()));
        }
    }

    // Single-line comments are checked a block at a time, so a reference on the next line counts.
    private static void ReportUntrackedWork(SyntaxTreeAnalysisContext context, SyntaxTrivia trivia)
    {
        if (!trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)
            && !trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
            && !trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
        {
            return;
        }

        var text = trivia.ToFullString();
        var match = DeferredWork.Match(text);
        if (match.Success && !TrackingReference.IsMatch(text))
        {
            context.ReportDiagnostic(Diagnostic.Create(ArchitectureRules.UntrackedTodo, trivia.GetLocation(), match.Value));
        }
    }

    private static void ReportBlock(SyntaxTreeAnalysisContext context, List<SyntaxTrivia> block)
    {
        var lines = block.Select(trivia => trivia.ToString().Substring(2).Trim()).ToList();
        if (lines.Any(LooksLikeCodeLine) || LooksLikeCodeBlock(lines))
        {
            context.ReportDiagnostic(Diagnostic.Create(ArchitectureRules.CommentedOutCode, block[0].GetLocation()));
        }

        if (!TrackingReference.IsMatch(string.Join(" ", lines)))
        {
            foreach (var trivia in block)
            {
                var match = DeferredWork.Match(trivia.ToString());
                if (match.Success)
                {
                    context.ReportDiagnostic(Diagnostic.Create(ArchitectureRules.UntrackedTodo, trivia.GetLocation(), match.Value));
                }
            }
        }
    }

    private static bool LooksLikeCodeLine(string line)
    {
        if (!line.EndsWith(";", System.StringComparison.Ordinal))
        {
            return false;
        }

        var parsed = Parse(line);
        if (parsed is null)
        {
            return false;
        }

        // Prose such as "see above;" parses as a declaration, so a bare declaration needs a code marker;
        // jump statements like "break;" or "throw error;" are code on their own.
        return HasCodeMarker(line) || parsed.Kind() is SyntaxKind.BreakStatement or SyntaxKind.ContinueStatement
            or SyntaxKind.ThrowStatement or SyntaxKind.ReturnStatement or SyntaxKind.YieldReturnStatement
            or SyntaxKind.YieldBreakStatement or SyntaxKind.GotoStatement
            || parsed is Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionStatementSyntax
            {
                Expression.RawKind: (int)SyntaxKind.PostIncrementExpression or (int)SyntaxKind.PreIncrementExpression
                    or (int)SyntaxKind.PostDecrementExpression or (int)SyntaxKind.PreDecrementExpression,
            };
    }

    private static bool LooksLikeCodeBlock(List<string> lines)
    {
        if (lines.Count < 2)
        {
            return false;
        }

        var joined = string.Join("\n", lines);
        return joined.Contains(";") && HasCodeMarker(joined) && Parses("{\n" + joined + "\n}");
    }

    private static bool HasCodeMarker(string text) => CodeMarkers.Any(marker => text.Contains(marker));

    private static bool Parses(string statement) => Parse(statement) is not null;

    private static Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax? Parse(string statement)
    {
        var parsed = SyntaxFactory.ParseStatement(statement);
        return parsed.FullSpan.Length == statement.Length && !parsed.GetDiagnostics().Any() ? parsed : null;
    }
}
