using System.Globalization;
using KdlSharp.Exceptions;
using KdlSharp.Serialization.Metadata;

namespace KdlSharp.Serialization.Reflection;

/// <summary>
/// Handles binding between CLR members and KDL values.
/// </summary>
internal static class MemberBinder
{
    /// <summary>
    /// Binds a KDL value to a CLR type.
    /// </summary>
    public static object? BindValue(KdlValue value, Type targetType)
    {
        // Handle null
        if (value.IsNull())
        {
            if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
            {
                throw new KdlSerializationException($"Cannot assign null to non-nullable type {targetType.Name}");
            }
            return null;
        }

        // Unwrap nullable types
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        // Handle strings
        if (value.ValueType == KdlValueType.String)
        {
            var stringValue = value.AsString()!;

            if (underlyingType == typeof(string))
                return stringValue;

            if (underlyingType == typeof(Guid))
            {
                try { return Guid.Parse(stringValue); }
                catch (Exception ex) { throw new KdlSerializationException($"Cannot convert '{stringValue}' to Guid", ex); }
            }

            if (underlyingType == typeof(DateTime))
            {
                try { return DateTime.Parse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind); }
                catch (Exception ex) { throw new KdlSerializationException($"Cannot convert '{stringValue}' to DateTime", ex); }
            }

            if (underlyingType == typeof(DateTimeOffset))
            {
                try { return DateTimeOffset.Parse(stringValue, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind); }
                catch (Exception ex) { throw new KdlSerializationException($"Cannot convert '{stringValue}' to DateTimeOffset", ex); }
            }

            if (underlyingType == typeof(Uri))
            {
                try { return new Uri(stringValue); }
                catch (Exception ex) { throw new KdlSerializationException($"Cannot convert '{stringValue}' to Uri", ex); }
            }

            if (underlyingType == typeof(TimeSpan))
            {
                try { return TimeSpan.Parse(stringValue, CultureInfo.InvariantCulture); }
                catch (Exception ex) { throw new KdlSerializationException($"Cannot convert '{stringValue}' to TimeSpan", ex); }
            }

            if (underlyingType.IsEnum)
                return Enum.Parse(underlyingType, stringValue);
        }

        // Handle numbers
        if (value.ValueType == KdlValueType.Number)
        {
            var kdlNumber = value as Values.KdlNumber;

            // Handle special values (infinity and NaN) - only valid for double/float
            if (kdlNumber?.IsSpecial == true)
            {
                var doubleValue = kdlNumber.AsDoubleValue();
                if (doubleValue.HasValue)
                {
                    if (underlyingType == typeof(double))
                        return doubleValue.Value;

                    if (underlyingType == typeof(float))
                        return (float)doubleValue.Value;
                }

                throw new KdlSerializationException(
                    $"Cannot convert special value ({(kdlNumber.IsNaN ? "NaN" : "Infinity")}) to {targetType.Name}");
            }

            var number = value.AsNumber()!.Value;

            if (underlyingType == typeof(int))
                return (int)number;

            if (underlyingType == typeof(long))
                return (long)number;

            if (underlyingType == typeof(short))
                return (short)number;

            if (underlyingType == typeof(byte))
                return (byte)number;

            if (underlyingType == typeof(decimal))
                return number;

            if (underlyingType == typeof(double))
                return (double)number;

            if (underlyingType == typeof(float))
                return (float)number;
        }

        // Handle booleans
        if (value.ValueType == KdlValueType.Boolean)
        {
            if (underlyingType == typeof(bool))
                return value.AsBoolean()!.Value;
        }

        throw new KdlSerializationException($"Cannot convert {value.ValueType} to {targetType.Name}");
    }

    /// <summary>
    /// Converts a CLR value to a KDL value.
    /// </summary>
    public static KdlValue ConvertToKdlValue(object? value)
    {
        if (value == null)
            return Values.KdlNull.Instance;

        var type = value.GetType();

        // Handle strings
        if (type == typeof(string))
            return new Values.KdlString((string)value);

        // Handle numbers
        if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
            return new Values.KdlNumber(Convert.ToDecimal(value));

        if (type == typeof(decimal))
            return new Values.KdlNumber((decimal)value);

        if (type == typeof(double))
        {
            var d = (double)value;
            if (double.IsPositiveInfinity(d))
                return Values.KdlNumber.PositiveInfinity();
            if (double.IsNegativeInfinity(d))
                return Values.KdlNumber.NegativeInfinity();
            if (double.IsNaN(d))
                return Values.KdlNumber.NaN();
            return new Values.KdlNumber(Convert.ToDecimal(value));
        }

        if (type == typeof(float))
        {
            var f = (float)value;
            if (float.IsPositiveInfinity(f))
                return Values.KdlNumber.PositiveInfinity();
            if (float.IsNegativeInfinity(f))
                return Values.KdlNumber.NegativeInfinity();
            if (float.IsNaN(f))
                return Values.KdlNumber.NaN();
            return new Values.KdlNumber(Convert.ToDecimal(value));
        }

        // Handle booleans
        if (type == typeof(bool))
            return (bool)value ? Values.KdlBoolean.True : Values.KdlBoolean.False;

        // Handle DateTime
        if (type == typeof(DateTime))
            return new Values.KdlString(((DateTime)value).ToString("O", CultureInfo.InvariantCulture));

        if (type == typeof(DateTimeOffset))
            return new Values.KdlString(((DateTimeOffset)value).ToString("O", CultureInfo.InvariantCulture));

        // Handle Guid
        if (type == typeof(Guid))
            return new Values.KdlString(((Guid)value).ToString());

        // Handle Uri
        if (type == typeof(Uri))
            return new Values.KdlString(((Uri)value).ToString());

        // Handle TimeSpan
        if (type == typeof(TimeSpan))
            return new Values.KdlString(((TimeSpan)value).ToString("c", CultureInfo.InvariantCulture));

        // Handle enums
        if (type.IsEnum)
            return new Values.KdlString(value.ToString()!);

        throw new KdlSerializationException($"Cannot convert {type.Name} to KdlValue");
    }

    /// <summary>
    /// Checks if a value should be ignored based on the ignore condition.
    /// </summary>
    public static bool ShouldIgnore(object? value, KdlIgnoreCondition condition)
    {
        return condition switch
        {
            KdlIgnoreCondition.Always => true,
            KdlIgnoreCondition.WhenNull => value == null,
            KdlIgnoreCondition.WhenDefault => IsDefaultValue(value),
            _ => false
        };
    }

    private static bool IsDefaultValue(object? value)
    {
        if (value == null)
            return true;

        var type = value.GetType();
        if (type.IsValueType)
        {
            var defaultValue = Activator.CreateInstance(type);
            return value.Equals(defaultValue);
        }

        return false;
    }
}

