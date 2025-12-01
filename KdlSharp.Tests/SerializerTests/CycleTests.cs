using FluentAssertions;
using KdlSharp.Exceptions;
using KdlSharp.Serialization;
using Xunit;

namespace KdlSharp.Tests.SerializerTests;

/// <summary>
/// Tests for circular reference detection during serialization.
/// </summary>
public class CycleTests
{
    [Fact]
    public void Serialize_SelfReference_ThrowsKdlSerializationException()
    {
        var serializer = new KdlSerializer();
        var node = new SelfReferentialNode { Name = "root" };
        node.Self = node;

        var act = () => serializer.Serialize(node);

        act.Should().Throw<KdlSerializationException>()
            .WithMessage("*Circular reference*SelfReferentialNode*");
    }

    [Fact]
    public void Serialize_MutualReference_ThrowsKdlSerializationException()
    {
        var serializer = new KdlSerializer();
        var nodeA = new NodeA { Name = "A" };
        var nodeB = new NodeB { Name = "B" };
        nodeA.Other = nodeB;
        nodeB.Other = nodeA;

        var act = () => serializer.Serialize(nodeA);

        act.Should().Throw<KdlSerializationException>()
            .WithMessage("*Circular reference*");
    }

    [Fact]
    public void Serialize_IndirectCycleThroughCollection_ThrowsKdlSerializationException()
    {
        var serializer = new KdlSerializer();
        var parent = new NodeWithChildren { Name = "parent" };
        var child = new NodeWithChildren { Name = "child", Parent = parent };
        parent.Children = new List<NodeWithChildren> { child };

        var act = () => serializer.Serialize(parent);

        act.Should().Throw<KdlSerializationException>()
            .WithMessage("*Circular reference*");
    }

    [Fact]
    public void Serialize_DeepCycle_ThrowsKdlSerializationException()
    {
        var serializer = new KdlSerializer();
        var node1 = new LinkedNode { Name = "1" };
        var node2 = new LinkedNode { Name = "2" };
        var node3 = new LinkedNode { Name = "3" };
        node1.Next = node2;
        node2.Next = node3;
        node3.Next = node1;

        var act = () => serializer.Serialize(node1);

        act.Should().Throw<KdlSerializationException>()
            .WithMessage("*Circular reference*");
    }

    [Fact]
    public void Serialize_SameObjectInDifferentBranches_Succeeds()
    {
        // Same object appearing in different branches (diamond pattern) is allowed
        // because it's not a cycle in the serialization path
        var serializer = new KdlSerializer();
        var shared = new SimpleNode { Name = "shared" };
        var parent = new DualChildNode
        {
            Name = "parent",
            Left = shared,
            Right = shared
        };

        var act = () => serializer.Serialize(parent);

        // Should not throw - same object in different branches is OK
        act.Should().NotThrow();
    }

    [Fact]
    public void Serialize_NoCycle_Succeeds()
    {
        var serializer = new KdlSerializer();
        var root = new SimpleTree
        {
            Name = "root",
            Child = new SimpleTree
            {
                Name = "child",
                Child = new SimpleTree { Name = "grandchild" }
            }
        };

        var result = serializer.Serialize(root);

        result.Should().Contain("root");
        result.Should().Contain("child");
        result.Should().Contain("grandchild");
    }

    [Fact]
    public void Serialize_CycleInCollectionElement_ThrowsKdlSerializationException()
    {
        var serializer = new KdlSerializer();
        var container = new NodeContainer { Name = "container" };
        var cyclicNode = new SelfReferentialNode { Name = "cyclic" };
        cyclicNode.Self = cyclicNode;
        container.Nodes = new List<SelfReferentialNode> { cyclicNode };

        var act = () => serializer.Serialize(container);

        act.Should().Throw<KdlSerializationException>()
            .WithMessage("*Circular reference*");
    }

    [Fact]
    public void Serialize_CircularReferenceMessage_ContainsHelpfulGuidance()
    {
        var serializer = new KdlSerializer();
        var node = new SelfReferentialNode { Name = "root" };
        node.Self = node;

        var act = () => serializer.Serialize(node);

        act.Should().Throw<KdlSerializationException>()
            .WithMessage("*Consider using IDs*");
    }

    // Test data types
    private class SelfReferentialNode
    {
        public string Name { get; set; } = "";
        public SelfReferentialNode? Self { get; set; }
    }

    private class NodeA
    {
        public string Name { get; set; } = "";
        public NodeB? Other { get; set; }
    }

    private class NodeB
    {
        public string Name { get; set; } = "";
        public NodeA? Other { get; set; }
    }

    private class NodeWithChildren
    {
        public string Name { get; set; } = "";
        public NodeWithChildren? Parent { get; set; }
        public List<NodeWithChildren>? Children { get; set; }
    }

    private class LinkedNode
    {
        public string Name { get; set; } = "";
        public LinkedNode? Next { get; set; }
    }

    private class SimpleNode
    {
        public string Name { get; set; } = "";
    }

    private class DualChildNode
    {
        public string Name { get; set; } = "";
        public SimpleNode? Left { get; set; }
        public SimpleNode? Right { get; set; }
    }

    private class SimpleTree
    {
        public string Name { get; set; } = "";
        public SimpleTree? Child { get; set; }
    }

    private class NodeContainer
    {
        public string Name { get; set; } = "";
        public List<SelfReferentialNode>? Nodes { get; set; }
    }
}
