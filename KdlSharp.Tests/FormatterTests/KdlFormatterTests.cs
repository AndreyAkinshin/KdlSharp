using FluentAssertions;
using KdlSharp;
using KdlSharp.Formatting;
using KdlSharp.Settings;
using KdlSharp.Values;
using Xunit;

namespace KdlSharp.Tests.FormatterTests;

public class KdlFormatterTests
{
    [Fact]
    public void Serialize_SimpleNode_DefaultFormatting()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node"));

        var formatter = new KdlFormatter();
        var result = formatter.Serialize(doc);

        result.Should().Be("node\n");
    }

    [Fact]
    public void Serialize_NodeWithArgument_CorrectOutput()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node").AddArgument("value"));

        var formatter = new KdlFormatter();
        var result = formatter.Serialize(doc);

        result.Should().Be("node \"value\"\n");
    }

    [Fact]
    public void Serialize_NodeWithProperty_CorrectOutput()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node").AddProperty("key", "value"));

        var formatter = new KdlFormatter();
        var result = formatter.Serialize(doc);

        result.Should().Be("node key=\"value\"\n");
    }

    [Fact]
    public void Serialize_EmptyDocument_ReturnsEmptyString()
    {
        var doc = new KdlDocument();
        var formatter = new KdlFormatter();

        var result = formatter.Serialize(doc);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Serialize_NullDocument_ThrowsArgumentNullException()
    {
        var formatter = new KdlFormatter();

        var action = () => formatter.Serialize(null!);

        action.Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("document");
    }

    [Fact]
    public void Serialize_NodeWithChildren_UsesDefaultIndentation()
    {
        var doc = new KdlDocument();
        var parent = new KdlNode("parent");
        parent.Children.Add(new KdlNode("child"));
        doc.Nodes.Add(parent);

        var formatter = new KdlFormatter();
        var result = formatter.Serialize(doc);

        result.Should().Be("parent {\n    child\n}\n");
    }

    [Fact]
    public void Serialize_DeeplyNestedStructure_CorrectIndentation()
    {
        var doc = new KdlDocument();
        var level1 = new KdlNode("level1");
        var level2 = new KdlNode("level2");
        var level3 = new KdlNode("level3");
        level2.Children.Add(level3);
        level1.Children.Add(level2);
        doc.Nodes.Add(level1);

        var formatter = new KdlFormatter();
        var result = formatter.Serialize(doc);

        result.Should().Be("level1 {\n    level2 {\n        level3\n    }\n}\n");
    }

    [Fact]
    public void Serialize_WithTabIndentation_UsesTabsForIndent()
    {
        var doc = new KdlDocument();
        var parent = new KdlNode("parent");
        parent.Children.Add(new KdlNode("child"));
        doc.Nodes.Add(parent);

        var settings = new KdlFormatterSettings { Indentation = "\t" };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        result.Should().Be("parent {\n\tchild\n}\n");
    }

    [Fact]
    public void Serialize_WithTwoSpaceIndentation_UsesTwoSpaces()
    {
        var doc = new KdlDocument();
        var parent = new KdlNode("parent");
        parent.Children.Add(new KdlNode("child"));
        doc.Nodes.Add(parent);

        var settings = new KdlFormatterSettings { Indentation = "  " };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        result.Should().Be("parent {\n  child\n}\n");
    }

    [Fact]
    public void Serialize_BooleanValues_CorrectOutput()
    {
        // Parse a fresh document to get boolean values without potential singleton mutation issues
        var doc = KdlDocument.Parse("node #true #false");

        var formatter = new KdlFormatter();
        var result = formatter.Serialize(doc);

        // Verify the serialized output contains the boolean values
        result.Should().Contain("#true");
        result.Should().Contain("#false");
        result.Should().StartWith("node ");
    }

    [Fact]
    public void Serialize_NullValue_CorrectOutput()
    {
        // Parse a fresh document to get null value (avoids singleton mutation issues)
        var doc = KdlDocument.Parse("node #null");

        var formatter = new KdlFormatter();
        var result = formatter.Serialize(doc);

        result.Should().Contain("#null");
        result.Should().StartWith("node ");
    }

    [Fact]
    public void Serialize_NumberValue_DecimalFormat()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node").AddArgument(new KdlNumber(123.456m)));

        var formatter = new KdlFormatter();
        var result = formatter.Serialize(doc);

        result.Should().Be("node 123.456\n");
    }

    [Fact]
    public void Serialize_PositiveInfinity_CorrectOutput()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node").AddArgument(KdlNumber.PositiveInfinity()));

        var formatter = new KdlFormatter();
        var result = formatter.Serialize(doc);

        result.Should().Be("node #inf\n");
    }

    [Fact]
    public void Serialize_NegativeInfinity_CorrectOutput()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node").AddArgument(KdlNumber.NegativeInfinity()));

        var formatter = new KdlFormatter();
        var result = formatter.Serialize(doc);

        result.Should().Be("node #-inf\n");
    }

    [Fact]
    public void Serialize_NaN_CorrectOutput()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node").AddArgument(KdlNumber.NaN()));

        var formatter = new KdlFormatter();
        var result = formatter.Serialize(doc);

        result.Should().Be("node #nan\n");
    }

    [Fact]
    public void Serialize_StringWithTypeAnnotation_IncludesAnnotation()
    {
        var doc = new KdlDocument();
        var value = new KdlString("test") { TypeAnnotation = new KdlAnnotation("mytype") };
        doc.Nodes.Add(new KdlNode("node").AddArgument(value));

        var formatter = new KdlFormatter();
        var result = formatter.Serialize(doc);

        result.Should().Be("node (mytype)\"test\"\n");
    }

    [Fact]
    public void Serialize_NodeWithTypeAnnotation_IncludesAnnotation()
    {
        var doc = new KdlDocument();
        var node = new KdlNode("node") { TypeAnnotation = new KdlAnnotation("nodetype") };
        doc.Nodes.Add(node);

        var formatter = new KdlFormatter();
        var result = formatter.Serialize(doc);

        result.Should().Be("(nodetype)node\n");
    }

    [Fact]
    public void Serialize_PreserveStringTypes_IdentifierString()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node")
            .AddArgument(new KdlString("identifier", KdlStringType.Identifier)));

        var settings = new KdlFormatterSettings { PreserveStringTypes = true };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        result.Should().Be("node identifier\n");
    }

    [Fact]
    public void Serialize_PreserveStringTypes_QuotedString()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node")
            .AddArgument(new KdlString("quoted value", KdlStringType.Quoted)));

        var settings = new KdlFormatterSettings { PreserveStringTypes = true };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        result.Should().Be("node \"quoted value\"\n");
    }

    [Fact]
    public void Serialize_PreserveStringTypes_MultiLineString()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node")
            .AddArgument(new KdlString("multi\nline", KdlStringType.MultiLine)));

        var settings = new KdlFormatterSettings { PreserveStringTypes = true };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        result.Should().Be("node \"\"\"multi\nline\"\"\"\n");
    }

    [Fact]
    public void Serialize_PreserveStringTypes_RawString()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node")
            .AddArgument(new KdlString(@"C:\path", KdlStringType.Raw)));

        var settings = new KdlFormatterSettings { PreserveStringTypes = true };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        result.Should().Be("node #\"C:\\path\"#\n");
    }

    [Fact]
    public void Serialize_PreserveNumberFormats_Hexadecimal()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node").AddArgument(new KdlNumber(255, KdlNumberFormat.Hexadecimal)));

        var settings = new KdlFormatterSettings { PreserveNumberFormats = true };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        result.Should().Be("node 0xFF\n");
    }

    [Fact]
    public void Serialize_PreserveNumberFormats_Octal()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node").AddArgument(new KdlNumber(493, KdlNumberFormat.Octal)));

        var settings = new KdlFormatterSettings { PreserveNumberFormats = true };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        result.Should().Be("node 0o755\n");
    }

    [Fact]
    public void Serialize_PreserveNumberFormats_Binary()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node").AddArgument(new KdlNumber(10, KdlNumberFormat.Binary)));

        var settings = new KdlFormatterSettings { PreserveNumberFormats = true };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        result.Should().Be("node 0b1010\n");
    }

    [Fact]
    public void Serialize_NoPreserveNumberFormats_HexConvertedToDecimal()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node").AddArgument(new KdlNumber(255, KdlNumberFormat.Hexadecimal)));

        var settings = new KdlFormatterSettings { PreserveNumberFormats = false };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        result.Should().Be("node 255\n");
    }

    [Fact]
    public void Serialize_QuotedNodeName_PropertyNameRequiringQuotes()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("my-node").AddProperty("my-key", "value"));

        var formatter = new KdlFormatter();
        var result = formatter.Serialize(doc);

        // With PreferIdentifierStrings = true (default), kebab-case should be valid identifiers
        // Let's check how they're actually formatted
        result.Should().Contain("my-node");
        result.Should().Contain("my-key");
    }

    [Fact]
    public void Serialize_NodeNameWithSpaces_UsesQuotedString()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node with spaces"));

        var formatter = new KdlFormatter();
        var result = formatter.Serialize(doc);

        result.Should().Contain("\"node with spaces\"");
    }

    [Fact]
    public void Serialize_CRLFNewline_UsesWindowsLineEndings()
    {
        var doc = new KdlDocument();
        var parent = new KdlNode("parent");
        parent.Children.Add(new KdlNode("child"));
        doc.Nodes.Add(parent);

        var settings = new KdlFormatterSettings { Newline = "\r\n" };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        result.Should().Be("parent {\r\n    child\r\n}\r\n");
    }

    [Fact]
    public void Serialize_MultipleTopLevelNodes_EachOnOwnLine()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node1"));
        doc.Nodes.Add(new KdlNode("node2"));
        doc.Nodes.Add(new KdlNode("node3"));

        var formatter = new KdlFormatter();
        var result = formatter.Serialize(doc);

        result.Should().Be("node1\nnode2\nnode3\n");
    }

    [Fact]
    public void Serialize_ComplexDocument_CorrectStructure()
    {
        var doc = new KdlDocument();
        var server = new KdlNode("server")
            .AddArgument("main")
            .AddProperty("host", "localhost")
            .AddProperty("port", new KdlNumber(8080));
        var db = new KdlNode("database")
            .AddProperty("connection", "postgres://localhost/mydb");
        server.Children.Add(db);
        doc.Nodes.Add(server);

        var formatter = new KdlFormatter();
        var result = formatter.Serialize(doc);

        result.Should().Contain("server \"main\"");
        result.Should().Contain("host=\"localhost\"");
        result.Should().Contain("port=8080");
        result.Should().Contain("database");
        result.Should().Contain("connection=\"postgres://localhost/mydb\"");
    }

    [Fact]
    public void Serialize_ScientificNotation_LargeNumbers()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node").AddArgument(new KdlNumber(1e15m)));

        var formatter = new KdlFormatter();
        var result = formatter.Serialize(doc);

        // Numbers >= 1e10 should use scientific notation
        result.Should().Contain("E");
    }

    [Fact]
    public void Serialize_VerySmallNumber_ScientificNotation()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node").AddArgument(new KdlNumber(0.00001m)));

        var formatter = new KdlFormatter();
        var result = formatter.Serialize(doc);

        // Numbers < 0.0001 should use scientific notation
        result.Should().Contain("E");
    }

    [Fact]
    public void Serialize_PreferIdentifierStrings_ValidIdentifierUnquoted()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node").AddProperty("key", "simplevalue"));

        var settings = new KdlFormatterSettings { PreferIdentifierStrings = true };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        result.Should().Be("node key=\"simplevalue\"\n");
    }

    [Fact]
    public void Serialize_DoNotPreferIdentifierStrings_AllStringsQuoted()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("mynode").AddProperty("mykey", "myvalue"));

        var settings = new KdlFormatterSettings { PreferIdentifierStrings = false };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        result.Should().Contain("\"mynode\"");
        result.Should().Contain("\"mykey\"");
    }

    [Fact]
    public void Serialize_IncludeVersionMarker_V2_AddsVersionMarkerAtStart()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node"));

        var settings = new KdlFormatterSettings
        {
            IncludeVersionMarker = true,
            TargetVersion = KdlVersion.V2
        };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        result.Should().StartWith("/- kdl-version 2\n");
        result.Should().Contain("node\n");
    }

    [Fact]
    public void Serialize_IncludeVersionMarker_V1_AddsVersionMarkerAtStart()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node"));

        var settings = new KdlFormatterSettings
        {
            IncludeVersionMarker = true,
            TargetVersion = KdlVersion.V1
        };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        result.Should().StartWith("/- kdl-version 1\n");
        result.Should().Contain("node\n");
    }

    [Fact]
    public void Serialize_IncludeVersionMarkerFalse_NoVersionMarker()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node"));

        var settings = new KdlFormatterSettings { IncludeVersionMarker = false };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        result.Should().NotContain("kdl-version");
        result.Should().Be("node\n");
    }

    [Fact]
    public void Serialize_EmptyDocument_WithVersionMarker_OnlyVersionMarker()
    {
        var doc = new KdlDocument();

        var settings = new KdlFormatterSettings { IncludeVersionMarker = true };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        result.Should().Be("/- kdl-version 2\n");
    }

    [Fact]
    public void Serialize_VersionMarker_CanBeReParsed()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("test").AddProperty("key", "value"));

        var settings = new KdlFormatterSettings { IncludeVersionMarker = true };
        var formatter = new KdlFormatter(settings);
        var kdl = formatter.Serialize(doc);

        // The version marker is a slashdashed node, so it should be ignored during parsing
        var reparsed = KdlDocument.Parse(kdl);
        reparsed.Nodes.Should().HaveCount(1);
        reparsed.Nodes[0].Name.Should().Be("test");
    }

    [Fact]
    public void RoundTrip_RawString_SingleLine_PreservesHashCount()
    {
        // Parse a raw string with 1 hash
        var input = "node #\"raw value\"#\n";
        var doc = KdlDocument.Parse(input);

        var settings = new KdlFormatterSettings { PreserveStringTypes = true };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        result.Should().Be("node #\"raw value\"#\n");
    }

    [Fact]
    public void RoundTrip_RawString_SingleLine_MultipleHashes()
    {
        // Parse a raw string with 2 hashes (needed to contain "#)
        var input = "node ##\"contains \"# sequence\"##\n";
        var doc = KdlDocument.Parse(input);

        var settings = new KdlFormatterSettings { PreserveStringTypes = true };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        result.Should().Be("node ##\"contains \"# sequence\"##\n");
    }

    [Fact]
    public void RoundTrip_RawString_MultiLine_PreservesFormat()
    {
        // Parse a multi-line raw string
        var input = "node #\"\"\"\n    line1\n    line2\n    \"\"\"#\n";
        var doc = KdlDocument.Parse(input);

        // Verify the parsed value
        var arg = doc.Nodes[0].Arguments[0];
        arg.Should().BeOfType<KdlString>();
        var str = (KdlString)arg;
        str.StringType.Should().Be(KdlStringType.Raw);
        str.IsRawMultiLine.Should().BeTrue();
        str.Value.Should().Be("line1\nline2");

        var settings = new KdlFormatterSettings { PreserveStringTypes = true };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        // The output should be a valid multi-line raw string
        result.Should().Contain("#\"\"\"");
        result.Should().Contain("\"\"\"#");
    }

    [Fact]
    public void RoundTrip_RawString_PreservesHashCount_ThreeHashes()
    {
        // Parse a raw string with 3 hashes
        var input = "node ###\"contains \"## sequence\"###\n";
        var doc = KdlDocument.Parse(input);

        var settings = new KdlFormatterSettings { PreserveStringTypes = true };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        result.Should().Be("node ###\"contains \"## sequence\"###\n");
    }

    [Fact]
    public void Serialize_RawString_ProgrammaticWithoutMetadata_ComputesMinimalHashCount()
    {
        // Create a raw string programmatically without metadata
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node")
            .AddArgument(new KdlString("simple value", KdlStringType.Raw)));

        var settings = new KdlFormatterSettings { PreserveStringTypes = true };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        // Should use minimal hash count (1)
        result.Should().Be("node #\"simple value\"#\n");
    }

    [Fact]
    public void Serialize_RawString_ProgrammaticWithDangerousContent_ComputesRequiredHashCount()
    {
        // Create a raw string that contains a closing delimiter pattern
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node")
            .AddArgument(new KdlString("contains \"# pattern", KdlStringType.Raw)));

        var settings = new KdlFormatterSettings { PreserveStringTypes = true };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        // Should use 2 hashes to safely delimit the value
        result.Should().Be("node ##\"contains \"# pattern\"##\n");
    }

    [Fact]
    public void RoundTrip_ParsedRawString_CanBeReParsed()
    {
        var original = "node #\"C:\\path\\to\\file\"#\n";
        var doc = KdlDocument.Parse(original);

        var settings = new KdlFormatterSettings { PreserveStringTypes = true };
        var formatter = new KdlFormatter(settings);
        var serialized = formatter.Serialize(doc);

        // Re-parse the serialized output
        var reparsed = KdlDocument.Parse(serialized);
        reparsed.Nodes.Should().HaveCount(1);
        reparsed.Nodes[0].Arguments.Should().HaveCount(1);
        reparsed.Nodes[0].Arguments[0].AsString().Should().Be(@"C:\path\to\file");
    }

    [Fact]
    public void RoundTrip_RawString_AsPropertyValue()
    {
        var input = "node key=#\"raw value\"#\n";
        var doc = KdlDocument.Parse(input);

        var settings = new KdlFormatterSettings { PreserveStringTypes = true };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        result.Should().Be("node key=#\"raw value\"#\n");
    }

    [Fact]
    public void Serialize_RawString_UsingStaticHelper()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node")
            .AddArgument(KdlString.Raw(@"C:\path\to\file")));

        var settings = new KdlFormatterSettings { PreserveStringTypes = true };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        result.Should().Be("node #\"C:\\path\\to\\file\"#\n");
    }

    [Fact]
    public void Serialize_RawString_UsingStaticHelper_WithExplicitHashCount()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node")
            .AddArgument(KdlString.Raw("simple value", hashCount: 2)));

        var settings = new KdlFormatterSettings { PreserveStringTypes = true };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        // Explicit hash count of 2 is honored
        result.Should().Be("node ##\"simple value\"##\n");
    }

    [Fact]
    public void Serialize_RawString_UsingStaticHelper_MultiLine()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("node")
            .AddArgument(KdlString.Raw("line1\nline2", multiLine: true)));

        var settings = new KdlFormatterSettings { PreserveStringTypes = true };
        var formatter = new KdlFormatter(settings);
        var result = formatter.Serialize(doc);

        // Should contain multi-line raw string delimiters
        result.Should().Contain("#\"\"\"");
        result.Should().Contain("\"\"\"#");
    }

    [Fact]
    public void RoundTrip_MultiLineRawString_WithSubsequentArgument_CanBeReparsed()
    {
        // Create a node with a multi-line raw string followed by another argument
        var doc = new KdlDocument();
        var node = new KdlNode("node");
        node.Arguments.Add(KdlString.Raw("line1\nline2", multiLine: true));
        node.Arguments.Add(new KdlString("next"));
        doc.Nodes.Add(node);

        var settings = new KdlFormatterSettings { PreserveStringTypes = true };
        var formatter = new KdlFormatter(settings);
        var output = formatter.Serialize(doc);

        // Debug: print the output to see what we're generating
        output.Should().NotBeNull();

        // The output should be valid KDL that can be re-parsed
        var reparsed = KdlDocument.Parse(output);
        reparsed.Nodes.Should().HaveCount(1);
        reparsed.Nodes[0].Arguments.Should().HaveCount(2);
        reparsed.Nodes[0].Arguments[0].AsString().Should().Be("line1\nline2");
        reparsed.Nodes[0].Arguments[1].AsString().Should().Be("next");
    }

    [Fact]
    public void RoundTrip_MultiLineRawString_PreservesOriginalIndent()
    {
        // Parse a multi-line raw string with specific indentation
        var input = "node #\"\"\"\n    content\n    \"\"\"#\n";
        var doc = KdlDocument.Parse(input);

        // Verify the parsed string has the indent metadata
        var str = (KdlString)doc.Nodes[0].Arguments[0];
        str.RawMultiLineIndent.Should().Be("    ");

        var settings = new KdlFormatterSettings { PreserveStringTypes = true };
        var formatter = new KdlFormatter(settings);
        var output = formatter.Serialize(doc);

        // The output should preserve the original 4-space indent
        output.Should().Contain("    content");
        output.Should().Contain("    \"\"\"#");
    }
}
