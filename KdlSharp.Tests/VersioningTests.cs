using AwesomeAssertions;
using KdlSharp;
using KdlSharp.Exceptions;
using KdlSharp.Settings;
using Xunit;

namespace KdlSharp.Tests;

public class VersioningTests
{
    [Fact]
    public void Parse_V2Syntax_DefaultParserSucceeds()
    {
        var kdl = "node #true #false #null";
        var doc = KdlDocument.Parse(kdl);

        doc.Nodes.Should().HaveCount(1);
        doc.Nodes[0].Arguments.Should().HaveCount(3);
    }

    [Fact]
    public void Parse_V1BareKeywords_DefaultParserFails()
    {
        var kdl = "node true false null";

        var ex = Assert.Throws<KdlParseException>(() => KdlDocument.Parse(kdl));
        ex.Message.Should().Contain("Bare keyword");
        ex.Message.Should().Contain("KDL v2");
    }

    [Fact]
    public void Parse_V1BareKeywords_V1ParserSucceeds()
    {
        var kdl = "node true false null";
        var settings = new KdlParserSettings { TargetVersion = KdlVersion.V1 };
        var doc = KdlDocument.Parse(kdl, settings);

        doc.Nodes[0].Arguments.Should().HaveCount(3);
        doc.Nodes[0].Arguments[0].AsBoolean().Should().BeTrue();
        doc.Nodes[0].Arguments[1].AsBoolean().Should().BeFalse();
        doc.Nodes[0].Arguments[2].IsNull().Should().BeTrue();
    }

    [Fact]
    public void Parse_V2PrefixedKeywords_V1ParserFails()
    {
        var kdl = "node #true";
        var settings = new KdlParserSettings { TargetVersion = KdlVersion.V1 };

        // In v1, # starts a raw string or is unknown
        var ex = Assert.Throws<KdlParseException>(() => KdlDocument.Parse(kdl, settings));
    }

    [Fact]
    public void Parse_NumberWithUnderscores_V2Succeeds()
    {
        var kdl = "node 1_000_000";
        var doc = KdlDocument.Parse(kdl);

        doc.Nodes[0].Arguments[0].AsNumber().Should().Be(1000000);
    }

    [Fact]
    public void Parse_NumberWithUnderscores_V1Fails()
    {
        var kdl = "node 1_000_000";
        var settings = new KdlParserSettings { TargetVersion = KdlVersion.V1 };

        var ex = Assert.Throws<KdlParseException>(() => KdlDocument.Parse(kdl, settings));
        ex.Message.Should().Contain("Underscores in numbers");
        ex.Message.Should().Contain("KDL v1");
    }

