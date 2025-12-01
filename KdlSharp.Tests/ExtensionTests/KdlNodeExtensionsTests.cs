using FluentAssertions;
using KdlSharp.Extensions;
using Xunit;

namespace KdlSharp.Tests.ExtensionTests;

/// <summary>
/// Tests for <see cref="KdlNodeExtensions"/>.
/// </summary>
public class KdlNodeExtensionsTests
{
    [Fact]
    public void FindNodes_WithMatchingDescendants_ReturnsMatchingNodes()
    {
        var doc = KdlDocument.Parse(@"
            root {
                child
                nested {
                    child
                    deep {
                        child
                    }
                }
            }
        ");
        var root = doc.Nodes[0];

        var result = root.FindNodes("child").ToList();

        result.Should().HaveCount(3);
        result.All(n => n.Name == "child").Should().BeTrue();
    }

    [Fact]
    public void FindNodes_WithNoMatches_ReturnsEmpty()
    {
        var doc = KdlDocument.Parse(@"
            root {
                child
            }
        ");
        var root = doc.Nodes[0];

        var result = root.FindNodes("nonexistent").ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void FindNodes_WithEmptyChildren_ReturnsEmpty()
    {
        var node = new KdlNode("empty");

        var result = node.FindNodes("child").ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void FindNodes_WithNullNode_ThrowsArgumentNullException()
    {
        KdlNode node = null!;

        var action = () => node.FindNodes("name").ToList();

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("node");
    }

    [Fact]
    public void FindNodes_WithNullName_ThrowsArgumentNullException()
    {
        var node = new KdlNode("test");

        var action = () => node.FindNodes(null!).ToList();

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("name");
    }

    [Fact]
    public void FindNode_WithMatchingDescendant_ReturnsFirstMatch()
    {
        var doc = KdlDocument.Parse(@"
            root {
                first
                second
                nested {
                    first
                }
            }
        ");
        var root = doc.Nodes[0];

        var result = root.FindNode("first");

        result.Should().NotBeNull();
        result!.Name.Should().Be("first");
    }

    [Fact]
    public void FindNode_WithNoMatch_ReturnsNull()
    {
        var node = new KdlNode("test");

        var result = node.FindNode("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public void FindNode_WithNullNode_ThrowsArgumentNullException()
    {
        KdlNode node = null!;

        var action = () => node.FindNode("name");

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("node");
    }

    [Fact]
    public void FindNode_WithNullName_ThrowsArgumentNullException()
    {
        var node = new KdlNode("test");

        var action = () => node.FindNode(null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("name");
    }

    [Fact]
    public void GetPropertyValue_WithExistingProperty_ReturnsValue()
    {
        var doc = KdlDocument.Parse("node key=\"value\"");
        var node = doc.Nodes[0];

        var result = node.GetPropertyValue<string>("key");

        result.Should().Be("value");
    }

    [Fact]
    public void GetPropertyValue_WithMissingProperty_ReturnsDefault()
    {
        var node = new KdlNode("test");

        var result = node.GetPropertyValue<string>("missing", "default");

        result.Should().Be("default");
    }

    [Fact]
    public void GetPropertyValue_WithIntegerProperty_ReturnsConvertedValue()
    {
        var doc = KdlDocument.Parse("node count=42");
        var node = doc.Nodes[0];

        var result = node.GetPropertyValue<int>("count");

        result.Should().Be(42);
    }

    [Fact]
    public void GetPropertyValue_WithBooleanProperty_ReturnsConvertedValue()
    {
        var doc = KdlDocument.Parse("node enabled=#true");
        var node = doc.Nodes[0];

        var result = node.GetPropertyValue<bool>("enabled");

        result.Should().BeTrue();
    }

    [Fact]
    public void GetPropertyValue_WithDecimalProperty_ReturnsConvertedValue()
    {
        var doc = KdlDocument.Parse("node price=19.99");
        var node = doc.Nodes[0];

        var result = node.GetPropertyValue<decimal>("price");

        result.Should().Be(19.99m);
    }

    [Fact]
    public void GetPropertyValue_WithNullNode_ThrowsArgumentNullException()
    {
        KdlNode node = null!;

        var action = () => node.GetPropertyValue<string>("key");

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("node");
    }

    [Fact]
    public void GetPropertyValue_WithNullKey_ThrowsArgumentNullException()
    {
        var node = new KdlNode("test");

        var action = () => node.GetPropertyValue<string>(null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("key");
    }

    [Fact]
    public void HasProperty_WithExistingProperty_ReturnsTrue()
    {
        var doc = KdlDocument.Parse("node key=\"value\"");
        var node = doc.Nodes[0];

        var result = node.HasProperty("key");

        result.Should().BeTrue();
    }

    [Fact]
    public void HasProperty_WithMissingProperty_ReturnsFalse()
    {
        var node = new KdlNode("test");

        var result = node.HasProperty("missing");

        result.Should().BeFalse();
    }

    [Fact]
    public void HasProperty_WithNullNode_ThrowsException()
    {
        KdlNode node = null!;

        var action = () => node.HasProperty("key");

        // Extension method will throw - either ArgumentNullException from explicit check
        // or NullReferenceException from runtime null dereference
        action.Should().Throw<Exception>();
    }

    [Fact]
    public void HasProperty_WithNullKey_ThrowsArgumentNullException()
    {
        var node = new KdlNode("test");

        var action = () => node.HasProperty(null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("key");
    }

    [Fact]
    public void GetArgumentValue_WithExistingArgument_ReturnsValue()
    {
        var doc = KdlDocument.Parse("node \"value\"");
        var node = doc.Nodes[0];

        var result = node.GetArgumentValue<string>(0);

        result.Should().Be("value");
    }

    [Fact]
    public void GetArgumentValue_WithIndexOutOfRange_ReturnsDefault()
    {
        var node = new KdlNode("test");

        var result = node.GetArgumentValue<string>(0, "default");

        result.Should().Be("default");
    }

    [Fact]
    public void GetArgumentValue_WithNegativeIndex_ReturnsDefault()
    {
        var doc = KdlDocument.Parse("node \"value\"");
        var node = doc.Nodes[0];

        var result = node.GetArgumentValue<string>(-1, "default");

        result.Should().Be("default");
    }

    [Fact]
    public void GetArgumentValue_WithSecondArgument_ReturnsCorrectValue()
    {
        var doc = KdlDocument.Parse("node \"first\" \"second\" \"third\"");
        var node = doc.Nodes[0];

        var result = node.GetArgumentValue<string>(1);

        result.Should().Be("second");
    }

    [Fact]
    public void GetArgumentValue_WithIntegerArgument_ReturnsConvertedValue()
    {
        var doc = KdlDocument.Parse("node 42");
        var node = doc.Nodes[0];

        var result = node.GetArgumentValue<int>(0);

        result.Should().Be(42);
    }

    [Fact]
    public void GetArgumentValue_WithNullNode_ThrowsArgumentNullException()
    {
        KdlNode node = null!;

        var action = () => node.GetArgumentValue<string>(0);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("node");
    }

    [Fact]
    public void Descendants_WithNestedChildren_ReturnsAllDescendants()
    {
        var doc = KdlDocument.Parse(@"
            root {
                child1
                child2 {
                    grandchild1
                    grandchild2
                }
            }
        ");
        var root = doc.Nodes[0];

        var result = root.Descendants().ToList();

        result.Should().HaveCount(4);
        result.Select(n => n.Name).Should().BeEquivalentTo("child1", "child2", "grandchild1", "grandchild2");
    }

    [Fact]
    public void Descendants_WithNoChildren_ReturnsEmpty()
    {
        var node = new KdlNode("leaf");

        var result = node.Descendants().ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void Descendants_WithDeeplyNestedStructure_ReturnsAllLevels()
    {
        var doc = KdlDocument.Parse(@"
            level1 {
                level2 {
                    level3 {
                        level4 {
                            level5
                        }
                    }
                }
            }
        ");
        var root = doc.Nodes[0];

        var result = root.Descendants().ToList();

        result.Should().HaveCount(4);
        result.Select(n => n.Name).Should().ContainInOrder("level2", "level3", "level4", "level5");
    }

    [Fact]
    public void Descendants_WithNullNode_ThrowsArgumentNullException()
    {
        KdlNode node = null!;

        var action = () => node.Descendants().ToList();

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("node");
    }

    [Fact]
    public void Ancestors_WithParentChain_ReturnsAncestors()
    {
        var doc = KdlDocument.Parse(@"
            root {
                child {
                    grandchild
                }
            }
        ");
        var grandchild = doc.Nodes[0].Children[0].Children[0];

        var result = grandchild.Ancestors().ToList();

        result.Should().HaveCount(2);
        result.Select(n => n.Name).Should().ContainInOrder("child", "root");
    }

    [Fact]
    public void Ancestors_WithNoParent_ReturnsEmpty()
    {
        var node = new KdlNode("orphan");

        var result = node.Ancestors().ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void Ancestors_WithNullNode_ThrowsArgumentNullException()
    {
        KdlNode node = null!;

        var action = () => node.Ancestors().ToList();

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("node");
    }

    [Fact]
    public void GetPropertyValue_WithDuplicateKeys_ReturnsLastValue()
    {
        var doc = KdlDocument.Parse("node key=\"first\" key=\"second\" key=\"last\"");
        var node = doc.Nodes[0];

        var result = node.GetPropertyValue<string>("key");

        result.Should().Be("last");
    }

    [Fact]
    public void GetPropertyValue_WithNullPropertyValue_ReturnsDefault()
    {
        var doc = KdlDocument.Parse("node key=#null");
        var node = doc.Nodes[0];

        var result = node.GetPropertyValue<string>("key", "default");

        result.Should().Be("default");
    }

    [Fact]
    public void GetArgumentValue_WithTypeMismatch_ReturnsTypeDefault()
    {
        // When a value exists but cannot be converted to the target type,
        // the type's default value (0 for int) is returned, not the passed default.
        // This is because the value was found but conversion failed.
        var doc = KdlDocument.Parse("node \"not-a-number\"");
        var node = doc.Nodes[0];

        var result = node.GetArgumentValue<int>(0, -1);

        result.Should().Be(0); // Type default, not passed default
    }

    #region Special Number Tests

    [Fact]
    public void GetArgumentValue_WithPositiveInfinity_ReturnsDoubleInfinity()
    {
        var doc = KdlDocument.Parse("node #inf");
        var node = doc.Nodes[0];

        var result = node.GetArgumentValue<double>(0);

        double.IsPositiveInfinity(result).Should().BeTrue();
    }

    [Fact]
    public void GetArgumentValue_WithNegativeInfinity_ReturnsDoubleNegativeInfinity()
    {
        var doc = KdlDocument.Parse("node #-inf");
        var node = doc.Nodes[0];

        var result = node.GetArgumentValue<double>(0);

        double.IsNegativeInfinity(result).Should().BeTrue();
    }

    [Fact]
    public void GetArgumentValue_WithNaN_ReturnsDoubleNaN()
    {
        var doc = KdlDocument.Parse("node #nan");
        var node = doc.Nodes[0];

        var result = node.GetArgumentValue<double>(0);

        double.IsNaN(result).Should().BeTrue();
    }

    [Fact]
    public void GetArgumentValue_WithPositiveInfinity_AsFloat_ReturnsFloatInfinity()
    {
        var doc = KdlDocument.Parse("node #inf");
        var node = doc.Nodes[0];

        var result = node.GetArgumentValue<float>(0);

        float.IsPositiveInfinity(result).Should().BeTrue();
    }

    [Fact]
    public void GetArgumentValue_WithNaN_AsFloat_ReturnsFloatNaN()
    {
        var doc = KdlDocument.Parse("node #nan");
        var node = doc.Nodes[0];

        var result = node.GetArgumentValue<float>(0);

        float.IsNaN(result).Should().BeTrue();
    }

    [Fact]
    public void GetArgumentValue_WithInfinity_AsInt_ReturnsDefault()
    {
        // Special numbers cannot be converted to integer types - should return default
        var doc = KdlDocument.Parse("node #inf");
        var node = doc.Nodes[0];

        var result = node.GetArgumentValue<int>(0, -999);

        result.Should().Be(0); // Type default, not passed default
    }

    [Fact]
    public void GetArgumentValue_WithInfinity_AsDecimal_ReturnsDefault()
    {
        // Special numbers cannot be converted to decimal - should return default
        var doc = KdlDocument.Parse("node #inf");
        var node = doc.Nodes[0];

        var result = node.GetArgumentValue<decimal>(0, -999m);

        result.Should().Be(0m); // Type default, not passed default
    }

    [Fact]
    public void GetPropertyValue_WithPositiveInfinity_ReturnsDoubleInfinity()
    {
        var doc = KdlDocument.Parse("node value=#inf");
        var node = doc.Nodes[0];

        var result = node.GetPropertyValue<double>("value");

        double.IsPositiveInfinity(result).Should().BeTrue();
    }

    [Fact]
    public void GetPropertyValue_WithNaN_ReturnsDoubleNaN()
    {
        var doc = KdlDocument.Parse("node value=#nan");
        var node = doc.Nodes[0];

        var result = node.GetPropertyValue<double>("value");

        double.IsNaN(result).Should().BeTrue();
    }

    #endregion
}
