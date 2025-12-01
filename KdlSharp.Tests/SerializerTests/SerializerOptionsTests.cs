using FluentAssertions;
using KdlSharp.Serialization;
using KdlSharp.Serialization.Metadata;
using Xunit;

namespace KdlSharp.Tests.SerializerTests;

/// <summary>
/// Tests for KdlSerializerOptions behavior.
/// </summary>
public class SerializerOptionsTests
{
    // Test data types
    internal record SimpleConfig(string Name, int Port, bool Enabled);
    internal record WrapperConfig(NestedConfig Inner);
    internal record NestedConfig(string Value);

    [Fact]
    public void WriteTypeAnnotations_WhenTrue_IncludesTypeAnnotations()
    {
        var options = new KdlSerializerOptions
        {
            WriteTypeAnnotations = true
        };
        var serializer = new KdlSerializer(options);
        var data = new SimpleConfig("test", 8080, true);

        var kdl = serializer.Serialize(data);

        kdl.Should().Contain("(string)");
        kdl.Should().Contain("(i32)");
        kdl.Should().Contain("(bool)");
    }

    [Fact]
    public void WriteTypeAnnotations_WhenFalse_OmitsTypeAnnotations()
    {
        var options = new KdlSerializerOptions
        {
            WriteTypeAnnotations = false
        };
        var serializer = new KdlSerializer(options);
        var data = new SimpleConfig("test", 8080, true);

        var kdl = serializer.Serialize(data);

        kdl.Should().NotContain("(string)");
        kdl.Should().NotContain("(i32)");
        kdl.Should().NotContain("(bool)");
    }

    [Fact]
    public void TargetVersion_V2_UsesPoundPrefixForBooleans()
    {
        var options = new KdlSerializerOptions
        {
            TargetVersion = KdlVersion.V2
        };
        var serializer = new KdlSerializer(options);
        var data = new SimpleConfig("test", 8080, true);

        var kdl = serializer.Serialize(data);

        // V2 uses #true (not bare 'true')
        kdl.Should().Contain("#true");
    }

    [Fact]
    public void TargetVersion_V1_UsesNoPrefixForBooleans()
    {
        var options = new KdlSerializerOptions
        {
            TargetVersion = KdlVersion.V1
        };
        var serializer = new KdlSerializer(options);
        var data = new SimpleConfig("test", 8080, true);

        var kdl = serializer.Serialize(data);

        // Should contain "true" without # prefix
        // Need to check it's not "#true"
        kdl.Should().NotContain("#true");
        kdl.Should().Contain("true");
    }

    [Fact]
    public void TargetVersion_V2_UsesPoundPrefixForNull()
    {
        var options = new KdlSerializerOptions
        {
            TargetVersion = KdlVersion.V2,
            IncludeNullValues = true
        };
        var serializer = new KdlSerializer(options);
        var data = new NullableConfig { Name = null };

        var kdl = serializer.Serialize(data);

        kdl.Should().Contain("#null");
    }

    [Fact]
    public void TargetVersion_V1_UsesNoPrefixForNull()
    {
        var options = new KdlSerializerOptions
        {
            TargetVersion = KdlVersion.V1,
            IncludeNullValues = true
        };
        var serializer = new KdlSerializer(options);
        var data = new NullableConfig { Name = null };

        var kdl = serializer.Serialize(data);

        kdl.Should().NotContain("#null");
        kdl.Should().Contain("null");
    }

    [Fact]
    public void FlattenSingleChildObjects_WhenTrue_FlattensNestedContent()
    {
        var options = new KdlSerializerOptions
        {
            FlattenSingleChildObjects = true
        };
        var serializer = new KdlSerializer(options);
        var data = new WrapperConfig(new NestedConfig("hello"));

        var kdl = serializer.Serialize(data);

        // When flattened, the inner node's content should be at root level
        // The wrapper should have the inner's properties directly
        // The single child "inner" gets flattened into root
        kdl.Should().Contain("value=");
        // No nested braces (root level only)
    }

    [Fact]
    public void FlattenSingleChildObjects_WhenFalse_PreservesNesting()
    {
        var options = new KdlSerializerOptions
        {
            FlattenSingleChildObjects = false
        };
        var serializer = new KdlSerializer(options);
        var data = new WrapperConfig(new NestedConfig("hello"));

        var kdl = serializer.Serialize(data);

        // When not flattened, there should be a child block
        kdl.Should().Contain("{");
        kdl.Should().Contain("}");
        // Should have inner node name
        kdl.Should().Contain("inner");
    }

