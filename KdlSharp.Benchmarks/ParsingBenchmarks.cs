using BenchmarkDotNet.Attributes;
using KdlSharp;

[MemoryDiagnoser]
public class ParsingBenchmarks
{
    private readonly string simpleKdl = @"
node1 ""arg1"" key=""value""
node2 {
    child 123
}
";

    // ~1KB KDL document (approximately 1024 bytes)
    // Contains 25 nodes with arguments and properties
    private readonly string oneKbKdl = @"
package ""my-application"" version=""2.1.0"" license=""MIT"" {
    author ""John Doe"" email=""john.doe@example.com""
    repository ""https://github.com/example/my-app""
}

config environment=""production"" debug=#false timeout=30000 {
    server host=""api.example.com"" port=8443 ssl=#true
    database driver=""postgres"" host=""db.example.com"" port=5432 {
        pool min=5 max=100 idle-timeout=60000
        credentials user=""app_user"" encrypted=#true
    }
    cache provider=""redis"" host=""cache.example.com"" port=6379 ttl=3600
    logging level=""info"" format=""json"" rotate=#true max-size=10485760
}

features {
    authentication enabled=#true provider=""oauth2"" timeout=300
    rate-limiting enabled=#true requests=1000 window=60
    compression enabled=#true algorithm=""gzip"" level=6
    monitoring enabled=#true interval=30 metrics=#true
}

routes {
    api prefix=""/api/v1"" middleware=""auth,rate-limit,compress""
    static prefix=""/assets"" cache=86400 compress=#true
    health path=""/health"" public=#true interval=10
    metrics path=""/metrics"" public=#false interval=30
}
";

    private readonly string complexKdl = string.Join("\n", Enumerable.Range(0, 1000).Select(i =>
        $"node{i} \"arg{i}\" key{i}=\"value{i}\""));

    [Benchmark]
    public KdlDocument ParseSimple()
    {
        return KdlDocument.Parse(simpleKdl);
    }

    /// <summary>
    /// Parses a ~1KB KDL document.
    /// </summary>
    [Benchmark]
    public KdlDocument Parse1KB()
    {
        return KdlDocument.Parse(oneKbKdl);
    }

    [Benchmark]
    public KdlDocument ParseComplex()
    {
        return KdlDocument.Parse(complexKdl);
    }

    [Benchmark]
    public string Serialize()
    {
        var doc = KdlDocument.Parse(simpleKdl);
        return doc.ToKdlString();
    }
}

/// <summary>
/// Throughput benchmark for measuring documents processed per second.
/// </summary>
[MemoryDiagnoser]
public class ThroughputBenchmarks
{
    // ~1KB KDL document (same as ParsingBenchmarks.oneKbKdl)
    private readonly string oneKbKdl = @"
package ""my-application"" version=""2.1.0"" license=""MIT"" {
    author ""John Doe"" email=""john.doe@example.com""
    repository ""https://github.com/example/my-app""
}

config environment=""production"" debug=#false timeout=30000 {
    server host=""api.example.com"" port=8443 ssl=#true
    database driver=""postgres"" host=""db.example.com"" port=5432 {
        pool min=5 max=100 idle-timeout=60000
        credentials user=""app_user"" encrypted=#true
    }
    cache provider=""redis"" host=""cache.example.com"" port=6379 ttl=3600
    logging level=""info"" format=""json"" rotate=#true max-size=10485760
}

features {
    authentication enabled=#true provider=""oauth2"" timeout=300
    rate-limiting enabled=#true requests=1000 window=60
    compression enabled=#true algorithm=""gzip"" level=6
    monitoring enabled=#true interval=30 metrics=#true
}

routes {
    api prefix=""/api/v1"" middleware=""auth,rate-limit,compress""
    static prefix=""/assets"" cache=86400 compress=#true
    health path=""/health"" public=#true interval=10
    metrics path=""/metrics"" public=#false interval=30
}
";

    /// <summary>
    /// Measures throughput by parsing a ~1KB document.
    /// BenchmarkDotNet reports operations/second which equals documents/second.
    /// </summary>
    [Benchmark]
    public KdlDocument Throughput1KB()
    {
        return KdlDocument.Parse(oneKbKdl);
    }
}
