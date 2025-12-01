using FluentAssertions;
using KdlSharp;
using KdlSharp.Formatting;
using KdlSharp.Settings;
using KdlSharp.Values;
using Xunit;

namespace KdlSharp.Tests.FormatterTests;

public class KdlWriterTests
{
    [Fact]
    public void WriteNode_SimpleNode_CorrectOutput()
    {
        using var stringWriter = new StringWriter();
        using var writer = new KdlWriter(stringWriter);

        writer.WriteNode(new KdlNode("node"));

        stringWriter.ToString().Should().Be("node\n");
    }

    [Fact]
    public void WriteNode_NullNode_ThrowsArgumentNullException()
    {
        using var stringWriter = new StringWriter();
        using var writer = new KdlWriter(stringWriter);

        var action = () => writer.WriteNode(null!);

        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("node");
    }

    [Fact]
    public void WriteValue_NullValue_ThrowsArgumentNullException()
    {
        using var stringWriter = new StringWriter();
        using var writer = new KdlWriter(stringWriter);

        var action = () => writer.WriteValue(null!);

        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("value");
    }

    [Fact]
    public void WriteValue_String_QuotedOutput()
    {
        using var stringWriter = new StringWriter();
        using var writer = new KdlWriter(stringWriter);

        writer.WriteValue(new KdlString("hello"));

        stringWriter.ToString().Should().Be("\"hello\"");
    }

    [Fact]
    public void WriteValue_Number_DecimalOutput()
    {
        using var stringWriter = new StringWriter();
        using var writer = new KdlWriter(stringWriter);

        writer.WriteValue(new KdlNumber(42));

        stringWriter.ToString().Should().Be("42");
    }

    [Fact]
    public void WriteValue_BooleanTrue_CorrectOutput()
    {
        using var stringWriter = new StringWriter();
        using var writer = new KdlWriter(stringWriter);

        // Parse to get a fresh boolean value (avoids singleton mutation issues)
        var doc = KdlDocument.Parse("node #true");
        var boolValue = doc.Nodes[0].Arguments[0];

        writer.WriteValue(boolValue);

        stringWriter.ToString().Should().Contain("#true");
    }

    [Fact]
    public void WriteValue_BooleanFalse_CorrectOutput()
    {
        using var stringWriter = new StringWriter();
        using var writer = new KdlWriter(stringWriter);

        // Parse to get a fresh boolean value (avoids singleton mutation issues)
        var doc = KdlDocument.Parse("node #false");
        var boolValue = doc.Nodes[0].Arguments[0];

        writer.WriteValue(boolValue);

        stringWriter.ToString().Should().Contain("#false");
    }

    [Fact]
    public void WriteValue_Null_CorrectOutput()
    {
        using var stringWriter = new StringWriter();
        using var writer = new KdlWriter(stringWriter);

        // Parse to get a fresh null value (avoids singleton mutation issues)
        var doc = KdlDocument.Parse("node #null");
        var nullValue = doc.Nodes[0].Arguments[0];

        writer.WriteValue(nullValue);

        stringWriter.ToString().Should().Contain("#null");
    }

    [Fact]
    public void WriteValue_StringWithTypeAnnotation_IncludesAnnotation()
    {
        using var stringWriter = new StringWriter();
        using var writer = new KdlWriter(stringWriter);

        var value = new KdlString("test") { TypeAnnotation = new KdlAnnotation("date") };
        writer.WriteValue(value);

        stringWriter.ToString().Should().Be("(date)\"test\"");
    }

    [Fact]
    public void WriteStartNode_SimpleNode_CorrectOutput()
    {
        using var stringWriter = new StringWriter();
        using var writer = new KdlWriter(stringWriter);

        writer.WriteStartNode("mynode");
        writer.WriteEndNode();

        stringWriter.ToString().Should().Be("mynode\n");
    }

    [Fact]
    public void WriteStartNode_WithAnnotation_CorrectOutput()
    {
        using var stringWriter = new StringWriter();
        using var writer = new KdlWriter(stringWriter);

        writer.WriteStartNode("mynode", new KdlAnnotation("type"));
        writer.WriteEndNode();

        stringWriter.ToString().Should().Be("(type)mynode\n");
    }

    [Fact]
    public void WriteArgument_SingleArgument_CorrectOutput()
    {
        using var stringWriter = new StringWriter();
        using var writer = new KdlWriter(stringWriter);

        writer.WriteStartNode("node");
        writer.WriteArgument(new KdlString("arg1"));
        writer.WriteEndNode();

        stringWriter.ToString().Should().Be("node \"arg1\"\n");
    }

