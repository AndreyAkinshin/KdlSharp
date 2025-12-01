namespace KdlSharp.Serialization.Metadata;

/// <summary>
/// Specifies how a property or field should be serialized in KDL.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class KdlPropertyAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the name of the property in the KDL output.
    /// If null, the property name will be converted according to the naming policy.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the position of this property when serialized as an argument.
    /// If -1 (default), the property is serialized as a KDL property (key=value).
    /// If set to 0 or greater, the property is serialized as a positional argument in the specified order.
    /// </summary>
    public int Position { get; set; } = -1;

    /// <summary>
    /// Gets or sets whether this property should be serialized as a KDL property (key=value).
    /// This is the default behavior when Position is not set.
    /// </summary>
    public bool IsProperty { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of KdlPropertyAttribute.
    /// </summary>
    public KdlPropertyAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of KdlPropertyAttribute with a custom name.
    /// </summary>
    /// <param name="name">The name to use in KDL output.</param>
    public KdlPropertyAttribute(string name)
    {
        Name = name;
    }
}

