using AwesomeAssertions;
using HoneyDrunk.Standards.Analyzers.EntityFramework;
using Xunit;

namespace HoneyDrunk.Standards.Analyzers.Tests;

public sealed class DataAccessAnalyzerTests
{
    private const string Model = """
        using Microsoft.EntityFrameworkCore;
        public sealed class OrderEntity { }
        public sealed class ShopDbContext : DbContext { public DbSet<OrderEntity> Orders { get; set; } = null!; }
        """;

    [Fact]
    public async Task DataService_MayUseTheContext()
    {
        var diagnostics = await RunAsync("""
            public sealed class OrderDataService(ShopDbContext db)
            {
                public void Add(OrderEntity order) => db.Orders.Add(order);
            }
            """);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task BusinessService_UsingTheContext_IsReportedOncePerMember()
    {
        var diagnostics = await RunAsync("""
            public sealed class OrderService(ShopDbContext db)
            {
                public void Place(OrderEntity order)
                {
                    db.Orders.Add(order);
                    db.Orders.Add(order);
                }
            }
            """);

        diagnostics.Select(diagnostic => diagnostic.Id).Should().Equal("HD0102");
        diagnostics[0].GetMessage().Should().Contain("'OrderService' uses").And.Contain("*DataService");
    }

    [Fact]
    public async Task ContextFactoryOutsideDataService_IsReported()
    {
        var diagnostics = await RunAsync("""
            public sealed class ReportEndpoint(IDbContextFactory<ShopDbContext> factory)
            {
                public object Get() => factory.CreateDbContext();
            }
            """);

        diagnostics.Select(diagnostic => diagnostic.Id).Should().Equal("HD0102");
    }

    [Fact]
    public async Task EntityFrameworkInfrastructure_IsAllowed()
    {
        var diagnostics = await RunAsync("""
            public sealed class ShopDbContextFactory : Microsoft.EntityFrameworkCore.Design.IDesignTimeDbContextFactory<ShopDbContext>
            {
                public ShopDbContext CreateDbContext(string[] args) { var db = new ShopDbContext(); return db; }
            }
            """);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task NestedTypeInsideDataService_IsAllowed()
    {
        var diagnostics = await RunAsync("""
            public sealed class OrderDataService
            {
                private sealed class Batch(ShopDbContext db) { public void Add(OrderEntity order) => db.Orders.Add(order); }
            }
            """);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task UnitOfWork_MayUseTheContext()
    {
        var diagnostics = await RunAsync("public sealed class ShopUnitOfWork(ShopDbContext db) { public object Context => db; }");

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task DataInfrastructureProjects_AreSkipped()
    {
        var diagnostics = await AnalyzerHarness.RunAsync(
            new DataAccessAnalyzer(),
            Model + "\npublic sealed class EfRepository(ShopDbContext db) { public object Set => db.Orders; }",
            globalOptions: new Dictionary<string, string> { ["build_property.HD_IsDataInfrastructure"] = "true" });

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task TestProjects_AreSkipped()
    {
        var diagnostics = await AnalyzerHarness.RunAsync(
            new DataAccessAnalyzer(),
            Model + "\npublic sealed class OrderTests(ShopDbContext db) { public object Seed() => db.Orders; }",
            globalOptions: new Dictionary<string, string> { ["build_property.HD_IsGridTestProject"] = "true" });

        diagnostics.Should().BeEmpty();
    }

    private static Task<IReadOnlyList<Microsoft.CodeAnalysis.Diagnostic>> RunAsync(string source) =>
        AnalyzerHarness.RunAsync(new DataAccessAnalyzer(), Model + "\n" + source);
}
