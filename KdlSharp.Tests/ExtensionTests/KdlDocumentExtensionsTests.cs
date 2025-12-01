using FluentAssertions;
using KdlSharp.Extensions;
using Xunit;

namespace KdlSharp.Tests.ExtensionTests;

/// <summary>
/// Tests for <see cref="KdlDocumentExtensions"/>.
/// </summary>
public class KdlDocumentExtensionsTests
{
    [Fact]
    public void FindNodes_WithMatchingTopLevelNodes_ReturnsMatches()
    {
        var doc = KdlDocument.Parse(@"
            package
            config
            package
            settings
            package
        ");

        var result = doc.FindNodes("package").ToList();

        result.Should().HaveCount(3);
        result.All(n => n.Name == "package").Should().BeTrue();
    }

    [Fact]
    public void FindNodes_WithNoMatches_ReturnsEmpty()
    {
        var doc = KdlDocument.Parse(@"
            config
            settings
        ");

        var result = doc.FindNodes("nonexistent").ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void FindNodes_WithEmptyDocument_ReturnsEmpty()
    {
        var doc = new KdlDocument();

        var result = doc.FindNodes("any").ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void FindNodes_DoesNotSearchDescendants()
    {
        var doc = KdlDocument.Parse(@"
            root {
                child
            }
        ");

        var result = doc.FindNodes("child").ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void FindNodes_WithNullDocument_ThrowsArgumentNullException()
    {
        KdlDocument doc = null!;

        var action = () => doc.FindNodes("name").ToList();

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("document");
    }

    [Fact]
    public void FindNodes_WithNullName_ThrowsArgumentNullException()
    {
        var doc = new KdlDocument();

        var action = () => doc.FindNodes(null!).ToList();

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("name");
    }

    [Fact]
    public void FindNode_WithMatchingTopLevelNode_ReturnsFirstMatch()
    {
        var doc = KdlDocument.Parse(@"
            first
            second
            first
        ");

        var result = doc.FindNode("first");

        result.Should().NotBeNull();
        result!.Name.Should().Be("first");
    }

    [Fact]
    public void FindNode_WithNoMatch_ReturnsNull()
    {
        var doc = KdlDocument.Parse("config");

        var result = doc.FindNode("nonexistent");

        result.Should().BeNull();
    }

    [Fact]
    public void FindNode_WithEmptyDocument_ReturnsNull()
    {
        var doc = new KdlDocument();

        var result = doc.FindNode("any");

        result.Should().BeNull();
    }

    [Fact]
    public void FindNode_WithNullDocument_ThrowsArgumentNullException()
    {
        KdlDocument doc = null!;

        var action = () => doc.FindNode("name");

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("document");
    }

    [Fact]
    public void FindNode_WithNullName_ThrowsArgumentNullException()
    {
        var doc = new KdlDocument();

        var action = () => doc.FindNode(null!);

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("name");
    }

    [Fact]
    public void AllNodes_WithNestedStructure_ReturnsAllNodes()
    {
        var doc = KdlDocument.Parse(@"
            root {
                child1
                child2 {
                    grandchild
                }
            }
            sibling
        ");

        var result = doc.AllNodes().ToList();

        result.Should().HaveCount(5);
        result.Select(n => n.Name).Should().BeEquivalentTo("root", "child1", "child2", "grandchild", "sibling");
    }

    [Fact]
    public void AllNodes_WithEmptyDocument_ReturnsEmpty()
    {
        var doc = new KdlDocument();

        var result = doc.AllNodes().ToList();

        result.Should().BeEmpty();
    }

    [Fact]
    public void AllNodes_WithFlatDocument_ReturnsAllTopLevel()
    {
        var doc = KdlDocument.Parse(@"
            node1
            node2
            node3
        ");

        var result = doc.AllNodes().ToList();

        result.Should().HaveCount(3);
    }

    [Fact]
    public void AllNodes_WithDeeplyNestedStructure_ReturnsAllLevels()
    {
        var doc = KdlDocument.Parse(@"
            level1 {
                level2 {
                    level3 {
                        level4
                    }
                }
            }
        ");

        var result = doc.AllNodes().ToList();

        result.Should().HaveCount(4);
        result.Select(n => n.Name).Should().ContainInOrder("level1", "level2", "level3", "level4");
    }

    [Fact]
    public void AllNodes_WithNullDocument_ThrowsArgumentNullException()
    {
        KdlDocument doc = null!;

        var action = () => doc.AllNodes().ToList();

        action.Should().Throw<ArgumentNullException>()
            .WithParameterName("document");
    }

    [Fact]
    public void AllNodes_EnumeratesInDepthFirstOrder()
    {
        var doc = KdlDocument.Parse(@"
            a {
                a1
                a2
            }
            b {
                b1
            }
        ");

        var result = doc.AllNodes().Select(n => n.Name).ToList();

        result.Should().ContainInOrder("a", "a1", "a2", "b", "b1");
    }
}
