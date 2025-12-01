using FluentAssertions;
using KdlSharp.Serialization;
using KdlSharp.Serialization.Metadata;
using Xunit;

namespace KdlSharp.Tests.SerializerTests;

public class PocoRoundTripTests
{
    [Fact]
    public void Serialize_SimpleRecord_Success()
    {
        var serializer = new KdlSerializer();
        var data = new SimpleData("Test", 42);

        var kdl = serializer.Serialize(data);

        kdl.Should().Contain("root");
        kdl.Should().Contain("Test");
        kdl.Should().Contain("42");
    }

    [Fact]
    public void RoundTrip_SimpleRecord_Success()
    {
        var serializer = new KdlSerializer();
        var original = new SimpleData("Test", 42);

        var kdl = serializer.Serialize(original);
        var deserialized = serializer.Deserialize<SimpleData>(kdl);

        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("Test");
        deserialized.Value.Should().Be(42);
    }

    [Fact]
    public void RoundTrip_NestedObject_Success()
    {
        var serializer = new KdlSerializer();
        var original = new Parent
        {
            Name = "Parent",
            Child = new Child { Value = 100 }
        };

        var kdl = serializer.Serialize(original);
        var deserialized = serializer.Deserialize<Parent>(kdl);

        deserialized.Should().NotBeNull();
        deserialized!.Name.Should().Be("Parent");
        deserialized.Child.Should().NotBeNull();
        deserialized.Child!.Value.Should().Be(100);
    }

    [Fact]
    public void Serialize_WithKebabCase_Success()
    {
        var options = new KdlSerializerOptions
        {
            PropertyNamingPolicy = KdlNamingPolicy.KebabCase
        };
        var serializer = new KdlSerializer(options);
        var data = new SimpleData("Test", 42);

        var kdl = serializer.Serialize(data);

        // KebabCase should convert "Name" to "name" and "Value" to "value"
        kdl.Should().Contain("name");
        kdl.Should().Contain("value");
    }

    [Fact]
    public void Serialize_WithAttributes_Success()
    {
        var serializer = new KdlSerializer();
        var data = new AttributedData { CustomName = "Test", IgnoredField = "Should not appear" };

        var kdl = serializer.Serialize(data);

        kdl.Should().Contain("custom");  // Renamed via attribute
        kdl.Should().NotContain("IgnoredField");  // Should be ignored
    }

    [Fact]
    public void Serialize_WithKdlNodeAttribute_UsesCustomNodeName()
    {
        var options = new KdlSerializerOptions { RootNodeName = "custom-node" };
        var serializer = new KdlSerializer(options);
        var data = new TypeWithKdlNodeAttribute { Name = "Test", Value = 42 };

        var kdl = serializer.Serialize(data);

        kdl.Should().Contain("custom-node");
        kdl.Should().Contain("Test");
        kdl.Should().Contain("42");
    }

    [Fact]
    public void Serialize_WithPositionAttribute_SerializesAsArguments()
    {
        var serializer = new KdlSerializer();
        var data = new TypeWithPositionAttribute
        {
            FirstArg = "Hello",
            SecondArg = 123,
            RegularProperty = "PropValue"
        };

        var kdl = serializer.Serialize(data);

        // Position attributes should serialize as arguments, not properties
        kdl.Should().Contain("\"Hello\"");
        kdl.Should().Contain("123");
        // Regular property should be serialized as key=value
        kdl.Should().Contain("regular-property=");
    }

    [Fact]
    public void RoundTrip_WithPositionAttribute_Success()
    {
        var serializer = new KdlSerializer();
        var original = new TypeWithPositionAttribute
        {
            FirstArg = "TestArg",
            SecondArg = 999,
            RegularProperty = "TestProp"
        };

        var kdl = serializer.Serialize(original);
        var deserialized = serializer.Deserialize<TypeWithPositionAttribute>(kdl);

        deserialized.Should().NotBeNull();
        deserialized!.FirstArg.Should().Be("TestArg");
        deserialized.SecondArg.Should().Be(999);
        deserialized.RegularProperty.Should().Be("TestProp");
    }

    [Fact]
    public void Serialize_SpecialNumbers_EmitsCorrectTokens()
    {
        var serializer = new KdlSerializer();
        var data = new SpecialNumberData
        {
            PositiveInf = double.PositiveInfinity,
            NegativeInf = double.NegativeInfinity,
            NaNValue = double.NaN,
            RegularValue = 42.5
        };

        var kdl = serializer.Serialize(data);

        kdl.Should().Contain("#inf");
        kdl.Should().Contain("#-inf");
        kdl.Should().Contain("#nan");
        kdl.Should().Contain("42.5");
    }

    [Fact]
    public void RoundTrip_SpecialNumbers_Success()
    {
        var serializer = new KdlSerializer();
        var original = new SpecialNumberData
        {
            PositiveInf = double.PositiveInfinity,
            NegativeInf = double.NegativeInfinity,
            NaNValue = double.NaN,
            RegularValue = 42.5
        };

        var kdl = serializer.Serialize(original);
        var deserialized = serializer.Deserialize<SpecialNumberData>(kdl);

        deserialized.Should().NotBeNull();
        double.IsPositiveInfinity(deserialized!.PositiveInf).Should().BeTrue();
        double.IsNegativeInfinity(deserialized.NegativeInf).Should().BeTrue();
        double.IsNaN(deserialized.NaNValue).Should().BeTrue();
        deserialized.RegularValue.Should().Be(42.5);
    }

    [Fact]
    public void RoundTrip_FloatSpecialNumbers_Success()
    {
        var serializer = new KdlSerializer();
        var original = new FloatSpecialData
        {
            PositiveInf = float.PositiveInfinity,
            NaNValue = float.NaN
        };

        var kdl = serializer.Serialize(original);
        var deserialized = serializer.Deserialize<FloatSpecialData>(kdl);

        deserialized.Should().NotBeNull();
        float.IsPositiveInfinity(deserialized!.PositiveInf).Should().BeTrue();
        float.IsNaN(deserialized.NaNValue).Should().BeTrue();
    }
}

// Test data types
internal record SimpleData(string Name, int Value);

internal class Parent
{
    public string Name { get; set; } = string.Empty;
    public Child? Child { get; set; }
}

internal class Child
{
    public int Value { get; set; }
}

internal class AttributedData
{
    [KdlProperty("custom")]
    public string CustomName { get; set; } = string.Empty;

    [KdlIgnore]
    public string IgnoredField { get; set; } = string.Empty;
}

// Test data types for attribute tests
[KdlNode("custom-node")]
internal class TypeWithKdlNodeAttribute
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

internal class TypeWithPositionAttribute
{
    [KdlProperty(Position = 0)]
    public string FirstArg { get; set; } = string.Empty;

    [KdlProperty(Position = 1)]
    public int SecondArg { get; set; }

    public string RegularProperty { get; set; } = string.Empty;
}

internal class SpecialNumberData
{
    public double PositiveInf { get; set; }
    public double NegativeInf { get; set; }
    public double NaNValue { get; set; }
    public double RegularValue { get; set; }
}

internal class FloatSpecialData
{
    public float PositiveInf { get; set; }
    public float NaNValue { get; set; }
}

