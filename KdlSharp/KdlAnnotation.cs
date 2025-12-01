namespace KdlSharp;

/// <summary>
/// Represents a type annotation like <c>(type-name)</c>.
/// </summary>
public sealed class KdlAnnotation
{
    /// <summary>
    /// Gets the type name.
    /// </summary>
    public string TypeName { get; }

    /// <summary>
    /// Initializes a new type annotation.
    /// </summary>
    public KdlAnnotation(string typeName)
    {
        TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
    }

    /// <summary>
    /// Returns the KDL representation: <c>(type-name)</c>.
    /// </summary>
    public override string ToString() => $"({TypeName})";

    /// <summary>
    /// Determines whether the specified object is equal to the current annotation.
    /// </summary>
    /// <param name="obj">The object to compare with the current annotation.</param>
    /// <returns>true if the specified object has the same type name; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        return obj is KdlAnnotation other && TypeName == other.TypeName;
    }

    /// <summary>
    /// Returns the hash code for this annotation.
    /// </summary>
    /// <returns>A hash code based on the type name.</returns>
    public override int GetHashCode()
    {
        return TypeName.GetHashCode();
    }
}