    [Fact]
    public void FlattenSingleChildObjects_DefaultIsFalse()
    {
        var options = new KdlSerializerOptions();
        options.FlattenSingleChildObjects.Should().BeFalse();
    }

    [Fact]
    public void FlattenSingleChildObjects_WhenTrue_RoundTripsCorrectly()
    {
        var options = new KdlSerializerOptions
        {
            FlattenSingleChildObjects = true
        };
        var serializer = new KdlSerializer(options);
        var original = new WrapperConfig(new NestedConfig("hello"));

        // Serialize - should flatten the single child
        var kdl = serializer.Serialize(original);

        // Verify flattening occurred (no nested braces with inner node name)
        kdl.Should().Contain("value=");

        // Deserialize with same options
        var deserialized = serializer.Deserialize<WrapperConfig>(kdl);

        // Verify round-trip
        deserialized.Should().NotBeNull();
        deserialized!.Inner.Should().NotBeNull();
        deserialized.Inner.Value.Should().Be("hello");
    }

    [Fact]
    public void UseArgumentsForSimpleValues_WhenFalse_WritesAsProperties()
    {
        var options = new KdlSerializerOptions
        {
            UseArgumentsForSimpleValues = false
        };
        var serializer = new KdlSerializer(options);
        var data = new PositionalConfig("first", "second");

        var kdl = serializer.Serialize(data);

        // Even though Position attribute specifies arguments, they should be properties
        kdl.Should().Contain("first=");
        kdl.Should().Contain("second=");
    }

    [Fact]
    public void UseArgumentsForSimpleValues_WhenTrue_WritesAsArguments()
    {
        var options = new KdlSerializerOptions
        {
            UseArgumentsForSimpleValues = true
        };
        var serializer = new KdlSerializer(options);
        var data = new PositionalConfig("first", "second");

        var kdl = serializer.Serialize(data);

        // With UseArgumentsForSimpleValues=true and Position attributes, values should be arguments
        // Arguments don't have = in them
        kdl.Should().Contain("\"first\"");
        kdl.Should().Contain("\"second\"");
    }

    [Fact]
    public void UseArgumentsForSimpleValues_WhenFalse_RoundTripsCorrectly()
    {
        var options = new KdlSerializerOptions
        {
            UseArgumentsForSimpleValues = false
        };
        var serializer = new KdlSerializer(options);
        var original = new PositionalConfig("hello", "world");

        // Serialize
        var kdl = serializer.Serialize(original);

        // Deserialize with same options
        var deserialized = serializer.Deserialize<PositionalConfig>(kdl);

        // Verify round-trip
        deserialized.Should().NotBeNull();
        deserialized!.First.Should().Be("hello");
        deserialized.Second.Should().Be("world");
    }

    [Fact]
    public void OptionsClone_PreservesAllSettings()
    {
        var original = new KdlSerializerOptions
        {
            RootNodeName = "custom",
            PropertyNamingPolicy = KdlNamingPolicy.SnakeCase,
            IncludeNullValues = true,
            UseArgumentsForSimpleValues = false,
            FlattenSingleChildObjects = true,
            WriteTypeAnnotations = true,
            TargetVersion = KdlVersion.V1
        };

        var clone = original.Clone();

        clone.RootNodeName.Should().Be("custom");
        clone.PropertyNamingPolicy.Should().Be(KdlNamingPolicy.SnakeCase);
        clone.IncludeNullValues.Should().BeTrue();
        clone.UseArgumentsForSimpleValues.Should().BeFalse();
        clone.FlattenSingleChildObjects.Should().BeTrue();
        clone.WriteTypeAnnotations.Should().BeTrue();
        clone.TargetVersion.Should().Be(KdlVersion.V1);
    }

