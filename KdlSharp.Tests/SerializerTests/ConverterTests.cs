using System.Globalization;
using FluentAssertions;
using KdlSharp.Exceptions;
using KdlSharp.Serialization;
using KdlSharp.Serialization.Converters;
using KdlSharp.Values;
using Xunit;

namespace KdlSharp.Tests.SerializerTests;

/// <summary>
/// Tests for built-in and custom converters.
/// </summary>
public class ConverterTests
{
    #region Built-in DateTime Converter Tests

    [Fact]
    public void Serialize_DateTime_Success()
    {
        var serializer = new KdlSerializer();
        var data = new DateTimeWrapper(new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc));

        var kdl = serializer.Serialize(data);

        kdl.Should().Contain("2024");
    }

    [Fact]
    public void RoundTrip_DateTime_Success()
    {
        var serializer = new KdlSerializer();
        var original = new DateTimeWrapper(new DateTime(2024, 6, 15, 10, 30, 0, DateTimeKind.Utc));

        var kdl = serializer.Serialize(original);
        var deserialized = serializer.Deserialize<DateTimeWrapper>(kdl);

        deserialized.Should().NotBeNull();
        deserialized!.Value.Year.Should().Be(2024);
        deserialized.Value.Month.Should().Be(6);
        deserialized.Value.Day.Should().Be(15);
    }

    #endregion

    #region Built-in Guid Converter Tests

    [Fact]
    public void Serialize_Guid_Success()
    {
        var serializer = new KdlSerializer();
        var guid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        var data = new GuidWrapper(guid);

        var kdl = serializer.Serialize(data);

        kdl.Should().Contain("550e8400-e29b-41d4-a716-446655440000");
    }

    [Fact]
    public void RoundTrip_Guid_Success()
    {
        var serializer = new KdlSerializer();
        var guid = Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
        var original = new GuidWrapper(guid);

        var kdl = serializer.Serialize(original);
        var deserialized = serializer.Deserialize<GuidWrapper>(kdl);

        deserialized.Should().NotBeNull();
        deserialized!.Value.Should().Be(guid);
    }

    #endregion

    #region Built-in Uri Converter Tests

    [Fact]
    public void Serialize_Uri_Success()
    {
        var serializer = new KdlSerializer();
        var data = new UriWrapper(new Uri("https://example.com/path"));

        var kdl = serializer.Serialize(data);

        kdl.Should().Contain("https://example.com/path");
    }

    [Fact]
    public void RoundTrip_Uri_Success()
    {
        var serializer = new KdlSerializer();
        var original = new UriWrapper(new Uri("https://example.com/path?query=value"));

        var kdl = serializer.Serialize(original);
        var deserialized = serializer.Deserialize<UriWrapper>(kdl);

        deserialized.Should().NotBeNull();
        deserialized!.Value.Should().Be(new Uri("https://example.com/path?query=value"));
    }

    #endregion

    #region Built-in TimeSpan Converter Tests

    [Fact]
    public void Serialize_TimeSpan_Success()
    {
        var serializer = new KdlSerializer();
        var data = new TimeSpanWrapper(TimeSpan.FromHours(2.5));

        var kdl = serializer.Serialize(data);

        kdl.Should().Contain("02:30:00");
    }

    [Fact]
    public void RoundTrip_TimeSpan_Success()
    {
        var serializer = new KdlSerializer();
        var original = new TimeSpanWrapper(TimeSpan.FromMinutes(90));

        var kdl = serializer.Serialize(original);
        var deserialized = serializer.Deserialize<TimeSpanWrapper>(kdl);

        deserialized.Should().NotBeNull();
        deserialized!.Value.Should().Be(TimeSpan.FromMinutes(90));
    }

    #endregion

    #region Built-in DateTimeOffset Converter Tests

    [Fact]
    public void Serialize_DateTimeOffset_Success()
    {
        var serializer = new KdlSerializer();
        var dto = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.FromHours(-5));
        var data = new DateTimeOffsetWrapper(dto);

        var kdl = serializer.Serialize(data);

        kdl.Should().Contain("2024");
    }

    [Fact]
    public void RoundTrip_DateTimeOffset_Success()
    {
        var serializer = new KdlSerializer();
        var dto = new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero);
        var original = new DateTimeOffsetWrapper(dto);

        var kdl = serializer.Serialize(original);
        var deserialized = serializer.Deserialize<DateTimeOffsetWrapper>(kdl);

        deserialized.Should().NotBeNull();
        deserialized!.Value.Year.Should().Be(2024);
        deserialized.Value.Month.Should().Be(6);
    }

    #endregion

    #region Culture-Invariant Tests

    [Fact]
    public void DateTime_RoundTrip_IsCultureInvariant()
    {
        // Arrange: save current culture and set a non-invariant culture
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            // Use German culture which has different date/time formatting
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            var serializer = new KdlSerializer();
            var original = new DateTimeWrapper(new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc));

            // Act: serialize and deserialize
            var kdl = serializer.Serialize(original);
            var deserialized = serializer.Deserialize<DateTimeWrapper>(kdl);

            // Assert: should round-trip correctly regardless of culture
            deserialized.Should().NotBeNull();
            deserialized!.Value.Should().Be(original.Value);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void DateTimeOffset_RoundTrip_IsCultureInvariant()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("fr-FR");

            var serializer = new KdlSerializer();
            var original = new DateTimeOffsetWrapper(new DateTimeOffset(2024, 12, 31, 23, 59, 59, TimeSpan.FromHours(5)));

            var kdl = serializer.Serialize(original);
            var deserialized = serializer.Deserialize<DateTimeOffsetWrapper>(kdl);

            deserialized.Should().NotBeNull();
            deserialized!.Value.Should().Be(original.Value);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void TimeSpan_RoundTrip_IsCultureInvariant()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ja-JP");

            var serializer = new KdlSerializer();
            var original = new TimeSpanWrapper(new TimeSpan(1, 23, 45, 59, 123));

            var kdl = serializer.Serialize(original);
            var deserialized = serializer.Deserialize<TimeSpanWrapper>(kdl);

            deserialized.Should().NotBeNull();
            deserialized!.Value.Should().Be(original.Value);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    #endregion

    #region Custom Converter Tests

    [Fact]
    public void CustomConverter_WhenRegistered_IsUsed()
    {
        var options = new KdlSerializerOptions();
        options.Converters.Add(new PointConverter());
        var serializer = new KdlSerializer(options);

        var data = new PointWrapper { Location = new Point(10, 20) };

        var kdl = serializer.Serialize(data);

        // Custom converter should serialize as "10,20"
        kdl.Should().Contain("10,20");
    }

    [Fact]
    public void CustomConverter_RoundTrip_Success()
    {
        var options = new KdlSerializerOptions();
        options.Converters.Add(new PointConverter());
        var serializer = new KdlSerializer(options);

        var original = new PointWrapper { Location = new Point(100, 200) };

        var kdl = serializer.Serialize(original);
        var deserialized = serializer.Deserialize<PointWrapper>(kdl);

        deserialized.Should().NotBeNull();
        deserialized!.Location.X.Should().Be(100);
        deserialized.Location.Y.Should().Be(200);
    }

    [Fact]
    public void CustomConverter_InvalidInput_ThrowsKdlSerializationException()
    {
        var options = new KdlSerializerOptions();
        options.Converters.Add(new PointConverter());
        var serializer = new KdlSerializer(options);

        var kdl = "root location=\"invalid\"";

        var act = () => serializer.Deserialize<PointWrapper>(kdl);

        act.Should().Throw<KdlSerializationException>();
    }

    #endregion
}

