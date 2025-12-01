using KdlSharp.Serialization;

namespace KdlSharp.Demo.Examples;

public static class Serialization
{
    public static void Run()
    {
        Console.WriteLine("=== Serialization & Deserialization Example ===\n");

        // Define POCOs (Plain Old CLR Objects)
        var config = new AppConfig(
            Name: "MyApp",
            UseSsl: true,
            Ports: new[] { 80, 443 },
            Database: new DatabaseSettings(
                Provider: "postgres",
                Credentials: new Credentials("admin", "s3cr3t")));

        // Demo 1: Basic serialization with KebabCase (recommended for KDL)
        Console.WriteLine("--- Naming Policies Demo ---\n");
        DemoNamingPolicies();

        // Demo 2: Full round-trip serialization
        Console.WriteLine("--- Round-trip Serialization ---\n");

        // Create serializer with options
        var serializer = new KdlSerializer(new KdlSerializerOptions
        {
            RootNodeName = "app-config",
            PropertyNamingPolicy = KdlNamingPolicy.KebabCase
        });

        // Serialize to KDL
        string kdlText = serializer.Serialize(config);
        Console.WriteLine("Serialized KDL:");
        Console.WriteLine(kdlText);
        Console.WriteLine();

        // Deserialize back to object
        var roundTrip = serializer.Deserialize<AppConfig>(kdlText);
        Console.WriteLine("Round-trip successful:");
        Console.WriteLine($"  Name: {roundTrip.Name}");
        Console.WriteLine($"  UseSsl: {roundTrip.UseSsl}");
        Console.WriteLine($"  Ports: {(roundTrip.Ports != null ? string.Join(", ", roundTrip.Ports) : "null")}");
        Console.WriteLine($"  Database Provider: {roundTrip.Database?.Provider ?? "null"}");
        Console.WriteLine($"  Database User: {roundTrip.Database?.Credentials?.User ?? "null"}");

        // Note: Record equality uses reference equality for arrays, so we compare fields
        var portsEqual = roundTrip.Ports?.SequenceEqual(config.Ports ?? Array.Empty<int>()) ?? config.Ports == null;
        var allFieldsMatch = roundTrip.Name == config.Name &&
                             roundTrip.UseSsl == config.UseSsl &&
                             portsEqual &&
                             roundTrip.Database == config.Database;
        Console.WriteLine($"\nAll fields match: {allFieldsMatch}");
        Console.WriteLine();

        // Demo 3: Collection serialization
        Console.WriteLine("--- Collection Serialization Demo ---\n");
        DemoCollectionSerialization();
    }

    private static void DemoCollectionSerialization()
    {
        var serializer = new KdlSerializer(new KdlSerializerOptions
        {
            RootNodeName = "project",
            PropertyNamingPolicy = KdlNamingPolicy.KebabCase
        });

        // Demo arrays and simple collections
        var project = new SimpleProjectConfig(
            Name: "my-project",
            Tags: new[] { "web", "api", "dotnet" },
            Ports: new List<int> { 80, 443, 8080 });

        var kdl = serializer.Serialize(project);
        Console.WriteLine("Collection serialization:");
        Console.WriteLine(kdl);

        // Demonstrate round-trip
        var restored = serializer.Deserialize<SimpleProjectConfig>(kdl);
        Console.WriteLine("Round-trip verification:");
        Console.WriteLine($"  Name: {restored.Name}");
        Console.WriteLine($"  Tags: {string.Join(", ", restored.Tags ?? Array.Empty<string>())}");
        Console.WriteLine($"  Ports: {string.Join(", ", restored.Ports ?? new List<int>())}");
        Console.WriteLine();
    }

    private static void DemoNamingPolicies()
    {
        // Simple object to show naming policy effects
        var simpleConfig = new SimpleConfig("MyValue", true, 42);

        // Show all naming policies
        var policies = new[]
        {
            (KdlNamingPolicy.PascalCase, "PascalCase (C# style)"),
            (KdlNamingPolicy.CamelCase, "camelCase (JavaScript style)"),
            (KdlNamingPolicy.SnakeCase, "snake_case (Python style)"),
            (KdlNamingPolicy.KebabCase, "kebab-case (KDL recommended)"),
            (KdlNamingPolicy.None, "None (exact CLR names)")
        };

        foreach (var (policy, description) in policies)
        {
            var serializer = new KdlSerializer(new KdlSerializerOptions
            {
                RootNodeName = "config",
                PropertyNamingPolicy = policy
            });

            var kdl = serializer.Serialize(simpleConfig);
            Console.WriteLine($"{description}:");
            Console.WriteLine(kdl);
        }
    }
}

// POCO types - no KDL dependencies needed!
public sealed record Credentials(string User, string Password);
public sealed record DatabaseSettings(string Provider, Credentials Credentials);
public sealed record AppConfig(string Name, bool UseSsl, int[] Ports, DatabaseSettings Database);

// Simple config to demonstrate naming policies
public sealed record SimpleConfig(string MyPropertyName, bool IsEnabled, int MaxRetryCount);

// Collection serialization types
public sealed record SimpleProjectConfig(
    string Name,
    string[]? Tags,
    List<int>? Ports);