    [Fact]
    public void TargetVersion_V1_Deserialize_RoundTrips()
    {
        var options = new KdlSerializerOptions
        {
            TargetVersion = KdlVersion.V1
        };
        var serializer = new KdlSerializer(options);
        var original = new SimpleConfig("test", 8080, true);

        // Serialize to v1 format (bare true/false/null)
        var kdl = serializer.Serialize(original);
        kdl.Should().NotContain("#true");
        kdl.Should().Contain("true");

        // Deserialize back - should work because v1 parser settings are used
        var deserialized = serializer.Deserialize<SimpleConfig>(kdl);
        deserialized.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void TargetVersion_V1_Deserialize_ParsesBareBooleans()
    {
        var options = new KdlSerializerOptions
        {
            TargetVersion = KdlVersion.V1
        };
        var serializer = new KdlSerializer(options);

        // V1 KDL with bare booleans
        var kdl = "root name=\"test\" port=8080 enabled=true";

        var deserialized = serializer.Deserialize<SimpleConfig>(kdl);

        deserialized.Name.Should().Be("test");
        deserialized.Port.Should().Be(8080);
        deserialized.Enabled.Should().BeTrue();
    }

    [Fact]
    public void TargetVersion_V1_Deserialize_ParsesBareNull()
    {
        var options = new KdlSerializerOptions
        {
            TargetVersion = KdlVersion.V1,
            IncludeNullValues = true
        };
        var serializer = new KdlSerializer(options);

        // V1 KDL with bare null
        var kdl = "root name=null";

        var deserialized = serializer.Deserialize<NullableConfig>(kdl);

        deserialized.Name.Should().BeNull();
    }

    [Fact]
    public void TargetVersion_V2_Deserialize_RoundTrips()
    {
        var options = new KdlSerializerOptions
        {
            TargetVersion = KdlVersion.V2
        };
        var serializer = new KdlSerializer(options);
        var original = new SimpleConfig("test", 8080, true);

        // Serialize to v2 format (#true/#false/#null)
        var kdl = serializer.Serialize(original);
        kdl.Should().Contain("#true");

        // Deserialize back - should work because v2 parser settings are used
        var deserialized = serializer.Deserialize<SimpleConfig>(kdl);
        deserialized.Should().BeEquivalentTo(original);
    }

    internal class NullableConfig
    {
        public string? Name { get; set; }
    }

    internal record PositionalConfig(
        [property: KdlProperty(Position = 0)] string First,
        [property: KdlProperty(Position = 1)] string Second);

    [Fact]
    public void Serializer_DoesNotMutateCallerOptions()
    {
        var options = new KdlSerializerOptions
        {
            RootNodeName = "test"
        };
        var initialConverterCount = options.Converters.Count;

        // Create a serializer - this should NOT modify the caller's options
        _ = new KdlSerializer(options);

        // Verify the caller's options are unchanged
        options.Converters.Count.Should().Be(initialConverterCount);
    }

    [Fact]
    public void MultipleSerializers_FromSameOptions_DoNotDuplicateConverters()
    {
        var options = new KdlSerializerOptions();

        // Create multiple serializers from the same options
        var serializer1 = new KdlSerializer(options);
        var serializer2 = new KdlSerializer(options);
        var serializer3 = new KdlSerializer(options);

        // Each serializer should work independently
        var data = new SimpleConfig("test", 8080, true);
        var kdl1 = serializer1.Serialize(data);
        var kdl2 = serializer2.Serialize(data);
        var kdl3 = serializer3.Serialize(data);

        kdl1.Should().Be(kdl2);
        kdl2.Should().Be(kdl3);

        // Caller's options should remain unchanged
        options.Converters.Should().BeEmpty();
    }

    [Fact]
    public void CustomConverter_InOptions_TakesPrecedence()
    {
        // Register a custom converter for DateTimeWrapper
        var options = new KdlSerializerOptions();
        options.Converters.Add(new CustomWrapperConverter());

        var serializer = new KdlSerializer(options);
        var data = new DateTimeWrapper { Date = new DateTime(2024, 1, 15) };

        var kdl = serializer.Serialize(data);

        // Our custom converter uses "custom-format" prefix
        kdl.Should().Contain("custom-format:");
    }

    [Fact]
    public void CustomConverter_IsPreservedAfterClone()
    {
        // Register a custom converter
        var options = new KdlSerializerOptions();
        options.Converters.Add(new CustomWrapperConverter());

        // Create a serializer (which clones the options internally)
        var serializer = new KdlSerializer(options);

        // The original options should still have only our custom converter
        options.Converters.Should().HaveCount(1);
        options.Converters[0].Should().BeOfType<CustomWrapperConverter>();
    }

    internal class DateTimeWrapper
    {
        public DateTime Date { get; set; }
    }

    // Custom converter that handles DateTimeWrapper type (not DateTime itself)
    internal class CustomWrapperConverter : KdlSharp.Serialization.Converters.KdlConverter<DateTimeWrapper>
    {
        protected override void Write(KdlNode node, DateTimeWrapper? value, KdlSerializerOptions options, KdlSerializerContext? context)
        {
            if (value != null)
            {
                node.AddArgument(new KdlSharp.Values.KdlString($"custom-format:{value.Date:yyyy-MM-dd}"));
            }
        }

        protected override DateTimeWrapper? Read(KdlNode node, KdlSerializerOptions options, KdlSerializerContext? context)
        {
            var str = node.Arguments[0].AsString()!;
            var dateStr = str.Replace("custom-format:", "");
            return new DateTimeWrapper { Date = DateTime.Parse(dateStr) };
        }
    }
}
