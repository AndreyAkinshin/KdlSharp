using FluentAssertions;
using KdlSharp.Parsing;
using Xunit;

namespace KdlSharp.Tests.ReaderTests;

public class KdlReaderTests
{
    [Fact]
    public void Read_SimpleString_ReturnsToken()
    {
        var kdl = "node \"value\"";
        using var reader = new StringReader(kdl);
        using var kdlReader = new KdlReader(reader);

        // Read first token (node name)
        kdlReader.Read().Should().BeTrue();
        kdlReader.TokenType.Should().Be(KdlTokenType.String);
        kdlReader.StringValue.Should().Be("node");
        kdlReader.Line.Should().Be(1);
        kdlReader.Column.Should().Be(1);

        // Read second token (argument)
        kdlReader.Read().Should().BeTrue();
        kdlReader.TokenType.Should().Be(KdlTokenType.String);
        kdlReader.StringValue.Should().Be("value");

        // No more tokens
        kdlReader.Read().Should().BeFalse();
        kdlReader.TokenType.Should().Be(KdlTokenType.EndOfFile);
    }

    [Fact]
    public void Read_Number_ReturnsNumericToken()
    {
        var kdl = "node 42";
        using var reader = new StringReader(kdl);
        using var kdlReader = new KdlReader(reader);

        // Read node name
        kdlReader.Read().Should().BeTrue();
        kdlReader.TokenType.Should().Be(KdlTokenType.String);

        // Read number
        kdlReader.Read().Should().BeTrue();
        kdlReader.TokenType.Should().Be(KdlTokenType.Number);
        kdlReader.NumberValue.Should().Be(42);
    }

    [Fact]
    public void Read_Boolean_ReturnsBooleanTokens()
    {
        var kdl = "node #true #false";
        using var reader = new StringReader(kdl);
        using var kdlReader = new KdlReader(reader);

        // Read node name
        kdlReader.Read().Should().BeTrue();

        // Read true
        kdlReader.Read().Should().BeTrue();
        kdlReader.TokenType.Should().Be(KdlTokenType.True);
        kdlReader.BooleanValue.Should().BeTrue();

        // Read false
        kdlReader.Read().Should().BeTrue();
        kdlReader.TokenType.Should().Be(KdlTokenType.False);
        kdlReader.BooleanValue.Should().BeFalse();
    }

    [Fact]
    public void Read_Null_ReturnsNullToken()
    {
        var kdl = "node #null";
        using var reader = new StringReader(kdl);
        using var kdlReader = new KdlReader(reader);

        // Read node name
        kdlReader.Read().Should().BeTrue();

        // Read null
        kdlReader.Read().Should().BeTrue();
        kdlReader.TokenType.Should().Be(KdlTokenType.Null);
        kdlReader.StringValue.Should().BeNull();
        kdlReader.NumberValue.Should().BeNull();
        kdlReader.BooleanValue.Should().BeNull();
    }

    [Fact]
    public void Read_StructuralTokens_ReturnsCorrectTypes()
    {
        var kdl = "node { }";
        using var reader = new StringReader(kdl);
        using var kdlReader = new KdlReader(reader);

        // Read node name
        kdlReader.Read().Should().BeTrue();
        kdlReader.TokenType.Should().Be(KdlTokenType.String);

        // Read opening brace
        kdlReader.Read().Should().BeTrue();
        kdlReader.TokenType.Should().Be(KdlTokenType.OpenBrace);

        // Read closing brace
        kdlReader.Read().Should().BeTrue();
        kdlReader.TokenType.Should().Be(KdlTokenType.CloseBrace);
    }

    [Fact]
    public void Read_Property_ReturnsKeyValueTokens()
    {
        var kdl = "node key=\"value\"";
        using var reader = new StringReader(kdl);
        using var kdlReader = new KdlReader(reader);

        // Read node name
        kdlReader.Read().Should().BeTrue();
        kdlReader.StringValue.Should().Be("node");

        // Read property key
        kdlReader.Read().Should().BeTrue();
        kdlReader.StringValue.Should().Be("key");

        // Read equals
        kdlReader.Read().Should().BeTrue();
        kdlReader.TokenType.Should().Be(KdlTokenType.Equals);

        // Read property value
        kdlReader.Read().Should().BeTrue();
        kdlReader.StringValue.Should().Be("value");
    }

