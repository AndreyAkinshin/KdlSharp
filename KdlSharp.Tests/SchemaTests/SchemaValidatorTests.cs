using AwesomeAssertions;
using KdlSharp;
using KdlSharp.Schema;
using KdlSharp.Schema.Rules;
using Xunit;

namespace KdlSharp.Tests.SchemaTests;

public class SchemaValidatorTests
{
    [Fact]
    public void Validate_EmptyDocument_WithPermissiveSchema_Success()
    {
        var doc = new KdlDocument();
        var schema = KdlSchema.CreatePermissiveSchema();

        var result = KdlSchema.Validate(doc, schema);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithRequiredProperty_Missing_Fails()
    {
        var kdl = "node";
        var doc = KdlDocument.Parse(kdl);

        var schemaNode = new SchemaNode(
            name: "node",
            properties: new[]
            {
                new SchemaProperty("required-prop", required: true)
            });

        var schema = new SchemaDocument(
            new SchemaInfo { Title = "Test" },
            nodes: new[] { schemaNode });

        var result = KdlSchema.Validate(doc, schema);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].RuleName.Should().Be("required-property");
    }

    [Fact]
    public void Validate_WithRequiredProperty_Present_Success()
    {
        var kdl = "node required-prop=\"value\"";
        var doc = KdlDocument.Parse(kdl);

        var schemaNode = new SchemaNode(
            name: "node",
            properties: new[]
            {
                new SchemaProperty("required-prop", required: true)
            });

        var schema = new SchemaDocument(
            new SchemaInfo { Title = "Test" },
            nodes: new[] { schemaNode });

        var result = KdlSchema.Validate(doc, schema);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ValueWithMinLength_TooShort_Fails()
    {
        var kdl = "node prop=\"ab\"";
        var doc = KdlDocument.Parse(kdl);

        var schemaNode = new SchemaNode(
            name: "node",
            properties: new[]
            {
                new SchemaProperty("prop", validationRules: new ValidationRule[]
                {
                    new MinLengthRule(5)
                })
            });

        var schema = new SchemaDocument(
            new SchemaInfo { Title = "Test" },
            nodes: new[] { schemaNode });

        var result = KdlSchema.Validate(doc, schema);

        result.IsValid.Should().BeFalse();
        result.Errors[0].RuleName.Should().Be("min-length");
    }

    [Fact]
    public void Validate_ValueWithMinLength_LongEnough_Success()
    {
        var kdl = "node prop=\"abcdef\"";
        var doc = KdlDocument.Parse(kdl);

        var schemaNode = new SchemaNode(
            name: "node",
            properties: new[]
            {
                new SchemaProperty("prop", validationRules: new ValidationRule[]
                {
                    new MinLengthRule(5)
                })
            });

        var schema = new SchemaDocument(
            new SchemaInfo { Title = "Test" },
            nodes: new[] { schemaNode });

        var result = KdlSchema.Validate(doc, schema);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ValueWithPattern_Matching_Success()
    {
        var kdl = "node email=\"test@example.com\"";
        var doc = KdlDocument.Parse(kdl);

        var schemaNode = new SchemaNode(
            name: "node",
            properties: new[]
            {
                new SchemaProperty("email", validationRules: new ValidationRule[]
                {
                    new PatternRule(@"^[^@]+@[^@]+\.[^@]+$")
                })
            });

        var schema = new SchemaDocument(
            new SchemaInfo { Title = "Test" },
            nodes: new[] { schemaNode });

        var result = KdlSchema.Validate(doc, schema);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ValueWithPattern_NotMatching_Fails()
    {
        var kdl = "node email=\"not-an-email\"";
        var doc = KdlDocument.Parse(kdl);

        var schemaNode = new SchemaNode(
            name: "node",
            properties: new[]
            {
                new SchemaProperty("email", validationRules: new ValidationRule[]
                {
                    new PatternRule(@"^[^@]+@[^@]+\.[^@]+$")
                })
            });

        var schema = new SchemaDocument(
            new SchemaInfo { Title = "Test" },
            nodes: new[] { schemaNode });

        var result = KdlSchema.Validate(doc, schema);

        result.IsValid.Should().BeFalse();
        result.Errors[0].RuleName.Should().Be("pattern");
    }

    [Fact]
    public void Validate_ValueCount_BelowMin_Fails()
    {
        var kdl = "node 1";
        var doc = KdlDocument.Parse(kdl);

        var schemaNode = new SchemaNode(
            name: "node",
            values: new SchemaValue(min: 2, max: 5));

        var schema = new SchemaDocument(
            new SchemaInfo { Title = "Test" },
            nodes: new[] { schemaNode });

        var result = KdlSchema.Validate(doc, schema);

        result.IsValid.Should().BeFalse();
        result.Errors[0].RuleName.Should().Be("min-values");
    }

    [Fact]
    public void Validate_ValueCount_AboveMax_Fails()
    {
        var kdl = "node 1 2 3 4 5 6";
        var doc = KdlDocument.Parse(kdl);

        var schemaNode = new SchemaNode(
            name: "node",
            values: new SchemaValue(min: 2, max: 5));

        var schema = new SchemaDocument(
            new SchemaInfo { Title = "Test" },
            nodes: new[] { schemaNode });

        var result = KdlSchema.Validate(doc, schema);

        result.IsValid.Should().BeFalse();
        result.Errors[0].RuleName.Should().Be("max-values");
    }

    [Fact]
    public void Validate_ValueCount_WithinRange_Success()
    {
        var kdl = "node 1 2 3";
        var doc = KdlDocument.Parse(kdl);

        var schemaNode = new SchemaNode(
            name: "node",
            values: new SchemaValue(min: 2, max: 5));

        var schema = new SchemaDocument(
            new SchemaInfo { Title = "Test" },
            nodes: new[] { schemaNode });

        var result = KdlSchema.Validate(doc, schema);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_UnexpectedNode_WithStrictSchema_Fails()
    {
        var kdl = @"
            expected
            unexpected
        ";
        var doc = KdlDocument.Parse(kdl);

        var schemaNode = new SchemaNode(name: "expected");

        var schema = new SchemaDocument(
            new SchemaInfo { Title = "Test" },
            nodes: new[] { schemaNode },
            otherNodesAllowed: false);

        var result = KdlSchema.Validate(doc, schema);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.RuleName == "unexpected-node");
    }

    [Fact]
    public void Validate_ComplexSchema_Success()
    {
        var kdl = @"
            package name=""my-app"" version=""1.0.0"" {
                author ""Alice""
                description ""A test app""
            }
        ";
        var doc = KdlDocument.Parse(kdl);

        var authorNode = new SchemaNode(name: "author", values: new SchemaValue(min: 1, max: 1));
        var descNode = new SchemaNode(name: "description", values: new SchemaValue(min: 1, max: 1));

        var packageNode = new SchemaNode(
            name: "package",
            properties: new[]
            {
                new SchemaProperty("name", required: true),
                new SchemaProperty("version", required: true)
            },
            children: new SchemaChildren(
                nodes: new[] { authorNode, descNode },
                otherNodesAllowed: false));

        var schema = new SchemaDocument(
            new SchemaInfo { Title = "Package Schema" },
            nodes: new[] { packageNode });

        var result = KdlSchema.Validate(doc, schema);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void FormatErrors_DisplaysAllErrors()
    {
        var kdl = "node";
        var doc = KdlDocument.Parse(kdl);

        var schemaNode = new SchemaNode(
            name: "node",
            properties: new[]
            {
                new SchemaProperty("prop1", required: true),
                new SchemaProperty("prop2", required: true)
            });

        var schema = new SchemaDocument(
            new SchemaInfo { Title = "Test" },
            nodes: new[] { schemaNode });

        var result = KdlSchema.Validate(doc, schema);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);

        var formatted = result.FormatErrors();
        formatted.Should().Contain("prop1");
        formatted.Should().Contain("prop2");
    }

    [Fact]
    public void Validate_TypeRule_WithMatchingTypeAnnotation_Success()
    {
        var kdl = "node prop=(string)\"value\"";
        var doc = KdlDocument.Parse(kdl);

        var schemaNode = new SchemaNode(
            name: "node",
            properties: new[]
            {
                new SchemaProperty("prop", validationRules: new ValidationRule[]
                {
                    new TypeRule("string")
                })
            });

        var schema = new SchemaDocument(
            new SchemaInfo { Title = "Test" },
            nodes: new[] { schemaNode });

        var result = KdlSchema.Validate(doc, schema);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_TypeRule_WithMismatchedTypeAnnotation_Fails()
    {
        var kdl = "node prop=(number)42";
        var doc = KdlDocument.Parse(kdl);

        var schemaNode = new SchemaNode(
            name: "node",
            properties: new[]
            {
                new SchemaProperty("prop", validationRules: new ValidationRule[]
                {
                    new TypeRule("string")
                })
            });

        var schema = new SchemaDocument(
            new SchemaInfo { Title = "Test" },
            nodes: new[] { schemaNode });

        var result = KdlSchema.Validate(doc, schema);

        result.IsValid.Should().BeFalse();
        result.Errors[0].RuleName.Should().Be("type");
    }

    [Fact]
    public void Validate_TypeRule_WithoutTypeAnnotation_Fails()
    {
        var kdl = "node prop=\"value\"";
        var doc = KdlDocument.Parse(kdl);

        var schemaNode = new SchemaNode(
            name: "node",
            properties: new[]
            {
                new SchemaProperty("prop", validationRules: new ValidationRule[]
                {
                    new TypeRule("string")
                })
            });

        var schema = new SchemaDocument(
            new SchemaInfo { Title = "Test" },
            nodes: new[] { schemaNode });

        var result = KdlSchema.Validate(doc, schema);

        result.IsValid.Should().BeFalse();
        result.Errors[0].RuleName.Should().Be("type");
    }

    [Fact]
    public void Validate_TagRule_WithMatchingTagAnnotation_Success()
    {
        var kdl = "node prop=(custom-tag)\"value\"";
        var doc = KdlDocument.Parse(kdl);

        var schemaNode = new SchemaNode(
            name: "node",
            properties: new[]
            {
                new SchemaProperty("prop", validationRules: new ValidationRule[]
                {
                    new TagRule("custom-tag")
                })
            });

        var schema = new SchemaDocument(
            new SchemaInfo { Title = "Test" },
            nodes: new[] { schemaNode });

        var result = KdlSchema.Validate(doc, schema);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_TagRule_WithMismatchedTagAnnotation_Fails()
    {
        var kdl = "node prop=(wrong-tag)\"value\"";
        var doc = KdlDocument.Parse(kdl);

        var schemaNode = new SchemaNode(
            name: "node",
            properties: new[]
            {
                new SchemaProperty("prop", validationRules: new ValidationRule[]
                {
                    new TagRule("expected-tag")
                })
            });

        var schema = new SchemaDocument(
            new SchemaInfo { Title = "Test" },
            nodes: new[] { schemaNode });

        var result = KdlSchema.Validate(doc, schema);

        result.IsValid.Should().BeFalse();
        result.Errors[0].RuleName.Should().Be("tag");
    }

    [Fact]
    public void Validate_TagRule_WithoutTagAnnotation_Fails()
    {
        var kdl = "node prop=\"value\"";
        var doc = KdlDocument.Parse(kdl);

        var schemaNode = new SchemaNode(
            name: "node",
            properties: new[]
            {
                new SchemaProperty("prop", validationRules: new ValidationRule[]
                {
                    new TagRule("expected-tag")
                })
            });

        var schema = new SchemaDocument(
            new SchemaInfo { Title = "Test" },
            nodes: new[] { schemaNode });

        var result = KdlSchema.Validate(doc, schema);

        result.IsValid.Should().BeFalse();
        result.Errors[0].RuleName.Should().Be("tag");
    }
}


