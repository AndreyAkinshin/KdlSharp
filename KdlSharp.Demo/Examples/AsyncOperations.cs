using KdlSharp;
using KdlSharp.Serialization;
using KdlSharp.Settings;

namespace KdlSharp.Demo.Examples;

/// <summary>
/// Demonstrates async operations for parsing and saving KDL documents.
/// </summary>
public static class AsyncOperations
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Async Operations Example ===\n");

        // Create a temporary directory for demo files
        var tempDir = Path.Combine(Path.GetTempPath(), "kdlsharp-demo");
        Directory.CreateDirectory(tempDir);
        var sampleFile = Path.Combine(tempDir, "sample.kdl");

        try
        {
            // Demo 1: Write a KDL file and read it back asynchronously
            await DemoFileOperationsAsync(sampleFile);

            // Demo 2: Stream-based async operations
            await DemoStreamOperationsAsync();

            // Demo 3: Cancellation token usage
            await DemoCancellationAsync(sampleFile);

            // Demo 4: Streaming serialization for async sequences
            await DemoStreamingSerializationAsync();
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

    private static async Task DemoFileOperationsAsync(string filePath)
    {
        Console.WriteLine("--- File Operations (Async) ---\n");

        // Create a document programmatically
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("server")
            .AddProperty("host", "localhost")
            .AddProperty("port", new KdlSharp.Values.KdlNumber(8080)));
        doc.Nodes.Add(new KdlNode("database")
            .AddProperty("connection", "postgres://localhost/mydb"));

        // Save asynchronously
        await doc.SaveAsync(filePath);
        Console.WriteLine($"Saved document to: {filePath}");

        // Read back asynchronously
        var loadedDoc = await KdlDocument.ParseFileAsync(filePath);
        Console.WriteLine($"Loaded {loadedDoc.Nodes.Count} nodes from file");

        // Access the data
        var server = loadedDoc.Nodes.First(n => n.Name == "server");
        Console.WriteLine($"Server host: {server.GetProperty("host")?.AsString()}");
        Console.WriteLine($"Server port: {server.GetProperty("port")?.AsNumber()}");

        Console.WriteLine();
    }

    private static async Task DemoStreamOperationsAsync()
    {
        Console.WriteLine("--- Stream Operations (Async) ---\n");

        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("config")
            .AddArgument("production")
            .AddProperty("debug", KdlSharp.Values.KdlBoolean.False));

        // Write to memory stream asynchronously
        using var memoryStream = new MemoryStream();
        await doc.WriteToAsync(memoryStream, leaveOpen: true);
        Console.WriteLine($"Written {memoryStream.Length} bytes to stream");

        // Reset stream position to read back
        memoryStream.Position = 0;

        // Read from stream asynchronously
        var loadedDoc = await KdlDocument.ParseStreamAsync(memoryStream, leaveOpen: true);
        Console.WriteLine($"Loaded {loadedDoc.Nodes.Count} nodes from stream");

        var config = loadedDoc.Nodes[0];
        Console.WriteLine($"Config: {config.Arguments[0].AsString()}, debug={config.GetProperty("debug")?.AsBoolean()}");

        Console.WriteLine();
    }

    private static async Task DemoCancellationAsync(string filePath)
    {
        Console.WriteLine("--- Cancellation Token Usage ---\n");

        // Create a cancellation token source
        using var cts = new CancellationTokenSource();

        // Create a document
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("task")
            .AddProperty("status", "running"));

        // Save with cancellation token
        await doc.SaveAsync(filePath, cancellationToken: cts.Token);
        Console.WriteLine("Saved document with cancellation token support");

        // Read with cancellation token
        var loadedDoc = await KdlDocument.ParseFileAsync(filePath, cancellationToken: cts.Token);
        Console.WriteLine($"Loaded document with cancellation token: {loadedDoc.Nodes.Count} nodes");

        // Demonstrate how to cancel (we won't actually cancel in this demo)
        Console.WriteLine("Note: Use cts.Cancel() to cancel ongoing operations");
        Console.WriteLine("Useful for long-running operations or user-initiated cancellation");

        Console.WriteLine();
    }

    private static async Task DemoStreamingSerializationAsync()
    {
        Console.WriteLine("--- Streaming Serialization (Async) ---\n");

        var serializer = new KdlSerializer(new KdlSerializerOptions
        {
            RootNodeName = "event"
        });

        // Create a memory stream to collect output
        using var memoryStream = new MemoryStream();

        // Serialize async sequence directly to stream
        await serializer.SerializeStreamAsync(GenerateEventsAsync(), memoryStream);

        // Read the result
        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream);
        var content = await reader.ReadToEndAsync();

        Console.WriteLine("Streamed KDL output:");
        Console.WriteLine(content);

        Console.WriteLine("Stream serialization is memory-efficient for large sequences:");
        Console.WriteLine("- Objects are serialized one at a time as they are yielded");
        Console.WriteLine("- No need to materialize the entire collection in memory");
        Console.WriteLine("- Supports cancellation via CancellationToken");

        Console.WriteLine();
    }

    /// <summary>
    /// Simulates an async data source (e.g., database cursor, API pagination)
    /// </summary>
    private static async IAsyncEnumerable<EventRecord> GenerateEventsAsync()
    {
        var events = new[]
        {
            new EventRecord("app-start", DateTime.UtcNow.AddMinutes(-10)),
            new EventRecord("user-login", DateTime.UtcNow.AddMinutes(-5)),
            new EventRecord("data-sync", DateTime.UtcNow)
        };

        foreach (var evt in events)
        {
            // Simulate async data source delay
            await Task.Delay(10);
            yield return evt;
        }
    }

    /// <summary>
    /// Example record type for streaming serialization
    /// </summary>
    private sealed record EventRecord(string Name, DateTime Timestamp);
}
