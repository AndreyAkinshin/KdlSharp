using KdlSharp;
using KdlSharp.Schema;
using KdlSharp.Schema.Rules;

namespace KdlSharp.Demo.Examples;

public static class SchemaValidation
{
    public static void Run()
    {
        Console.WriteLine("=== Schema Validation Example ===\n");

        // Example 1: Validate with permissive schema
        Console.WriteLine("1. Permissive Schema:");
        var doc1 = KdlDocument.Parse("node value=\"anything\"");
        var permissiveSchema = KdlSchema.CreatePermissiveSchema();
        var result1 = KdlSchema.Validate(doc1, permissiveSchema);
        Console.WriteLine($"   Valid: {result1.IsValid}");

        // Example 2: Validate required properties
        Console.WriteLine("\n2. Required Properties:");
        var doc2 = KdlDocument.Parse(@"
            package name=""my-app"" version=""1.0.0""
        ");

        var packageNode = new SchemaNode(
            name: "package",
            properties: new[]
            {
                new SchemaProperty("name", required: true),
                new SchemaProperty("version", required: true)
            });

        var schema2 = new SchemaDocument(
            new SchemaInfo { Title = "Package Schema" },
            nodes: new[] { packageNode });

        var result2 = KdlSchema.Validate(doc2, schema2);
        Console.WriteLine($"   Valid: {result2.IsValid}");
        Console.WriteLine($"   Errors: {result2.Errors.Count}");

        // Example 3: Validate with validation rules
        Console.WriteLine("\n3. String Length Validation:");
        var doc3 = KdlDocument.Parse("user email=\"test@example.com\"");

        var userNode = new SchemaNode(
            name: "user",
            properties: new[]
            {
                new SchemaProperty("email",
                    required: true,
                    validationRules: new ValidationRule[]
                    {
                        new MinLengthRule(5),
                        new PatternRule(@"^[^@]+@[^@]+\.[^@]+$")
                    })
            });

        var schema3 = new SchemaDocument(
            new SchemaInfo { Title = "User Schema" },
            nodes: new[] { userNode });

        var result3 = KdlSchema.Validate(doc3, schema3);
        Console.WriteLine($"   Valid: {result3.IsValid}");

        // Example 4: Show validation errors
        Console.WriteLine("\n4. Validation Errors:");
        var doc4 = KdlDocument.Parse("node");

        var strictNode = new SchemaNode(
            name: "node",
            properties: new[]
            {
                new SchemaProperty("required-prop", required: true)
            });

        var schema4 = new SchemaDocument(
            new SchemaInfo { Title = "Strict Schema" },
            nodes: new[] { strictNode });

        var result4 = KdlSchema.Validate(doc4, schema4);
        Console.WriteLine($"   Valid: {result4.IsValid}");
        if (!result4.IsValid)
        {
            Console.WriteLine("   Errors:");
            foreach (var error in result4.Errors)
            {
                Console.WriteLine($"     - [{error.RuleName}] {error.Path}: {error.Message}");
            }
        }

        Console.WriteLine();
    }
}

