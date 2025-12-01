using FluentAssertions;
using KdlSharp;
using KdlSharp.Formatting;
using KdlSharp.Parsing;
using KdlSharp.Serialization;
using Xunit;

namespace KdlSharp.Tests;

/// <summary>
/// Regression tests for synchronous stream APIs to prevent buffer size issues.
/// </summary>
public class StreamApiTests
{
    #region KdlDocument.ParseStream Tests

    [Fact]
    public void ParseStream_ValidStream_Success()
    {
        // Arrange
        var kdl = "node key=\"value\"";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(kdl));

        // Act
        var doc = KdlDocument.ParseStream(stream);

        // Assert
        doc.Nodes.Should().HaveCount(1);
        doc.Nodes[0].Name.Should().Be("node");
        doc.Nodes[0].GetProperty("key")!.AsString().Should().Be("value");
    }

    [Fact]
    public void ParseStream_LeaveOpen_StreamRemainAccessible()
    {
        // Arrange
        var kdl = "node 123";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(kdl));

        // Act
        var doc = KdlDocument.ParseStream(stream, leaveOpen: true);

        // Assert - stream should still be accessible
        stream.CanRead.Should().BeTrue();
        doc.Nodes[0].Arguments[0].AsInt32().Should().Be(123);
    }

    #endregion

    #region KdlDocument.WriteTo Tests

    [Fact]
    public void WriteTo_ValidDocument_Success()
    {
        // Arrange
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("test").AddArgument(42));
        using var stream = new MemoryStream();

        // Act
        doc.WriteTo(stream, leaveOpen: true);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        content.Should().Contain("test");
        content.Should().Contain("42");
    }

    [Fact]
    public void WriteTo_RoundTrip_PreservesContent()
    {
        // Arrange
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("config").AddProperty("port", 8080));
        using var stream = new MemoryStream();

        // Act
        doc.WriteTo(stream, leaveOpen: true);
        stream.Position = 0;
        var loadedDoc = KdlDocument.ParseStream(stream, leaveOpen: true);

        // Assert
        loadedDoc.Nodes[0].Name.Should().Be("config");
        loadedDoc.Nodes[0].GetProperty("port")!.AsInt32().Should().Be(8080);
    }

    #endregion

    #region KdlParser.ParseStream Tests

    [Fact]
    public void KdlParser_ParseStream_Success()
    {
        // Arrange
        var kdl = "node \"value\"";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(kdl));
        var parser = new KdlParser();

        // Act
        var doc = parser.ParseStream(stream, leaveOpen: true);

        // Assert
        doc.Nodes.Should().HaveCount(1);
        doc.Nodes[0].Arguments[0].AsString().Should().Be("value");
    }

    [Fact]
    public async Task KdlParser_ParseNodesStreamAsync_FromStream_Success()
    {
        // Arrange
        var kdl = "node1 \"value1\"\nnode2 \"value2\"\nnode3 \"value3\"";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(kdl));
        var parser = new KdlParser();

        // Act
        var nodes = new List<KdlNode>();
        await foreach (var node in parser.ParseNodesStreamAsync(stream, leaveOpen: true))
        {
            nodes.Add(node);
        }

        // Assert
        nodes.Should().HaveCount(3);
        nodes[0].Name.Should().Be("node1");
        nodes[1].Name.Should().Be("node2");
        nodes[2].Name.Should().Be("node3");
    }

    #endregion

    #region KdlFormatter.SerializeToStream Tests

    [Fact]
    public void KdlFormatter_SerializeToStream_Success()
    {
        // Arrange
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("data").AddProperty("count", 100));
        using var stream = new MemoryStream();
        var formatter = new KdlFormatter();

        // Act
        formatter.SerializeToStream(doc, stream, leaveOpen: true);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        content.Should().Contain("data");
        content.Should().Contain("count");
        content.Should().Contain("100");
    }

    #endregion

    #region KdlSerializer Stream Tests

    [Fact]
    public void KdlSerializer_SerializeToStream_Success()
    {
        // Arrange
        var obj = new TestRecord("hello", 42);
        using var stream = new MemoryStream();
        var serializer = new KdlSerializer();

        // Act
        serializer.Serialize(obj, stream);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        content.Should().Contain("hello");
        content.Should().Contain("42");
    }

    [Fact]
    public void KdlSerializer_DeserializeFromStream_Success()
    {
        // Arrange
        var kdl = "root name=\"test\" value=99";
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(kdl));
        var serializer = new KdlSerializer();

        // Act
        var result = serializer.Deserialize<TestRecord>(stream);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("test");
        result.Value.Should().Be(99);
    }

    #endregion

    #region Helper Types

    public sealed record TestRecord(string Name, int Value);

    #endregion
}
