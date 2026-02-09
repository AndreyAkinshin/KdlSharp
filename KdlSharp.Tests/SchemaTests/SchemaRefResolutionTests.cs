using AwesomeAssertions;
using KdlSharp.Exceptions;
using KdlSharp.Schema;
using Xunit;

namespace KdlSharp.Tests.SchemaTests;

/// <summary>
/// Tests for schema reference resolution using KDL Query.
/// </summary>
public class SchemaRefResolutionTests
{
    [Fact]
    public void Parse_SchemaWithDefinitions_Success()
    {
        var kdl = @"
document {
    info {
        title ""Schema with Definitions""
    }
    definitions {
        node ""common-string"" id=""common-string"" {
            type ""string""
            min-length 1
            max-length 100
        }
        prop ""common-id"" id=""common-id"" {
            required #true
            type ""string""
        }
    }
    node ""user"" {
        prop ""id"" {
            required #true
        }
        prop ""name"" {
            min-length 1
        }
    }
}";

        var schema = KdlSchema.Parse(kdl);

        // Verify definitions were parsed
        schema.Definitions.Should().NotBeNull();
        schema.Definitions!.NodeDefinitions.Should().ContainKey("common-string");
        schema.Definitions.PropertyDefinitions.Should().ContainKey("common-id");
    }

    [Fact]
    public void Parse_SchemaWithRefInDefinitions_StoresDefinitions()
    {
        // This test verifies that definitions with IDs are stored correctly
        // Full ref resolution would merge these into referencing nodes
        var kdl = @"
document {
    info {
        title ""Ref Example""
    }
    definitions {
        value id=""positive-int"" {
            type ""integer""
            "">""  0
        }
        prop ""id-prop"" id=""id-prop"" {
            required #true
            type ""string""
            pattern ""^[a-z0-9-]+$""
        }
    }
}";

        var schema = KdlSchema.Parse(kdl);

        schema.Definitions.Should().NotBeNull();
        schema.Definitions!.ValueDefinitions.Should().ContainKey("positive-int");
        schema.Definitions.PropertyDefinitions.Should().ContainKey("id-prop");

        // Verify the definition content
        var posInt = schema.Definitions.ValueDefinitions["positive-int"];
        posInt.ValidationRules.Should().Contain(r => r.RuleName == "type");
        posInt.ValidationRules.Should().Contain(r => r.RuleName == ">");

        var idProp = schema.Definitions.PropertyDefinitions["id-prop"];
        idProp.Required.Should().BeTrue();
        idProp.ValidationRules.Should().Contain(r => r.RuleName == "type");
        idProp.ValidationRules.Should().Contain(r => r.RuleName == "pattern");
    }

    [Fact]
    public void Parse_ComplexSchemaWithMultipleDefinitions_Success()
    {
        var kdl = @"
document {
    info {
        title ""Complex Schema""
        version ""1.0.0""
    }
    definitions {
        node ""base-entity"" id=""base-entity"" {
            prop ""id"" {
                required #true
                type ""string""
            }
            prop ""created"" {
                type ""datetime""
            }
        }
        children ""audit-fields"" id=""audit-fields"" {
            node ""created-by""
            node ""updated-by""
        }
    }
    node ""user"" {
        prop ""id"" {
            required #true
        }
        prop ""name"" {
            required #true
        }
        children {
            node ""role""
        }
    }
}";

        var schema = KdlSchema.Parse(kdl);

        schema.Definitions.Should().NotBeNull();
        schema.Definitions!.NodeDefinitions.Should().ContainKey("base-entity");
        schema.Definitions.ChildrenDefinitions.Should().ContainKey("audit-fields");

        // Verify structure
        schema.Nodes.Should().HaveCount(1);
        schema.Nodes[0].Name.Should().Be("user");
    }

    [Fact]
    public void Parse_SchemaWithNestedDefinitions_ParsesCorrectly()
    {
        var kdl = @"
document {
    info {
        title ""Nested Definitions""
    }
    definitions {
        node ""entity"" id=""entity"" {
            prop ""id"" {
                required #true
                type ""string""
            }
            children {
                node ""metadata"" {
                    prop ""version"" {
                        type ""integer""
                    }
                }
            }
        }
    }
    node ""document"" {
        prop ""title"" {
            required #true
        }
    }
}";

        var schema = KdlSchema.Parse(kdl);

        schema.Definitions.Should().NotBeNull();
        var entityDef = schema.Definitions!.NodeDefinitions["entity"];
        entityDef.Properties.Should().HaveCount(1);
        entityDef.Children.Should().NotBeNull();
        entityDef.Children!.Nodes.Should().HaveCount(1);
    }

