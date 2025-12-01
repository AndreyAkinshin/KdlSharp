using FluentAssertions;
using KdlSharp;
using KdlSharp.Values;
using Xunit;

namespace KdlSharp.Tests;

/// <summary>
/// Tests for value cloning behavior, particularly TypeAnnotation preservation.
/// </summary>
public class ValueCloneTests
{
    #region KdlBoolean Clone Tests

    [Fact]
    public void KdlBoolean_Clone_WithoutAnnotation_ReturnsSingleton()
    {
        // Arrange - parse a boolean without type annotation to get a clean value
        var doc = KdlDocument.Parse("node #true");
        var original = doc.Nodes[0].Arguments[0];
        original.TypeAnnotation.Should().BeNull(); // Verify no annotation

        // Act
        var clone = original.Clone();

        // Assert - should return the singleton when no annotation
        clone.Should().BeSameAs(KdlBoolean.True);
    }

    [Fact]
    public void KdlBoolean_Clone_False_WithoutAnnotation_ReturnsSingleton()
    {
        // Arrange - parse a false boolean without type annotation
        var doc = KdlDocument.Parse("node #false");
        var original = doc.Nodes[0].Arguments[0];
        original.TypeAnnotation.Should().BeNull();

        // Act
        var clone = original.Clone();

        // Assert - should return the singleton
        clone.Should().BeSameAs(KdlBoolean.False);
    }

    [Fact]
    public void KdlBoolean_Clone_PreservesTypeAnnotation_True()
    {
        // Arrange - parse a boolean with type annotation
        var doc = KdlDocument.Parse("node (bool)#true");
        var original = doc.Nodes[0].Arguments[0];
        original.TypeAnnotation.Should().NotBeNull();

        // Act
        var clone = original.Clone();

        // Assert
        clone.Should().NotBeSameAs(original);
        clone.Should().BeOfType<KdlBoolean>();
        ((KdlBoolean)clone).Value.Should().BeTrue();
        clone.TypeAnnotation.Should().NotBeNull();
        clone.TypeAnnotation!.TypeName.Should().Be("bool");
    }

    [Fact]
    public void KdlBoolean_Clone_PreservesTypeAnnotation_False()
    {
        // Arrange - parse a boolean with type annotation
        var doc = KdlDocument.Parse("node (custom-bool)#false");
        var original = doc.Nodes[0].Arguments[0];

        // Act
        var clone = original.Clone();

        // Assert
        clone.Should().NotBeSameAs(original);
        clone.Should().BeOfType<KdlBoolean>();
        ((KdlBoolean)clone).Value.Should().BeFalse();
        clone.TypeAnnotation.Should().NotBeNull();
        clone.TypeAnnotation!.TypeName.Should().Be("custom-bool");
    }

    [Fact]
    public void KdlBoolean_Clone_WithAnnotation_CreatesNewInstance()
    {
        // Arrange - parse a boolean with type annotation
        var doc = KdlDocument.Parse("node (test)#true");
        var annotated = doc.Nodes[0].Arguments[0];
        annotated.TypeAnnotation.Should().NotBeNull();

        // Act
        var clone = annotated.Clone();

        // Assert - clone should be a new instance, not the singleton
        clone.Should().NotBeSameAs(KdlBoolean.True);
        clone.TypeAnnotation.Should().NotBeNull();
        clone.TypeAnnotation!.TypeName.Should().Be("test");
    }

    #endregion

    #region KdlNull Clone Tests

    [Fact]
    public void KdlNull_Clone_WithoutAnnotation_ReturnsSingleton()
    {
        // Arrange - parse a null without type annotation
        var doc = KdlDocument.Parse("node #null");
        var original = doc.Nodes[0].Arguments[0];
        original.TypeAnnotation.Should().BeNull();

        // Act
        var clone = original.Clone();

        // Assert
        clone.Should().BeSameAs(KdlNull.Instance);
    }

    [Fact]
    public void KdlNull_Clone_PreservesTypeAnnotation()
    {
        // Arrange - parse a null with type annotation
        var doc = KdlDocument.Parse("node (nullable)#null");
        var original = doc.Nodes[0].Arguments[0];

        // Act
        var clone = original.Clone();

        // Assert
        clone.Should().NotBeSameAs(KdlNull.Instance);
        clone.Should().BeOfType<KdlNull>();
        clone.IsNull().Should().BeTrue();
        clone.TypeAnnotation.Should().NotBeNull();
        clone.TypeAnnotation!.TypeName.Should().Be("nullable");
    }

    [Fact]
    public void KdlNull_Clone_WithAnnotation_CreatesNewInstance()
    {
        // Arrange - parse a null with annotation
        var doc = KdlDocument.Parse("node (custom)#null");
        var annotatedNull = doc.Nodes[0].Arguments[0];
        annotatedNull.TypeAnnotation.Should().NotBeNull();

        // Act
        var clone = annotatedNull.Clone();

        // Assert - clone should be a new instance
        clone.Should().NotBeSameAs(KdlNull.Instance);
        clone.TypeAnnotation.Should().NotBeNull();
        clone.TypeAnnotation!.TypeName.Should().Be("custom");
    }

    #endregion

    #region Additional Clone Tests

    [Fact]
    public void ParsedBoolean_Clone_PreservesAnnotationType()
    {
        // Arrange - parse a boolean with type annotation
        var doc = KdlDocument.Parse("node (flag)#true");
        var parsedBool = doc.Nodes[0].Arguments[0];

        // Verify initial state
        parsedBool.TypeAnnotation.Should().NotBeNull();
        parsedBool.TypeAnnotation!.TypeName.Should().Be("flag");

        // Act
        var clone = parsedBool.Clone();

        // Assert
        clone.Should().NotBeSameAs(parsedBool);
        clone.TypeAnnotation.Should().NotBeNull();
        clone.TypeAnnotation!.TypeName.Should().Be("flag");
        clone.AsBoolean().Should().BeTrue();
    }

    [Fact]
    public void ParsedNull_Clone_PreservesAnnotationType()
    {
        // Arrange - parse a null with type annotation
        var doc = KdlDocument.Parse("node (optional)#null");
        var parsedNull = doc.Nodes[0].Arguments[0];

        // Verify initial state
        parsedNull.TypeAnnotation.Should().NotBeNull();
        parsedNull.TypeAnnotation!.TypeName.Should().Be("optional");

        // Act
        var clone = parsedNull.Clone();

        // Assert
        clone.Should().NotBeSameAs(parsedNull);
        clone.TypeAnnotation.Should().NotBeNull();
        clone.TypeAnnotation!.TypeName.Should().Be("optional");
        clone.IsNull().Should().BeTrue();
    }

    #endregion
}