    [Fact]
    public void Read_MultipleNodes_ReturnsNewlineTokens()
    {
        var kdl = "node1\nnode2";
        using var reader = new StringReader(kdl);
        using var kdlReader = new KdlReader(reader);

        // Read first node
        kdlReader.Read().Should().BeTrue();
        kdlReader.StringValue.Should().Be("node1");

        // Read newline
        kdlReader.Read().Should().BeTrue();
        kdlReader.TokenType.Should().Be(KdlTokenType.Newline);

        // Read second node
        kdlReader.Read().Should().BeTrue();
        kdlReader.StringValue.Should().Be("node2");
    }

    [Fact]
    public void Read_EmptyDocument_ReturnsFalse()
    {
        var kdl = "";
        using var reader = new StringReader(kdl);
        using var kdlReader = new KdlReader(reader);

        kdlReader.Read().Should().BeFalse();
        kdlReader.TokenType.Should().Be(KdlTokenType.EndOfFile);
    }

    [Fact]
    public void ReadValue_String_ReturnsKdlString()
    {
        var kdl = "\"hello\"";
        using var reader = new StringReader(kdl);
        using var kdlReader = new KdlReader(reader);

        var value = kdlReader.ReadValue();
        value.Should().NotBeNull();
        value!.ValueType.Should().Be(KdlValueType.String);
        value.AsString().Should().Be("hello");
    }

    [Fact]
    public void ReadValue_Number_ReturnsKdlNumber()
    {
        var kdl = "123";
        using var reader = new StringReader(kdl);
        using var kdlReader = new KdlReader(reader);

        var value = kdlReader.ReadValue();
        value.Should().NotBeNull();
        value!.ValueType.Should().Be(KdlValueType.Number);
        value.AsNumber().Should().Be(123);
    }

