using AwesomeAssertions;
using KdlSharp;
using Xunit;

namespace KdlSharp.Tests;

public class NodeMutationTests
{
    // ===== RemoveChild =====

    [Fact]
    public void RemoveChild_Existing_ReturnsTrue()
    {
        var parent = new KdlNode("parent");
        var child = new KdlNode("child");
        parent.AddChild(child);

        parent.RemoveChild(child).Should().BeTrue();
        parent.Children.Should().BeEmpty();
    }

    [Fact]
    public void RemoveChild_ClearsParent()
    {
        var parent = new KdlNode("parent");
        var child = new KdlNode("child");
        parent.AddChild(child);

        parent.RemoveChild(child);

        child.Parent.Should().BeNull();
    }

    [Fact]
    public void RemoveChild_NotFound_ReturnsFalse()
    {
        var parent = new KdlNode("parent");
        var orphan = new KdlNode("orphan");

        parent.RemoveChild(orphan).Should().BeFalse();
    }

    [Fact]
    public void RemoveChild_Null_Throws()
    {
        var node = new KdlNode("node");

        var act = () => node.RemoveChild(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RemoveChild_PreservesOtherChildren()
    {
        var parent = new KdlNode("parent");
        var child1 = new KdlNode("child1");
        var child2 = new KdlNode("child2");
        var child3 = new KdlNode("child3");
        parent.AddChildren(child1, child2, child3);

        parent.RemoveChild(child2);

        parent.Children.Should().HaveCount(2);
        parent.Children[0].Name.Should().Be("child1");
        parent.Children[1].Name.Should().Be("child3");
    }

    // ===== RemoveProperty (by key) =====

    [Fact]
    public void RemoveProperty_ByKey_Existing_ReturnsTrue()
    {
        var node = new KdlNode("node")
            .AddProperty("host", "localhost");

        node.RemoveProperty("host").Should().BeTrue();
        node.Properties.Should().BeEmpty();
    }

    [Fact]
    public void RemoveProperty_ByKey_NotFound_ReturnsFalse()
    {
        var node = new KdlNode("node")
            .AddProperty("host", "localhost");

        node.RemoveProperty("port").Should().BeFalse();
        node.Properties.Should().HaveCount(1);
    }

    [Fact]
    public void RemoveProperty_ByKey_RemovesAllDuplicates()
    {
        var node = new KdlNode("node")
            .AddProperty("key", "first")
            .AddProperty("key", "second")
            .AddProperty("other", "keep");

        node.RemoveProperty("key").Should().BeTrue();
        node.Properties.Should().HaveCount(1);
        node.Properties[0].Key.Should().Be("other");
    }

    [Fact]
    public void RemoveProperty_ByKey_Null_Throws()
    {
        var node = new KdlNode("node");

        var act = () => node.RemoveProperty((string)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // ===== RemoveProperty (by reference) =====

    [Fact]
    public void RemoveProperty_ByReference_Existing_ReturnsTrue()
    {
        var node = new KdlNode("node")
            .AddProperty("a", "1")
            .AddProperty("b", "2");
        var prop = node.Properties[0];

        node.RemoveProperty(prop).Should().BeTrue();
        node.Properties.Should().HaveCount(1);
        node.Properties[0].Key.Should().Be("b");
    }

    [Fact]
    public void RemoveProperty_ByReference_NotFound_ReturnsFalse()
    {
        var node = new KdlNode("node")
            .AddProperty("a", "1");
        var other = new KdlProperty("b", (KdlValue)"2");

        node.RemoveProperty(other).Should().BeFalse();
    }

    [Fact]
    public void RemoveProperty_ByReference_Null_Throws()
    {
        var node = new KdlNode("node");

        var act = () => node.RemoveProperty((KdlProperty)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // ===== RemoveArgument =====

    [Fact]
    public void RemoveArgument_Existing_ReturnsTrue()
    {
        var node = new KdlNode("node");
        var arg = (KdlValue)"hello";
        node.AddArgument(arg);

        node.RemoveArgument(arg).Should().BeTrue();
        node.Arguments.Should().BeEmpty();
    }

    [Fact]
    public void RemoveArgument_NotFound_ReturnsFalse()
    {
        var node = new KdlNode("node")
            .AddArgument("hello");
        var other = (KdlValue)"world";

        node.RemoveArgument(other).Should().BeFalse();
    }

    [Fact]
    public void RemoveArgument_Null_Throws()
    {
        var node = new KdlNode("node");

        var act = () => node.RemoveArgument(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // ===== AddChild reparenting =====

    [Fact]
    public void AddChild_Reparent_RemovesFromPreviousParent()
    {
        var parentA = new KdlNode("parentA");
        var parentB = new KdlNode("parentB");
        var child = new KdlNode("child");

        parentA.AddChild(child);
        parentB.AddChild(child);

        parentA.Children.Should().BeEmpty();
        parentB.Children.Should().HaveCount(1);
        child.Parent.Should().BeSameAs(parentB);
    }

    [Fact]
    public void AddChild_Reparent_PreservesOtherChildrenInOldParent()
    {
        var parentA = new KdlNode("parentA");
        var parentB = new KdlNode("parentB");
        var child1 = new KdlNode("child1");
        var child2 = new KdlNode("child2");

        parentA.AddChildren(child1, child2);
        parentB.AddChild(child1);

        parentA.Children.Should().HaveCount(1);
        parentA.Children[0].Name.Should().Be("child2");
        parentB.Children.Should().HaveCount(1);
        parentB.Children[0].Name.Should().Be("child1");
    }

    [Fact]
    public void AddChild_NoParent_SetsParent()
    {
        var parent = new KdlNode("parent");
        var child = new KdlNode("child");

        child.Parent.Should().BeNull();
        parent.AddChild(child);
        child.Parent.Should().BeSameAs(parent);
    }

    [Fact]
    public void AddChild_SameParent_MovesToEnd()
    {
        var parent = new KdlNode("parent");
        var child1 = new KdlNode("child1");
        var child2 = new KdlNode("child2");
        parent.AddChildren(child1, child2);

        parent.AddChild(child1);

        parent.Children.Should().HaveCount(2);
        parent.Children[0].Name.Should().Be("child2");
        parent.Children[1].Name.Should().Be("child1");
        child1.Parent.Should().BeSameAs(parent);
    }

    [Fact]
    public void AddChild_Self_Throws()
    {
        var node = new KdlNode("node");

        var act = () => node.AddChild(node);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RemoveChild_AfterReparent_ReturnsFalse()
    {
        var parentA = new KdlNode("parentA");
        var parentB = new KdlNode("parentB");
        var child = new KdlNode("child");

        parentA.AddChild(child);
        parentB.AddChild(child);

        parentA.RemoveChild(child).Should().BeFalse();
        parentB.Children.Should().HaveCount(1);
        child.Parent.Should().BeSameAs(parentB);
    }

    [Fact]
    public void AddChildren_Reparent_BatchMove()
    {
        var parentA = new KdlNode("parentA");
        var parentB = new KdlNode("parentB");
        var child1 = new KdlNode("child1");
        var child2 = new KdlNode("child2");
        var child3 = new KdlNode("child3");
        parentA.AddChildren(child1, child2, child3);

        parentB.AddChildren(child1, child3);

        parentA.Children.Should().HaveCount(1);
        parentA.Children[0].Name.Should().Be("child2");
        parentB.Children.Should().HaveCount(2);
        parentB.Children[0].Name.Should().Be("child1");
        parentB.Children[1].Name.Should().Be("child3");
    }
}
