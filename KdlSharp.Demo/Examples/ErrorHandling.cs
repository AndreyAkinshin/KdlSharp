using KdlSharp;
using KdlSharp.Exceptions;

namespace KdlSharp.Demo.Examples;

public static class ErrorHandling
{
    public static void Run()
    {
        Console.WriteLine("=== Error Handling Example ===\n");

        // Example 1: Parse error with detailed context
        Console.WriteLine("Example 1: Parse error");
        try
        {
            string invalidKdl = @"
node {
    property ""unterminated
}";
            KdlDocument.Parse(invalidKdl);
        }
        catch (KdlParseException ex)
        {
            Console.WriteLine($"Parse failed at line {ex.Line}, column {ex.Column}");
            Console.WriteLine($"Error: {ex.Message}");
            if (ex.SourceContext != null)
            {
                Console.WriteLine($"\nContext:\n{ex.SourceContext}");
            }
        }

        Console.WriteLine();

        // Example 2: Version conflict error
        Console.WriteLine("Example 2: Version conflict");
        try
        {
            // Using v1 syntax (bare keywords) with default (v2) parser
            string v1Kdl = "node true false null";
            KdlDocument.Parse(v1Kdl);
        }
        catch (KdlParseException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        Console.WriteLine();

        // Example 3: TryParse pattern
        Console.WriteLine("Example 3: TryParse pattern");
        string maybeValidKdl = "node #true";

        if (KdlDocument.TryParse(maybeValidKdl, out var doc))
        {
            Console.WriteLine($"Successfully parsed: {doc.Nodes.Count} node(s)");
        }
        else
        {
            Console.WriteLine("Parse failed (no exception thrown)");
        }

        Console.WriteLine();

        // Example 4: TryParse with error details
        Console.WriteLine("Example 4: TryParse with error details");
        string invalidKdl2 = "node true";  // v1 syntax

        if (KdlDocument.TryParse(invalidKdl2, out var doc2, out var error))
        {
            Console.WriteLine($"Successfully parsed");
        }
        else
        {
            Console.WriteLine($"Parse failed: {error?.Message ?? "Unknown error"}");
        }

        Console.WriteLine();
    }
}

