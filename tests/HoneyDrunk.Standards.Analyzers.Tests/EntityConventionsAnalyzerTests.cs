using AwesomeAssertions;
using HoneyDrunk.Standards.Analyzers.EntityFramework;
using Xunit;

namespace HoneyDrunk.Standards.Analyzers.Tests;

public sealed class EntityConventionsAnalyzerTests
{
    private const string Usings = """
        using System.ComponentModel.DataAnnotations;
        using System.ComponentModel.DataAnnotations.Schema;
        using Microsoft.EntityFrameworkCore;
        using Microsoft.EntityFrameworkCore.Metadata.Builders;
        """;

    [Fact]
    public async Task CompliantModel_ReportsNothing()
    {
        var diagnostics = await RunAsync("""
            public sealed class OrderEntity { public int Id { get; set; } }
            public sealed class OrderEntityConfiguration : IEntityTypeConfiguration<OrderEntity>
            {
                public void Configure(EntityTypeBuilder<OrderEntity> builder) => builder.ToTable("Order");
            }
            public sealed class ShopDbContext : DbContext
            {
                public DbSet<OrderEntity> Orders { get; set; } = null!;
                protected void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyConfiguration(new OrderEntityConfiguration());
            }
            """);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task EntityWithoutSuffix_IsReported()
    {
        var diagnostics = await RunAsync("""
            public sealed class Order { public int Id { get; set; } }
            public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
            {
                public void Configure(EntityTypeBuilder<Order> builder) => builder.ToTable("Order");
            }
            public sealed class ShopDbContext : DbContext { public DbSet<Order> Orders { get; set; } = null!; }
            """);

        diagnostics.Select(diagnostic => diagnostic.Id).Should().Equal("HD0101");
        diagnostics[0].GetMessage().Should().Contain("'OrderEntity'");
    }

    [Fact]
    public async Task EntityKnownOnlyFromConfiguration_IsStillAnEntity()
    {
        var diagnostics = await RunAsync("""
            public sealed class Invoice { }
            public sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
            {
                public void Configure(EntityTypeBuilder<Invoice> builder) => builder.ToTable("Invoice");
            }
            """);

        diagnostics.Select(diagnostic => diagnostic.Id).Should().Equal("HD0101");
    }

    [Fact]
    public async Task MappingAttributesOnEntity_AreReportedOncePerAttribute()
    {
        var diagnostics = await RunAsync("""
            [Table("Order")]
            [Index("Number")]
            public sealed class OrderEntity
            {
                [Key] public int Id { get; set; }
                [Required, MaxLength(20)] public string Number { get; set; } = "";
                [Column("total")] public decimal Total { get; set; }
            }
            public sealed class OrderEntityConfiguration : IEntityTypeConfiguration<OrderEntity>
            {
                public void Configure(EntityTypeBuilder<OrderEntity> builder) => builder.ToTable("Order");
            }
            """);

        diagnostics.Select(diagnostic => diagnostic.Id).Should().AllBe("HD0103").And.HaveCount(6);
        diagnostics.Select(diagnostic => diagnostic.GetMessage()).Should().Contain(message => message.Contains("[Key]"));
    }

    [Fact]
    public async Task ValidationAttributesOnContracts_AreNotReported()
    {
        var diagnostics = await RunAsync("""
            public sealed class CreateOrderRequest { [Required, MaxLength(20)] public string Number { get; set; } = ""; }
            """);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task DbSetWithoutConfigurationClass_IsReported()
    {
        var diagnostics = await RunAsync("""
            public sealed class OrderEntity { }
            public sealed class ShopDbContext : DbContext { public DbSet<OrderEntity> Orders { get; set; } = null!; }
            """);

        diagnostics.Select(diagnostic => diagnostic.Id).Should().Equal("HD0104");
    }

    [Fact]
    public async Task InlineModelBuilderConfiguration_IsReported()
    {
        var diagnostics = await RunAsync("""
            public sealed class OrderEntity { }
            public sealed class OrderEntityConfiguration : IEntityTypeConfiguration<OrderEntity>
            {
                public void Configure(EntityTypeBuilder<OrderEntity> builder) => builder.ToTable("Order");
            }
            public sealed class ShopDbContext : DbContext
            {
                public DbSet<OrderEntity> Orders { get; set; } = null!;
                protected void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.Entity<OrderEntity>().ToTable("Orders");
            }
            """);

        diagnostics.Select(diagnostic => diagnostic.Id).Should().Equal("HD0105");
    }

    [Fact]
    public async Task TestProjects_AreSkipped()
    {
        var diagnostics = await AnalyzerHarness.RunAsync(
            new EntityConventionsAnalyzer(),
            Usings + "\npublic sealed class Probe { }\npublic sealed class ProbeDbContext : DbContext { public DbSet<Probe> Probes { get; set; } = null!; }",
            globalOptions: new Dictionary<string, string> { ["build_property.HD_IsGridTestProject"] = "true" });

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task ProjectWithoutEntityFramework_IsSkipped()
    {
        var diagnostics = await AnalyzerHarness.RunAsync(
            new EntityConventionsAnalyzer(),
            "public sealed class Order { [System.ComponentModel.DataAnnotations.Key] public int Id { get; set; } }",
            withEntityFramework: false);

        diagnostics.Should().BeEmpty();
    }

    private static Task<IReadOnlyList<Microsoft.CodeAnalysis.Diagnostic>> RunAsync(string source) =>
        AnalyzerHarness.RunAsync(new EntityConventionsAnalyzer(), Usings + "\n" + source);
}
