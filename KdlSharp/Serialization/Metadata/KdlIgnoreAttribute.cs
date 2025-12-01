namespace KdlSharp.Serialization.Metadata;

/// <summary>
/// Specifies that a property or field should be ignored during serialization.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class KdlIgnoreAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the condition under which the property should be ignored.
    /// </summary>
    public KdlIgnoreCondition Condition { get; set; } = KdlIgnoreCondition.Always;

    /// <summary>
    /// Initializes a new instance of KdlIgnoreAttribute.
    /// </summary>
    public KdlIgnoreAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of KdlIgnoreAttribute with a specific condition.
    /// </summary>
    /// <param name="condition">The condition under which to ignore the property.</param>
    public KdlIgnoreAttribute(KdlIgnoreCondition condition)
    {
        Condition = condition;
    }
}

/// <summary>
/// Specifies conditions for ignoring properties during serialization.
/// </summary>
public enum KdlIgnoreCondition
{
    /// <summary>
    /// Always ignore the property.
    /// </summary>
    Always,

    /// <summary>
    /// Ignore the property if its value is null.
    /// </summary>
    WhenNull,

    /// <summary>
    /// Ignore the property if its value is the default for its type.
    /// </summary>
    WhenDefault
}

