using AwesomeAssertions;
using KdlSharp;
using KdlSharp.Values;
using Xunit;

namespace KdlSharp.Tests;

public class BasicParsingTests
{
    [Fact]
    public void Parse_SimpleNode_Success()
    {
        var kdl = "node";
        var doc = KdlDocument.Parse(kdl);

        doc.Nodes.Should().HaveCount(1);
        doc.Nodes[0].Name.Should().Be("node");
    }

    [Fact]
    public void Parse_NodeWithArgument_Success()
    {
        var kdl = "node \"arg1\"";
        var doc = KdlDocument.Parse(kdl);

        doc.Nodes[0].Arguments.Should().HaveCount(1);
        doc.Nodes[0].Arguments[0].AsString().Should().Be("arg1");
    }

    [Fact]
    public void Parse_NodeWithProperty_Success()
    {
        var kdl = "node key=\"value\"";
        var doc = KdlDocument.Parse(kdl);

        doc.Nodes[0].HasProperty("key").Should().BeTrue();
        doc.Nodes[0].GetProperty("key")!.AsString().Should().Be("value");
    }

    [Fact]
    public void Parse_NodeWithChildren_Success()
    {
        var kdl = @"
node {
    child
}";
        var doc = KdlDocument.Parse(kdl);

        doc.Nodes[0].Children.Should().HaveCount(1);
        doc.Nodes[0].Children[0].Name.Should().Be("child");
    }

    [Fact]
    public void Parse_MultipleNodes_Success()
    {
        var kdl = @"
node1
node2
";
        var doc = KdlDocument.Parse(kdl);

        doc.Nodes.Should().HaveCount(2);
        doc.Nodes[0].Name.Should().Be("node1");
        doc.Nodes[1].Name.Should().Be("node2");
    }

    [Fact]
    public void Parse_Number_Success()
    {
        var kdl = "node 123";
        var doc = KdlDocument.Parse(kdl);

        doc.Nodes[0].Arguments[0].AsNumber().Should().Be(123);
    }

    [Fact]
    public void Parse_Boolean_Success()
    {
        var kdl = "node #true #false";
        var doc = KdlDocument.Parse(kdl);

        doc.Nodes[0].Arguments[0].AsBoolean().Should().BeTrue();
        doc.Nodes[0].Arguments[1].AsBoolean().Should().BeFalse();
    }

    [Fact]
    public void Parse_Null_Success()
    {
        var kdl = "node #null";
        var doc = KdlDocument.Parse(kdl);

        doc.Nodes[0].Arguments[0].IsNull().Should().BeTrue();
    }

    [Fact]
    public void RoundTrip_SimpleDocument_Success()
    {
        var original = @"node ""arg"" key=""value"" {
    child 123
}";
        var doc = KdlDocument.Parse(original);
        var serialized = doc.ToKdlString();
        var doc2 = KdlDocument.Parse(serialized);

