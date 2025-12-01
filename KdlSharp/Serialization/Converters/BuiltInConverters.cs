using System.Globalization;
using KdlSharp.Exceptions;
using KdlSharp.Values;

namespace KdlSharp.Serialization.Converters;

/// <summary>
/// Provides built-in converters for common types.
/// </summary>
internal static class BuiltInConverters
{
    /// <summary>
    /// Gets all built-in converters.
    /// </summary>
    public static IEnumerable<IKdlConverter> GetAll()
    {
        yield return new DateTimeConverter();
        yield return new DateTimeOffsetConverter();
        yield return new GuidConverter();
        yield return new UriConverter();
        yield return new TimeSpanConverter();
    }
}

/// <summary>
/// Converter for DateTime values.
/// </summary>
internal sealed class DateTimeConverter : KdlConverter<DateTime>
{
    protected override void Write(KdlNode node, DateTime value, KdlSerializerOptions options, KdlSerializerContext? context)
    {
        node.AddArgument(new KdlString(value.ToString("O", CultureInfo.InvariantCulture)));
    }

    protected override DateTime Read(KdlNode node, KdlSerializerOptions options, KdlSerializerContext? context)
    {
        if (node.Arguments.Count > 0)
        {
            var arg = node.Arguments[0];
            if (arg.IsNull())
                return default;

            var str = arg.AsString();
            if (str != null && DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dt))
                return dt;
        }

        throw new KdlSerializationException("Cannot deserialize DateTime from node");
    }
}

/// <summary>
/// Converter for DateTimeOffset values.
/// </summary>
internal sealed class DateTimeOffsetConverter : KdlConverter<DateTimeOffset>
{
    protected override void Write(KdlNode node, DateTimeOffset value, KdlSerializerOptions options, KdlSerializerContext? context)
    {
        node.AddArgument(new KdlString(value.ToString("O", CultureInfo.InvariantCulture)));
    }

    protected override DateTimeOffset Read(KdlNode node, KdlSerializerOptions options, KdlSerializerContext? context)
    {
        if (node.Arguments.Count > 0)
        {
            var arg = node.Arguments[0];
            if (arg.IsNull())
                return default;

            var str = arg.AsString();
            if (str != null && DateTimeOffset.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dto))
                return dto;
        }

        throw new KdlSerializationException("Cannot deserialize DateTimeOffset from node");
    }
}

/// <summary>
/// Converter for Guid values.
/// </summary>
internal sealed class GuidConverter : KdlConverter<Guid>
{
    protected override void Write(KdlNode node, Guid value, KdlSerializerOptions options, KdlSerializerContext? context)
    {
        node.AddArgument(new KdlString(value.ToString()));
    }

    protected override Guid Read(KdlNode node, KdlSerializerOptions options, KdlSerializerContext? context)
    {
        if (node.Arguments.Count > 0)
        {
            var arg = node.Arguments[0];
            if (arg.IsNull())
                return default;

            var str = arg.AsString();
            if (str != null && Guid.TryParse(str, out var guid))
                return guid;
        }

        throw new KdlSerializationException("Cannot deserialize Guid from node");
    }
}

/// <summary>
/// Converter for Uri values.
/// </summary>
internal sealed class UriConverter : KdlConverter<Uri>
{
    protected override void Write(KdlNode node, Uri? value, KdlSerializerOptions options, KdlSerializerContext? context)
    {
        if (value != null)
        {
            node.AddArgument(new KdlString(value.ToString()));
        }
        else
        {
            node.AddArgument(KdlNull.Instance);
        }
    }

    protected override Uri? Read(KdlNode node, KdlSerializerOptions options, KdlSerializerContext? context)
    {
        if (node.Arguments.Count > 0)
        {
            var arg = node.Arguments[0];
            if (arg.IsNull())
                return null;

            var str = arg.AsString();
            if (str != null && Uri.TryCreate(str, UriKind.RelativeOrAbsolute, out var uri))
                return uri;
        }

        throw new KdlSerializationException("Cannot deserialize Uri from node");
    }
}

/// <summary>
/// Converter for TimeSpan values.
/// </summary>
internal sealed class TimeSpanConverter : KdlConverter<TimeSpan>
{
    protected override void Write(KdlNode node, TimeSpan value, KdlSerializerOptions options, KdlSerializerContext? context)
    {
        node.AddArgument(new KdlString(value.ToString("c", CultureInfo.InvariantCulture)));
    }

    protected override TimeSpan Read(KdlNode node, KdlSerializerOptions options, KdlSerializerContext? context)
    {
        if (node.Arguments.Count > 0)
        {
            var arg = node.Arguments[0];
            if (arg.IsNull())
                return default;

            var str = arg.AsString();
            if (str != null && TimeSpan.TryParseExact(str, "c", CultureInfo.InvariantCulture, out var ts))
                return ts;
        }

        throw new KdlSerializationException("Cannot deserialize TimeSpan from node");
    }
}

