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

    [Theory]
    [InlineData("public interface IOrders { ShopDbContext Context { get; } }")]
    [InlineData("public interface IOrders { DbSet<OrderEntity> Orders { get; } }")]
    [InlineData("public interface IOrders { void Load(IDbContextFactory<ShopDbContext> factory); }")]
    [InlineData("public sealed class OrderService { private ShopDbContext _db = null!; }")]
    [InlineData("public sealed class OrderService { public void Load(ShopDbContext db) { } }")]
    [InlineData("public sealed class OrderService(ShopDbContext db) { }")]
    [InlineData("public sealed class OrderService { public ShopDbContext Load() => null!; }")]
    public async Task UnusedDataAccessDeclaration_IsReported(string source)
    {
        var diagnostics = await RunAsync(source);

        diagnostics.Select(diagnostic => diagnostic.Id).Should().Equal("HD0102");
    }

    [Theory]
    [InlineData("public sealed class OrderService { public void Load(ShopDbContext db) { db.Orders.Add(new OrderEntity()); } }")]
    [InlineData("public sealed class OrderService { public ShopDbContext Context => new ShopDbContext(); }")]
    [InlineData("public sealed class OrderService { private ShopDbContext _db = new ShopDbContext(); }")]
    public async Task DeclarationAndOperations_ReportOnlyOncePerMember(string source)
    {
        var diagnostics = await RunAsync(source);

        diagnostics.Select(diagnostic => diagnostic.Id).Should().Equal("HD0102");
    }

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

        // The constructor declares the dependency and Place uses it: two distinct members.
        diagnostics.Select(diagnostic => diagnostic.Id).Should().Equal("HD0102", "HD0102");
        diagnostics.Select(diagnostic => diagnostic.Location.SourceSpan).Distinct().Should().HaveCount(2);
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

        diagnostics.Select(diagnostic => diagnostic.Id).Should().Equal("HD0102", "HD0102");
    }

    [Fact]
    public async Task ConstructingTheContextOutsideDataService_IsReported()
    {
        var diagnostics = await RunAsync("public static class Seeder { public static object Run() => new ShopDbContext(); }");

        diagnostics.Select(diagnostic => diagnostic.Id).Should().Equal("HD0102");
    }

    [Theory]
    [InlineData("public sealed class Seeder { public object Run() { ShopDbContext Create() => new ShopDbContext(); return Create(); } }")]
    [InlineData("public sealed class Seeder { public object Run() { System.Func<ShopDbContext> create = () => new ShopDbContext(); return create; } }")]
    public async Task NestedFunctionReturningContext_IsStillReported(string source)
    {
        var diagnostics = await RunAsync(source);

        diagnostics.Should().NotBeEmpty();
        diagnostics.Select(diagnostic => diagnostic.Id).Should().AllBe("HD0102");
    }

    [Fact]
    public async Task ContextReturnedByAMethod_IsReported()
    {
        var diagnostics = await RunAsync("""
            public sealed class ContextSource { public ShopDbContext Get() => null!; }
            public sealed class OrderService(ContextSource source) { public object Orders() => source.Get().Orders; }
            """);

        diagnostics.Select(diagnostic => diagnostic.Id).Should().Contain("HD0102");
        diagnostics.Select(diagnostic => diagnostic.GetMessage()).Should().Contain(message => message.Contains("'OrderService'"));
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