        doc2.Nodes[0].Name.Should().Be("node");
        doc2.Nodes[0].Arguments[0].AsString().Should().Be("arg");
        doc2.Nodes[0].GetProperty("key")!.AsString().Should().Be("value");
        doc2.Nodes[0].Children[0].Name.Should().Be("child");
        doc2.Nodes[0].Children[0].Arguments[0].AsNumber().Should().Be(123);
    }

    [Fact]
    public void Parse_PositiveInfinity_Success()
    {
        var kdl = "node #inf";
        var doc = KdlDocument.Parse(kdl);

        var value = doc.Nodes[0].Arguments[0] as KdlNumber;
        value.Should().NotBeNull();
        value!.IsSpecial.Should().BeTrue();
        value.IsPositiveInfinity.Should().BeTrue();
        value.IsNegativeInfinity.Should().BeFalse();
        value.IsNaN.Should().BeFalse();
        value.Format.Should().Be(KdlNumberFormat.Infinity);

        // AsDouble should return actual positive infinity
        value.AsDoubleValue().Should().Be(double.PositiveInfinity);

        // AsNumber returns null for special values
        value.AsNumber().Should().BeNull();
    }

    [Fact]
    public void Parse_NegativeInfinity_Success()
    {
        var kdl = "node #-inf";
        var doc = KdlDocument.Parse(kdl);

        var value = doc.Nodes[0].Arguments[0] as KdlNumber;
        value.Should().NotBeNull();
        value!.IsSpecial.Should().BeTrue();
        value.IsPositiveInfinity.Should().BeFalse();
        value.IsNegativeInfinity.Should().BeTrue();
        value.IsNaN.Should().BeFalse();
        value.Format.Should().Be(KdlNumberFormat.Infinity);

        // AsDouble should return actual negative infinity
        value.AsDoubleValue().Should().Be(double.NegativeInfinity);
    }

    [Fact]
    public void Parse_NaN_Success()
    {
        var kdl = "node #nan";
        var doc = KdlDocument.Parse(kdl);

        var value = doc.Nodes[0].Arguments[0] as KdlNumber;
        value.Should().NotBeNull();
        value!.IsSpecial.Should().BeTrue();
        value.IsPositiveInfinity.Should().BeFalse();
        value.IsNegativeInfinity.Should().BeFalse();
        value.IsNaN.Should().BeTrue();
        value.Format.Should().Be(KdlNumberFormat.NaN);

        // AsDouble should return actual NaN
        double.IsNaN(value.AsDoubleValue()!.Value).Should().BeTrue();
    }

    [Fact]
    public void RoundTrip_SpecialNumbers_Success()
    {
        var kdl = "node #inf #-inf #nan";
        var doc = KdlDocument.Parse(kdl);
        var serialized = doc.ToKdlString();

        serialized.Should().Contain("#inf");
        serialized.Should().Contain("#-inf");
        serialized.Should().Contain("#nan");

        // Verify round-trip
        var doc2 = KdlDocument.Parse(serialized);
        var args = doc2.Nodes[0].Arguments;

        ((KdlNumber)args[0]).IsPositiveInfinity.Should().BeTrue();
        ((KdlNumber)args[1]).IsNegativeInfinity.Should().BeTrue();
        ((KdlNumber)args[2]).IsNaN.Should().BeTrue();
    }

    [Fact]
    public void SpecialNumber_Clone_PreservesSemantics()
    {
        var posInf = KdlNumber.PositiveInfinity();
        var negInf = KdlNumber.NegativeInfinity();
        var nan = KdlNumber.NaN();

        var clonedPosInf = posInf.Clone() as KdlNumber;
        var clonedNegInf = negInf.Clone() as KdlNumber;
        var clonedNan = nan.Clone() as KdlNumber;

        clonedPosInf!.IsPositiveInfinity.Should().BeTrue();
        clonedPosInf.AsDoubleValue().Should().Be(double.PositiveInfinity);

        clonedNegInf!.IsNegativeInfinity.Should().BeTrue();
        clonedNegInf.AsDoubleValue().Should().Be(double.NegativeInfinity);

        clonedNan!.IsNaN.Should().BeTrue();
        double.IsNaN(clonedNan.AsDoubleValue()!.Value).Should().BeTrue();
    }

    [Fact]
    public void SpecialNumber_Value_ThrowsForSpecialValues()
    {
        var posInf = KdlNumber.PositiveInfinity();

        var act = () => _ = posInf.Value;

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*decimal*");
    }

    [Fact]
    public void TryParse_ValidKdl_ReturnsTrue()
    {
        var success = KdlDocument.TryParse("node 123", out var doc);

        success.Should().BeTrue();
        doc.Should().NotBeNull();
        doc!.Nodes.Should().HaveCount(1);
    }

    [Fact]
    public void TryParse_InvalidKdl_ReturnsFalse()
    {
        var success = KdlDocument.TryParse("node }", out var doc);

        success.Should().BeFalse();
        doc.Should().BeNull();
    }

    [Fact]
    public void TryParse_WithError_InvalidKdl_ReturnsError()
    {
        var success = KdlDocument.TryParse("node }", out var doc, out var error);

        success.Should().BeFalse();
        doc.Should().BeNull();
        error.Should().NotBeNull();
        error!.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TryParse_NullInput_ReturnsFalse()
    {
        var success = KdlDocument.TryParse(null, out var doc);

        success.Should().BeFalse();
        doc.Should().BeNull();
    }

    [Fact]
    public void TryParse_WithError_NullInput_ReturnsFalseWithNullError()
    {
        var success = KdlDocument.TryParse(null, out var doc, out var error);

        success.Should().BeFalse();
        doc.Should().BeNull();
        error.Should().BeNull(); // No parse error for null input, just returns false
    }

    [Fact]
    public void TryParse_EmptyString_ReturnsEmptyDocument()
    {
        var success = KdlDocument.TryParse("", out var doc);

        success.Should().BeTrue();
        doc.Should().NotBeNull();
        doc!.Nodes.Should().BeEmpty();
    }

    [Fact]
    public void Parse_DuplicateProperties_PreservesAll()
    {
        var kdl = "node key=\"first\" key=\"second\" key=\"last\"";
        var doc = KdlDocument.Parse(kdl);

        // All duplicate properties should be preserved in the document model
        doc.Nodes[0].Properties.Should().HaveCount(3);
        doc.Nodes[0].Properties[0].Key.Should().Be("key");
        doc.Nodes[0].Properties[0].Value.AsString().Should().Be("first");
        doc.Nodes[0].Properties[1].Key.Should().Be("key");
        doc.Nodes[0].Properties[1].Value.AsString().Should().Be("second");
        doc.Nodes[0].Properties[2].Key.Should().Be("key");
        doc.Nodes[0].Properties[2].Value.AsString().Should().Be("last");
    }

    [Fact]
    public void Parse_DuplicateProperties_GetPropertyReturnsRightmost()
    {
        var kdl = "node key=\"first\" key=\"second\" key=\"last\"";
        var doc = KdlDocument.Parse(kdl);

        // GetProperty should return the rightmost value per KDL spec
        doc.Nodes[0].GetProperty("key")!.AsString().Should().Be("last");
    }

    [Fact]
    public void Parse_DuplicateProperties_GetAllPropertiesReturnsAll()
    {
        var kdl = "node key=\"first\" key=\"second\" key=\"last\"";
        var doc = KdlDocument.Parse(kdl);

        var allValues = doc.Nodes[0].GetAllProperties("key").ToList();
        allValues.Should().HaveCount(3);
        allValues[0].AsString().Should().Be("first");
        allValues[1].AsString().Should().Be("second");
        allValues[2].AsString().Should().Be("last");
    }

    [Fact]
    public void RoundTrip_DuplicateProperties_PreservesAll()
    {
        var kdl = "node key=\"first\" key=\"second\" key=\"last\"";
        var doc = KdlDocument.Parse(kdl);
        var serialized = doc.ToKdlString();
        var doc2 = KdlDocument.Parse(serialized);

        // After round-trip, all duplicates should still be present
        doc2.Nodes[0].Properties.Should().HaveCount(3);
    }

    #region String Type Preservation Tests

    [Fact]
    public void Parse_IdentifierString_PreservesStringType()
    {
        var kdl = "node identifier-value";
        var doc = KdlDocument.Parse(kdl);

        var str = doc.Nodes[0].Arguments[0] as KdlString;
        str.Should().NotBeNull();
        str!.StringType.Should().Be(KdlStringType.Identifier);
    }

    [Fact]
    public void Parse_QuotedString_PreservesStringType()
    {
        var kdl = "node \"quoted value\"";
        var doc = KdlDocument.Parse(kdl);

        var str = doc.Nodes[0].Arguments[0] as KdlString;
        str.Should().NotBeNull();
        str!.StringType.Should().Be(KdlStringType.Quoted);
    }

    [Fact]
    public void Parse_RawString_PreservesStringType()
    {
        var kdl = "node #\"raw string\"#";
        var doc = KdlDocument.Parse(kdl);

        var str = doc.Nodes[0].Arguments[0] as KdlString;
        str.Should().NotBeNull();
        str!.StringType.Should().Be(KdlStringType.Raw);
    }

    [Fact]
    public void Parse_MultiLineString_PreservesStringType()
    {
        var kdl = "node \"\"\"\n    multi-line\n    \"\"\"";
        var doc = KdlDocument.Parse(kdl);

        var str = doc.Nodes[0].Arguments[0] as KdlString;
        str.Should().NotBeNull();
        str!.StringType.Should().Be(KdlStringType.MultiLine);
    }

    [Fact]
    public void RoundTrip_IdentifierString_PreservesFormat_WhenSettingEnabled()
    {
        var kdl = "node identifier-value";
        var doc = KdlDocument.Parse(kdl);

        // Use formatter with PreserveStringTypes enabled
        var settings = new KdlSharp.Settings.KdlFormatterSettings { PreserveStringTypes = true };
        var formatter = new KdlSharp.Formatting.KdlFormatter(settings);
        var serialized = formatter.Serialize(doc);

        // Identifier strings should remain unquoted in output
        serialized.Should().Contain("identifier-value");
        serialized.Should().NotContain("\"identifier-value\"");
    }

    [Fact]
    public void RoundTrip_QuotedString_PreservesFormat_WhenSettingEnabled()
    {
        var kdl = "node \"quoted value\"";
        var doc = KdlDocument.Parse(kdl);

        // Use formatter with PreserveStringTypes enabled
        var settings = new KdlSharp.Settings.KdlFormatterSettings { PreserveStringTypes = true };
        var formatter = new KdlSharp.Formatting.KdlFormatter(settings);
        var serialized = formatter.Serialize(doc);

        // Quoted strings should remain quoted in output
        serialized.Should().Contain("\"quoted value\"");
    }

    [Fact]
    public void RoundTrip_RawString_PreservesFormat_WhenSettingEnabled()
    {
        var kdl = "node #\"raw value\"#";
        var doc = KdlDocument.Parse(kdl);

        // Use formatter with PreserveStringTypes enabled
        var settings = new KdlSharp.Settings.KdlFormatterSettings { PreserveStringTypes = true };
        var formatter = new KdlSharp.Formatting.KdlFormatter(settings);
        var serialized = formatter.Serialize(doc);

        // Raw strings should be serialized in raw format
        serialized.Should().Contain("#\"raw value\"#");
    }

    #endregion
}