    [Fact]
    public void Parse_VersionMarker_V2()
    {
        var kdl = "/- kdl-version 2\nnode #true";
        var doc = KdlDocument.Parse(kdl);

        doc.Version.Should().Be(KdlVersion.V2);
        doc.Nodes[0].Arguments[0].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Parse_VersionMarker_V1()
    {
        var kdl = "/- kdl-version 1\nnode true";
        var settings = new KdlParserSettings { TargetVersion = KdlVersion.V1 };
        var doc = KdlDocument.Parse(kdl, settings);

        doc.Version.Should().Be(KdlVersion.V1);
        doc.Nodes[0].Arguments[0].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Parse_VersionMarkerConflict_Throws()
    {
        var kdl = "/- kdl-version 1\nnode true";
        var settings = new KdlParserSettings { TargetVersion = KdlVersion.V2 };

        var ex = Assert.Throws<KdlParseException>(() => KdlDocument.Parse(kdl, settings));
        ex.Message.Should().Contain("kdl-version 1");
        ex.Message.Should().Contain("configured for V2");
    }

    [Fact]
    public void Parse_AutoMode_DetectsV2()
    {
        var kdl = "node #true";
        var settings = new KdlParserSettings { TargetVersion = KdlVersion.Auto };
        var doc = KdlDocument.Parse(kdl, settings);

        doc.Version.Should().Be(KdlVersion.V2);
    }

    [Fact]
    public void Parse_AutoMode_FallsBackToV1()
    {
        var kdl = "node true";
        var settings = new KdlParserSettings { TargetVersion = KdlVersion.Auto };
        var doc = KdlDocument.Parse(kdl, settings);

        doc.Version.Should().Be(KdlVersion.V1);
    }

    [Fact]
    public void Parse_AutoMode_WithV1VersionMarker_UsesV1()
    {
        var kdl = "/- kdl-version 1\nnode true false null";
        var settings = new KdlParserSettings { TargetVersion = KdlVersion.Auto };
        var doc = KdlDocument.Parse(kdl, settings);

        doc.Version.Should().Be(KdlVersion.V1);
        doc.Nodes[0].Arguments[0].AsBoolean().Should().BeTrue();
        doc.Nodes[0].Arguments[1].AsBoolean().Should().BeFalse();
        doc.Nodes[0].Arguments[2].IsNull().Should().BeTrue();
    }

    [Fact]
    public void Parse_AutoMode_WithV2VersionMarker_UsesV2()
    {
        var kdl = "/- kdl-version 2\nnode #true #false #null";
        var settings = new KdlParserSettings { TargetVersion = KdlVersion.Auto };
        var doc = KdlDocument.Parse(kdl, settings);

        doc.Version.Should().Be(KdlVersion.V2);
        doc.Nodes[0].Arguments[0].AsBoolean().Should().BeTrue();
        doc.Nodes[0].Arguments[1].AsBoolean().Should().BeFalse();
        doc.Nodes[0].Arguments[2].IsNull().Should().BeTrue();
    }

    [Fact]
    public void Parse_AutoMode_AmbiguousDocument_DefaultsToV2()
    {
        // Document that is valid in both v1 and v2
        var kdl = "node \"hello\" 123";
        var settings = new KdlParserSettings { TargetVersion = KdlVersion.Auto };
        var doc = KdlDocument.Parse(kdl, settings);

        // Without explicit markers or v2-specific syntax, defaults to v2
        doc.Version.Should().Be(KdlVersion.V2);
        doc.Nodes[0].Arguments[0].AsString().Should().Be("hello");
        doc.Nodes[0].Arguments[1].AsNumber().Should().Be(123);
    }

    [Fact]
    public void Parse_AutoMode_V2SpecificSyntax_FailsV1Fallback()
    {
        // V2 prefixed keywords can't parse in v1
        var kdl = "node #true";
        var settings = new KdlParserSettings { TargetVersion = KdlVersion.Auto };
        var doc = KdlDocument.Parse(kdl, settings);

        // Should successfully parse as v2
        doc.Version.Should().Be(KdlVersion.V2);
        doc.Nodes[0].Arguments[0].AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void Parse_AutoMode_V1OnlySyntax_FallsBackFromV2()
    {
        // Bare keywords only work in v1
        var kdl = "node true false null";
        var settings = new KdlParserSettings { TargetVersion = KdlVersion.Auto };
        var doc = KdlDocument.Parse(kdl, settings);

        // Should fall back to v1 after v2 fails
        doc.Version.Should().Be(KdlVersion.V1);
    }

    [Fact]
    public void Parse_AutoMode_UnderscoresInNumbers_UsesV2()
    {
        var kdl = "node 1_000_000";
        var settings = new KdlParserSettings { TargetVersion = KdlVersion.Auto };
        var doc = KdlDocument.Parse(kdl, settings);

        doc.Version.Should().Be(KdlVersion.V2);
        doc.Nodes[0].Arguments[0].AsNumber().Should().Be(1000000);
    }

    [Fact]
    public void Parse_AutoMode_MixedVersionSyntax_Fails()
    {
        // Using both v1 bare keywords and v2 syntax in same document
        // V2 parser will fail on bare 'true', v1 parser will fail on '#false'
        var kdl = "node true #false";
        var settings = new KdlParserSettings { TargetVersion = KdlVersion.Auto };

        // Neither version can parse this, so it should fail
        var ex = Assert.Throws<KdlParseException>(() => KdlDocument.Parse(kdl, settings));
    }

    [Fact]
    public void Parse_V2MarkerWithV1Syntax_ThrowsParseError()
    {
        // Version marker says v2, but content uses v1 bare keywords
        var kdl = "/- kdl-version 2\nnode true";
        var settings = new KdlParserSettings { TargetVersion = KdlVersion.Auto };

        // Auto mode tries V2 first - it fails on bare 'true' (V1 syntax)
        // Then it falls back to V1, which detects the V2 marker conflict
        var ex = Assert.Throws<KdlParseException>(() => KdlDocument.Parse(kdl, settings));
        // The V1 parser detects the conflicting version marker
        ex.Message.Should().Contain("kdl-version 2");
    }

    [Fact]
    public void Parse_V1MarkerWithV2Syntax_ThrowsConflict()
    {
        // Version marker says v1, but content uses v2 prefixed keywords
        var kdl = "/- kdl-version 1\nnode #true";
        var settings = new KdlParserSettings { TargetVersion = KdlVersion.Auto };

        // When marker specifies v1, we use v1 parser which should fail on '#true'
        var ex = Assert.Throws<KdlParseException>(() => KdlDocument.Parse(kdl, settings));
    }

    [Fact]
    public void Parse_AutoMode_SpecialNumbers_UsesV2()
    {
        var kdl = "node #inf #-inf #nan";
        var settings = new KdlParserSettings { TargetVersion = KdlVersion.Auto };
        var doc = KdlDocument.Parse(kdl, settings);

        doc.Version.Should().Be(KdlVersion.V2);
    }

    [Fact]
    public void Parse_AutoMode_MultilineString_UsesV2()
    {
        var kdl = "node \"\"\"\nmulti\nline\n\"\"\"";
        var settings = new KdlParserSettings { TargetVersion = KdlVersion.Auto };
        var doc = KdlDocument.Parse(kdl, settings);

        doc.Version.Should().Be(KdlVersion.V2);
    }

    [Fact]
    public void Formatter_IncludeVersionMarker_V2()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("test").AddArgument(true));

        var formatter = new Formatting.KdlFormatter(new KdlFormatterSettings
        {
            IncludeVersionMarker = true,
            TargetVersion = KdlVersion.V2
        });

        var kdl = formatter.Serialize(doc);

        kdl.Should().StartWith("/- kdl-version 2\n");
        kdl.Should().Contain("#true");
    }

    [Fact]
    public void Formatter_IncludeVersionMarker_V1()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("test").AddArgument(true));

        var formatter = new Formatting.KdlFormatter(new KdlFormatterSettings
        {
            IncludeVersionMarker = true,
            TargetVersion = KdlVersion.V1
        });

        var kdl = formatter.Serialize(doc);

        kdl.Should().StartWith("/- kdl-version 1\n");
        kdl.Should().Contain("true");
        kdl.Should().NotContain("#true");
    }

    [Fact]
    public void Formatter_NoVersionMarker_ByDefault()
    {
        var doc = new KdlDocument();
        doc.Nodes.Add(new KdlNode("test").AddArgument(true));

        var formatter = new Formatting.KdlFormatter();
        var kdl = formatter.Serialize(doc);

        kdl.Should().NotStartWith("/-");
    }
}

