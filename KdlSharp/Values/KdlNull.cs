namespace KdlSharp.Values;

/// <summary>
/// Represents a KDL null value.
/// </summary>
public sealed class KdlNull : KdlValue
{
    /// <summary>
    /// Gets the value type (Null).
    /// </summary>
    public override KdlValueType ValueType => KdlValueType.Null;

    /// <summary>
    /// Singleton instance for null.
    /// </summary>
    public static KdlNull Instance { get; } = new KdlNull();

    /// <summary>
    /// Initializes a new null value instance (internal for Clone support with TypeAnnotation).
    /// </summary>
    internal KdlNull() { }

    /// <summary>
    /// Returns true indicating this is a null value.
    /// </summary>
    /// <returns>Always true.</returns>
    public override bool IsNull() => true;

    /// <summary>
    /// Converts this value to a KDL string representation (#null).
    /// </summary>
    /// <returns>The KDL string representation.</returns>
    public override string ToKdlString() => "#null";

    /// <summary>
    /// Creates a shallow clone of this value.
    /// </summary>
    /// <returns>
    /// The singleton Instance if no TypeAnnotation is set;
    /// otherwise, a new instance with the TypeAnnotation preserved.
    /// </returns>
    public override KdlValue Clone()
    {
        if (TypeAnnotation == null)
        {
            return Instance;
        }

        return new KdlNull() { TypeAnnotation = TypeAnnotation };
    }
}

