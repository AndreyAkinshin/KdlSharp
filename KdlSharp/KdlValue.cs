namespace KdlSharp;

/// <summary>
/// Base class for all KDL values (strings, numbers, booleans, null).
/// </summary>
public abstract class KdlValue
{
    /// <summary>
    /// Gets or sets the type annotation for this value, or <c>null</c> if none.
    /// </summary>
    public KdlAnnotation? TypeAnnotation { get; set; }

    /// <summary>
    /// Gets the type of this value.
    /// </summary>
    public abstract KdlValueType ValueType { get; }

    /// <summary>
    /// Gets the source position where this value was parsed, or <c>null</c> if constructed programmatically.
    /// </summary>
    public SourcePosition? SourcePosition { get; internal set; }

    /// <summary>
    /// Converts this value to a KDL string representation.
    /// </summary>
    public abstract string ToKdlString();

    /// <summary>
    /// Attempts to get this value as a string.
    /// </summary>
    /// <returns>The string value, or <c>null</c> if this is not a string.</returns>
    public virtual string? AsString() => null;

    /// <summary>
    /// Attempts to get this value as a number.
    /// </summary>
    public virtual decimal? AsNumber() => null;

    /// <summary>
    /// Attempts to get this value as a 32-bit integer.
    /// </summary>
    /// <returns>The integer value, or <c>null</c> if this is not a number.</returns>
    /// <remarks>
    /// This performs a conversion from the underlying decimal representation.
    /// Values outside the range of <see cref="int"/> will overflow.
    /// </remarks>
    public int? AsInt32() => AsNumber() is decimal d ? (int)d : null;

    /// <summary>
    /// Attempts to get this value as a 64-bit integer.
    /// </summary>
    /// <returns>The long value, or <c>null</c> if this is not a number.</returns>
    /// <remarks>
    /// This performs a conversion from the underlying decimal representation.
    /// Values outside the range of <see cref="long"/> will overflow.
    /// </remarks>
    public long? AsInt64() => AsNumber() is decimal d ? (long)d : null;

    /// <summary>
    /// Attempts to get this value as a double-precision floating point number.
    /// </summary>
    /// <returns>The double value, or <c>null</c> if this is not a number.</returns>
    /// <remarks>
    /// This performs a conversion from the underlying decimal representation.
    /// Precision may be lost for values that cannot be exactly represented as a double.
    /// </remarks>
    public double? AsDouble() => AsNumber() is decimal d ? (double)d : null;

    /// <summary>
    /// Attempts to get this value as a boolean.
    /// </summary>
    public virtual bool? AsBoolean() => null;

    /// <summary>
    /// Gets whether this value is null.
    /// </summary>
    public virtual bool IsNull() => false;

    /// <summary>
    /// Creates a deep clone of this value.
    /// </summary>
    public abstract KdlValue Clone();

    /// <summary>
    /// Implicitly converts a string to a KDL string value.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    public static implicit operator KdlValue(string value) => new Values.KdlString(value);

    /// <summary>
    /// Implicitly converts an integer to a KDL number value.
    /// </summary>
    /// <param name="value">The integer value to convert.</param>
    public static implicit operator KdlValue(int value) => new Values.KdlNumber(value);

    /// <summary>
    /// Implicitly converts a long to a KDL number value.
    /// </summary>
    /// <param name="value">The long value to convert.</param>
    public static implicit operator KdlValue(long value) => new Values.KdlNumber(value);

    /// <summary>
    /// Implicitly converts a double to a KDL number value.
    /// </summary>
    /// <remarks>
    /// This conversion casts the double to decimal, which may lose precision for very large
    /// or very small values that exceed decimal's range (approximately ±7.9×10^28).
    /// </remarks>
    public static implicit operator KdlValue(double value) => new Values.KdlNumber((decimal)value);

    /// <summary>
    /// Implicitly converts a decimal to a KDL number value.
    /// </summary>
    /// <param name="value">The decimal value to convert.</param>
    public static implicit operator KdlValue(decimal value) => new Values.KdlNumber(value);

    /// <summary>
    /// Implicitly converts a boolean to a KDL boolean value.
    /// </summary>
    /// <param name="value">The boolean value to convert.</param>
    public static implicit operator KdlValue(bool value) => value ? Values.KdlBoolean.True : Values.KdlBoolean.False;
}

/// <summary>
/// Enumeration of KDL value types.
/// </summary>
public enum KdlValueType
{
    /// <summary>
    /// String value.
    /// </summary>
    String,

    /// <summary>
    /// Number value (decimal).
    /// </summary>
    Number,

    /// <summary>
    /// Boolean value (true or false).
    /// </summary>
    Boolean,

    /// <summary>
    /// Null value.
    /// </summary>
    Null
}

