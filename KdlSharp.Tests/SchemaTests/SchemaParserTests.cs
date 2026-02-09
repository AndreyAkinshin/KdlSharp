using AwesomeAssertions;
using KdlSharp.Schema;
using Xunit;

namespace KdlSharp.Tests.SchemaTests;

public class SchemaParserTests
{
    [Fact]
    public void Parse_MinimalSchema_Success()
    {
        var kdl = @"
document {
    info {
        title ""Test Schema""
    }
}";

        var schema = KdlSchema.Parse(kdl);

        schema.Should().NotBeNull();
        schema.Info.Title.Should().Be("Test Schema");
    }

    [Fact]
    public void Parse_SchemaWithNodeDefinition_Success()
    {
        var kdl = @"
document {
    info {
        title ""Package Schema""
    }
    node ""package"" {
        prop ""name"" {
            required #true
        }
        prop ""version"" {
            required #true
        }
    }
}";

        var schema = KdlSchema.Parse(kdl);

        schema.Nodes.Should().HaveCount(1);
        schema.Nodes[0].Name.Should().Be("package");
        schema.Nodes[0].Properties.Should().HaveCount(2);

        var nameProp = schema.Nodes[0].Properties.First(p => p.Key == "name");
        nameProp.Required.Should().BeTrue();

        var versionProp = schema.Nodes[0].Properties.First(p => p.Key == "version");
        versionProp.Required.Should().BeTrue();
    }

    [Fact]
    public void Parse_SchemaWithValues_Success()
    {
        var kdl = @"
document {
    info {
        title ""Config Schema""
    }
    node ""setting"" {
        value {
            min 1
            max 3
        }
    }
}";

        var schema = KdlSchema.Parse(kdl);

        schema.Nodes[0].Values.Should().NotBeNull();
        schema.Nodes[0].Values!.Min.Should().Be(1);
        schema.Nodes[0].Values!.Max.Should().Be(3);
    }

    [Fact]
    public void Parse_SchemaWithChildren_Success()
    {
        var kdl = @"
document {
    info {
        title ""Nested Schema""
    }
    node ""parent"" {
        children {
            node ""child"" {
                prop ""id""
            }
        }
    }
}";

        var schema = KdlSchema.Parse(kdl);

        schema.Nodes[0].Children.Should().NotBeNull();
        schema.Nodes[0].Children!.Nodes.Should().HaveCount(1);
        schema.Nodes[0].Children!.Nodes[0].Name.Should().Be("child");
        schema.Nodes[0].Children!.Nodes[0].Properties.Should().HaveCount(1);
        schema.Nodes[0].Children!.Nodes[0].Properties[0].Key.Should().Be("id");
    }

    [Fact]
    public void Parse_SchemaWithStringValidations_Success()
    {
        var kdl = @"
document {
    info {
        title ""String Validation Schema""
    }
    node ""user"" {
        prop ""name"" {
            min-length 3
            max-length 50
            pattern ""^[a-zA-Z]+$""
        }
    }
}";

        var schema = KdlSchema.Parse(kdl);

        var prop = schema.Nodes[0].Properties[0];
        prop.ValidationRules.Should().HaveCount(3);
        prop.ValidationRules.Should().Contain(r => r.RuleName == "min-length");
        prop.ValidationRules.Should().Contain(r => r.RuleName == "max-length");
        prop.ValidationRules.Should().Contain(r => r.RuleName == "pattern");
    }

    [Fact]
    public void Parse_SchemaWithNumberValidations_Success()
    {
        var kdl = @"
document {
    info {
        title ""Number Validation Schema""
    }
    node ""config"" {
        prop ""port"" {
            "">"" 1024
            ""<="" 65535
        }
    }
}";

        var schema = KdlSchema.Parse(kdl);

        var prop = schema.Nodes[0].Properties[0];
        prop.ValidationRules.Should().HaveCount(2);
        prop.ValidationRules.Should().Contain(r => r.RuleName == ">");
        prop.ValidationRules.Should().Contain(r => r.RuleName == "<=");
    }

    [Fact]
    public void Parse_SchemaWithTypeValidation_Success()
    {
        var kdl = @"
document {
    info {
        title ""Type Schema""
    }
    node ""data"" {
        prop ""value"" {
            type ""string""
        }
    }
}";

        var schema = KdlSchema.Parse(kdl);

        var prop = schema.Nodes[0].Properties[0];
        prop.ValidationRules.Should().Contain(r => r.RuleName == "type");
    }

    [Fact]
    public void Parse_SchemaWithEnumValidation_Success()
    {
        var kdl = @"
document {
    info {
        title ""Enum Schema""
    }
    node ""setting"" {
        prop ""mode"" {
            enum ""dev"" ""test"" ""prod""
        }
    }
}";

        var schema = KdlSchema.Parse(kdl);

        var prop = schema.Nodes[0].Properties[0];
        prop.ValidationRules.Should().Contain(r => r.RuleName == "enum");
    }

