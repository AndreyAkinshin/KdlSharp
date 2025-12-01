using KdlSharp.Serialization.Metadata;

namespace KdlSharp.Serialization;

/// <summary>
/// Base class for KDL serializer contexts (for source generation support).
/// </summary>
public abstract class KdlSerializerContext
{
    /// <summary>
    /// Gets the serializer options.
    /// </summary>
    public KdlSerializerOptions Options { get; }

    /// <summary>
    /// Initializes a new context with the specified options.
    /// </summary>
    protected KdlSerializerContext(KdlSerializerOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Gets type metadata for the specified type.
    /// </summary>
    public abstract IKdlTypeMetadata GetTypeMetadata(Type type);
}

// Metadata types have been moved to KdlSharp.Serialization.Metadata namespace