    [Fact]
    public void ReadValue_Boolean_ReturnsKdlBoolean()
    {
        var kdl = "#true";
        using var reader = new StringReader(kdl);
        using var kdlReader = new KdlReader(reader);

        var value = kdlReader.ReadValue();
        value.Should().NotBeNull();
        value!.ValueType.Should().Be(KdlValueType.Boolean);
        value.AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void ReadValue_Null_ReturnsKdlNull()
    {
        var kdl = "#null";
        using var reader = new StringReader(kdl);
        using var kdlReader = new KdlReader(reader);

        var value = kdlReader.ReadValue();
        value.Should().NotBeNull();
        value!.ValueType.Should().Be(KdlValueType.Null);
        value.IsNull().Should().BeTrue();
    }

    [Fact]
    public void Skip_AdvancesToNextToken()
    {
        var kdl = "node1 node2";
        using var reader = new StringReader(kdl);
        using var kdlReader = new KdlReader(reader);

        // Read first token
        kdlReader.Read().Should().BeTrue();
        kdlReader.StringValue.Should().Be("node1");

        // Skip to next token
        kdlReader.Skip().Should().BeTrue();
        kdlReader.StringValue.Should().Be("node2");
    }

    [Fact]
    public void Line_Column_TrackPosition()
    {
        var kdl = "node1\n  node2";
        using var reader = new StringReader(kdl);
        using var kdlReader = new KdlReader(reader);

        // First token at line 1, column 1
        kdlReader.Read().Should().BeTrue();
        kdlReader.Line.Should().Be(1);
        kdlReader.Column.Should().Be(1);

        // Newline at line 1, column 6
        kdlReader.Read().Should().BeTrue();
        kdlReader.TokenType.Should().Be(KdlTokenType.Newline);

        // Second token at line 2, column 3 (after 2 spaces)
        kdlReader.Read().Should().BeTrue();
        kdlReader.Line.Should().Be(2);
    }

    [Fact]
    public void Dispose_WithLeaveOpenFalse_DisposesReader()
    {
        var kdl = "node";
        var stringReader = new StringReader(kdl);
        var kdlReader = new KdlReader(stringReader, leaveOpen: false);

        kdlReader.Dispose();

        // StringReader should be disposed and throw on read
        Assert.Throws<ObjectDisposedException>(() => stringReader.Read());
    }

    [Fact]
    public void Dispose_WithLeaveOpenTrue_LeavesReaderOpen()
    {
        var kdl = "node";
        var stringReader = new StringReader(kdl);
        var kdlReader = new KdlReader(stringReader, leaveOpen: true);

        kdlReader.Dispose();

        // StringReader should still be usable
        stringReader.Read().Should().BeGreaterThan(-1);
    }

    [Fact]
    public void Read_AfterDispose_ThrowsObjectDisposedException()
    {
        var kdl = "node";
        using var reader = new StringReader(kdl);
        var kdlReader = new KdlReader(reader);
        kdlReader.Dispose();

        Assert.Throws<ObjectDisposedException>(() => kdlReader.Read());
    }

    [Fact]
    public void Read_HexNumber_ReturnsCorrectValue()
    {
        var kdl = "0xFF";
        using var reader = new StringReader(kdl);
        using var kdlReader = new KdlReader(reader);

        kdlReader.Read().Should().BeTrue();
        kdlReader.TokenType.Should().Be(KdlTokenType.Number);
        kdlReader.NumberValue.Should().Be(255);
    }

    [Fact]
    public void Read_TypeAnnotation_ReturnsParenTokens()
    {
        var kdl = "(type)value";
        using var reader = new StringReader(kdl);
        using var kdlReader = new KdlReader(reader);

        // Read opening paren
        kdlReader.Read().Should().BeTrue();
        kdlReader.TokenType.Should().Be(KdlTokenType.OpenParen);

        // Read type name
        kdlReader.Read().Should().BeTrue();
        kdlReader.TokenType.Should().Be(KdlTokenType.String);
        kdlReader.StringValue.Should().Be("type");

        // Read closing paren
        kdlReader.Read().Should().BeTrue();
        kdlReader.TokenType.Should().Be(KdlTokenType.CloseParen);

        // Read value
        kdlReader.Read().Should().BeTrue();
        kdlReader.StringValue.Should().Be("value");
    }

    [Fact]
    public void Read_Semicolon_ReturnsSemicolonToken()
    {
        var kdl = "node1; node2";
        using var reader = new StringReader(kdl);
        using var kdlReader = new KdlReader(reader);

        // Read first node
        kdlReader.Read().Should().BeTrue();
        kdlReader.StringValue.Should().Be("node1");

        // Read semicolon
        kdlReader.Read().Should().BeTrue();
        kdlReader.TokenType.Should().Be(KdlTokenType.Semicolon);

        // Read second node
        kdlReader.Read().Should().BeTrue();
        kdlReader.StringValue.Should().Be("node2");
    }

    [Fact]
    public void Read_NumberWithUnderscore_ReturnsCorrectValue()
    {
        var kdl = "1_000_000";
        using var reader = new StringReader(kdl);
        using var kdlReader = new KdlReader(reader);

        kdlReader.Read().Should().BeTrue();
        kdlReader.TokenType.Should().Be(KdlTokenType.Number);
        kdlReader.NumberValue.Should().Be(1000000);
    }

    [Fact]
    public void Read_MultilineString_ReturnsCorrectValue()
    {
        var kdl = "\"\"\"\nHello\nWorld\n\"\"\"";
        using var reader = new StringReader(kdl);
        using var kdlReader = new KdlReader(reader);

        kdlReader.Read().Should().BeTrue();
        kdlReader.TokenType.Should().Be(KdlTokenType.String);
        kdlReader.StringValue.Should().Be("Hello\nWorld");
    }

    [Fact]
    public void Read_ComplexDocument_ParsesAllTokens()
    {
        var kdl = @"
package ""myapp"" version=""1.0.0"" {
    author ""Alice""
}";
        using var reader = new StringReader(kdl);
        using var kdlReader = new KdlReader(reader);

        var tokens = new List<(KdlTokenType Type, string? Value)>();
        while (kdlReader.Read())
        {
            tokens.Add((kdlReader.TokenType, kdlReader.StringValue ?? kdlReader.NumberValue?.ToString()));
        }

        // Should have: newline, package, "myapp", version, =, "1.0.0", {, newline, author, "Alice", newline, }
        tokens.Should().HaveCountGreaterThan(10);
        tokens.Should().Contain(t => t.Type == KdlTokenType.OpenBrace);
        tokens.Should().Contain(t => t.Type == KdlTokenType.CloseBrace);
        tokens.Should().Contain(t => t.Value == "package");
        tokens.Should().Contain(t => t.Value == "myapp");
        tokens.Should().Contain(t => t.Value == "Alice");
    }
}

