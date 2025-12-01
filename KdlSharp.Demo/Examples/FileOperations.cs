using KdlSharp;
using KdlSharp.Settings;

namespace KdlSharp.Demo.Examples;

/// <summary>
/// Demonstrates file I/O operations for KDL documents.
/// </summary>
public static class FileOperations
{
    public static void Run()
    {
        Console.WriteLine("=== File Operations Example ===\n");

        // Create a temporary directory for demo files
        var tempDir = Path.Combine(Path.GetTempPath(), "kdlsharp-fileops-demo");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Demo 1: Basic file read/write
            DemoBasicFileOperations(tempDir);

            // Demo 2: Error handling for file operations
            DemoErrorHandling(tempDir);

            // Demo 3: Custom formatter settings
            DemoFormatterSettings(tempDir);
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }

        Console.WriteLine();
    }

    private static void DemoBasicFileOperations(string tempDir)
    {
        Console.WriteLine("--- Basic File Operations ---\n");

        var filePath = Path.Combine(tempDir, "config.kdl");

        // Create a document
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("application")
            .AddArgument("my-app")
            .AddProperty("version", "1.0.0"));

        var settings = new KdlNode("settings");
        settings.AddProperty("debug", KdlSharp.Values.KdlBoolean.True);
        settings.AddProperty("max-connections", new KdlSharp.Values.KdlNumber(100));
        doc.Nodes.Add(settings);

        // Save to file
        doc.Save(filePath);
        Console.WriteLine($"Saved document to: {filePath}");

        // Read from file
        var loadedDoc = KdlDocument.ParseFile(filePath);
        Console.WriteLine($"Loaded document with {loadedDoc.Nodes.Count} nodes");

        // Verify content
        var app = loadedDoc.Nodes[0];
        Console.WriteLine($"Application: {app.Arguments[0].AsString()} v{app.GetProperty("version")?.AsString()}");

        var loadedSettings = loadedDoc.Nodes[1];
        Console.WriteLine($"Debug mode: {loadedSettings.GetProperty("debug")?.AsBoolean()}");
        Console.WriteLine($"Max connections: {loadedSettings.GetProperty("max-connections")?.AsNumber()}");

        Console.WriteLine();
    }

    private static void DemoErrorHandling(string tempDir)
    {
        Console.WriteLine("--- Error Handling for File Operations ---\n");

        // Attempt to read non-existent file
        var nonExistentPath = Path.Combine(tempDir, "does-not-exist.kdl");
        try
        {
            KdlDocument.ParseFile(nonExistentPath);
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"Expected error: File not found - {Path.GetFileName(ex.FileName)}");
        }

        // Create a file with invalid KDL content
        var invalidPath = Path.Combine(tempDir, "invalid.kdl");
        File.WriteAllText(invalidPath, "invalid { unclosed");

        try
        {
            KdlDocument.ParseFile(invalidPath);
        }
        catch (KdlSharp.Exceptions.KdlParseException ex)
        {
            Console.WriteLine($"Expected error: Parse error - {ex.Message}");
        }

        Console.WriteLine();
    }

    private static void DemoFormatterSettings(string tempDir)
    {
        Console.WriteLine("--- Custom Formatter Settings ---\n");

        var filePath = Path.Combine(tempDir, "formatted.kdl");

        // Create a document with nested structure
        var doc = new KdlDocument();
        var root = new KdlNode("config");
        var child = new KdlNode("database");
        child.AddProperty("host", "localhost");
        child.AddProperty("port", new KdlSharp.Values.KdlNumber(5432));
        root.Children.Add(child);
        doc.Nodes.Add(root);

        // Save with tab indentation
        var tabSettings = new KdlFormatterSettings { Indentation = "\t" };
        doc.Save(filePath, tabSettings);
        Console.WriteLine("Saved with tab indentation:");
        Console.WriteLine(File.ReadAllText(filePath));

        // Save with 2-space indentation
        var twoSpaceSettings = new KdlFormatterSettings { Indentation = "  " };
        doc.Save(filePath, twoSpaceSettings);
        Console.WriteLine("Saved with 2-space indentation:");
        Console.WriteLine(File.ReadAllText(filePath));
    }
}
