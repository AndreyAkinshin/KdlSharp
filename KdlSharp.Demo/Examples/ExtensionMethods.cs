using KdlSharp;
using KdlSharp.Extensions;

namespace KdlSharp.Demo.Examples;

/// <summary>
/// Demonstrates LINQ-like extension methods for KDL documents.
/// </summary>
public static class ExtensionMethods
{
    public static void Run()
    {
        Console.WriteLine("=== Extension Methods Example ===\n");

        // Parse a complex document for demonstration
        // Note: Properties use key=value syntax, child nodes use just a name
        string kdl = @"
application ""my-app"" version=""2.0.0"" {
    settings debug=#true log-level=""info"" {
        features experimental=#false beta=#true
    }
    database {
        primary host=""localhost"" port=5432 name=""mydb""
        replica host=""replica.local"" port=5432 name=""mydb""
    }
    server host=""0.0.0.0"" port=8080 workers=4
}
";
        var doc = KdlDocument.Parse(kdl);

        // Example 1: FindNode - Find first matching top-level node
        Console.WriteLine("--- FindNode (Document) ---");
        var app = doc.FindNode("application");
        if (app != null)
        {
            Console.WriteLine($"Found: {app.Name}");
            Console.WriteLine($"  First argument: {app.Arguments[0].AsString()}");
            Console.WriteLine($"  Version: {app.GetProperty("version")?.AsString()}");
        }
        Console.WriteLine();

        // Example 2: FindNodes - Find all matching nodes
        Console.WriteLine("--- FindNodes (Node) ---");
        var dbNode = app?.FindNode("database");
        if (dbNode != null)
        {
            // Find all database server nodes (primary, replica)
            Console.WriteLine("Database servers:");
            foreach (var child in dbNode.Children)
            {
                var host = child.GetPropertyValue<string>("host");
                var port = child.GetPropertyValue<int>("port");
                Console.WriteLine($"  - {child.Name}: {host}:{port}");
            }
        }
        Console.WriteLine();

        // Example 3: Descendants - Enumerate all descendant nodes
        Console.WriteLine("--- Descendants ---");
        if (app != null)
        {
            var descendants = app.Descendants().ToList();
            Console.WriteLine($"Total descendant nodes under 'application': {descendants.Count}");
            Console.WriteLine("Node names:");
            foreach (var node in descendants.Take(8))
            {
                Console.WriteLine($"  - {node.Name}");
            }
            if (descendants.Count > 8)
            {
                Console.WriteLine($"  ... and {descendants.Count - 8} more");
            }
        }
        Console.WriteLine();

        // Example 4: Ancestors - Navigate up the tree
        Console.WriteLine("--- Ancestors ---");
        var settings = app?.FindNode("settings");
        var features = settings?.FindNode("features");
        if (features != null)
        {
            var ancestorNames = features.Ancestors().Select(n => n.Name).ToList();
            Console.WriteLine($"Ancestors of 'features': {string.Join(" → ", ancestorNames)}");
        }
        Console.WriteLine();

        // Example 5: GetPropertyValue with type conversion
        Console.WriteLine("--- GetPropertyValue<T> ---");
        // Reuse the settings variable from Example 4
        if (settings != null)
        {
            var debug = settings.GetPropertyValue<bool>("debug");
            var logLevel = settings.GetPropertyValue<string>("log-level");
            Console.WriteLine($"Debug mode: {debug}");
            Console.WriteLine($"Log level: {logLevel}");
        }

        var server = app?.FindNode("server");
        if (server != null)
        {
            var port = server.GetPropertyValue<int>("port");
            var workers = server.GetPropertyValue<int>("workers");
            var timeout = server.GetPropertyValue<int>("timeout", 30); // with default
            Console.WriteLine($"Server port: {port}");
            Console.WriteLine($"Workers: {workers}");
            Console.WriteLine($"Timeout (with default): {timeout}");
        }
        Console.WriteLine();

        // Example 6: GetArgumentValue - Access arguments by index
        Console.WriteLine("--- GetArgumentValue<T> ---");
        if (app != null)
        {
            var appName = app.GetArgumentValue<string>(0);
            var secondArg = app.GetArgumentValue<string>(1, "no-second-arg"); // with default
            Console.WriteLine($"App name (arg 0): {appName}");
            Console.WriteLine($"Second arg (with default): {secondArg}");
        }
        Console.WriteLine();

        // Example 7: AllNodes - Get all nodes in document recursively
        Console.WriteLine("--- AllNodes (Document) ---");
        var allNodes = doc.AllNodes().ToList();
        Console.WriteLine($"Total nodes in document: {allNodes.Count}");

        // Group by name for summary
        var grouped = allNodes.GroupBy(n => n.Name)
            .OrderByDescending(g => g.Count())
            .Take(5);
        Console.WriteLine("Most common node names:");
        foreach (var group in grouped)
        {
            Console.WriteLine($"  - {group.Key}: {group.Count()}");
        }
        Console.WriteLine();

        // Example 8: HasProperty - Check if property exists
        Console.WriteLine("--- HasProperty ---");
        if (server != null)
        {
            Console.WriteLine($"Has 'host' property: {server.HasProperty("host")}");
            Console.WriteLine($"Has 'ssl' property: {server.HasProperty("ssl")}");
        }

        Console.WriteLine();
    }
}
