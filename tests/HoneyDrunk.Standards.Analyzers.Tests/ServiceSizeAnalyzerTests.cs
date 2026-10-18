using AwesomeAssertions;
using HoneyDrunk.Standards.Analyzers.Services;
using Xunit;

namespace HoneyDrunk.Standards.Analyzers.Tests;

public sealed class ServiceSizeAnalyzerTests
{
    [Fact]
    public async Task SmallService_ReportsNothing()
    {
        var diagnostics = await RunAsync(Service("OrderService", methods: 10, padding: 0));

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task TooManyMethods_IsReported()
    {
        var diagnostics = await RunAsync(Service("OrderService", methods: 11, padding: 0));

        diagnostics.Select(diagnostic => diagnostic.Id).Should().Equal("HD0106");
        diagnostics[0].GetMessage().Should().Contain("11 public or internal methods (limit 10)");
    }

    [Fact]
    public async Task PrivateHelpers_DoNotCount()
    {
        var source = Service("OrderService", methods: 10, padding: 0).Replace("}\n}", "}\n    private void Helper() { }\n}", StringComparison.Ordinal);

        var diagnostics = await RunAsync(source);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task TooManyLinesAcrossPartials_IsReported()
    {
        var first = "public partial class OrderDataService\n{\n" + new string('\n', 200) + "}\n";
        var second = "public partial class OrderDataService\n{\n" + new string('\n', 200) + "}\n";

        var diagnostics = await RunAsync(first + second);

        diagnostics.Select(diagnostic => diagnostic.Id).Should().Equal("HD0106");
        diagnostics[0].GetMessage().Should().Contain("lines (limit 300)");
    }

    [Fact]
    public async Task ConfiguredLimits_AreHonored()
    {
        var diagnostics = await AnalyzerHarness.RunAsync(
            new ServiceSizeAnalyzer(),
            Service("OrderService", methods: 11, padding: 0),
            withEntityFramework: false,
            fileOptions: new Dictionary<string, string> { [ServiceSizeAnalyzer.MaxMethodsOption] = "12" });

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task NonServiceClasses_AreIgnored()
    {
        var diagnostics = await RunAsync(Service("OrderMapper", methods: 30, padding: 400));

        diagnostics.Should().BeEmpty();
    }

    private static string Service(string name, int methods, int padding) =>
        $"public sealed class {name}\n{{\n" + new string('\n', padding)
        + string.Concat(Enumerable.Range(0, methods).Select(index => $"    public void Run{index}() {{ }}\n"))
        + "}\n";

    private static Task<IReadOnlyList<Microsoft.CodeAnalysis.Diagnostic>> RunAsync(string source) =>
        AnalyzerHarness.RunAsync(new ServiceSizeAnalyzer(), source, withEntityFramework: false);
}
