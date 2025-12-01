using KdlSharp;
using KdlSharp.Extensions;
using KdlSharp.Settings;

namespace KdlSharp.Demo.Examples;

public static class AdvancedUsage
{
    public static void Run()
    {
        Console.WriteLine("=== Advanced Usage Example ===\n");

        // Example 1: Programmatic document construction
        Console.WriteLine("Example 1: Programmatic construction");
        var doc = new KdlDocument();

        doc.Nodes.Add(
            new KdlNode("server")
                .AddProperty("host", "localhost")
                .AddProperty("port", 8080)
                .AddChild(
                    new KdlNode("database")
                        .AddProperty("connection", "postgres://localhost/mydb")
                )
        );

        Console.WriteLine(doc.ToKdlString());

        // Example 2: Using extension methods
        Console.WriteLine("\nExample 2: Extension methods");
        var serverNode = doc.FindNode("server");
        if (serverNode != null)
        {
            var port = serverNode.GetPropertyValue<int>("port");
            Console.WriteLine($"Server port: {port}");

            var dbNode = serverNode.FindNode("database");
            if (dbNode != null)
            {
                Console.WriteLine($"Found database node: {dbNode.Name}");
            }
        }

        // Example 3: Version-aware parsing
        Console.WriteLine("\nExample 3: Version-aware parsing");

        // Parse v1 document
        string v1Kdl = "config true false null";
        var v1Settings = new KdlParserSettings { TargetVersion = KdlVersion.V1 };
        var v1Doc = KdlDocument.Parse(v1Kdl, v1Settings);
        Console.WriteLine($"Parsed v1 document: {v1Doc.Version}");

        // Parse v2 document (default)
        string v2Kdl = "config #true #false #null";
        var v2Doc = KdlDocument.Parse(v2Kdl);
        Console.WriteLine($"Parsed v2 document: {v2Doc.Version}");

        // Example 4: Custom formatting
        Console.WriteLine("\nExample 4: Custom formatting");
        var formatSettings = new KdlFormatterSettings
        {
            Indentation = "  ",  // 2 spaces
            Compact = false,
            PreferIdentifierStrings = true
        };

        Console.WriteLine(doc.ToKdlString(formatSettings));

        // Example 5: Type annotations
        Console.WriteLine("\nExample 5: Type annotations");

        // Parse document with type annotations
        string kdlWithAnnotations = @"
(person)user ""John"" age=(u32)30
(date)created ""2024-01-01""
(uuid)id ""550e8400-e29b-41d4-a716-446655440000""
";
        var annotatedDoc = KdlDocument.Parse(kdlWithAnnotations);

        // Access type annotations from parsed values
        var userNode = annotatedDoc.Nodes[0];
        Console.WriteLine($"Node type annotation: {userNode.TypeAnnotation?.TypeName ?? "none"}");
        Console.WriteLine($"Age property type annotation: {userNode.GetProperty("age")?.TypeAnnotation?.TypeName ?? "none"}");

        // Create nodes with type annotations programmatically
        var typedDoc = new KdlDocument();
        var typedNode = new KdlNode("data")
        {
            TypeAnnotation = new KdlAnnotation("record")
        };

        // Add a value with type annotation
        var dateValue = new KdlSharp.Values.KdlString("2024-12-01")
        {
            TypeAnnotation = new KdlAnnotation("date")
        };
        typedNode.AddArgument(dateValue);

        // Add a property with type annotation
        var countValue = new KdlSharp.Values.KdlNumber(42)
        {
            TypeAnnotation = new KdlAnnotation("i32")
        };
        typedNode.Properties.Add(new KdlProperty("count", countValue));

        typedDoc.Nodes.Add(typedNode);
        Console.WriteLine("Document with programmatic annotations:");
        Console.WriteLine(typedDoc.ToKdlString());

        Console.WriteLine();
    }
}

