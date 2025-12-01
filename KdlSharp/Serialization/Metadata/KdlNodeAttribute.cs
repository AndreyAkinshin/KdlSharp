namespace KdlSharp.Serialization.Metadata;

/// <summary>
/// Specifies the node name to use when serializing a type to KDL.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class KdlNodeAttribute : Attribute
{
    /// <summary>
    /// Gets the node name to use in KDL output.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initializes a new instance of KdlNodeAttribute with the specified node name.
    /// </summary>
    /// <param name="name">The node name to use in KDL output.</param>
    public KdlNodeAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}

