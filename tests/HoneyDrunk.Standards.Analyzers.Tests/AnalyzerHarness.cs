using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HoneyDrunk.Standards.Analyzers.Tests;

/// <summary>
/// Compiles C# snippets against minimal Entity Framework stand-ins and returns the analyzer's diagnostics.
/// The analyzers match EF types by metadata name, so stand-ins with the same names behave like the real package.
/// </summary>
internal static class AnalyzerHarness
{
    public const string EntityFramework = """
        namespace Microsoft.EntityFrameworkCore
        {
            public class DbContext { }
            public class DbSet<TEntity> where TEntity : class { public void Add(TEntity entity) { } }
            public interface IDbContextFactory<TContext> where TContext : DbContext { TContext CreateDbContext(); }
            public interface IEntityTypeConfiguration<TEntity> where TEntity : class { void Configure(Metadata.Builders.EntityTypeBuilder<TEntity> builder); }
            public class ModelBuilder
            {
                public Metadata.Builders.EntityTypeBuilder<TEntity> Entity<TEntity>() where TEntity : class => new();
                public ModelBuilder ApplyConfiguration<TEntity>(IEntityTypeConfiguration<TEntity> configuration) where TEntity : class => this;
            }
            [System.AttributeUsage(System.AttributeTargets.Class)] public sealed class IndexAttribute : System.Attribute { public IndexAttribute(params string[] names) { } }
        }
        namespace Microsoft.EntityFrameworkCore.Metadata.Builders
        {
            public class EntityTypeBuilder<TEntity> where TEntity : class { public EntityTypeBuilder<TEntity> ToTable(string name) => this; }
        }
        namespace Microsoft.EntityFrameworkCore.Migrations { public abstract class Migration { } }
        namespace Microsoft.EntityFrameworkCore.Design { public interface IDesignTimeDbContextFactory<TContext> where TContext : DbContext { TContext CreateDbContext(string[] args); } }
        """;

    private static readonly ImmutableArray<MetadataReference> References = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
        .Split(Path.PathSeparator)
        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
        .ToImmutableArray();

    public static async Task<IReadOnlyList<Diagnostic>> RunAsync(
        DiagnosticAnalyzer analyzer,
        string source,
        bool withEntityFramework = true,
        IReadOnlyDictionary<string, string>? globalOptions = null,
        IReadOnlyDictionary<string, string>? fileOptions = null)
    {
        var trees = new List<SyntaxTree> { CSharpSyntaxTree.ParseText(source, path: "Source.cs") };
        if (withEntityFramework)
        {
            trees.Add(CSharpSyntaxTree.ParseText(EntityFramework, path: "EntityFramework.cs"));
        }

        var compilation = CSharpCompilation.Create(
            "AnalyzerTest",
            trees,
            References,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var compileErrors = compilation.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();
        if (compileErrors.Count > 0)
        {
            throw new InvalidOperationException("Test source does not compile: " + string.Join("; ", compileErrors));
        }

        var options = new AnalyzerOptions(
            ImmutableArray<AdditionalText>.Empty,
            new OptionsProvider(globalOptions ?? new Dictionary<string, string>(), fileOptions ?? new Dictionary<string, string>()));
        var results = await compilation
            .WithAnalyzers(ImmutableArray.Create(analyzer), options)
            .GetAnalyzerDiagnosticsAsync();
        return results.Where(diagnostic => diagnostic.Location.SourceTree?.FilePath == "Source.cs").ToList();
    }

    private sealed class OptionsProvider(IReadOnlyDictionary<string, string> global, IReadOnlyDictionary<string, string> file) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new Options(global);

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => new Options(file);

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => new Options(file);
    }

    private sealed class Options(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => values.TryGetValue(key, out value);
    }
}
