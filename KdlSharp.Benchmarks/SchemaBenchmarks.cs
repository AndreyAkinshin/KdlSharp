using BenchmarkDotNet.Attributes;
using KdlSharp;
using KdlSharp.Schema;
using KdlSharp.Schema.Rules;

/// <summary>
/// Benchmarks for KDL Schema validation operations.
/// </summary>
[MemoryDiagnoser]
public class SchemaBenchmarks
{
    private KdlDocument smallDoc = null!;
    private KdlDocument mediumDoc = null!;
    private KdlDocument largeDoc = null!;

    private SchemaDocument simpleSchema = null!;
    private SchemaDocument complexSchema = null!;
    private SchemaDocument permissiveSchema = null!;

    private readonly string smallKdl = @"
package name=""my-app"" version=""1.0.0""
";

    private readonly string mediumKdl = @"
config {
    server host=""localhost"" port=8080 ssl=#true {
        timeout 30000
        max-connections 100
    }
    database driver=""postgres"" host=""db.example.com"" port=5432 {
        pool min=5 max=50
        credentials user=""app_user"" password=""secret""
    }
}

application name=""my-app"" version=""2.0.0"" {
    author ""John Doe"" email=""john@example.com""
    license ""MIT""
}
";

    private readonly string largeKdl;

    public SchemaBenchmarks()
    {
        // Generate a larger document for stress testing
        var nodes = new List<string>();
        for (int i = 0; i < 100; i++)
        {
            nodes.Add($@"
item{i} name=""item-{i}"" value={i} enabled=#true {{
    metadata created=""2024-01-01"" updated=""2024-12-01""
    tags ""tag1"" ""tag2"" ""tag3""
}}");
        }
        largeKdl = string.Join("\n", nodes);
    }

    [GlobalSetup]
    public void Setup()
    {
        // Parse documents
        smallDoc = KdlDocument.Parse(smallKdl);
        mediumDoc = KdlDocument.Parse(mediumKdl);
        largeDoc = KdlDocument.Parse(largeKdl);

        // Create schemas
        permissiveSchema = KdlSchema.CreatePermissiveSchema();

        // Simple schema with required properties
        simpleSchema = new SchemaDocument(
            new SchemaInfo { Title = "Simple Schema" },
            nodes: new[]
            {
                new SchemaNode("package",
                    properties: new[]
                    {
                        new SchemaProperty("name", required: true),
                        new SchemaProperty("version", required: true)
                    })
            });

        // Complex schema with validation rules
        complexSchema = new SchemaDocument(
            new SchemaInfo { Title = "Complex Schema" },
            nodes: new[]
            {
                new SchemaNode("config",
                    children: new SchemaChildren(new[]
                    {
                        new SchemaNode("server",
                            properties: new[]
                            {
                                new SchemaProperty("host", required: true),
                                new SchemaProperty("port", required: true,
                                    validationRules: new ValidationRule[]
                                    {
                                        new GreaterThanRule(0),
                                        new LessThanRule(65536)
                                    }),
                                new SchemaProperty("ssl")
                            }),
                        new SchemaNode("database",
                            properties: new[]
                            {
                                new SchemaProperty("driver", required: true),
                                new SchemaProperty("host", required: true),
                                new SchemaProperty("port",
                                    validationRules: new ValidationRule[]
                                    {
                                        new GreaterThanRule(0),
                                        new LessThanRule(65536)
                                    })
                            })
                    })),
                new SchemaNode("application",
                    properties: new[]
                    {
                        new SchemaProperty("name", required: true,
                            validationRules: new ValidationRule[]
                            {
                                new MinLengthRule(1),
                                new MaxLengthRule(100)
                            }),
                        new SchemaProperty("version", required: true,
                            validationRules: new ValidationRule[]
                            {
                                new PatternRule(@"^\d+\.\d+\.\d+$")
                            }),
                        new SchemaProperty("license")
                    },
                    children: new SchemaChildren(new[]
                    {
                        new SchemaNode("author",
                            properties: new[]
                            {
                                new SchemaProperty("email",
                                    validationRules: new ValidationRule[]
                                    {
                                        new PatternRule(@"^[^@]+@[^@]+\.[^@]+$")
                                    })
                            })
                    }))
            });
    }

    // === Simple Schema Validation ===

    /// <summary>
    /// Validates a small document against a simple schema with required properties.
    /// </summary>
    [Benchmark]
    public ValidationResult ValidateSimpleSchema()
    {
        return KdlSchema.Validate(smallDoc, simpleSchema);
    }

    /// <summary>
    /// Validates a small document against a permissive (no rules) schema.
    /// </summary>
    [Benchmark]
    public ValidationResult ValidatePermissiveSchema()
    {
        return KdlSchema.Validate(smallDoc, permissiveSchema);
    }

    // === Complex Schema Validation ===

    /// <summary>
    /// Validates a medium document against a complex schema with multiple rules.
    /// </summary>
    [Benchmark]
    public ValidationResult ValidateComplexSchema()
    {
        return KdlSchema.Validate(mediumDoc, complexSchema);
    }

    /// <summary>
    /// Validates a medium document against a permissive schema.
    /// </summary>
    [Benchmark]
    public ValidationResult ValidateMediumDocPermissive()
    {
        return KdlSchema.Validate(mediumDoc, permissiveSchema);
    }

    // === Large Document Validation ===

    /// <summary>
    /// Validates a large document (100 nodes) against a permissive schema.
    /// </summary>
    [Benchmark]
    public ValidationResult ValidateLargeDocPermissive()
    {
        return KdlSchema.Validate(largeDoc, permissiveSchema);
    }
}
