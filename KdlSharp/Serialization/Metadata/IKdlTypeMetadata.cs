namespace KdlSharp.Serialization.Metadata;

/// <summary>
/// Provides metadata about a type for KDL serialization.
/// </summary>
public interface IKdlTypeMetadata
{
    /// <summary>
    /// Gets the CLR type this metadata describes.
    /// </summary>
    Type Type { get; }

    /// <summary>
    /// Gets the node name to use when serializing this type.
    /// </summary>
    string NodeName { get; }

    /// <summary>
    /// Gets the collection of serializable members (properties and fields).
    /// </summary>
    IReadOnlyList<KdlMemberMetadata> Members { get; }

    /// <summary>
    /// Gets whether this type is a record type.
    /// </summary>
    bool IsRecord { get; }

    /// <summary>
    /// Gets whether this type is a collection type.
    /// </summary>
    bool IsCollection { get; }
}