// Test data types
internal record DateTimeWrapper(DateTime Value);
internal record GuidWrapper(Guid Value);
internal record UriWrapper(Uri Value);
internal record TimeSpanWrapper(TimeSpan Value);
internal record DateTimeOffsetWrapper(DateTimeOffset Value);

internal class PointWrapper
{
    public Point Location { get; set; }
}

internal struct Point
{
    public int X { get; }
    public int Y { get; }

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }
}

/// <summary>
/// Custom converter for Point type that serializes as "X,Y" string.
/// </summary>
internal class PointConverter : KdlConverter<Point>
{
    protected override void Write(KdlNode node, Point value, KdlSerializerOptions options, KdlSerializerContext? context)
    {
        node.AddArgument(new KdlString($"{value.X},{value.Y}"));
    }

    protected override Point Read(KdlNode node, KdlSerializerOptions options, KdlSerializerContext? context)
    {
        if (node.Arguments.Count > 0)
        {
            var str = node.Arguments[0].AsString();
            if (str != null)
            {
                var parts = str.Split(',');
                if (parts.Length == 2 &&
                    int.TryParse(parts[0], out var x) &&
                    int.TryParse(parts[1], out var y))
                {
                    return new Point(x, y);
                }
            }
        }

        throw new KdlSerializationException("Cannot deserialize Point from node");
    }
}
