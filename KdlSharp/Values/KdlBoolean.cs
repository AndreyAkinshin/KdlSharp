namespace KdlSharp.Values;

/// <summary>
/// Represents a KDL boolean value.
/// </summary>
public sealed class KdlBoolean : KdlValue
{
    /// <summary>
    /// Gets the boolean value.
    /// </summary>
    public bool Value { get; }

    /// <summary>
    /// Gets the value type (Boolean).
    /// </summary>
    public override KdlValueType ValueType => KdlValueType.Boolean;

    /// <summary>
    /// Initializes a new boolean value.
    /// </summary>
    /// <remarks>
    /// Prefer using <see cref="True"/> and <see cref="False"/> singletons instead of creating new instances.
    /// </remarks>
    internal KdlBoolean(bool value)
    {
        Value = value;
    }

    /// <summary>
    /// Singleton instance for <c>true</c>.
    /// </summary>
    public static KdlBoolean True { get; } = new KdlBoolean(true);

    /// <summary>
    /// Singleton instance for <c>false</c>.
    /// </summary>
    public static KdlBoolean False { get; } = new KdlBoolean(false);

    /// <summary>
    /// Returns this value as a boolean.
    /// </summary>
    /// <returns>The boolean value.</returns>
    public override bool? AsBoolean() => Value;

    /// <summary>
    /// Converts this value to a KDL string representation (#true or #false).
    /// </summary>
    /// <returns>The KDL string representation.</returns>
    public override string ToKdlString() => Value ? "#true" : "#false";

    /// <summary>
    /// Creates a shallow clone of this value.
    /// </summary>
    /// <returns>
    /// The singleton True or False instance if no TypeAnnotation is set;
    /// otherwise, a new instance with the TypeAnnotation preserved.
    /// </returns>
    public override KdlValue Clone()
    {
        if (TypeAnnotation == null)
        {
            return Value ? True : False;
        }

        return new KdlBoolean(Value) { TypeAnnotation = TypeAnnotation };
    }
}

