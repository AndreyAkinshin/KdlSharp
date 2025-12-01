using KdlSharp;
using KdlSharp.Query;

namespace KdlSharp.Demo.Examples;

public static class Queries
{
    public static void Run()
    {
        Console.WriteLine("=== Query Language Example ===\n");

        var kdl = @"
            package {
                name ""my-app""
                version ""1.0.0""
                dependencies platform=""windows"" {
                    winapi ""1.0.0"" path=""./crates/my-fork""
                }
                dependencies {
                    miette ""2.0.0"" dev=#true
                }
            }
        ";

        var doc = KdlDocument.Parse(kdl);

        // Example 1: Find nodes by name
        Console.WriteLine("1. Find all 'dependencies' nodes:");
        var deps = KdlQuery.Execute(doc, "dependencies");
        foreach (var dep in deps)
        {
            Console.WriteLine($"   - {dep.Name}");
        }

        // Example 2: Find with property filter
        Console.WriteLine("\n2. Find dependencies with 'platform' property:");
        var platformDeps = KdlQuery.Execute(doc, "dependencies[platform]");
        foreach (var dep in platformDeps)
        {
            var platform = dep.GetProperty("platform")?.AsString();
            Console.WriteLine($"   - {dep.Name} (platform={platform})");
        }

        // Example 3: Find direct children
        Console.WriteLine("\n3. Find direct children of package:");
        var packageChildren = KdlQuery.Execute(doc, "package > []");
        foreach (var child in packageChildren)
        {
            Console.WriteLine($"   - {child.Name}");
        }

        // Example 4: Find all descendants
        Console.WriteLine("\n4. Find all descendants of package:");
        var allDescendants = KdlQuery.Execute(doc, "package >> []");
        foreach (var node in allDescendants)
        {
            Console.WriteLine($"   - {node.Name}");
        }

        // Example 5: Complex query
        Console.WriteLine("\n5. Find winapi node (child of dependencies):");
        var winapi = KdlQuery.Execute(doc, "dependencies >> [name() ^= \"win\"]");
        foreach (var node in winapi)
        {
            Console.WriteLine($"   - {node.Name}");
        }

        // Example 6: Compile and reuse query
        Console.WriteLine("\n6. Compiled query (reusable):");
        var compiled = KdlQuery.Compile("package >> name");
        var names = compiled.Execute(doc);
        foreach (var node in names)
        {
            Console.WriteLine($"   - Found: {node.Name}");
        }

        Console.WriteLine();
    }
}

