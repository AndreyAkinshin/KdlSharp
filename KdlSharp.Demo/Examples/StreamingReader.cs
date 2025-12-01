using KdlSharp;
using KdlSharp.Parsing;

namespace KdlSharp.Demo.Examples;

/// <summary>
/// Demonstrates low-level streaming KDL parsing with KdlReader.
/// </summary>
public static class StreamingReaderDemo
{
    public static void Run()
    {
        Console.WriteLine("=== KdlReader Streaming Demo ===\n");

        // Example 1: Basic token-by-token reading
        BasicTokenReading();

        // Example 2: Processing large files efficiently
        LargeFileProcessing();

        // Example 3: Custom deserialization with streaming
        CustomDeserialization();
    }

    private static void BasicTokenReading()
    {
        Console.WriteLine("Example 1: Basic Token Reading");
        Console.WriteLine("--------------------------------");

        var kdl = @"
package ""myapp"" version=""1.0.0"" {
    author ""Alice"" email=""alice@example.com""
    dependencies {
        lodash ""^4.17.0""
        react ""^18.0.0""
    }
}";

        using var reader = new StringReader(kdl);
        using var kdlReader = new KdlReader(reader);

        Console.WriteLine("Tokens:");
        while (kdlReader.Read())
        {
            var location = $"[{kdlReader.Line}:{kdlReader.Column}]";

            switch (kdlReader.TokenType)
            {
                case KdlTokenType.String:
                    Console.WriteLine($"  {location} String: \"{kdlReader.StringValue}\"");
                    break;

                case KdlTokenType.Number:
                    Console.WriteLine($"  {location} Number: {kdlReader.NumberValue}");
                    break;

                case KdlTokenType.True:
                case KdlTokenType.False:
                    Console.WriteLine($"  {location} Boolean: {kdlReader.BooleanValue}");
                    break;

                case KdlTokenType.Null:
                    Console.WriteLine($"  {location} Null");
                    break;

                case KdlTokenType.OpenBrace:
                    Console.WriteLine($"  {location} {{");
                    break;

                case KdlTokenType.CloseBrace:
                    Console.WriteLine($"  {location} }}");
                    break;

                case KdlTokenType.Newline:
                    Console.WriteLine($"  {location} <newline>");
                    break;
            }
        }

        Console.WriteLine();
    }

    private static void LargeFileProcessing()
    {
        Console.WriteLine("Example 2: Large File Processing");
        Console.WriteLine("---------------------------------");

        // Simulate a large KDL file with many records
        var recordCount = 1000;
        var largeKdl = string.Join("\n",
            Enumerable.Range(1, recordCount)
                .Select(i => $"record id={i} name=\"Record {i}\" active=#true"));

        Console.WriteLine($"Processing {recordCount} records with streaming...");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var processedCount = 0;

        using var reader = new StringReader(largeKdl);
        using var kdlReader = new KdlReader(reader);

        // Count records without loading entire document into memory
        while (kdlReader.Read())
        {
            if (kdlReader.TokenType == KdlTokenType.String &&
                kdlReader.StringValue == "record")
            {
                processedCount++;
            }
        }

        stopwatch.Stop();

        Console.WriteLine($"Processed {processedCount} records in {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Memory efficient: Tokens processed one at a time\n");
    }

    private static void CustomDeserialization()
    {
        Console.WriteLine("Example 3: Custom Deserialization");
        Console.WriteLine("----------------------------------");

        // For custom deserialization, using KdlDocument.Parse is the recommended approach.
        // The streaming KdlReader is lower-level and best for counting/filtering tokens.
        // Here we demonstrate extracting properties from a parsed document.

        var kdl = @"
config {
    server host=""localhost"" port=8080
    database connection=""postgres://localhost/mydb""
    features {
        caching #true
        logging #true
        metrics #false
    }
}";

        var doc = KdlDocument.Parse(kdl);
        var config = new Dictionary<string, string>();

        // Extract all properties from the document recursively
        void ExtractProperties(KdlNode node)
        {
            foreach (var prop in node.Properties)
            {
                var value = prop.Value.ValueType switch
                {
                    KdlValueType.String => prop.Value.AsString(),
                    KdlValueType.Number => prop.Value.AsNumber()?.ToString(),
                    KdlValueType.Boolean => prop.Value.AsBoolean()?.ToString()?.ToLower(),
                    _ => null
                };

                if (value != null)
                {
                    config[prop.Key] = value;
                }
            }

            foreach (var child in node.Children)
            {
                ExtractProperties(child);
            }
        }

        foreach (var node in doc.Nodes)
        {
            ExtractProperties(node);
        }

        Console.WriteLine("Extracted configuration:");
        foreach (var (key, value) in config.OrderBy(kv => kv.Key))
        {
            Console.WriteLine($"  {key} = {value}");
        }

        Console.WriteLine();
    }

    // Advanced: Process stream of values
    public static void ReadValues(string kdl)
    {
        Console.WriteLine("Example: Reading Values");
        Console.WriteLine("-----------------------");

        using var reader = new StringReader(kdl);
        using var kdlReader = new KdlReader(reader);

        var values = new List<KdlValue>();

        while (kdlReader.Read())
        {
            if (kdlReader.TokenType is
                KdlTokenType.String or
                KdlTokenType.Number or
                KdlTokenType.True or
                KdlTokenType.False or
                KdlTokenType.Null)
            {
                var value = kdlReader.ReadValue();
                if (value != null)
                {
                    values.Add(value);
                }
            }
        }

        Console.WriteLine($"Read {values.Count} values");
        foreach (var value in values.Take(10))
        {
            Console.WriteLine($"  - {value.ValueType}: {value}");
        }

        if (values.Count > 10)
        {
            Console.WriteLine($"  ... and {values.Count - 10} more");
        }

        Console.WriteLine();
    }
}

