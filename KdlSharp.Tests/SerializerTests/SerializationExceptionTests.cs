using AwesomeAssertions;
using KdlSharp.Exceptions;
using KdlSharp.Serialization;
using Xunit;

namespace KdlSharp.Tests.SerializerTests;

/// <summary>
/// Tests for KdlSerializationException to ensure it's thrown in expected scenarios.
/// </summary>
public class SerializationExceptionTests
{
    [Fact]
    public void Deserialize_EmptyDocument_ThrowsKdlSerializationException()
    {
        var serializer = new KdlSerializer();
        var emptyKdl = "";

        var act = () => serializer.Deserialize<SimpleTestData>(emptyKdl);

        act.Should().Throw<KdlSerializationException>()
            .WithMessage("*Document contains no nodes*");
    }

    [Fact]
    public void Deserialize_InvalidDateTime_ThrowsKdlSerializationException()
    {
        var serializer = new KdlSerializer();
        var kdl = "root value=\"not-a-date\"";

        var act = () => serializer.Deserialize<DateTimeWrapper>(kdl);

        act.Should().Throw<KdlSerializationException>()
            .WithMessage("*Cannot convert*to DateTime*");
    }

    [Fact]
    public void Deserialize_InvalidDateTimeOffset_ThrowsKdlSerializationException()
    {
        var serializer = new KdlSerializer();
        var kdl = "root value=\"not-a-date\"";

        var act = () => serializer.Deserialize<DateTimeOffsetWrapper>(kdl);

        act.Should().Throw<KdlSerializationException>()
            .WithMessage("*Cannot convert*to DateTimeOffset*");
    }

    [Fact]
    public void Deserialize_InvalidGuid_ThrowsKdlSerializationException()
    {
        var serializer = new KdlSerializer();
        var kdl = "root value=\"not-a-guid\"";

        var act = () => serializer.Deserialize<GuidWrapper>(kdl);

        act.Should().Throw<KdlSerializationException>()
            .WithMessage("*Cannot convert*to Guid*");
    }

    [Fact]
    public void Deserialize_InvalidUri_ThrowsKdlSerializationException()
    {
        var serializer = new KdlSerializer();
        var kdl = "root value=\":::invalid-uri:::\"";

        var act = () => serializer.Deserialize<UriWrapper>(kdl);

        act.Should().Throw<KdlSerializationException>()
            .WithMessage("*Cannot convert*to Uri*");
    }

    [Fact]
    public void Deserialize_InvalidTimeSpan_ThrowsKdlSerializationException()
    {
        var serializer = new KdlSerializer();
        var kdl = "root value=\"not-a-timespan\"";

        var act = () => serializer.Deserialize<TimeSpanWrapper>(kdl);

        act.Should().Throw<KdlSerializationException>()
            .WithMessage("*Cannot*TimeSpan*");
    }

    [Fact]
    public void Deserialize_NullToNonNullableValueType_ThrowsKdlSerializationException()
    {
        var serializer = new KdlSerializer();
        var kdl = "root value=#null";

        var act = () => serializer.Deserialize<NonNullableValueTypeWrapper>(kdl);

        act.Should().Throw<KdlSerializationException>()
            .WithMessage("*Cannot assign null to non-nullable type*");
    }

    // Test data types
    private record SimpleTestData(string Name, int Value);
    private record DateTimeWrapper(DateTime Value);
    private record DateTimeOffsetWrapper(DateTimeOffset Value);
    private record GuidWrapper(Guid Value);
    private record UriWrapper(Uri Value);
    private record TimeSpanWrapper(TimeSpan Value);
    private record NonNullableValueTypeWrapper(int Value);
}

