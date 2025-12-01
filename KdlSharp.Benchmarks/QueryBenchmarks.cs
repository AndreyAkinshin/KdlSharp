using BenchmarkDotNet.Attributes;
using KdlSharp;
using KdlSharp.Query;

/// <summary>
/// Benchmarks for KDL Query Language operations.
/// </summary>
[MemoryDiagnoser]
public class QueryBenchmarks
{
    private KdlDocument smallDoc = null!;
    private KdlDocument mediumDoc = null!;
    private CompiledQuery compiledSimpleQuery = null!;
    private CompiledQuery compiledDescendantQuery = null!;
    private CompiledQuery compiledComplexQuery = null!;

    private readonly string smallKdl = @"
package ""my-app"" version=""1.0.0"" {
    author ""Alice""
    dependencies {
        lodash ""^4.17.0""
        react ""^18.0.0""
    }
}
";

    private readonly string mediumKdl = @"
config {
    server host=""localhost"" port=8080 {
        ssl enabled=#true cert=""server.crt""
        database {
            primary host=""db1.example.com"" port=5432
            replica host=""db2.example.com"" port=5432
        }
    }
    features {
        auth enabled=#true timeout=300
        cache enabled=#true ttl=3600
        logging level=""info"" format=""json""
    }
    routes {
        api prefix=""/api/v1"" {
            users path=""/users"" methods=""GET,POST,PUT,DELETE""
            products path=""/products"" methods=""GET,POST""
            orders path=""/orders"" methods=""GET,POST,PUT""
        }
        static prefix=""/assets"" cache=86400
        health path=""/health"" public=#true
    }
}
packages {
    frontend ""react"" ""^18.0"" {
        dependencies {
            react-dom ""^18.0""
            react-router ""^6.0""
        }
    }
    backend ""express"" ""^4.18"" {
        dependencies {
            cors ""^2.8""
            helmet ""^7.0""
        }
    }
}
";

    [GlobalSetup]
    public void Setup()
    {
        smallDoc = KdlDocument.Parse(smallKdl);
        mediumDoc = KdlDocument.Parse(mediumKdl);

        // Pre-compile queries for comparison
        compiledSimpleQuery = KdlQuery.Compile("package");
        compiledDescendantQuery = KdlQuery.Compile("config >> database");
        compiledComplexQuery = KdlQuery.Compile("packages >> dependencies || routes >> api");
    }

    // === Query Parsing Benchmarks ===

    /// <summary>
    /// Measures time to parse a simple query string (single selector).
    /// </summary>
    [Benchmark]
    public CompiledQuery ParseSimpleQuery()
    {
        return KdlQuery.Compile("package");
    }

    /// <summary>
    /// Measures time to parse a descendant query (>> operator).
    /// </summary>
    [Benchmark]
    public CompiledQuery ParseDescendantQuery()
    {
        return KdlQuery.Compile("config >> server >> database");
    }

    /// <summary>
    /// Measures time to parse a complex query (multiple operators).
    /// </summary>
    [Benchmark]
    public CompiledQuery ParseComplexQuery()
    {
        return KdlQuery.Compile("packages >> frontend >> dependencies || config >> features");
    }

    // === Query Execution Benchmarks (Small Document) ===

    /// <summary>
    /// Executes a simple top-level query on a small document.
    /// </summary>
    [Benchmark]
    public int ExecuteSimpleQuerySmallDoc()
    {
        return KdlQuery.Execute(smallDoc, "package").Count();
    }

    /// <summary>
    /// Executes a descendant query on a small document.
    /// </summary>
    [Benchmark]
    public int ExecuteDescendantQuerySmallDoc()
    {
        return KdlQuery.Execute(smallDoc, "package >> dependencies").Count();
    }

    // === Query Execution Benchmarks (Medium Document) ===

    /// <summary>
    /// Executes a simple top-level query on a medium document.
    /// </summary>
    [Benchmark]
    public int ExecuteSimpleQueryMediumDoc()
    {
        return KdlQuery.Execute(mediumDoc, "config").Count();
    }

    /// <summary>
    /// Executes a descendant query on a medium document.
    /// </summary>
    [Benchmark]
    public int ExecuteDescendantQueryMediumDoc()
    {
        return KdlQuery.Execute(mediumDoc, "config >> database").Count();
    }

    /// <summary>
    /// Executes a complex query with OR operator on a medium document.
    /// </summary>
    [Benchmark]
    public int ExecuteComplexQueryMediumDoc()
    {
        return KdlQuery.Execute(mediumDoc, "packages >> dependencies || routes >> api").Count();
    }

    // === Compiled Query Execution Benchmarks ===

    /// <summary>
    /// Executes a pre-compiled simple query (shows benefit of compilation).
    /// </summary>
    [Benchmark]
    public int ExecuteCompiledSimpleQuery()
    {
        return compiledSimpleQuery.Execute(smallDoc).Count();
    }

    /// <summary>
    /// Executes a pre-compiled descendant query.
    /// </summary>
    [Benchmark]
    public int ExecuteCompiledDescendantQuery()
    {
        return compiledDescendantQuery.Execute(mediumDoc).Count();
    }

    /// <summary>
    /// Executes a pre-compiled complex query.
    /// </summary>
    [Benchmark]
    public int ExecuteCompiledComplexQuery()
    {
        return compiledComplexQuery.Execute(mediumDoc).Count();
    }
}
