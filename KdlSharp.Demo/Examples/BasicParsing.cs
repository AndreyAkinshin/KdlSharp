using KdlSharp;

namespace KdlSharp.Demo.Examples;

public static class BasicParsing
{
    public static void Run()
    {
        Console.WriteLine("=== Basic Parsing Example ===\n");

        string kdl = @"
package ""my-app"" version=""1.0.0"" {
    author ""Alice"" email=""alice@example.com""
    dependencies {
        lodash ""^4.17.0""
        react ""^18.0.0""
    }
}
";

        // Parse the KDL document
        var doc = KdlDocument.Parse(kdl);

        // Access nodes
        var packageNode = doc.Nodes[0];
        Console.WriteLine($"Node name: {packageNode.Name}");
        Console.WriteLine($"First argument: {packageNode.Arguments[0].AsString()}");
        Console.WriteLine($"Version property: {packageNode.GetProperty("version")?.AsString()}");

        // Navigate children
        var author = packageNode.Children.First(n => n.Name == "author");
        Console.WriteLine($"Author: {author.Arguments[0].AsString()}");
        Console.WriteLine($"Email: {author.GetProperty("email")?.AsString()}");

        // Access dependencies
        var deps = packageNode.Children.First(n => n.Name == "dependencies");
        Console.WriteLine("\nDependencies:");
        foreach (var dep in deps.Children)
        {
            Console.WriteLine($"  - {dep.Name}: {dep.Arguments[0].AsString()}");
        }

        Console.WriteLine();
    }
}