    [Fact]
    public void Parse_SchemaWithInfoMetadata_Success()
    {
        var kdl = @"
document {
    info {
        title ""Full Info Schema""
        description ""A complete schema with all metadata""
        version ""1.0.0""
        author ""Alice""
        author ""Bob""
        license ""MIT""
        link ""https://example.com"" rel=""documentation""
    }
}";

        var schema = KdlSchema.Parse(kdl);

        schema.Info.Title.Should().Be("Full Info Schema");
        schema.Info.Description.Should().Be("A complete schema with all metadata");
        schema.Info.Version.Should().Be("1.0.0");
        schema.Info.Authors.Should().HaveCount(2);
        schema.Info.Authors.Should().Contain("Alice");
        schema.Info.Authors.Should().Contain("Bob");
        schema.Info.License.Should().Be("MIT");
        schema.Info.Links.Should().HaveCount(1);
        schema.Info.Links[0].Url.Should().Be("https://example.com");
        schema.Info.Links[0].Rel.Should().Be("documentation");
    }

    [Fact]
    public void Parse_SchemaWithOtherNodesAllowed_Success()
    {
        var kdl = @"
document {
    info {
        title ""Permissive Schema""
    }
    other-nodes-allowed #true
}";

        var schema = KdlSchema.Parse(kdl);

        schema.OtherNodesAllowed.Should().BeTrue();
    }

    [Fact]
    public void Parse_SchemaWithOtherNodesNotAllowed_Success()
    {
        var kdl = @"
document {
    info {
        title ""Strict Schema""
    }
    other-nodes-allowed #false
    node ""allowed""
}";

        var schema = KdlSchema.Parse(kdl);

        schema.OtherNodesAllowed.Should().BeFalse();
    }

    [Fact]
    public void Parse_SchemaWithTags_Success()
    {
        var kdl = @"
document {
    info {
        title ""Tag Schema""
    }
    tag ""important"" {
        node ""task""
    }
}";

        var schema = KdlSchema.Parse(kdl);

        schema.Tags.Should().HaveCount(1);
        schema.Tags[0].Name.Should().Be("important");
    }

    [Fact]
    public void Parse_SchemaWithMultipleNodes_Success()
    {
        var kdl = @"
document {
    info {
        title ""Multi-Node Schema""
    }
    node ""config""
    node ""data""
    node ""metadata""
}";

        var schema = KdlSchema.Parse(kdl);

        schema.Nodes.Should().HaveCount(3);
        schema.Nodes.Select(n => n.Name).Should().Contain(new[] { "config", "data", "metadata" });
    }

    [Fact]
    public void Parse_SchemaWithPropertyDescriptions_Success()
    {
        var kdl = @"
document {
    info {
        title ""Described Schema""
    }
    node ""user"" description=""User entity"" {
        prop ""name"" description=""User's full name""
    }
}";

        var schema = KdlSchema.Parse(kdl);

        schema.Nodes[0].Description.Should().Be("User entity");
        schema.Nodes[0].Properties[0].Description.Should().Be("User's full name");
    }

    [Fact]
    public void Parse_SchemaWithFormatValidation_Success()
    {
        var kdl = @"
document {
    info {
        title ""Format Schema""
    }
    node ""user"" {
        prop ""email"" {
            format ""email""
        }
        prop ""website"" {
            format ""url""
        }
    }
}";

        var schema = KdlSchema.Parse(kdl);

        var emailProp = schema.Nodes[0].Properties.First(p => p.Key == "email");
        emailProp.ValidationRules.Should().Contain(r => r.RuleName == "format");

        var websiteProp = schema.Nodes[0].Properties.First(p => p.Key == "website");
        websiteProp.ValidationRules.Should().Contain(r => r.RuleName == "format");
    }

    [Fact]
    public void Parse_SchemaWithoutDocumentNode_Throws()
    {
        var kdl = @"
info {
    title ""Invalid Schema""
}";

        var ex = Assert.Throws<Exceptions.KdlSchemaException>(() => KdlSchema.Parse(kdl));
        ex.Message.Should().Contain("document");
    }

    [Fact]
    public void Parse_EmptyDocument_Success()
    {
        var kdl = @"
document {
    info {
        title ""Empty Schema""
    }
}";

        var schema = KdlSchema.Parse(kdl);

        schema.Nodes.Should().BeEmpty();
        schema.OtherNodesAllowed.Should().BeFalse(); // Default
    }

    [Fact]
    public void Parse_SchemaWithModuloValidation_Success()
    {
        var kdl = @"
document {
    info {
        title ""Modulo Schema""
    }
    node ""even"" {
        prop ""value"" {
            % 2
        }
    }
}";

        var schema = KdlSchema.Parse(kdl);

        var prop = schema.Nodes[0].Properties[0];
        prop.ValidationRules.Should().Contain(r => r.RuleName == "%");
    }

    [Fact]
    public void Parse_SchemaWithMinMaxRules_Success()
    {
        var kdl = @"
document {
    info {
        title ""MinMax Schema""
    }
    node ""list"" {
        min 1
        max 10
    }
}";

        var schema = KdlSchema.Parse(kdl);

        schema.Nodes[0].ValidationRules.Should().Contain(r => r.RuleName == "min");
        schema.Nodes[0].ValidationRules.Should().Contain(r => r.RuleName == "max");
    }
}