    [Fact]
    public void WriteArgument_MultipleArguments_CorrectSpacing()
    {
        using var stringWriter = new StringWriter();
        using var writer = new KdlWriter(stringWriter);

        writer.WriteStartNode("node");
        writer.WriteArgument(new KdlString("arg1"));
        writer.WriteArgument(new KdlNumber(42));
        writer.WriteEndNode();

        stringWriter.ToString().Should().Be("node \"arg1\" 42\n");
    }

    [Fact]
    public void WriteProperty_SingleProperty_CorrectOutput()
    {
        using var stringWriter = new StringWriter();
        using var writer = new KdlWriter(stringWriter);

        writer.WriteStartNode("node");
        writer.WriteProperty("key", new KdlString("value"));
        writer.WriteEndNode();

        stringWriter.ToString().Should().Be("node key=\"value\"\n");
    }

    [Fact]
    public void WriteStartChildren_WriteEndChildren_CorrectStructure()
    {
        using var stringWriter = new StringWriter();
        using var writer = new KdlWriter(stringWriter);

        writer.WriteStartNode("parent");
        writer.WriteStartChildren();
        writer.WriteNode(new KdlNode("child"));
        writer.WriteEndChildren();

        stringWriter.ToString().Should().Be("parent {\n    child\n}\n");
    }

    [Fact]
    public void WriteNode_NestedChildren_CorrectIndentation()
    {
        using var stringWriter = new StringWriter();
        using var writer = new KdlWriter(stringWriter);

        writer.WriteStartNode("level1");
        writer.WriteStartChildren();

        writer.WriteStartNode("level2");
        writer.WriteStartChildren();
        writer.WriteNode(new KdlNode("level3"));
        writer.WriteEndChildren();

        writer.WriteEndChildren();

        stringWriter.ToString().Should().Be("level1 {\n    level2 {\n        level3\n    }\n}\n");
    }

    [Fact]
    public void Constructor_NullWriter_ThrowsArgumentNullException()
    {
        var action = () => new KdlWriter(null!);

        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("writer");
    }

    [Fact]
    public void WriteNode_WithCustomSettings_UsesSettings()
    {
        using var stringWriter = new StringWriter();
        var settings = new KdlFormatterSettings { Indentation = "\t" };
        using var writer = new KdlWriter(stringWriter, settings);

        var parent = new KdlNode("parent");
        parent.Children.Add(new KdlNode("child"));
        writer.WriteNode(parent);

        stringWriter.ToString().Should().Be("parent {\n\tchild\n}\n");
    }

    [Fact]
    public void Flush_CalledMultipleTimes_NoException()
    {
        using var stringWriter = new StringWriter();
        using var writer = new KdlWriter(stringWriter);

        writer.WriteStartNode("node");
        writer.Flush();
        writer.WriteEndNode();
        writer.Flush();

        stringWriter.ToString().Should().Be("node\n");
    }

    [Fact]
    public void Dispose_FlushesOutput()
    {
        var stringWriter = new StringWriter();
        var writer = new KdlWriter(stringWriter);

        writer.WriteStartNode("node");
        writer.WriteEndNode();
        writer.Dispose();

        stringWriter.ToString().Should().Be("node\n");
    }

    [Fact]
    public void WriteProperty_PropertyKeyWithSpecialChars_Quoted()
    {
        using var stringWriter = new StringWriter();
        using var writer = new KdlWriter(stringWriter);

        writer.WriteStartNode("node");
        writer.WriteProperty("my key", new KdlString("value"));
        writer.WriteEndNode();

        stringWriter.ToString().Should().Contain("\"my key\"");
    }

    [Fact]
    public void WriteNode_NodeWithAllComponents_CorrectOrder()
    {
        using var stringWriter = new StringWriter();
        using var writer = new KdlWriter(stringWriter);

        var node = new KdlNode("node") { TypeAnnotation = new KdlAnnotation("type") };
        node.AddArgument("arg");
        node.AddProperty("key", "value");
        node.Children.Add(new KdlNode("child"));
        writer.WriteNode(node);

        var result = stringWriter.ToString();
        var typeIdx = result.IndexOf("(type)");
        var nodeIdx = result.IndexOf("node");
        var argIdx = result.IndexOf("\"arg\"");
        var propIdx = result.IndexOf("key=");
        var childIdx = result.IndexOf("child");

        typeIdx.Should().BeLessThan(nodeIdx);
        nodeIdx.Should().BeLessThan(argIdx);
        argIdx.Should().BeLessThan(propIdx);
        propIdx.Should().BeLessThan(childIdx);
    }
}
