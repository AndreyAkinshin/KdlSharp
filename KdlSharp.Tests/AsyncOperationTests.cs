using FluentAssertions;
using KdlSharp;
using KdlSharp.Serialization;
using Xunit;

namespace KdlSharp.Tests;

/// <summary>
/// Tests for asynchronous parsing, serialization, and deserialization operations.
/// </summary>
public class AsyncOperationTests
{
    #region ParseFileAsync Tests

    [Fact]
    public async Task ParseFileAsync_ValidFile_Success()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, "node \"value\"");

            // Act
            var doc = await KdlDocument.ParseFileAsync(tempFile);

            // Assert
            doc.Nodes.Should().HaveCount(1);
            doc.Nodes[0].Name.Should().Be("node");
            doc.Nodes[0].Arguments[0].AsString().Should().Be("value");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ParseFileAsync_ComplexDocument_Success()
    {
        // Arrange
        var kdl = @"
package ""my-app"" version=""1.0.0"" {
    author ""Alice""
    dependencies {
        lodash ""^4.17.0""
    }
}";
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, kdl);

            // Act
            var doc = await KdlDocument.ParseFileAsync(tempFile);

            // Assert
            doc.Nodes.Should().HaveCount(1);
            doc.Nodes[0].Name.Should().Be("package");
            doc.Nodes[0].GetProperty("version")!.AsString().Should().Be("1.0.0");
            doc.Nodes[0].Children.Should().HaveCount(2);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    #endregion

    #region ParseStreamAsync Tests

    [Fact]
    public async Task ParseStreamAsync_ValidStream_Success()
    {
        // Arrange
        var kdl = "node key=\"value\"";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(kdl));

        // Act
        var doc = await KdlDocument.ParseStreamAsync(stream);

        // Assert
        doc.Nodes.Should().HaveCount(1);
        doc.Nodes[0].Name.Should().Be("node");
        doc.Nodes[0].GetProperty("key")!.AsString().Should().Be("value");
    }

    [Fact]
    public async Task ParseStreamAsync_EmptyStream_ReturnsEmptyDocument()
    {
        // Arrange
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(""));

        // Act
        var doc = await KdlDocument.ParseStreamAsync(stream);

        // Assert
        doc.Nodes.Should().BeEmpty();
    }

    [Fact]
    public async Task ParseStreamAsync_MultipleNodes_Success()
    {
        // Arrange
        var kdl = @"
node1 ""arg1""
node2 key=""value""
";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(kdl));

        // Act
        var doc = await KdlDocument.ParseStreamAsync(stream);

        // Assert
        doc.Nodes.Should().HaveCount(2);
        doc.Nodes[0].Name.Should().Be("node1");
        doc.Nodes[1].Name.Should().Be("node2");
    }

    #endregion

    #region SaveAsync Tests

    [Fact]
    public async Task SaveAsync_ValidDocument_Success()
    {
        // Arrange
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node").AddProperty("key", "value"));
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            await doc.SaveAsync(tempFile);

            // Assert
            var content = await File.ReadAllTextAsync(tempFile);
            content.Should().Contain("node");
            content.Should().Contain("key");
            content.Should().Contain("value");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SaveAsync_RoundTrip_PreservesContent()
    {
        // Arrange
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("config")
            .AddArgument("app")
            .AddProperty("port", 8080)
            .AddChild(new KdlNode("database").AddProperty("host", "localhost")));
        var tempFile = Path.GetTempFileName();

        try
        {
            // Act
            await doc.SaveAsync(tempFile);
            var loadedDoc = await KdlDocument.ParseFileAsync(tempFile);

            // Assert
            loadedDoc.Nodes[0].Name.Should().Be("config");
            loadedDoc.Nodes[0].Arguments[0].AsString().Should().Be("app");
            loadedDoc.Nodes[0].GetProperty("port")!.AsNumber().Should().Be(8080);
            loadedDoc.Nodes[0].Children[0].Name.Should().Be("database");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    #endregion

    #region WriteToAsync Tests

    [Fact]
    public async Task WriteToAsync_ValidDocument_Success()
    {
        // Arrange
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node").AddArgument(42));
        using var stream = new MemoryStream();

        // Act - use leaveOpen: true to keep stream accessible
        await doc.WriteToAsync(stream, null, leaveOpen: true);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();
        content.Should().Contain("node");
        content.Should().Contain("42");
    }

    [Fact]
    public async Task WriteToAsync_RoundTrip_PreservesContent()
    {
        // Arrange
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("test").AddProperty("value", "hello"));
        using var stream = new MemoryStream();

        // Act
        await doc.WriteToAsync(stream, null, leaveOpen: true);
        stream.Position = 0;
        var loadedDoc = await KdlDocument.ParseStreamAsync(stream, null, leaveOpen: true);

        // Assert
        loadedDoc.Nodes[0].Name.Should().Be("test");
        loadedDoc.Nodes[0].GetProperty("value")!.AsString().Should().Be("hello");
    }

    #endregion

    #region DeserializeStreamAsync Tests

    [Fact]
    public async Task DeserializeStreamAsync_SimpleRecord_Success()
    {
        // Arrange
        var kdl = "root name=\"TestApp\" port=8080";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(kdl));
        var serializer = new KdlSerializer();

        // Act
        var result = await serializer.DeserializeStreamAsync<SimpleConfig>(stream);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("TestApp");
        result.Port.Should().Be(8080);
    }

    [Fact]
    public async Task DeserializeStreamAsync_ComplexObject_Success()
    {
        // Arrange
        var kdl = @"root name=""MyApp"" {
    database provider=""postgres"" {
        credentials user=""admin"" password=""secret""
    }
}";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(kdl));
        var serializer = new KdlSerializer(new KdlSerializerOptions
        {
            PropertyNamingPolicy = KdlNamingPolicy.KebabCase
        });

        // Act
        var result = await serializer.DeserializeStreamAsync<AppSettings>(stream);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("MyApp");
        result.Database.Should().NotBeNull();
        result.Database!.Provider.Should().Be("postgres");
    }

    #endregion

    #region SerializeStreamAsync Tests

    [Fact]
    public async Task SerializeStreamAsync_MultipleObjects_Success()
    {
        // Arrange
        var items = GetAsyncItems();
        using var stream = new MemoryStream();
        var serializer = new KdlSerializer();

        // Act
        await serializer.SerializeStreamAsync(items, stream);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        content.Should().Contain("Item1");
        content.Should().Contain("Item2");
        content.Should().Contain("Item3");
    }

    [Fact]
    public async Task SerializeStreamAsync_EmptySequence_WritesNothing()
    {
        // Arrange
        var items = GetEmptyAsyncItems();
        using var stream = new MemoryStream();
        var serializer = new KdlSerializer();

        // Act
        await serializer.SerializeStreamAsync(items, stream);

        // Assert
        stream.Length.Should().Be(0);
    }

    [Fact]
    public async Task SerializeStreamAsync_RespectsCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var items = GetSlowAsyncItems(cts);
        using var stream = new MemoryStream();
        var serializer = new KdlSerializer();

        // Act & Assert - should throw when cancelled
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await serializer.SerializeStreamAsync(items, stream, null, cts.Token);
        });
    }

    [Fact]
    public async Task SerializeStreamAsync_RespectsTargetVersion()
    {
        // Arrange
        var items = GetBooleanAsyncItems();
        using var stream = new MemoryStream();
        var serializer = new KdlSerializer(new KdlSerializerOptions
        {
            TargetVersion = KdlVersion.V1
        });

        // Act
        await serializer.SerializeStreamAsync(items, stream);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        // V1 uses bare 'true' instead of '#true'
        content.Should().Contain("=true");
        content.Should().NotContain("#true");
    }

    private static async IAsyncEnumerable<StreamItem> GetAsyncItems()
    {
        await Task.Yield();
        yield return new StreamItem("Item1", 1);
        yield return new StreamItem("Item2", 2);
        yield return new StreamItem("Item3", 3);
    }

    private static async IAsyncEnumerable<StreamItem> GetEmptyAsyncItems()
    {
        await Task.Yield();
        yield break;
    }

    private static async IAsyncEnumerable<StreamItem> GetSlowAsyncItems(CancellationTokenSource cts)
    {
        await Task.Yield();
        yield return new StreamItem("First", 1);
        cts.Cancel(); // Cancel after first item
        yield return new StreamItem("Second", 2); // This should not be reached
    }

    private static async IAsyncEnumerable<BooleanItem> GetBooleanAsyncItems()
    {
        await Task.Yield();
        yield return new BooleanItem("Test", true);
    }

    #endregion

    #region Test Helper Types

    public sealed record SimpleConfig(string Name, int Port);
    public sealed record DatabaseSettings(string Provider, Credentials? Credentials);
    public sealed record Credentials(string User, string Password);
    public sealed record AppSettings(string Name, DatabaseSettings? Database);
    public sealed record StreamItem(string Name, int Value);
    public sealed record BooleanItem(string Name, bool Enabled);

    #endregion
}