    [Fact]
    public void Parse_SchemaWithValueDefinition_Success()
    {
        var kdl = @"
document {
    info {
        title ""Value Definition Example""
    }
    definitions {
        value id=""port-range"" {
            type ""integer""
            "">""  1024
            ""<="" 65535
        }
    }
    node ""server"" {
        prop ""port"" {
            required #true
        }
    }
}";

        var schema = KdlSchema.Parse(kdl);

        schema.Definitions.Should().NotBeNull();
        var portRange = schema.Definitions!.ValueDefinitions["port-range"];
        portRange.ValidationRules.Should().HaveCount(3); // type, >, <=
    }

    [Fact]
    public void Parse_SchemaWithChildrenDefinition_Success()
    {
        var kdl = @"
document {
    info {
        title ""Children Definition Example""
    }
    definitions {
        children id=""address-fields"" {
            node ""street"" {
                prop ""line1"" { required #true }
                prop ""line2""
            }
            node ""city"" { required #true }
            node ""postal-code"" { required #true }
        }
    }
}";

        var schema = KdlSchema.Parse(kdl);

        schema.Definitions.Should().NotBeNull();
        var addressFields = schema.Definitions!.ChildrenDefinitions["address-fields"];
        addressFields.Nodes.Should().HaveCount(3);
        addressFields.Nodes.Select(n => n.Name).Should().Contain(new[] { "street", "city", "postal-code" });
    }

    [Fact]
    public void Parse_SchemaDefinitionsWithDuplicateIds_UsesLast()
    {
        var kdl = @"
document {
    info {
        title ""Duplicate IDs""
    }
    definitions {
        node ""first"" id=""my-id"" {
            prop ""a""
        }
        node ""second"" id=""my-id"" {
            prop ""b""
        }
    }
}";

        var schema = KdlSchema.Parse(kdl);

        schema.Definitions.Should().NotBeNull();
        schema.Definitions!.NodeDefinitions.Should().ContainKey("my-id");

        // Last definition with same ID should win
        var def = schema.Definitions.NodeDefinitions["my-id"];
        def.Name.Should().Be("second");
    }

    [Fact]
    public void Parse_DefinitionsWithoutIds_NotAddedToDictionary()
    {
        var kdl = @"
document {
    info {
        title ""No IDs""
    }
    definitions {
        node ""unnamed"" {
            prop ""field""
        }
    }
}";

        var schema = KdlSchema.Parse(kdl);

        // Definitions without IDs should not be added to the dictionary
        schema.Definitions.Should().BeNull();
    }

    [Fact]
    public void Parse_EmptyDefinitionsBlock_ReturnsNull()
    {
        var kdl = @"
document {
    info {
        title ""Empty Definitions""
    }
    definitions {
        // Empty
    }
}";

        var schema = KdlSchema.Parse(kdl);

        schema.Definitions.Should().BeNull();
    }

    [Fact]
    public void Parse_NodeWithRefToDefinition_MergesProperties()
    {
        var kdl = @"
document {
    info {
        title ""Ref Resolution Test""
    }
    definitions {
        node ""base-entity"" id=""base-entity"" {
            prop ""id"" {
                required #true
                type ""string""
            }
            prop ""created"" {
                type ""datetime""
            }
        }
    }
    node ""user"" ref=""definitions > node[id=\""base-entity\""]"" {
        prop ""name"" {
            required #true
        }
    }
}";

        var schema = KdlSchema.Parse(kdl);

        schema.Nodes.Should().HaveCount(1);
        var userNode = schema.Nodes[0];
        userNode.Name.Should().Be("user");

        // Should have merged properties from base-entity
        userNode.Properties.Should().HaveCount(3);
        userNode.Properties.Should().Contain(p => p.Key == "name" && p.Required);
        userNode.Properties.Should().Contain(p => p.Key == "id" && p.Required);
        userNode.Properties.Should().Contain(p => p.Key == "created");
    }

    [Fact]
    public void Parse_NodeWithRefToDefinition_LocalOverridesRef()
    {
        var kdl = @"
document {
    info {
        title ""Override Test""
    }
    definitions {
        node ""base"" id=""base"" {
            prop ""field"" {
                type ""string""
            }
        }
    }
    node ""derived"" ref=""definitions > node[id=\""base\""]"" {
        prop ""field"" {
            required #true
            type ""integer""
        }
    }
}";

        var schema = KdlSchema.Parse(kdl);

        var derivedNode = schema.Nodes[0];
        // Local definition should take precedence
        derivedNode.Properties.Should().HaveCount(1);
        derivedNode.Properties[0].Key.Should().Be("field");
        derivedNode.Properties[0].Required.Should().BeTrue();
        derivedNode.Properties[0].ValidationRules.Should().Contain(r => r.RuleName == "type");
    }

    [Fact]
    public void Parse_PropertyWithRef_MergesValidationRules()
    {
        var kdl = @"
document {
    info {
        title ""Property Ref Test""
    }
    definitions {
        prop ""validated-string"" id=""validated-string"" {
            type ""string""
            min-length 1
            max-length 255
        }
    }
    node ""config"" {
        prop ""name"" ref=""definitions > prop[id=\""validated-string\""]"" {
            required #true
        }
    }
}";

        var schema = KdlSchema.Parse(kdl);

        var configNode = schema.Nodes[0];
        var nameProp = configNode.Properties.First(p => p.Key == "name");

        nameProp.Required.Should().BeTrue();
        nameProp.ValidationRules.Should().Contain(r => r.RuleName == "type");
        nameProp.ValidationRules.Should().Contain(r => r.RuleName == "min-length");
        nameProp.ValidationRules.Should().Contain(r => r.RuleName == "max-length");
    }

    [Fact]
    public void Parse_ChildrenWithRef_MergesNodes()
    {
        var kdl = @"
document {
    info {
        title ""Children Ref Test""
    }
    definitions {
        children id=""common-children"" {
            node ""metadata"" {
                prop ""version""
            }
            node ""timestamps"" {
                prop ""created""
                prop ""updated""
            }
        }
    }
    node ""entity"" {
        children ref=""definitions > children[id=\""common-children\""]"" {
            node ""data"" {
                prop ""value""
            }
        }
    }
}";

        var schema = KdlSchema.Parse(kdl);

        var entityNode = schema.Nodes[0];
        entityNode.Children.Should().NotBeNull();

        // Should have local node plus merged nodes from ref
        entityNode.Children!.Nodes.Should().HaveCount(3);
        entityNode.Children.Nodes.Should().Contain(n => n.Name == "data");
        entityNode.Children.Nodes.Should().Contain(n => n.Name == "metadata");
        entityNode.Children.Nodes.Should().Contain(n => n.Name == "timestamps");
    }

    [Fact]
    public void Parse_ChainedRefs_ResolvesCorrectly()
    {
        var kdl = @"
document {
    info {
        title ""Chained Ref Test""
    }
    definitions {
        node ""level1"" id=""level1"" {
            prop ""a""
        }
        node ""level2"" id=""level2"" ref=""definitions > node[id=\""level1\""]"" {
            prop ""b""
        }
    }
    node ""final"" ref=""definitions > node[id=\""level2\""]"" {
        prop ""c""
    }
}";

        var schema = KdlSchema.Parse(kdl);

        var finalNode = schema.Nodes[0];
        // Should have all properties from the chain
        finalNode.Properties.Should().HaveCount(3);
        finalNode.Properties.Should().Contain(p => p.Key == "a");
        finalNode.Properties.Should().Contain(p => p.Key == "b");
        finalNode.Properties.Should().Contain(p => p.Key == "c");
    }

    [Fact]
    public void Parse_CircularRef_ThrowsException()
    {
        var kdl = @"
document {
    info {
        title ""Circular Ref Test""
    }
    definitions {
        node ""a"" id=""a"" ref=""definitions > node[id=\""b\""]"" {
            prop ""x""
        }
        node ""b"" id=""b"" ref=""definitions > node[id=\""a\""]"" {
            prop ""y""
        }
    }
    node ""test"" ref=""definitions > node[id=\""a\""]""
}";

        var act = () => KdlSchema.Parse(kdl);

        act.Should().Throw<KdlSchemaException>()
            .WithMessage("*ircular reference*");
    }

    [Fact]
    public void Parse_ValueWithRef_MergesConstraints()
    {
        var kdl = @"
document {
    info {
        title ""Value Ref Test""
    }
    definitions {
        value id=""positive-int"" {
            type ""integer""
            "">""  0
        }
    }
    node ""config"" {
        value ref=""definitions > value[id=\""positive-int\""]"" {
            ""<="" 100
        }
    }
}";

        var schema = KdlSchema.Parse(kdl);

        var configNode = schema.Nodes[0];
        configNode.Values.Should().NotBeNull();

        var valueRules = configNode.Values!.ValidationRules;
        valueRules.Should().Contain(r => r.RuleName == "type");
        valueRules.Should().Contain(r => r.RuleName == ">");
        valueRules.Should().Contain(r => r.RuleName == "<=");
    }
}


